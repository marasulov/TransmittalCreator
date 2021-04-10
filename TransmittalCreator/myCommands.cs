// (C) Copyright 2020 by HP Inc. 
//

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;
using DV2177.Common;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using TransmittalCreator.Models;
using TransmittalCreator.Services;
using TransmittalCreator.Services.Blocks;
using TransmittalCreator.ViewModel;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using OpenFileDialog = Autodesk.AutoCAD.Windows.OpenFileDialog;
using Table = Autodesk.AutoCAD.DatabaseServices.Table;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(TransmittalCreator.MyCommands))]

namespace TransmittalCreator
{
    public class MyCommands : Utils, IExtensionApplication
    {
        [CommandMethod("SELKW")]
        public void GetIdsSelectionOrAllBlocks()
        {
            BlockSelector blockSelector = new BlockSelector();
            blockSelector.GetIdsSelectionOrAllBlocks();
        }

        #region HvacTable

        // Modal Command with pickfirst selection
        [CommandMethod("hvacTable", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void CreateHvacTable(Hvac hvac) // This method can have any name
        {
          hvac.CreateHvacTable();
        }

        #endregion

        [CommandMethod("LISTATT")]
        public static void ListAttributes1()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;
            var filter = new SelectionFilter(new[] {new TypedValue(0, "INSERT")});
            var opts = new PromptSelectionOptions();
            opts.MessageForAdding = "Select block references: ";

            var res = ed.GetSelection(opts, filter);
            if (res.Status != PromptStatus.OK)
                return;
            string str = "";

            string str1 = "";
            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject so in res.Value)
                {
                    var br = (BlockReference) tr.GetObject(so.ObjectId, OpenMode.ForRead);
                    var vargeom = br.GeometricExtents;
                    ed.WriteMessage(vargeom.MinPoint[0].ToString(CultureInfo.InvariantCulture));
                    str += $"{br.Name} {br.Position:0.00}\r\n";
                    if (br.AttributeCollection.Count > 0)
                    {
                        str += "Attributes:\r\n";
                        foreach (ObjectId id in br.AttributeCollection)
                        {
                            var att = (AttributeReference) id.GetObject(OpenMode.ForRead);
                            str += $"\tTag: {att.Tag} Text: {att.TextString}\r\n";
                        }
                    }

                    str += "\r\n";
                }

                tr.Commit();
            }

            ed.WriteMessage(str);
            ed.WriteMessage(str1);
        }

        //TODO refactor have to delete below methods if no need

        [CommandMethod("ListLayouts")]
        public static void ListLayoutsMethod()

        {
            Document doc = Application.DocumentManager.MdiActiveDocument;

            Database db = doc.Database;

            Editor ed = doc.Editor;

            LayoutManager layoutMgr = LayoutManager.Current;

            ed.WriteMessage(String.Format("{0}Active Layout is : {1}", Environment.NewLine, layoutMgr.CurrentLayout));

            ed.WriteMessage(String.Format("{0}Number of Layouts: {1}{0}List of all Layouts:", Environment.NewLine,
                layoutMgr.LayoutCount));

            List<LayoutModel> layouts = new List<LayoutModel>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DBDictionary layoutDic = tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead, false) as DBDictionary;

                foreach (DBDictionaryEntry entry in layoutDic)
                {
                    ObjectId layoutId = entry.Value;
                    Layout layout = tr.GetObject(layoutId, OpenMode.ForRead) as Layout;

                    ed.WriteMessage(String.Format("{0}--> {1}", Environment.NewLine, layout.LayoutName));
                    layouts.Add(new LayoutModel(layoutId, layout));
                }

