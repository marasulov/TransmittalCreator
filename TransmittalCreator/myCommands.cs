// (C) Copyright 2020 by HP Inc. 
//
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.LayerManager;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;
using DV2177.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using TransmittalCreator.Models;
using TransmittalCreator.Services;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Db = Autodesk.AutoCAD.DatabaseServices;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(TransmittalCreator.MyCommands))]

namespace TransmittalCreator
{
    public class MyCommands : Utils, IExtensionApplication
    {
        public static void GetSelectionWithKeywords()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            // Create our options object
            PromptSelectionOptions pso = new PromptSelectionOptions();
            // Add our keywords
            pso.Keywords.Add("creatEPd");
            pso.Keywords.Add("createDwg");
            pso.Keywords.Add("creatEpdfwg");
            // Set our prompts to include our keywords
            string kws = pso.Keywords.GetDisplayString(true);
            pso.MessageForAdding =
              "\nAdd objects to selection or " + kws;
            pso.MessageForRemoval =
              "\nRemove objects from selection or " + kws;
            // Implement a callback for when keywords are entered
            string inputStr = "";

            pso.KeywordInput +=
                delegate (object sender, SelectionTextInputEventArgs e)
                {
                    //ed.WriteMessage("\nKeyword entered: {0}", e.Input);
                    inputStr = e.Input;
                    if (inputStr == "createPDf")
                    {
                        ed.WriteMessage("\n здесь метод для pdf");
                        ListAttributes1();
                        return;
                    }
                    else if (inputStr == "createDwg")
                    {
                        ed.WriteMessage("\nздесь метод для dwg");
                        CreateTransmittalAndPDF();
                        return;
                    }
                    else if (inputStr == "creatEPdfwg")
                    {
                        ed.WriteMessage("\nздесь метод для pdf and dwg");
                        return;
                    }
                    ed.WriteMessage("\nKeyword entered: {0}", e.Input);
                };

            // Finally run the selection and show any results
            PromptSelectionResult psr = ed.GetSelection(pso);

        }

