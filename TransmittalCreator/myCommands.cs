// (C) Copyright 2020 by HP Inc. 
//
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;
using DV2177.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.LayerManager;
using TransmittalCreator.Models;
using TransmittalCreator.Services;
using TransmittalCreator.ViewModel;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

using Db = Autodesk.AutoCAD.DatabaseServices;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(TransmittalCreator.MyCommands))]

namespace TransmittalCreator
{
    public class MyCommands : Utils, IExtensionApplication
    {
        // Modal Command with localized name
        [CommandMethod("MyGroup", "MyCommand", "MyCommandLocal", CommandFlags.Modal)]
        public void MyCommand() // This method can have any name
        {
            // Put your command code here
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed;
            if (doc != null)
            {
                ed = doc.Editor;
                ed.WriteMessage("Hello, this is your first command.helooooooo");

            }
        }

        // Modal Command with pickfirst selection
        [CommandMethod("MyGroup", "MyPickFirst", "MyPickFirstLocal", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void MyPickFirst() // This method can have any name
        {
            //PromptSelectionResult result = Application.DocumentManager.MdiActiveDocument.Editor.GetSelection();
            StandartCopier stdCopier = new StandartCopier();
            bool isCopied = stdCopier.CopyParamsFiles();
            if (isCopied) Active.Editor.WriteMessage("Файлы {0}, {1} скопированы", stdCopier.Pc3Location, stdCopier.PmpLocation);
        }

        [CommandMethod("LISTATT")]
        public void ListAttributes1()
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

        [CommandMethod("PlotCurrentLayout")]
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
                    string canonName = printModel.GetCanonNameByExtents();

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

        [CommandMethod("NewDBTest")]
        public void NewDBTest()
        {
            Db.Database db2 = Utils.CreateDatabaseFromTemplate(
                @"C:\Users\yusufzhon.marasulov\AppData\Local\Autodesk\AutoCAD 2019\R23.0\enu\Template\UzleAll.dwt", null);

            db2.SaveAs(@"d:\23.dwg", Db.DwgVersion.Current);
        }


        [CommandMethod("sel24")]
        public void SelectCrossingWindow_24()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Point3d p1 = new Point3d(5112.2723, 1697.3971, 0);
            Point3d p2 = new Point3d(6388.6557, 2291.3971, 0);
            PromptSelectionResult psr = doc.Editor.SelectCrossingWindow(p1, p2);
            if (psr.Status == PromptStatus.OK)
            {
                int cnt = 0;
                using (doc.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId oID in psr.Value.GetObjectIds())
                    {
                        Entity ent = (Entity) oID.GetObject(Autodesk.AutoCAD.DatabaseServices.OpenMode.ForRead);
                        cnt += 1;
                        doc.Editor.WriteMessage(ent.ToString(), "SelectCrossingWindow Entity  " + cnt.ToString());
                    }
                }
            }
        }

        [CommandMethod("test")]
            public void Test()
            {
                try
                {
                    Document Doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                    Database currentDb = Doc.Database;
                    Editor ed = Doc.Editor;
                    Database sourceDb = new Database(false, true);

                    var acObjIdCollsource = new ObjectIdCollection();
                    var opf = new Autodesk.AutoCAD.Windows.OpenFileDialog("title", "", "dwg", "name",
                        Autodesk.AutoCAD.Windows.OpenFileDialog.OpenFileDialogFlags.AllowAnyExtension);
                    var layerFilter = LayerFilter.DialogResult.OK;

                    sourceDb.ReadDwgFile(opf.Filename, FileShare.Read, true, "");


                    using (var tr = sourceDb.TransactionManager.StartTransaction())
                    {
                        var acBlkTblCurrentDoc = tr.GetObject(sourceDb.BlockTableId, OpenMode.ForRead, false, false) as BlockTable;
                        var acBlkTblRecCurrentDoc = tr.GetObject(acBlkTblCurrentDoc[BlockTableRecord.ModelSpace], OpenMode.ForRead, false, true) as BlockTableRecord;
                        foreach (ObjectId ObjId in acBlkTblRecCurrentDoc)
                        {
                            acObjIdCollsource.Add(ObjId);
                        }
                        tr.Commit();
                    }
                    using (var tr = currentDb.TransactionManager.StartTransaction())
                    {
                        var bt = tr.GetObject(currentDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                        var btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                        IdMapping acIdMap = new IdMapping();
                        sourceDb.WblockCloneObjects(
                            acObjIdCollsource,
                            btr.ObjectId,
                            acIdMap, DuplicateRecordCloning.Replace, false);
                        tr.Commit();
                    }
                }
                catch (Autodesk.AutoCAD.Runtime.Exception exception)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog(exception.Message + exception.StackTrace);
                }
            }