                tr.Commit();
            }
        }

        [CommandMethod("CreateTrPdfFromFiles")]
        public static void CreateTransmittalAndPdfFromFiles()
        {
            List<Sheet> dict = new List<Sheet>();
            List<PrintModel> printModels = new List<PrintModel>();

            Active.Document.SendStringToExecute("REGENALL ", true, false, true);

            short bgPlot =                (short) Application.GetSystemVariable("BACKGROUNDPLOT");
            Application.SetSystemVariable("BACKGROUNDPLOT", 0);
            List<string> docsToPlot = new List<string>();
            docsToPlot.Add(@"C:\Users\yusufzhon.marasulov\Desktop\test\test.dwg");
            docsToPlot.Add(@"C:\Users\yusufzhon.marasulov\Desktop\test\test1.dwg");
            BatchTransmittal(docsToPlot);
        }


        static public void BatchTransmittal(List<string> docsToPlot)
        {
            List<Sheet> dict = new List<Sheet>();
            List<PrintModel> printModels = new List<PrintModel>();
            Active.Document.SendStringToExecute("REGENALL ", true, false, true);
            Document doc = Active.Document;
            foreach (string filename in docsToPlot)
            {
                using (DocumentLock doclock = doc.LockDocument())
                {
                    Database db = new Database(false, true);
                    db.ReadDwgFile(filename, System.IO.FileShare.Read, true, "");
                    System.IO.FileInfo fi = new System.IO.FileInfo(filename);
                    string docName = fi.Name.Substring(0, fi.Name.Length - 4);
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        ObjectIdCollection idArray = Utils.SelectDynamicBlockReferences();
                        //TODO надо проверить предыдущий и нижние методы на поиск по Id
                        GetSheetsFromBlocks(Active.Editor, dict, tr, idArray);
                        string selAttrName = "НОМЕР_ЛИСТА";
                        GetPrintParametersToPdf(Active.Editor, printModels, tr, idArray, selAttrName);

                        Utils utils = new Utils();
                        //utils.CreateOnlyVed(dict);
                        utils.CreateJsonFile(dict);

                        foreach (var printModel in printModels)
                        {
                            //Active.Editor.WriteMessage("{0} печатаем ", printModel.DocNumber);
                            PlotCurrentLayout(printModel.DocNumber, printModel);
                        }
                        tr.Commit();
                    }
                }
            }
          
        }


        [CommandMethod("CreateTranspdf")]
        public static void CreateTransmittalAndPdf()
        {
            List<Sheet> dict = new List<Sheet>();
            List<PrintModel> printModels = new List<PrintModel>();
            Active.Document.SendStringToExecute("REGENALL ", true, false, true);
            using (Transaction tr = Active.Database.TransactionManager.StartTransaction())
            {
                //фильтр для выбора только блока
                BlockSelector blockSelector = new BlockSelector();
                blockSelector.GetFilterForSelectBlockId();
                var res = blockSelector.SelectionResult;

                if (res.Status != PromptStatus.OK)
                {
                    Active.Editor.WriteMessage("Надо выбрать блок");
                    return;
                }

                SelectionSet selSet = res.Value;
                ObjectId[] idArrayTemp = selSet.GetObjectIds();

                ObjectIdCollection idArray = new ObjectIdCollection();
                foreach (var objectId in idArrayTemp)
                {
                    BlockReference blRef = (BlockReference) tr.GetObject(objectId, OpenMode.ForRead);
                    BlockTableRecord block =
                        tr.GetObject(blRef.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                    if (!(block is null))
                    {
                        string blockName = block.Name;
                        if (blockName == "Формат" | blockName == "ФорматM25") idArray.Add(objectId);
                    }
                }

                GetSheetsFromBlocks(Active.Editor, dict, tr, idArray);
                string selAttrName = "НОМЕР_ЛИСТА";
                GetPrintParametersToPdf(Active.Editor, printModels, tr, idArray, selAttrName);

                Utils utils = new Utils();
                //utils.CreateOnlyVed(dict);
                utils.CreateJsonFile(dict);

                foreach (var printModel in printModels)
                {
                    //Active.Editor.WriteMessage("{0} печатаем ", printModel.DocNumber);
                    PlotCurrentLayout(printModel.DocNumber, printModel);
                }

                //utils.CreateOnlytrans(dict);

                tr.Commit();
            }
        }

        [CommandMethod("CreatePdf")]
        public void CreatePdfName()
        {
            Active.Document.SendStringToExecute("REGENALL ", true, false, true);
            Dictionary<string, string> attrList = new Dictionary<string, string>();
            Window1 window = new Window1(new BlockViewModel(attrList));
            Application.ShowModalWindow(window);
            if (window.isClicked)
            {
                //ObjectIdCollection objectIds = Utils.SelectDynamicBlockReferences();
                List<Sheet> dict = new List<Sheet>();


                //string blockName = window.BlockName;
                //BlockAttribute attributeName = window.comboAttributs.SelectedItem as BlockAttribute;
                //bool attributeChecked = window.attributeCheckBox.IsChecked.Value;
                //string suffix = window.suffixTextBox.Text;
                //string prefix = window.prefixTextBox.Text;
                //int numberingValue = int.Parse(window.numberingTextbox.Text);

                using (Transaction tr = Active.Database.TransactionManager.StartTransaction())
                {
                    BlockSelector blockSelector = new BlockSelector();
                    blockSelector.GetFilterForSelectBlockId(window.BlockName);
                    bool isExec = true;
                    var res = blockSelector.SelectionResult;

                    if (res.Status != PromptStatus.OK)
                    {
                        isExec = false;
                        Active.Editor.WriteMessage("Надо выбрать блок");
                    }

                    if (isExec)
                    {
                        SelectionSet selSet = res.Value;
                        ObjectId[] idArrayTemp = selSet.GetObjectIds();
                        //ObjectIdCollection idArray = new ObjectIdCollection(idArrayTemp);
                        //TODO printing X Y
                        //string selAttrName = attributeName.AttributeName;


                        FileNameCreator fileNameCreator = new FileNameCreator(window, idArrayTemp);

                        //GetPrintParametersToPdf(Active.Editor, printModels, tr, objectIds, selAttrName);

                        fileNameCreator.GetPrintParametersToPdf(tr);

                        Utils utils = new Utils();
                        utils.CreateJsonFile(dict);

                        foreach (var printModel in fileNameCreator.GetPrintModels())
                        {
                            PlotCurrentLayout(printModel.DocNumber, printModel);
                        }
                    }

                    tr.Commit();
                }
            }
        }


        [CommandMethod("CreateDwg")]
        public static void CreateDwg()
        {
            Active.Document.SendStringToExecute("REGENALL ", true, false, true);
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            Dictionary<ObjectId, bool> layersDictionary = LayerManipulation.GetLayersIsBlockedCol();
            if (doc == null) return;
            doc.LockOrUnlockLayers(false, ignoreCurrent: false, lockZero: true);

            bool isExec = true;
            using (Transaction tr = Active.Database.TransactionManager.StartTransaction())
            {
                BlockSelector blockSelector = new BlockSelector();
                blockSelector.GetFilterForSelectBlockId();
                var res = blockSelector.SelectionResult;
                if (res.Status != PromptStatus.OK)
                {
                    isExec = false;
                    Active.Editor.WriteMessage("Надо выбрать блок");
                }

                if (isExec)
                {
                    SelectionSet selSet = res.Value;
                    ObjectId[] idArrayTemp = selSet.GetObjectIds();

                    //idArray.Select(id => (BlockReference) tr.GetObject(id, OpenMode.ForRead))
                    //    .Where(br =>
                    //        ((BlockTableRecord) tr.GetObject(br.DynamicBlockTableRecord, OpenMode.ForRead)).Name ==
                    //        "Формат")
                    //    .Select(br => br.ObjectId);

                    ObjectIdCollection idArray = new ObjectIdCollection();
                    foreach (var objectId in idArrayTemp)
                    {
                        BlockReference blRef = (BlockReference) tr.GetObject(objectId, OpenMode.ForRead);
                        BlockTableRecord block =
                            tr.GetObject(blRef.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                        string blockName = block.Name;

                        if (blockName == "Формат") idArray.Add(objectId);
                        else if (blockName == "ФорматM25") idArray.Add(objectId);

                        //Active.Document.Editor.WriteMessage(blockName);
                    }

                    //MyCommands.GetSheetsFromBlocks(Active.Editor, dict, tr, idArray);
                    //MyCommands.GetPrintParametersToPdf(Active.Editor, printModels, tr, idArray);
                    //Active.Editor.WriteMessage("печать {0} - {1}", printModels[0].DocNumber, printModels.Count);

                    foreach (ObjectId objectId in idArray)
                    {
                        ObjectCopier objectCopier = new ObjectCopier(objectId);
                        ObjectIdCollection objectIds = objectCopier.SelectCrossingWindow();
                        BlockReference blkRef = (BlockReference) tr.GetObject(objectId, OpenMode.ForRead);
                        string selAttrName = "НОМЕР_ЛИСТА_2";
                        string fileName = Utils.GetBlockAttributeValue(blkRef, selAttrName);

                        //HostApplicationServices hs = HostApplicationServices.Current;
                        //string path = Application.GetSystemVariable("DWGPREFIX");
                        //hs.FindFile(doc.Name, doc.Database, FindFileHint.Default);
                        string createdwgFolder = Path.GetFileNameWithoutExtension(db.OriginalFileName);

                        string folderdwg = Path.GetDirectoryName(db.OriginalFileName);
                        string dwgFilename = Path.Combine(folderdwg, fileName + ".dwg");
                        objectCopier.CopyObjectsNewDatabases(objectIds, dwgFilename);
                        // objectCopier.CopyObjectsBetweenDatabases(objectIds, dwgFilename);
                        Active.Editor.WriteMessage("{0} сохранен", dwgFilename);
                        string newFileName = ZoomFilesAndSave(dwgFilename);
                        File.Delete(dwgFilename);
                        System.IO.File.Move(newFileName, dwgFilename);
                    }
                }

                doc.LockLayers(layersDictionary);
                tr.Commit();
            }
        }


        public static string ZoomFilesAndSave(string fileName)
        {
            string newFileName = "";
            using (Database db = new Database(false, false))
            {
                db.ReadDwgFile(fileName, FileOpenMode.OpenForReadAndReadShare, true, null);
                Database prevDb = HostApplicationServices.WorkingDatabase;
                HostApplicationServices.WorkingDatabase = db;
                db.UpdateExt(true);
                using (ViewportTable vTab = db.ViewportTableId.GetObject(OpenMode.ForRead) as ViewportTable)
                {
                    ObjectId acVptId = vTab["*Active"];
                    using (ViewportTableRecord vpTabRec = acVptId.GetObject(OpenMode.ForWrite) as ViewportTableRecord)
                    {
                        double scrRatio = (vpTabRec.Width / vpTabRec.Height);
                        Matrix3d matWCS2DCS = Matrix3d.PlaneToWorld(vpTabRec.ViewDirection);
                        matWCS2DCS = Matrix3d.Displacement(vpTabRec.Target - Point3d.Origin) * matWCS2DCS;
                        matWCS2DCS = Matrix3d.Rotation(-vpTabRec.ViewTwist,
                                         vpTabRec.ViewDirection,
                                         vpTabRec.Target)
                                     * matWCS2DCS;
                        matWCS2DCS = matWCS2DCS.Inverse();
                        Extents3d extents = new Extents3d(db.Extmin, db.Extmax);
                        extents.TransformBy(matWCS2DCS);
                        double width = (extents.MaxPoint.X - extents.MinPoint.X);
                        double height = (extents.MaxPoint.Y - extents.MinPoint.Y);
                        Point2d center = new Point2d((extents.MaxPoint.X + extents.MinPoint.X) * 0.5,
                            (extents.MaxPoint.Y + extents.MinPoint.Y) * 0.5);
                        if (width > (height * scrRatio))
                            height = width / scrRatio;
                        vpTabRec.Height = height;
                        vpTabRec.Width = height * scrRatio;
                        vpTabRec.CenterPoint = center;
                    }
                }

                HostApplicationServices.WorkingDatabase = prevDb;
                newFileName = fileName.Substring(0, fileName.Length - 4) + "z.dwg";
                db.SaveAs(newFileName, DwgVersion.Current);
            }

            return newFileName;
        }


        [CommandMethod("BlockIterator")]
        public static void BlockIterator_Method()
        {
            Database database = Active.Database;
            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                BlockTable blkTable = (BlockTable) transaction.GetObject(database.BlockTableId, OpenMode.ForRead);

                if (!blkTable.Has("ФорматM25"))
                {
                    Active.Editor.WriteMessage("такого блока нет");
                    return;
                }

                TypedValue[] typedValues = new TypedValue[2];
                typedValues.SetValue(new TypedValue((int) DxfCode.Start, "INSERT"), 0);
                typedValues.SetValue(new TypedValue((int) DxfCode.BlockName, "ФорматM25"), 1);

                SelectionFilter filter = new SelectionFilter(typedValues);

                PromptSelectionResult psr = Active.Editor.SelectAll(filter);

                if (psr.Status == PromptStatus.OK)
                {
                    SelectionSet ss = psr.Value;
                    string attrVal = string.Empty;
                    string header = string.Empty;

                    //StreamWriter writer = new StreamWriter();

                    SelectedObject selectedObject = ss[0];
                    BlockReference bref =
                        transaction.GetObject(selectedObject.ObjectId, OpenMode.ForWrite) as BlockReference;

                    if (bref.AttributeCollection.Count > 0)
                    {
                        header = "InsertionPtX, InsertionPtY,";
                        foreach (ObjectId attReferenceId in bref.AttributeCollection)
                        {
                            DBObject obj = transaction.GetObject(attReferenceId, OpenMode.ForRead);
                            AttributeReference attributeReference = obj as AttributeReference;

                            if (attributeReference != null)
                            {
                                header += attributeReference.Tag + ",";
                            }
                        }

                        Active.Editor.WriteMessage(header.Substring(0, header.Length - 1));
                    }

                    foreach (SelectedObject sobj in ss)
                    {
                        BlockReference br = (BlockReference) transaction.GetObject(sobj.ObjectId, OpenMode.ForWrite);
                        if (br.AttributeCollection.Count > 0)
                        {
                            attrVal += br.Position.X.ToString() + "," + br.Position.Y.ToString();

                            foreach (ObjectId attReferenceId in br.AttributeCollection)
                            {
                                DBObject obj = transaction.GetObject(attReferenceId, OpenMode.ForRead);
                                AttributeReference attRef = obj as AttributeReference;
                                if (attRef != null)
                                {
                                    attrVal += attRef.Tag + ",";
                                }
                            }

                            Active.Editor.WriteMessage(attrVal.Substring(0, header.Length - 1));
                        }
                    }

                    Active.Editor.WriteMessage($"Форматм25 найден {ss.Count.ToString()}");
                }
                else
                {
                    Active.Editor.WriteMessage("такого блока нет");
                }


                transaction.Commit();
            }
        }

        [CommandMethod("ListarBloques")]
        public void ListarBloques()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // open the block table which contains all the BlockTableRecords (block definitions and spaces)
                var blockTable = (BlockTable) tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                // open the model space BlockTableRecord
                var modelSpace =
                    (BlockTableRecord) tr.GetObject(blockTable[BlockTableRecord.PaperSpace], OpenMode.ForRead);

                // iterate through the model space 
                foreach (ObjectId id in modelSpace)
                {
                    // check if the current ObjectId is a block reference one
                    if (id.ObjectClass.DxfName == "INSERT")
                    {
                        // open the block reference
                        var blockReference = (BlockReference) tr.GetObject(id, OpenMode.ForRead);

                        // print the block name to the command line
                        ed.WriteMessage("\n" + blockReference.Name);
                    }
                }

                tr.Commit();
            }
        }

        private static void DisplayDynBlockProperties(Editor ed, BlockReference br, string name)
        {
            // Only continue is we have a valid dynamic block
            if (br != null && br.IsDynamicBlock)
            {
                ed.WriteMessage("\nDynamic properties for \"{0}\"\n", name);

                // Get the dynamic block's property collection
                DynamicBlockReferencePropertyCollection pc =
                    br.DynamicBlockReferencePropertyCollection;
                // Loop through, getting the info for each property
                foreach (DynamicBlockReferenceProperty prop in pc)
                {
                    // Start with the property name, type and description
                    ed.WriteMessage("\nProperty: \"{0}\" : {1}", prop.PropertyName, prop.UnitsType);

                    if (prop.Description != "")
                        ed.WriteMessage("\n  Description: {0}", prop.Description);
                    // Is it read-only?
                    if (prop.ReadOnly)
                        ed.WriteMessage(" (Read Only)");
                    // Get the allowed values, if it's constrained
                    bool first = true;
                    foreach (object value in prop.GetAllowedValues())
                    {
                        ed.WriteMessage((first ? "\n  Allowed values: [" : ", "));
                        ed.WriteMessage("\"{0}\"", value);
                        first = false;
                    }

                    if (!first) ed.WriteMessage("]");
                    // And finally the current value
                    ed.WriteMessage("\n  Current value: \"{0}\"\n", prop.Value);
                }
            }
        }

        //////////////////////////////////////////////////////////////////////////

        //Use: This command will publish all the layouts in the current

        //      drawing to a multi sheet dwf.

        //////////////////////////////////////////////////////////////////////////

        [CommandMethod("PublishAllLayouts")]
        static public void PublishAllLayouts()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            //put the plot in foreground
            short bgPlot = (short) Application.GetSystemVariable("BACKGROUNDPLOT");
            Application.SetSystemVariable("BACKGROUNDPLOT", 0);
            //get the layout ObjectId List
            System.Collections.Generic.List<ObjectId> layoutIds = GetLayoutIds(db);
            string dwgFileName = (string) Application.GetSystemVariable("DWGNAME");
            string dwgPath = (string) Application.GetSystemVariable("DWGPREFIX");
            using (Transaction Tx = db.TransactionManager.StartTransaction())
            {
                DsdEntryCollection collection = new DsdEntryCollection();
                foreach (ObjectId layoutId in layoutIds)
                {
                    Layout layout = Tx.GetObject(layoutId, OpenMode.ForRead)
                        as Layout;
                    DsdEntry entry = new DsdEntry();
                    entry.DwgName = dwgPath + dwgFileName;
                    entry.Layout = layout.LayoutName;
                    entry.Title = "Layout_" + layout.LayoutName;
                    entry.NpsSourceDwg = entry.DwgName;
                    entry.Nps = "Setup1";
                    collection.Add(entry);
                    //TODO have to think about creating collection depend of block view
                }
                dwgFileName = dwgFileName.Substring(0, dwgFileName.Length - 4);
                DsdData dsdData = new DsdData();
                dsdData.SheetType = SheetType.MultiPdf; //SheetType.MultiPdf
                dsdData.ProjectPath = dwgPath;
                dsdData.DestinationName =
                    dsdData.ProjectPath + dwgFileName + ".pdf";
                if (System.IO.File.Exists(dsdData.DestinationName))
                    System.IO.File.Delete(dsdData.DestinationName);
                dsdData.SetDsdEntryCollection(collection);
                string dsdFile = dsdData.ProjectPath + dwgFileName + ".dsd";
                //Workaround to avoid promp for dwf file name
                //set PromptForDwfName=FALSE in dsdData using
                //StreamReader/StreamWriter
                dsdData.WriteDsd(dsdFile);
                System.IO.StreamReader sr =
                    new System.IO.StreamReader(dsdFile);
                string str = sr.ReadToEnd();
                sr.Close();
                str = str.Replace(
                    "PromptForDwfName=TRUE", "PromptForDwfName=FALSE");
                System.IO.StreamWriter sw =
                    new System.IO.StreamWriter(dsdFile);
                sw.Write(str);
                sw.Close();
                dsdData.ReadDsd(dsdFile);
                System.IO.File.Delete(dsdFile);
                PlotConfig plotConfig =
                    Autodesk.AutoCAD.PlottingServices.PlotConfigManager.SetCurrentConfig("DWG_To_PDF_Uzle.pc3");
                //PlotConfig pc = Autodesk.AutoCAD.PlottingServices.
                //  PlotConfigManager.SetCurrentConfig("DWG To PDF.pc3");
                Autodesk.AutoCAD.Publishing.Publisher publisher =
                    Autodesk.AutoCAD.ApplicationServices.Application.Publisher;
                publisher.AboutToBeginPublishing +=
                    new Autodesk.AutoCAD.Publishing.
                        AboutToBeginPublishingEventHandler(
                            Publisher_AboutToBeginPublishing);
                publisher.PublishExecute(dsdData, plotConfig);
                Tx.Commit();
            }
            //reset the background plot value
            Application.SetSystemVariable("BACKGROUNDPLOT", bgPlot);
        }
        private static System.Collections.Generic.List<ObjectId>
            GetLayoutIds(Database db)
        {
            System.Collections.Generic.List<ObjectId> layoutIds =
                new System.Collections.Generic.List<ObjectId>();
            using (Transaction Tx = db.TransactionManager.StartTransaction())
            {
                DBDictionary layoutDic = Tx.GetObject(
                        db.LayoutDictionaryId,
                        OpenMode.ForRead, false)
                    as DBDictionary;
                foreach (DBDictionaryEntry entry in layoutDic)
                {
                    layoutIds.Add(entry.Value);
                }
            }
            return layoutIds;
        }
        static void Publisher_AboutToBeginPublishing(
            object sender,
            Autodesk.AutoCAD.Publishing.AboutToBeginPublishingEventArgs e)
        {
            Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(
                "\nAboutToBeginPublishing!!");
        }


        //////////////////////////////////////////////////////////////////////////

