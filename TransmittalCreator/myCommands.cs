﻿// (C) Copyright 2020 by HP Inc. 
//

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
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
        public void CreateHvacTable() // This method can have any name
        {
            string documents = Path.GetDirectoryName(Active.Document.Name);
            Environment.SetEnvironmentVariable("MYDOCUMENTS", documents);

            var ofd = new OpenFileDialog("Select a file using an OpenFileDialog", documents,
                "xlsx; *",
                "File Date Test T22",
                OpenFileDialog.OpenFileDialogFlags.DefaultIsFolder |
                OpenFileDialog.OpenFileDialogFlags.ForceDefaultFolder // .AllowMultiple
            );
            DialogResult sdResult = ofd.ShowDialog();

            if (sdResult != System.Windows.Forms.DialogResult.OK) return;

            string filename = ofd.Filename;

            List<HvacTable> hvacTables = CreateHvacTableListFromFile(filename);

            Type type = typeof(HvacTable);

            int tableCols = type.GetProperties().Length;
            CreateLayer();

            MyMessageFilter filter = new MyMessageFilter();

            System.Windows.Forms.Application.AddMessageFilter(filter);

            foreach (var hvacTable in hvacTables)
            {
                // Check for user input events
                System.Windows.Forms.Application.DoEvents();
                if (filter.bCanceled == true)
                {
                    Active.Editor.WriteMessage("\nLoop cancelled.");
                    break;
                }
                AddTable(hvacTable, tableCols);
            }
            System.Windows.Forms.Application.RemoveMessageFilter(filter);
        }

        public class MyMessageFilter : IMessageFilter
        {
            public const int WM_KEYDOWN = 0x0100;
            public bool bCanceled = false;
            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg == WM_KEYDOWN)
                {
                    // Check for the Escape keypress
                    Keys kc = (Keys)(int)m.WParam & Keys.KeyCode;

                    if (m.Msg == WM_KEYDOWN && kc == Keys.Escape)
                    {
                        bCanceled = true;
                    }
                    // Return true to filter all keypresses
                    return true;
                }
                // Return false to let other messages through
                return false;
            }
        }

        private List<HvacTable> CreateHvacTableListFromFile(string filename)
        {
            FileInfo fileInfo = new FileInfo(filename);
            List<HvacTable> listData = new List<HvacTable>();

            using (ExcelPackage package = new ExcelPackage(fileInfo))
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                //create an instance of the the first sheet in the loaded file
                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                int rowStart = 7;
                int rowCount = worksheet.Dimension.End.Row;

                for (int i = rowStart; i < rowCount - 1; i++)
                {
                    var roomcell = worksheet.Cells[i, 1].Value;

                    if (roomcell != null)
                    {
                        bool totalStr = roomcell.ToString().ToUpper().Contains("TOTAL");

                        if (!totalStr)
                        {
                            string roomNumber = worksheet.Cells[i, 1].Value?.ToString().Trim();
                            string roomName = worksheet.Cells[i, 2].Value?.ToString().Trim();
                            string roomTemp = worksheet.Cells[i, 5].Value?.ToString().Trim();
                            string heating = worksheet.Cells[i, 26].Value?.ToString().Trim();
                            string cooling = worksheet.Cells[i, 34].Value?.ToString().Trim();
                            string supply = worksheet.Cells[i, 39].Value?.ToString().Trim();

                            string supplyIn = "П";
                            var supplyInd = worksheet.Cells[i, 38].Value?.ToString().Trim() ?? supplyIn;
                            string exhaustInd = "В";
                            if (worksheet.Cells[i, 40].Value != null)
                                exhaustInd = worksheet.Cells[i, 40].Value.ToString().Trim();

                            string exhaust = worksheet.Cells[i, 41].Value?.ToString().Trim();

                            listData.Add(new HvacTable(roomNumber, roomName, roomTemp, heating, cooling, supply, supplyInd,
                                exhaust, exhaustInd));
                        }
                    }
                }
            }

            return listData;
        }
        #endregion


        private void CreateLayer()
        {
            using (Transaction tr = Active.Database.TransactionManager.StartTransaction())
            {
                LayerTable ltb = (LayerTable)tr.GetObject(Active.Database.LayerTableId,
                    OpenMode.ForRead);

                //create a new layout.

                if (!ltb.Has("Hvac_Calc"))

                {
                    ltb.UpgradeOpen();

                    LayerTableRecord newLayer = new LayerTableRecord();

                    newLayer.Name = "Hvac_Calc";
                    newLayer.LineWeight = LineWeight.LineWeight005;
                    newLayer.Description = "This is new layer";
                    newLayer.IsPlottable = false;

                    //red color
                    //newLayer.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(255, 0, 0);
                    ltb.Add(newLayer);
                    tr.AddNewlyCreatedDBObject(newLayer, true);
                }

                tr.Commit();
                //make it as current
                Active.Database.Clayer = ltb["Hvac_Calc"];
            }
        }

        private static void SetCellPropsWithValue(Table tb, int curRow, int curCol, int textHeight, short colorNumber, string stringValue)
        {
            var curPos = tb.Cells[curRow, curCol];
            curPos.TextHeight = textHeight;
            curPos.TextString = stringValue;
            curPos.Alignment = CellAlignment.MiddleCenter;
            curPos.ContentColor = Color.FromColorIndex(ColorMethod.ByAci, colorNumber);
        }

        [CommandMethod("SDB")]
        public static void SelectBlocksByName()
        {
            Archive.SelectDynamicBlocks();
        }

        [CommandMethod("LISTATT")]
        public static void ListAttributes1()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;
            var filter = new SelectionFilter(new[] { new TypedValue(0, "INSERT") });
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
                    var br = (BlockReference)tr.GetObject(so.ObjectId, OpenMode.ForRead);
                    var vargeom = br.GeometricExtents;
                    ed.WriteMessage(vargeom.MinPoint[0].ToString(CultureInfo.InvariantCulture));
                    str += $"{br.Name} {br.Position:0.00}\r\n";
                    if (br.AttributeCollection.Count > 0)
                    {
                        str += "Attributes:\r\n";
                        foreach (ObjectId id in br.AttributeCollection)
                        {
                            var att = (AttributeReference)id.GetObject(OpenMode.ForRead);
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

        //TODO refactor


        [CommandMethod("ListLayouts")]

        public static void ListLayoutsMethod()

        {

            Document doc

                = Application.DocumentManager.MdiActiveDocument;

            Database db = doc.Database;

            Editor ed = doc.Editor;



            LayoutManager layoutMgr = LayoutManager.Current;



            ed.WriteMessage

            (

                String.Format

                (

                    "{0}Active Layout is : {1}",

                    Environment.NewLine,

                    layoutMgr.CurrentLayout

                )

            );



            ed.WriteMessage

            (

                String.Format

                (

                    "{0}Number of Layouts: {1}{0}List of all Layouts:",

                    Environment.NewLine,

                    layoutMgr.LayoutCount

                )

            );



            using (Transaction tr

                = db.TransactionManager.StartTransaction())

            {

                DBDictionary layoutDic

                    = tr.GetObject(

                        db.LayoutDictionaryId,

                        OpenMode.ForRead,

                        false

                    ) as DBDictionary;



                foreach (DBDictionaryEntry entry in layoutDic)

                {

                    ObjectId layoutId = entry.Value;



                    Layout layout

                        = tr.GetObject(

                            layoutId,

                            OpenMode.ForRead

                        ) as Layout;



                    ed.WriteMessage(

                        String.Format(

                            "{0}--> {1}",

                            Environment.NewLine,

                            layout.LayoutName

                        )

                    );

                }

                tr.Commit();

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
                    BlockReference blRef = (BlockReference)tr.GetObject(objectId, OpenMode.ForRead);
                    BlockTableRecord block =
                        tr.GetObject(blRef.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                    if (!(block is null))
                    {
                        string blockName = block.Name;

                        if (blockName == "Формат" | blockName == "ФорматM25") idArray.Add(objectId);
                        //else if (blockName == "ФорматM25") idArray.Add(objectId);
                    }
                }

                GetSheetsFromBlocks(Active.Editor, dict, tr, idArray);
                string selAttrName = "НОМЕР_ЛИСТА";
                GetPrintParametersToPdf(Active.Editor, printModels, tr, idArray, selAttrName);
                //Active.Editor.WriteMessage("печать {0} - {1}", printModels[0].DocNumber, printModels.Count);

                //foreach (ObjectId objectId in idArray)
                //{

                //    ObjectCopier objectCopier = new ObjectCopier(objectId);
                //    ObjectIdCollection objectIds = objectCopier.SelectCrossingWindow();
                //    string fileName = Utils.GetBlockAttributeValue(tr, objectId);
                //    objectCopier.CopyObjectsBetweenDatabases(objectIds, fileName);
                //}

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
        //TODO finish

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
                        BlockReference blRef = (BlockReference)tr.GetObject(objectId, OpenMode.ForRead);
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
                        BlockReference blkRef = (BlockReference)tr.GetObject(objectId, OpenMode.ForRead);
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

        [CommandMethod("UL")]
        public void UnlockLayers()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            doc.LockOrUnlockLayers(false);
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

        [CommandMethod("LockLayer")]
        public static void LockLayer()
        {
            // Get the current document and database
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            // Start a transaction
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Open the Layer table for read
                LayerTable acLyrTbl;
                acLyrTbl = acTrans.GetObject(acCurDb.LayerTableId,
                    OpenMode.ForRead) as LayerTable;
                string sLayerName = "ABC";
                LayerTableRecord acLyrTblRec;
                if (acLyrTbl.Has(sLayerName) == false)
                {
                    acLyrTblRec = new LayerTableRecord();
                    // Assign the layer a name
                    acLyrTblRec.Name = sLayerName;
                    // Upgrade the Layer table for write
                    acLyrTbl.UpgradeOpen();
                    // Append the new layer to the Layer table and the transaction
                    acLyrTbl.Add(acLyrTblRec);
                    acTrans.AddNewlyCreatedDBObject(acLyrTblRec, true);
                }
                else
                {
                    acLyrTblRec = acTrans.GetObject(acLyrTbl[sLayerName],
                        OpenMode.ForWrite) as LayerTableRecord;
                }

                // Lock the layer
                acLyrTblRec.IsLocked = true;
                // Save the changes and dispose of the transaction
                acTrans.Commit();
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

        public static void GetKeywordFromUser()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            PromptKeywordOptions pKeyOpts = new PromptKeywordOptions("");
            pKeyOpts.Message = "\nEnter an option ";
            pKeyOpts.Keywords.Add("CREatedwg");
            pKeyOpts.Keywords.Add("ONlydwg");
            pKeyOpts.AllowNone = true;

            PromptResult pKeyRes = acDoc.Editor.GetKeywords(pKeyOpts);
            if (pKeyRes.StringResult == "CREatedwg")
            {
                Application.ShowAlertDialog("Entered keyword: " +
                                            pKeyRes.StringResult);
            }

            else if (pKeyRes.StringResult == "ONlydwg")
            {
                Application.ShowAlertDialog("Entered keysdfsdfword: " +
                                            pKeyOpts.Message);
            }

            else
            {
                ListAttributes1();
            }
        }

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