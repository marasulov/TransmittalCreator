﻿// (C) Copyright 2020 by HP Inc. 
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
using TransmittalCreator.Models;
using TransmittalCreator.Services;
using TransmittalCreator.ViewModel;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(TransmittalCreator.MyCommands))]

namespace TransmittalCreator
{

    // This class is instantiated by AutoCAD for each document when
    // a command is called by the user the first time in the context
    // of a given document. In other words, non static data in this class
    // is implicitly per-document!
    public class MyCommands : Utils, IExtensionApplication
    {
        // The CommandMethod attribute can be applied to any public  member 
        // function of any public class.
        // The function should take no arguments and return nothing.
        // If the method is an intance member then the enclosing class is 
        // intantiated for each document. If the member is a static member then
        // the enclosing class is NOT intantiated.
        //
        // NOTE: CommandMethod has overloads where you can provide helpid and
        // context menu.

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
            string str1 = "sadsadsa";
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
        public static void PlotCurrentLayout(string pdfFileName, Extents3d extents3d, string formatValue)
        {
            // Get the current document and database, and start a transaction
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            acDoc.SendStringToExecute("REGENALL ", true, false, true);
            //short bgPlot = (short)Application.GetSystemVariable("BACKGROUNDPLOT");
            //Application.SetSystemVariable("BACKGROUNDPLOT", 0);

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

                acPlSetVdr.SetPlotPaperUnits(acPlSet, PlotPaperUnit.Millimeters);
                // Set the plot type
                //Point3d minPoint3dWcs = new Point3d(5112.2723, 1697.3971, 0);
                //Point3d minPoint3d = Autodesk.AutoCAD.Internal.Utils.UcsToDisplay(minPoint3dWcs, false);
                //Point3d maxPoint3dWcs = new Point3d(6388.6557, 2291.3971, 0);
                //Point3d maxPoint3d = Autodesk.AutoCAD.Internal.Utils.UcsToDisplay(maxPoint3dWcs, false);
                //Extents2d points = new Extents2d(new Point2d(minPoint3d[0], minPoint3d[1]), new Point2d(maxPoint3d[0], maxPoint3d[1]));
                //extents3d = new Extents3d(minPoint3dWcs, maxPoint3dWcs);
                PdfCreator pdfCreator = new PdfCreator(extents3d);
                Extents2d points = pdfCreator.Extents3dToExtents2d();
                bool isHor = pdfCreator.IsFormatHorizontal();
                pdfCreator.GetBlockDimensions();
                string canonName = pdfCreator.GetCanonNameByExtents();

                //acDoc.Utility.TranslateCoordinates(point1, acWorld, acDisplayDCS, False);
                acPlSetVdr.SetPlotWindowArea(acPlSet, points);
                acPlSetVdr.SetPlotType(acPlSet, Autodesk.AutoCAD.DatabaseServices.PlotType.Window);
                // Set the plot scale
                acPlSetVdr.SetUseStandardScale(acPlSet, false);
                acPlSetVdr.SetStdScaleType(acPlSet, StdScaleType.ScaleToFit);
                // Center the plot
                acPlSetVdr.SetPlotCentered(acPlSet, true);
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

                            acPlEng.BeginDocument(acPlInfo, acDoc.Name, null, 1, true, "d:\\myplot"+pdfFileName);
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

        [CommandMethod("SetDynamicBlkProperty")]
        static public void SetDynamicBlkProperty()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptEntityOptions prEntOptions = new PromptEntityOptions(
                "Выберите вставку динамического блока...");

            PromptEntityResult prEntResult = ed.GetEntity(prEntOptions);

            if (prEntResult.Status != PromptStatus.OK)
            {
                ed.WriteMessage("Ошибка...");
                return;
            }

            using (Transaction Tx = db.TransactionManager.StartTransaction())
            {
                BlockReference bref = Tx.GetObject(
                        prEntResult.ObjectId,
                        OpenMode.ForWrite)
                    as BlockReference;

                double blockWidth = 0, blockHeidht = 0;
                if (bref.IsDynamicBlock)
                {
                    DynamicBlockReferencePropertyCollection props =
                        bref.DynamicBlockReferencePropertyCollection;

                    foreach (DynamicBlockReferenceProperty prop in props)
                    {
                        object[] values = prop.GetAllowedValues();

                        if (prop.PropertyName == "Ширина")
                        {
                            blockWidth = double.Parse(prop.Value.ToString(), System.Globalization.CultureInfo.InvariantCulture);
                            ed.WriteMessage(blockWidth.ToString());
                        }
                        if (prop.PropertyName == "Высота")
                        {
                            blockHeidht = double.Parse(prop.Value.ToString(), System.Globalization.CultureInfo.InvariantCulture);
                            ed.WriteMessage("\n{0}", blockHeidht.ToString());
                        }
                    }
                }

                Tx.Commit();
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
                ObjectIdCollection objectIds = Utils.SelectDynamicBlockReferences(window.NameBlock.Text);

                List<Sheet> dict = new List<Sheet>();
                List<PrintModel> printModels = new List<PrintModel>();

                BlockModel objectNameEn = window.ComboObjectNameEn.SelectedItem as BlockModel;
                BlockModel objectNameRu = window.ComboObjectNameRu.SelectedItem as BlockModel;

                BlockModel position = window.ComboBoxPosition.SelectedItem as BlockModel;
                BlockModel nomination = window.ComboBoxNomination.SelectedItem as BlockModel;
                BlockModel comment = window.ComboBoxComment.SelectedItem as BlockModel;
                BlockModel trItem = window.ComboBoxTrItem.SelectedItem as BlockModel;
                BlockModel trDocNumber = window.ComboBoxTrDocNumber.SelectedItem as BlockModel;
                BlockModel trDocTitleEn = window.ComboBoxTrDocTitleEn.SelectedItem as BlockModel;
                BlockModel trDocTitleRu = window.ComboBoxTrDocTitleRu.SelectedItem as BlockModel;

                AttributModel attributModel = new AttributModel(objectNameEn, objectNameRu, position, nomination,
                    comment, trItem, trDocNumber, trDocTitleEn, trDocTitleRu);

                using (Transaction tr = Active.Database.TransactionManager.StartTransaction())
                {
                    MyCommands.GetSheetsFromBlocks(Active.Editor, dict, tr, objectIds, attributModel);
                    MyCommands.GetExtentsNamePdf(Active.Editor, printModels, tr, objectIds);
                    
                    if (window.transmittalCheckBox.IsChecked == true)
                    {
                        Utils utils = new Utils();
                        //utils.CreateOnlyVed(dict);
                        //utils.CreateOnlytrans(dict);
                        foreach (var printModel in printModels)
                        {
                            PlotCurrentLayout(printModel.DocNumber, printModel.BlockExtents3d, printModel.FormatValue);
                        }
                    }
                    else
                    {
                        //Utils utils = new Utils();
                        //utils.CreateOnlyVed(dict);
                        foreach (var printModel in printModels)
                        {
                            PlotCurrentLayout(printModel.DocNumber, printModel.BlockExtents3d, printModel.FormatValue);
                        }
                    }

                    tr.Commit();
                }
            }
        }

        public void Initialize()
        {
            throw new NotImplementedException();
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