//Use: Will batch publish to single dwf every layout of

//      each document provided as input

//     Make sure each of your drawing contains a page setup

//      named Setup1 before running.

//          

//////////////////////////////////////////////////////////////////////////

        [CommandMethod("BatchPublishCmd", CommandFlags.Session)]
        static public void BatchPublishCmd()
        {
            short bgPlot =                (short) Application.GetSystemVariable("BACKGROUNDPLOT");
            Application.SetSystemVariable("BACKGROUNDPLOT", 0);
            System.Collections.Generic.List<string> docsToPlot =
                new System.Collections.Generic.List<string>();
            docsToPlot.Add(@"C:\Users\yusufzhon.marasulov\Desktop\test\test.dwg");
            docsToPlot.Add(@"C:\Users\yusufzhon.marasulov\Desktop\test\test1.dwg");
            BatchPublish(docsToPlot);
        }


        static public void BatchPublish(System.Collections.Generic.List<string> docsToPlot)
        {
            DsdEntryCollection collection = new DsdEntryCollection();
            Document doc = Application.DocumentManager.MdiActiveDocument;
            foreach (string filename in docsToPlot)
            {
                using (DocumentLock doclock = doc.LockDocument())
                {
                    Database db = new Database(false, true);
                    db.ReadDwgFile(
                        filename, System.IO.FileShare.Read, true, "");
                    System.IO.FileInfo fi = new System.IO.FileInfo(filename);
                    string docName =
                        fi.Name.Substring(0, fi.Name.Length - 4);
                    using (Transaction Tx =
                        db.TransactionManager.StartTransaction())
                    {
                        foreach (ObjectId layoutId in getLayoutIds(db))
                        {
                            Layout layout = Tx.GetObject(
                                    layoutId,
                                    OpenMode.ForRead)
                                as Layout;
                            DsdEntry entry = new DsdEntry();
                            entry.DwgName = filename;
                            entry.Layout = layout.LayoutName;
                            entry.Title = docName + "_" + layout.LayoutName;
                            entry.NpsSourceDwg = entry.DwgName;
                            entry.Nps = "Setup1";
                            collection.Add(entry);
                        }
                        Tx.Commit();
                    }
                }
            }
            DsdData dsdData = new DsdData();
            dsdData.SheetType = SheetType.MultiPdf;
            dsdData.ProjectPath = @"C:\Users\yusufzhon.marasulov\Desktop\test";
            //Not used for "SheetType.SingleDwf"
            //dsdData.DestinationName = dsdData.ProjectPath + "\\output.dwf";
            dsdData.SetDsdEntryCollection(collection);
            string dsdFile = dsdData.ProjectPath + "\\dsdData.dsd";
            //Workaround to avoid promp for dwf file name
            //set PromptForDwfName=FALSE in dsdData
            //using StreamReader/StreamWriter
            if (System.IO.File.Exists(dsdFile))
                System.IO.File.Delete(dsdFile);
            dsdData.WriteDsd(dsdFile);
            System.IO.StreamReader sr = new System.IO.StreamReader(dsdFile);
            string str = sr.ReadToEnd();
            sr.Close();
            str = str.Replace(
                "PromptForDwfName=TRUE", "PromptForDwfName=FALSE");
            System.IO.StreamWriter sw = new System.IO.StreamWriter(dsdFile);
            sw.Write(str);
            sw.Close();
            dsdData.ReadDsd(dsdFile);
            System.IO.File.Delete(dsdFile);
            PlotConfig plotConfig =
                Autodesk.AutoCAD.PlottingServices.PlotConfigManager.SetCurrentConfig("DWG_To_PDF_Uzle.pc3");
            Autodesk.AutoCAD.Publishing.Publisher publisher =
                Autodesk.AutoCAD.ApplicationServices.Application.Publisher;
            publisher.PublishExecute(dsdData, plotConfig);
        }


        private static System.Collections.Generic.List<ObjectId>
            getLayoutIds(Database db)
        {
            System.Collections.Generic.List<ObjectId> layoutIds =
                new System.Collections.Generic.List<ObjectId>();
            using (Transaction Tx = db.TransactionManager.StartTransaction())
            {
                DBDictionary layoutDic = Tx.GetObject(
                        db.LayoutDictionaryId,
                        OpenMode.ForRead, false)
                    as DBDictionary;
                foreach (DBDictionaryEntry entry in layoutDic)
                {
                    layoutIds.Add(entry.Value);
                }
            }
            return layoutIds;
        }


        [CommandMethod("selb")]
        public void ListAttribute()
        {
            ObjectIdCollection objectIds = Utils.SelectDynamicBlockReferences();
        }


        [CommandMethod("CtTransm")]
        public void ListAttributes()
        {
            Dictionary<string, string> attrList = new Dictionary<string, string>();

            MainWindow window = new MainWindow(new BlockViewModel(attrList));

            Application.ShowModalWindow(window);

            if (window.isClicked == true)
            {
                //var objectIds = Utils.GetAllCurrentSpaceBlocksByName(window.NameBlock.Text);
                ObjectIdCollection objectIds = Utils.SelectDynamicBlockReferences();

                List<Sheet> dict = new List<Sheet>();
                List<PrintModel> printModels = new List<PrintModel>();

                BlockAttribute objectNameEn = window.ComboObjectNameEn.SelectedItem as BlockAttribute;
                BlockAttribute objectNameRu = window.ComboObjectNameRu.SelectedItem as BlockAttribute;

                BlockAttribute position = window.ComboBoxPosition.SelectedItem as BlockAttribute;
                BlockAttribute nomination = window.ComboBoxNomination.SelectedItem as BlockAttribute;
                BlockAttribute comment = window.ComboBoxComment.SelectedItem as BlockAttribute;
                BlockAttribute trItem = window.ComboBoxTrItem.SelectedItem as BlockAttribute;
                BlockAttribute trDocNumber = window.ComboBoxTrDocNumber.SelectedItem as BlockAttribute;
                BlockAttribute trDocTitleEn = window.ComboBoxTrDocTitleEn.SelectedItem as BlockAttribute;
                BlockAttribute trDocTitleRu = window.ComboBoxTrDocTitleRu.SelectedItem as BlockAttribute;

                AttributModel attributModel = new AttributModel(objectNameEn, objectNameRu, position, nomination,
                    comment, trItem, trDocNumber, trDocTitleEn, trDocTitleRu);

                using (Transaction tr = Active.Database.TransactionManager.StartTransaction())
                {
                    MyCommands.GetSheetsFromBlocks(Active.Editor, dict, tr, objectIds);
                    string selAttrName = "НОМЕР_ЛИСТА";
                    MyCommands.GetPrintParametersToPdf(Active.Editor, printModels, tr, objectIds, selAttrName);

                    if (window.transmittalCheckBox.IsChecked == true)
                    {
                        Utils utils = new Utils();
                        utils.CreateOnlyVed(dict);
                        utils.CreateOnlytrans(dict);
                        foreach (var printModel in printModels)
                        {
                            PlotCurrentLayout(printModel.DocNumber, printModel);
                        }
                    }
                    else
                    {
                        //Utils utils = new Utils();
                        //utils.CreateOnlyVed(dict);
                        foreach (var printModel in printModels)
                        {
                            //PlotCurrentLayout(printModel.DocNumber, printModel.BlockExtents3d, printModel.StampViewName);
                        }
                    }

                    tr.Commit();
                }
            }
        }

        public void Initialize()
        {
            StandartCopier standartCopier = new StandartCopier();
            if (!File.Exists(standartCopier.Pc3Location) & !File.Exists(standartCopier.PmpLocation))
            {
                bool isCopied = standartCopier.CopyParamsFiles();
                if (isCopied)
                    Active.Editor.WriteMessage("Файлы {0}, {1} скопированы", standartCopier.Pc3Location,
                        standartCopier.PmpLocation);
                else
                {
                    Active.Editor.WriteMessage(
                        "Не удалось скопировать файлы настройки, скопируйте с сервера \\\\uz-fs\\install\\CAD\\Blocks файлы {0}  в {1} и {2} ",
                        standartCopier.Pc3Dest, standartCopier.Pc3Location, standartCopier.PmpLocation);
                }
            }
            else
            {
                Active.Editor.WriteMessage("Файлы настройки присутствуют, для перевода в pdf наберите CreateTranspdf");
            }

            Active.Editor.WriteMessage("Файлы настройки присутствуют, для перевода в pdf наберите CreateTranspdf");
        }

        public void Terminate()
        {
            throw new NotImplementedException();
        }


        #region из сети

        //public static void GetKeywordFromUser()
        //{
        //    Document acDoc = Application.DocumentManager.MdiActiveDocument;
        //    PromptKeywordOptions pKeyOpts = new PromptKeywordOptions("");
        //    pKeyOpts.Message = "\nEnter an option ";
        //    pKeyOpts.Keywords.Add("CREatedwg");
        //    pKeyOpts.Keywords.Add("ONlydwg");
        //    pKeyOpts.AllowNone = true;

        //    PromptResult pKeyRes = acDoc.Editor.GetKeywords(pKeyOpts);
        //    if (pKeyRes.StringResult == "CREatedwg")
        //    {
        //        Application.ShowAlertDialog("Entered keyword: " +
        //                                    pKeyRes.StringResult);
        //    }

        //    else if (pKeyRes.StringResult == "ONlydwg")
        //    {
        //        Application.ShowAlertDialog("Entered keysdfsdfword: " +
        //                                    pKeyOpts.Message);
        //    }

        //    else
        //    {
        //        ListAttributes1();
        //    }
        //}

        [CommandMethod("BlockExt")]
        public void BlockExt()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            PromptEntityOptions enOpt =
                new PromptEntityOptions("\nВыберите примитив: ");

            PromptEntityResult enRes = ed.GetEntity(enOpt);
            if (enRes.Status == PromptStatus.OK)
            {
                Extents3d blockExt =
                    new Extents3d(Point3d.Origin, Point3d.Origin);
                Matrix3d mat = Matrix3d.Identity;
                using (Entity en =
                    enRes.ObjectId.GetObject(OpenMode.ForRead) as Entity)
                {
                    GetBlockExtents(en, ref blockExt, ref mat);
                }

                string s =
                    "MinPoint: " + blockExt.MinPoint.ToString() + " " +
                    "MaxPoint: " + blockExt.MaxPoint.ToString();
                ed.WriteMessage(s);
                //------------------------------------------------------------
                // Только для тестирования полученного габаритного контейнера
                //------------------------------------------------------------

                #region TestinExts

                using (BlockTableRecord curSpace =
                    doc.Database.CurrentSpaceId.GetObject(OpenMode.ForWrite) as BlockTableRecord)
                {
                    Point3dCollection pts = new Point3dCollection();
                    pts.Add(blockExt.MinPoint);
                    pts.Add(new Point3d(blockExt.MinPoint.X,
                        blockExt.MaxPoint.Y, blockExt.MinPoint.Z));
                    pts.Add(blockExt.MaxPoint);
                    pts.Add(new Point3d(blockExt.MaxPoint.X,
                        blockExt.MinPoint.Y, blockExt.MinPoint.Z));
                    using (Polyline3d poly =
                        new Polyline3d(Poly3dType.SimplePoly, pts, true))
                    {
                        curSpace.AppendEntity(poly);
                    }
                }

                #endregion
            }
        }

        /// <summary>
        /// Рекурсивное получение габаритного контейнера для выбранного примитива.
        /// </summary>
        /// <param name="en">Имя примитива</param>
        /// <param name="ext">Габаритный контейнер</param>
        /// <param name="mat">Матрица преобразования из системы координат блока в МСК.</param>
        void GetBlockExtents(Entity en, ref Extents3d ext, ref Matrix3d mat)
        {
            if (!IsLayerOn(en.LayerId))
                return;
            if (en is BlockReference)
            {
                BlockReference bref = en as BlockReference;
                Matrix3d matIns = mat * bref.BlockTransform;
                using (BlockTableRecord btr =
                    bref.BlockTableRecord.GetObject(OpenMode.ForRead) as BlockTableRecord)
                {
                    foreach (ObjectId id in btr)
                    {
                        using (DBObject obj = id.GetObject(OpenMode.ForRead) as DBObject)
                        {
                            Entity enCur = obj as Entity;
                            if (enCur == null || enCur.Visible != true)
                                continue;
                            // Пропускаем неконстантные и невидимые определения атрибутов
                            AttributeDefinition attDef = enCur as AttributeDefinition;
                            if (attDef != null && (!attDef.Constant || attDef.Invisible))
                                continue;
                            GetBlockExtents(enCur, ref ext, ref matIns);
                        }
                    }
                }

                // Отдельно обрабатываем атрибуты блока
                if (bref.AttributeCollection.Count > 0)
                {
                    foreach (ObjectId idAtt in bref.AttributeCollection)
                    {
                        using (AttributeReference attRef =
                            idAtt.GetObject(OpenMode.ForRead) as AttributeReference)
                        {
                            if (!attRef.Invisible && attRef.Visible)
                                GetBlockExtents(attRef, ref ext, ref mat);
                        }
                    }
                }
            }
            else
            {
                if (mat.IsUniscaledOrtho())
                {
                    using (Entity enTr = en.GetTransformedCopy(mat))
                    {
                        if (enTr is Dimension)
                            (enTr as Dimension).RecomputeDimensionBlock(true);
                        if (enTr is Table)
                            (enTr as Table).RecomputeTableBlock(true);
                        if (IsEmptyExt(ref ext))
                            try
                            {
                                ext = enTr.GeometricExtents;
                            }
                            catch
                            {
                                // ignored
                            }
                        else
                        {
                            try
                            {
                                ext.AddExtents(enTr.GeometricExtents);
                            }
                            catch
                            {
                                // ignored
                            }
                        }

                        return;
                    }
                }
                else
                {
                    try
                    {
                        Extents3d curExt = en.GeometricExtents;
                        curExt.TransformBy(mat);
                        if (IsEmptyExt(ref ext))
                            ext = curExt;
                        else
                            ext.AddExtents(curExt);
                    }
                    catch
                    {
                    }

                    return;
                }
            }
        }

        /// <summary>
        /// Определяет видны ли объекты на слое, указанном его ObjectId.
        /// </summary>
        /// <param name="layerId">ObjectId слоя.</param>
        /// <returns></returns>
        bool IsLayerOn(ObjectId layerId)
        {
            using (LayerTableRecord ltr = layerId.GetObject(OpenMode.ForRead) as LayerTableRecord)
            {
                if (ltr.IsFrozen) return false;
                if (ltr.IsOff) return false;
            }

            return true;
        }

        /// <summary>
        /// Определят не пустой ли габаритный контейнер.
        /// </summary>
        /// <param name="ext">Габаритный контейнер.</param>
        /// <returns></returns>
        bool IsEmptyExt(ref Extents3d ext)
        {
            if (ext.MinPoint.DistanceTo(ext.MaxPoint) < Tolerance.Global.EqualPoint)
                return true;
            else
                return false;
        }

        #endregion
    }
}