        // Modal Command with pickfirst selection
        [CommandMethod("MyGroup1", "MyPickFirst", "MyPickFirstLocal", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void MyPickFirst() // This method can have any name
        {
            Dictionary<ObjectId, bool> layersDictionary = LayerManipulation.GetLayersIsBlockedCol();
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            doc.LockOrUnlockLayers(false);
            foreach (var item in layersDictionary)
            {
                Active.Editor.WriteMessage("\n{0}-{1}",item.Key.ToString(), item.Value.ToString());
            }
            doc.LockLayers(layersDictionary);

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
                    ed.WriteMessage(vargeom.MinPoint[0].ToString());
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

        [CommandMethod("CreateTranspdf")]
        public static void CreateTransmittalAndPDF()
        {
            List<Sheet> dict = new List<Sheet>();
            List<PrintModel> printModels = new List<PrintModel>();
            Active.Document.SendStringToExecute("REGENALL ", true, false, true);

            using (Transaction tr = Active.Database.TransactionManager.StartTransaction())
            {
                TypedValue[] filList = new TypedValue[1] { new TypedValue((int)DxfCode.Start, "INSERT") };
                SelectionFilter filter = new SelectionFilter(filList);
                PromptSelectionOptions opts = new PromptSelectionOptions();
                opts.MessageForAdding = "Select block references: ";
                PromptSelectionResult res = Active.Editor.GetSelection(opts, filter);
                
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
                    BlockTableRecord block = tr.GetObject(blRef.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                    string blockName = block.Name;

                    if (blockName == "Формат") idArray.Add(objectId);
                    else if (blockName == "ФорматM25") idArray.Add(objectId);

                    Active.Document.Editor.WriteMessage(blockName);
                }

                MyCommands.GetSheetsFromBlocks(Active.Editor, dict, tr, idArray);
                MyCommands.GetExtentsNamePdf(Active.Editor, printModels, tr, idArray);
                //Active.Editor.WriteMessage("печать {0} - {1}", printModels[0].DocNumber, printModels.Count);

                //foreach (ObjectId objectId in idArray)
                //{

                //    ObjectCopier objectCopier = new ObjectCopier(objectId);
                //    ObjectIdCollection objectIds = objectCopier.SelectCrossingWindow();
                //    string fileName = Utils.GetFileNameFromBlockAttribute(tr, objectId);
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

        [CommandMethod("CreateDwg")]
        public static void CreateDwg()
        {
            List<Sheet> dict = new List<Sheet>();
            List<PrintModel> printModels = new List<PrintModel>();
            Active.Document.SendStringToExecute("REGENALL ", true, false, true);
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            Dictionary<ObjectId, bool> layersDictionary = LayerManipulation.GetLayersIsBlockedCol();
            
            if (doc == null) return;
            doc.LockOrUnlockLayers(false, ignoreCurrent:false, lockZero:true);

            bool isExec = true;

            using (Transaction tr = Active.Database.TransactionManager.StartTransaction())
            {
                TypedValue[] filList = new TypedValue[1] { new TypedValue((int)DxfCode.Start, "INSERT") };
                SelectionFilter filter = new SelectionFilter(filList);
                PromptSelectionOptions opts = new PromptSelectionOptions();
                opts.MessageForAdding = "Select block references: ";
                PromptSelectionResult res = Active.Editor.GetSelection(opts, filter);

                if (res.Status != PromptStatus.OK)
                {
                    isExec = false;
                    Active.Editor.WriteMessage("Надо выбрать блок");
                }

                if (isExec != false)
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
                        BlockTableRecord block = tr.GetObject(blRef.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                        string blockName = block.Name;

                        if (blockName == "Формат") idArray.Add(objectId);
                        else if (blockName == "ФорматM25") idArray.Add(objectId);

                        //Active.Document.Editor.WriteMessage(blockName);
                    }

                    //MyCommands.GetSheetsFromBlocks(Active.Editor, dict, tr, idArray);
                    //MyCommands.GetExtentsNamePdf(Active.Editor, printModels, tr, idArray);
                    //Active.Editor.WriteMessage("печать {0} - {1}", printModels[0].DocNumber, printModels.Count);

                    foreach (ObjectId objectId in idArray)
                    {
                        ObjectCopier objectCopier = new ObjectCopier(objectId);
                        ObjectIdCollection objectIds = objectCopier.SelectCrossingWindow();
                        BlockReference blkRef = (BlockReference)tr.GetObject(objectId, OpenMode.ForRead);
                        string fileName = Utils.GetFileNameFromBlockAttribute(blkRef);

                        //HostApplicationServices hs = HostApplicationServices.Current;
                        //string path = Application.GetSystemVariable("DWGPREFIX");
                        //hs.FindFile(doc.Name, doc.Database, FindFileHint.Default);
                        string path = Path.GetFullPath(db.OriginalFileName);
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

                    //Utils utils = new Utils();
                    //utils.CreateOnlyVed(dict);
                    //utils.CreateJsonFile(dict);

                    //foreach (var printModel in printModels)
                    //{

                    //    Active.Editor.WriteMessage("{0} печатаем ", printModel.DocNumber);
                    //    PlotCurrentLayout(printModel.DocNumber, printModel);
                    //}

                    //utils.CreateOnlytrans(dict);
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

        public static void PlotCurrentLayout(string pdfFileName, PrintModel printModel)
        {
            // Get the current document and database, and start a transaction
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            short bgPlot = (short)Application.GetSystemVariable("BACKGROUNDPLOT");
            Application.SetSystemVariable("BACKGROUNDPLOT", 0);
            try
            {
                using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
                {
                    // Reference the Layout Manager
                    LayoutManager acLayoutMgr;
                    acLayoutMgr = LayoutManager.Current;
                    // Get the current layout and output its name in the Command Line window
                    Layout acLayout;
                    acLayout = acTrans.GetObject(acLayoutMgr.GetLayoutId(acLayoutMgr.CurrentLayout), OpenMode.ForRead) as Layout;

                    // Get the PlotInfo from the layout
                    PlotInfo acPlInfo = new PlotInfo();
                    acPlInfo.Layout = acLayout.ObjectId;

                    // Get a copy of the PlotSettings from the layout
                    PlotSettings acPlSet = new PlotSettings(acLayout.ModelType);
                    acPlSet.CopyFrom(acLayout);
                    // Update the PlotSettings object
                    PlotSettingsValidator acPlSetVdr = PlotSettingsValidator.Current;

                    //acPlSetVdr.SetPlotPaperUnits(acPlSet, PlotPaperUnit.Millimeters);
                    // Set the plot type
                    //Point3d minPoint3dWcs = new Point3d(5112.2723, 1697.3971, 0);
                    //Point3d minPoint3d = Autodesk.AutoCAD.Internal.Utils.UcsToDisplay(minPoint3dWcs, false);
                    //Point3d maxPoint3dWcs = new Point3d(6388.6557, 2291.3971, 0);
                    //Point3d maxPoint3d = Autodesk.AutoCAD.Internal.Utils.UcsToDisplay(maxPoint3dWcs, false);
                    //Extents2d points = new Extents2d(new Point2d(minPoint3d[0], minPoint3d[1]), new Point2d(maxPoint3d[0], maxPoint3d[1]));
                    //extents3d = new Extents3d(minPoint3dWcs, maxPoint3dWcs);
                    //PdfCreator pdfCreator = new PdfCreator(extents3d);

                    Extents2d points = new Extents2d(printModel.BlockPosition, printModel.BlockDimensions);

                    bool isHor = printModel.IsFormatHorizontal();
                    //pdfCreator.GetBlockDimensions();
                    string canonName = printModel.GetCanonNameByWidthAndHeight();

                    //acDoc.Utility.TranslateCoordinates(point1, acWorld, acDisplayDCS, False);
                    acPlSetVdr.SetPlotWindowArea(acPlSet, points);
                    acPlSetVdr.SetPlotType(acPlSet, Autodesk.AutoCAD.DatabaseServices.PlotType.Window);
                    if (!isHor)
                        acPlSetVdr.SetPlotRotation(acPlSet, PlotRotation.Degrees090);
                    acPlSetVdr.SetPlotRotation(acPlSet, PlotRotation.Degrees000);
                    // Set the plot scale
                    acPlSetVdr.SetUseStandardScale(acPlSet, false);
                    acPlSetVdr.SetStdScaleType(acPlSet, StdScaleType.ScaleToFit);
                    // Center the plot
                    acPlSetVdr.SetPlotCentered(acPlSet, true);
                    //acPlSetVdr.SetClosestMediaName(acPlSet,printModel.width,printModel.height,PlotPaperUnit.Millimeters,true);
                    //string curCanonName = PdfCreator.GetLocalNameByAtrrValue(formatValue);
                    acPlSetVdr.SetPlotConfigurationName(acPlSet, "DWG_To_PDF_Uzle.pc3", canonName);
                    //acPlSetVdr.SetCanonicalMediaName(acPlSet, curCanonName);

                    // Set the plot device to use

                    // Set the plot info as an override since it will
                    // not be saved back to the layout
                    acPlInfo.OverrideSettings = acPlSet;
                    // Validate the plot info
                    PlotInfoValidator acPlInfoVdr = new PlotInfoValidator();
                    acPlInfoVdr.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
                    acPlInfoVdr.Validate(acPlInfo);

                    // Check to see if a plot is already in progress
                    if (PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting)
                    {
                        using (PlotEngine acPlEng = PlotFactory.CreatePublishEngine())
                        {
                            // Track the plot progress with a Progress dialog
                            PlotProgressDialog acPlProgDlg = new PlotProgressDialog(false, 1, true);
                            using (acPlProgDlg)
                            {
                                // Define the status messages to display when plotting starts
                                acPlProgDlg.set_PlotMsgString(PlotMessageIndex.DialogTitle, "Plot Progress");
                                acPlProgDlg.set_PlotMsgString(PlotMessageIndex.CancelJobButtonMessage, "Cancel Job");
                                acPlProgDlg.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, "Cancel Sheet");
                                acPlProgDlg.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption, "Sheet Set Progress");
                                acPlProgDlg.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "Sheet Progress");
                                // Set the plot progress range
                                acPlProgDlg.LowerPlotProgressRange = 0;
                                acPlProgDlg.UpperPlotProgressRange = 100;
                                acPlProgDlg.PlotProgressPos = 0;
                                // Display the Progress dialog
                                acPlProgDlg.OnBeginPlot();
                                acPlProgDlg.IsVisible = true;
                                // Start to plot the layout
                                acPlEng.BeginPlot(acPlProgDlg, null);
                                // Define the plot output
                                string filename = Path.Combine(Path.GetDirectoryName(acDoc.Name), pdfFileName);
                                Active.Editor.WriteMessage(filename);

                                acPlEng.BeginDocument(acPlInfo, acDoc.Name, null, 1, true, filename);
                                // Display information about the current plot
                                acPlProgDlg.set_PlotMsgString(PlotMessageIndex.Status, "Plotting: " + acDoc.Name + " - " + acLayout.LayoutName);
                                // Set the sheet progress range
                                acPlProgDlg.OnBeginSheet();
                                acPlProgDlg.LowerSheetProgressRange = 0;
                                acPlProgDlg.UpperSheetProgressRange = 100;
                                acPlProgDlg.SheetProgressPos = 0;
                                // Plot the first sheet/layout
                                PlotPageInfo acPlPageInfo = new PlotPageInfo();
                                acPlEng.BeginPage(acPlPageInfo, acPlInfo, true, null);
                                acPlEng.BeginGenerateGraphics(null);
                                acPlEng.EndGenerateGraphics(null);
                                // Finish plotting the sheet/layout
                                acPlEng.EndPage(null);
                                acPlProgDlg.SheetProgressPos = 100;
                                acPlProgDlg.OnEndSheet();
                                // Finish plotting the document
                                acPlEng.EndDocument(null);
                                // Finish the plot
                                acPlProgDlg.PlotProgressPos = 100;
                                acPlProgDlg.OnEndPlot();
                                acPlEng.EndPlot(null);
                            }
                        }
                    }
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog(e.Message);
                throw;
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
                using (ViewportTable vTab = db.ViewportTableId.Open(OpenMode.ForRead) as ViewportTable)
                {
                    ObjectId acVptId = vTab["*Active"];
                    using (ViewportTableRecord vpTabRec = acVptId.Open(OpenMode.ForWrite) as ViewportTableRecord)
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

            //UsingTransaction(LockLayers);
            // Start a transaction
            UsingTransaction(
                acTrans =>
                {

                // Open the Layer table for read
                LayerTable acLyrTbl;
                acLyrTbl = acTrans.GetObject(Active.Database.LayerTableId,
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
                
            
                }
            );
        }

        public static void UsingTransaction(Action<Transaction> action)
        {
            using (var tr = Active.Database.TransactionManager.StartTransaction())
            {
                try
                {
                    action(tr);
                    tr.Commit();
                }
                catch (System.Exception)
                {
                    tr.Abort();
                    throw;
                }
            }
        }
        private static void LockLayers(Transaction acTrans)
        {
            LayerTable acLyrTbl;
            acLyrTbl = acTrans.GetObject(Active.Database.LayerTableId,
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

        //[CommandMethod("CtTransm")]
        //public void ListAttributes()
        //{
        //    //Dictionary<string, string> attrList = new Dictionary<string, string>();

        //    //MainWindow window = new MainWindow(new BlockViewModel(attrList));

        //    //Application.ShowModalWindow(window);

        //    //if (window.isClicked == true)
        //    //{
        //    //var objectIds = Utils.GetAllCurrentSpaceBlocksByName(window.NameBlock.Text);
        //    ObjectIdCollection objectIds = Utils.SelectDynamicBlockReferences();

        //    List<Sheet> dict = new List<Sheet>();
        //    List<PrintModel> printModels = new List<PrintModel>();

        //    //BlockModel objectNameEn = window.ComboObjectNameEn.SelectedItem as BlockModel;
        //    //BlockModel objectNameRu = window.ComboObjectNameRu.SelectedItem as BlockModel;

        //    //BlockModel position = window.ComboBoxPosition.SelectedItem as BlockModel;
        //    //BlockModel nomination = window.ComboBoxNomination.SelectedItem as BlockModel;
        //    //BlockModel comment = window.ComboBoxComment.SelectedItem as BlockModel;
        //    //BlockModel trItem = window.ComboBoxTrItem.SelectedItem as BlockModel;
        //    //BlockModel trDocNumber = window.ComboBoxTrDocNumber.SelectedItem as BlockModel;
        //    //BlockModel trDocTitleEn = window.ComboBoxTrDocTitleEn.SelectedItem as BlockModel;
        //    //BlockModel trDocTitleRu = window.ComboBoxTrDocTitleRu.SelectedItem as BlockModel;

        //    //AttributModel attributModel = new AttributModel(objectNameEn, objectNameRu, position, nomination,
        //    //    comment, trItem, trDocNumber, trDocTitleEn, trDocTitleRu);

        //    using (Transaction tr = Active.Database.TransactionManager.StartTransaction())
        //    {
        //        //MyCommands.GetSheetsFromBlocks(Active.Editor, dict, tr, objectIds);
        //        //MyCommands.GetExtentsNamePdf(Active.Editor, printModels, tr, objectIds);

        //        //if (window.transmittalCheckBox.IsChecked == true)
        //        //{
        //            Utils utils = new Utils();
        //            //utils.CreateOnlyVed(dict);
        //            //utils.CreateOnlytrans(dict);
        //            foreach (var printModel in printModels)
        //            {
        //                //PlotCurrentLayout(printModel.DocNumber, printModel., printModel.StampViewName);
        //            }
        //        //}
        //        //else
        //        //{
        //        //    //Utils utils = new Utils();
        //        //    //utils.CreateOnlyVed(dict);
        //        //    foreach (var printModel in printModels)
        //        //    {
        //        //        //PlotCurrentLayout(printModel.DocNumber, printModel.BlockExtents3d, printModel.StampViewName);
        //        //    }
        //        //}

        //        tr.Commit();
        //    }
        //    //}
        //}

        public void Initialize()
        {
            StandartCopier standartCopier = new StandartCopier();
            if (!File.Exists(standartCopier.Pc3Location) & !File.Exists(standartCopier.PmpLocation))
            {
                bool isCopied = standartCopier.CopyParamsFiles();
                if (isCopied) Active.Editor.WriteMessage("Файлы {0}, {1} скопированы", standartCopier.Pc3Location, standartCopier.PmpLocation);
                else
                {
                    Active.Editor.WriteMessage("Не удалось скопировать файлы настройки, скопируйте с сервера \\\\uz-fs\\install\\CAD\\Blocks файлы {0}  в {1} и {2} ",
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
                Application.ShowAlertDialog("Entered kaseyword: " +
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

        private static SearchOption IncludeSubdirs(Editor ed, DirectoryInfo dir)
        {
            // Вопрос - включая подпапки?
            SearchOption recursive = SearchOption.AllDirectories;
            if (dir.GetDirectories().Length > 0)
            {
                var opt = new PromptKeywordOptions("\nВключая подпапки");
                opt.Keywords.Add("Да");
                opt.Keywords.Add("Нет");
                opt.Keywords.Default = "Да";
                var res = ed.GetKeywords(opt);
                if (res.Status == PromptStatus.OK)
                {
                    if (res.StringResult == "Нет")
                    {
                        recursive = SearchOption.TopDirectoryOnly;
                    }
                }
            }
            ed.WriteMessage("\nПапка для переопределения блока " + dir.FullName);
            if (recursive == SearchOption.AllDirectories)
                ed.WriteMessage("\nВключая подпапки");
            else
                ed.WriteMessage("\nТолько в этой папке, без подпапок.");
            return recursive;
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
                  enRes.ObjectId.Open(OpenMode.ForRead) as Entity)
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
                  doc.Database.CurrentSpaceId.Open(OpenMode.ForWrite) as BlockTableRecord)
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
                  bref.BlockTableRecord.Open(OpenMode.ForRead) as BlockTableRecord)
                {
                    foreach (ObjectId id in btr)
                    {
                        using (DBObject obj = id.Open(OpenMode.ForRead) as DBObject)
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
                          idAtt.Open(OpenMode.ForRead) as AttributeReference)
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
                        {
                            try { ext = enTr.GeometricExtents; } catch { };
                        }
                        else
                        {
                            try { ext.AddExtents(enTr.GeometricExtents); } catch { };
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
                    catch { }
                    return;
                }
            }

            return;

        }
        /// <summary>
        /// Определяет видны ли объекты на слое, указанном его ObjectId.
        /// </summary>
        /// <param name="layerId">ObjectId слоя.</param>
        /// <returns></returns>
        bool IsLayerOn(ObjectId layerId)
        {
            using (LayerTableRecord ltr = layerId.Open(OpenMode.ForRead) as LayerTableRecord)
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