            [CommandMethod("CopyObjectsBetweenDatabases", CommandFlags.Session)]
            public static void CopyObjectsBetweenDatabases()
            {
                ObjectIdCollection acObjIdColl = new ObjectIdCollection();
                // Get the current document and database
                Document acDoc = Application.DocumentManager.MdiActiveDocument;
                Database acCurDb = acDoc.Database;
                // Lock the current document
                using (DocumentLock acLckDocCur = acDoc.LockDocument())
                {
                    // Start a transaction
                    using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
                    {
                        // Open the Block table record for read
                        BlockTable acBlkTbl;
                        acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
                                                     OpenMode.ForRead) as BlockTable;
                        // Open the Block table record Model space for write
                        BlockTableRecord acBlkTblRec;
                        acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                        OpenMode.ForWrite) as BlockTableRecord;
                        // Create a circle that is at (0,0,0) with a radius of 5
                        Circle acCirc1 = new Circle();
                        acCirc1.SetDatabaseDefaults();
                        acCirc1.Center = new Point3d(0, 0, 0);
                        acCirc1.Radius = 5;
                        // Add the new object to the block table record and the transaction
                        acBlkTblRec.AppendEntity(acCirc1);
                        acTrans.AddNewlyCreatedDBObject(acCirc1, true);
                        // Create a circle that is at (0,0,0) with a radius of 7
                        Circle acCirc2 = new Circle();
                        acCirc2.SetDatabaseDefaults();
                        acCirc2.Center = new Point3d(0, 0, 0);
                        acCirc2.Radius = 7;
                        // Add the new object to the block table record and the transaction
                        acBlkTblRec.AppendEntity(acCirc2);
                        acTrans.AddNewlyCreatedDBObject(acCirc2, true);
                        // Add all the objects to copy to the new document
                        acObjIdColl = new ObjectIdCollection();
                        acObjIdColl.Add(acCirc1.ObjectId);
                        acObjIdColl.Add(acCirc2.ObjectId);
                        // Save the new objects to the database
                        acTrans.Commit();
                    }
                    // Unlock the document
                }
                // Change the file and path to match a drawing template on your workstation
                string sLocalRoot = Application.GetSystemVariable("LOCALROOTPREFIX") as string;
                string sTemplatePath = sLocalRoot + "Template\\acadiso.dwt";
                // Create a new drawing to copy the objects to
                DocumentCollection acDocMgr = Application.DocumentManager;
                Document acNewDoc = acDocMgr.Add(sTemplatePath);
                Database acDbNewDoc = acNewDoc.Database;
                // Lock the new document
                using (DocumentLock acLckDoc = acNewDoc.LockDocument())
                {
                    // Start a transaction in the new database
                    using (Transaction acTrans = acDbNewDoc.TransactionManager.StartTransaction())
                    {
                        // Open the Block table for read
                        BlockTable acBlkTblNewDoc;
                        acBlkTblNewDoc = acTrans.GetObject(acDbNewDoc.BlockTableId,
                                                           OpenMode.ForRead) as BlockTable;
                        // Open the Block table record Model space for read
                        BlockTableRecord acBlkTblRecNewDoc;
                        acBlkTblRecNewDoc = acTrans.GetObject(acBlkTblNewDoc[BlockTableRecord.ModelSpace],
                                                            OpenMode.ForRead) as BlockTableRecord;
                        // Clone the objects to the new database
                        IdMapping acIdMap = new IdMapping();
                        acCurDb.WblockCloneObjects(acObjIdColl, acBlkTblRecNewDoc.ObjectId, acIdMap,
                                                   DuplicateRecordCloning.Ignore, false);
                        // Save the copied objects to the database
                        acTrans.Commit();
                    }
                    // Unlock the document
                }
                // Set the new document current
                acDocMgr.MdiActiveDocument = acNewDoc;
            }


            [CommandMethod("CreateTranspdf")]
            public void CreateTransmittalAndPDF()
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
                        throw new ArgumentException("Надо выбрать блок");
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

                        Active.Document.Editor.WriteMessage(blockName);
                    }

                    MyCommands.GetSheetsFromBlocks(Active.Editor, dict, tr, idArray);
                    MyCommands.GetExtentsNamePdf(Active.Editor, printModels, tr, idArray);
                    //Active.Editor.WriteMessage("печать {0} - {1}", printModels[0].DocNumber, printModels.Count);

                    Utils utils = new Utils();
                    utils.CreateOnlyVed(dict);
                    utils.CreateJsonFile(dict);

                    foreach (var printModel in printModels)
                    {
                        Active.Editor.WriteMessage("{0} печатаем ", printModel.DocNumber);
                        PlotCurrentLayout(printModel.DocNumber, printModel);
                    }

                    //utils.CreateOnlytrans(dict);

                    tr.Commit();
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
            //                //PlotCurrentLayout(printModel.DocNumber, printModel., printModel.FormatValue);
            //            }
            //        //}
            //        //else
            //        //{
            //        //    //Utils utils = new Utils();
            //        //    //utils.CreateOnlyVed(dict);
            //        //    foreach (var printModel in printModels)
            //        //    {
            //        //        //PlotCurrentLayout(printModel.DocNumber, printModel.BlockExtents3d, printModel.FormatValue);
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
