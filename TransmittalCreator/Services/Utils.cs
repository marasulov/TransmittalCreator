using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;
using DV2177.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TransmittalCreator.Models;
using Db = Autodesk.AutoCAD.DatabaseServices;
using Exception = Autodesk.AutoCAD.Runtime.Exception;

namespace TransmittalCreator.Services
{
    public class Utils
    {
        /// <summary>
        /// plotting method
        /// </summary>
        /// <param name="pdfFileName"> name</param>
        /// <param name="printModel">print param model</param>
        public static void PlotCurrentLayout(string pdfFileName, PrintModel printModel)
        {
            // Get the current document and database, and start a transaction
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            //short bgPlot = (short)Application.GetSystemVariable("BACKGROUNDPLOT");
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
                    acLayout =
                        acTrans.GetObject(acLayoutMgr.GetLayoutId(acLayoutMgr.CurrentLayout),
                            OpenMode.ForRead) as Layout;

                    // Get the PlotInfo from the layout
                    PlotInfo acPlInfo = new PlotInfo();
                    acPlInfo.Layout = acLayout.ObjectId;

                    // Get a copy of the PlotSettings from the layout
                    PlotSettings acPlSet = new PlotSettings(acLayout.ModelType);
                    
                    acPlSet.CopyFrom(acLayout);
                    // Update the PlotSettings object
                    PlotSettingsValidator acPlSetVdr = PlotSettingsValidator.Current;
                    var sheetList = acPlSetVdr.GetPlotStyleSheetList();
                    acPlSetVdr.SetCurrentStyleSheet(acPlSet, "monochrome.ctb");
                    string ss = acPlSet.CurrentStyleSheet;
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
                    acPlSetVdr.SetPlotType(acPlSet, Db.PlotType.Window);
                    if (!isHor)
                        acPlSetVdr.SetPlotRotation(acPlSet, PlotRotation.Degrees090);
                    //else if(canonName =="ISO_full_bleed_A4_(297.00_x_210.00_MM)")
                    //    acPlSetVdr.SetPlotRotation(acPlSet, PlotRotation.Degrees090);
                    else
                    {
                        acPlSetVdr.SetPlotRotation(acPlSet, PlotRotation.Degrees000);
                    }

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
                                acPlProgDlg.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption,
                                    "Sheet Set Progress");
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
                                acPlProgDlg.set_PlotMsgString(PlotMessageIndex.Status,
                                    "Plotting: " + acDoc.Name + " - " + acLayout.LayoutName);
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
            catch (Exception e)
            {
                Application.ShowAlertDialog(e.Message);
            }
        }


        /// <summary>
        /// get dict from attributes of block
        /// </summary>
        /// <param name="tr"></param>
        /// <param name="attrList"></param>
        /// <returns></returns>
        public static (string, Dictionary<string, string>) GetAttrList(Transaction tr, Dictionary<string, string> attrList)
        {
            string blockName = "";
            try
            {
                // Build a filter list so that only
                // block references are selected
                TypedValue[] filList = new TypedValue[1] { new TypedValue((int)DxfCode.Start, "INSERT") };
                SelectionFilter filter = new SelectionFilter(filList);
                PromptSelectionOptions opts = new PromptSelectionOptions();
                opts.SingleOnly = true;
                opts.SinglePickInSpace = true;
                opts.MessageForAdding = "Select block references: ";
                PromptSelectionResult res = Active.Editor.GetSelection(opts, filter);
                
                // Do nothing if selection is unsuccessful
                if (res.Status != PromptStatus.OK)
                {
                    throw new InvalidOperationException("block not selected");
                }
                if(res.Value.Count > 1)
                {
                    throw new InvalidOperationException("For selecting block attributes, please select only one block");
                }
                SelectionSet selSet = res.Value;
                ObjectId[] idArray = selSet.GetObjectIds();
                foreach (ObjectId blkId in idArray)
                {
                    BlockReference blkRef = (BlockReference)tr.GetObject(blkId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(blkRef.DynamicBlockTableRecord, OpenMode.ForRead);
                    blockName = btr.Name;
                    Active.Editor.WriteMessage("\nBlock: " + blockName);
                    btr.Dispose();
                    AttributeCollection attCol = blkRef.AttributeCollection;
                    foreach (ObjectId attId in attCol)
                    {
                        AttributeReference attRef = (AttributeReference)tr.GetObject(attId, OpenMode.ForRead);
                        string str = ("\n  Attribute Tag: " + attRef.Tag + "\n    Attribute String: " + attRef.TextString);
                        attrList.Add(attRef.Tag, attRef.TextString);
                        Active.Editor.WriteMessage(str);
                    }
                }
                tr.Commit();
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(("Exception: " + ex.Message));
            }
            finally
            {
                tr.Dispose();
            }

            return (blockName, attrList);
        }

        public static List<Sheet> GetSheetsFromBlocks(Editor ed, List<Sheet> dict, Transaction tr, ObjectIdCollection idArray)
        {

            // Build a filter list so that only
            // block references are selected

            string sheetNumber = "", docNumber = "", objectNameEng = "", docTitleEng = "", objectNameRu = "", docTitleRu = "";

            foreach (ObjectId blkId in idArray)
            {

                BlockReference blkRef = (BlockReference)tr.GetObject(blkId, OpenMode.ForRead);
                string stampViewValue = GetBlockAttributeVlueByAttrName(blkRef);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForRead);
                ed.WriteMessage("\nBlock: " + btr.Name);
                btr.Dispose();
                AttributeCollection attCol = blkRef.AttributeCollection;
                
                foreach (ObjectId attId in attCol)
                {
                    AttributeReference attRef = (AttributeReference)tr.GetObject(attId, OpenMode.ForRead);
                    bool vis = attRef.Visible;
                    //ed.WriteMessage("\n{0} значение {1} видимость {2}", attRef.Tag, attRef.TextString, vis.ToString());
                    switch (attRef.Tag)
                    {
                        case "НОМЕР_ЛИСТА":
                            docNumber = attRef.TextString;
                            break;
                        case "НАЗВАНИЕEN":
                            objectNameEng = attRef.TextString;
                            break;
                        case "ЛИСТEN":
                            docTitleEng = attRef.TextString;
                            break;
                        case "НАЗВАНИЕRU":
                            objectNameRu = attRef.TextString;
                            break;
                        case "НАЗВАНИЕ_ЛИСТАRU":
                            docTitleRu = attRef.TextString;
                            break;
                        case "ЛИСТ":
                            sheetNumber = attRef.TextString;
                            break;
                    }
                }

                dict.Add(new Sheet(sheetNumber, docNumber, objectNameEng, docTitleEng, objectNameRu, docTitleRu, stampViewValue));
            }

            return dict;
        }

        /// <summary>
        /// создает колекцию листов из блоков штампа
        /// </summary>
        /// <param name="ed">editor</param>
        /// <param name="dict"> коллекция листов</param>
        /// <param name="tr"> транзакция </param>
        /// <returns>коллекция листов </returns>
        public static List<Sheet> GetSheetsFromBlocks(Editor ed, List<Sheet> dict, Transaction tr, ObjectIdCollection objectIdCollection, AttributModel attributModel)
        {
            string objectNameEn = attributModel.ObjectNameEn;
            string objectNameRu = attributModel.ObjectNameRu;
            string listNumber = attributModel.Position;
            string nomination = attributModel.Nomination;
            string commentAttr = attributModel.Comment;
            string trItem = attributModel.TrItem;
            string trDocNumber = attributModel.TrDocNumber;
            string trDocTitleEn = attributModel.TrDocTitleEn;
            string trDocTitleRu = attributModel.TrDocTitleRu;

            string sheetNumber = "", docNumber = "", comment = "", objectNameEng = "", docTitleEng = "", objectNameRus = "", docTitleRu = "";

            foreach (ObjectId blkId in objectIdCollection)
            {
                BlockReference blkRef = (BlockReference)tr.GetObject(blkId, OpenMode.ForRead);

                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForRead);

                Extents3d extents3d = blkRef.GeometricExtents;
                ed.WriteMessage("\nBlock:{0} габариты {1} ", btr.Name, extents3d.MinPoint.ToString(), extents3d.MaxPoint.ToString());
                btr.Dispose();

                AttributeCollection attCol = blkRef.AttributeCollection;


                var attrDict = AttributeExtensions.GetAttributesValues(blkRef);

                docTitleEng = attrDict.FirstOrDefault(x => x.Key == objectNameEn).Value;
                docTitleRu = attrDict.FirstOrDefault(x => x.Key == objectNameRu).Value;


                sheetNumber = attrDict.FirstOrDefault(x => x.Key == listNumber).Value;
                docNumber = attrDict.FirstOrDefault(x => x.Key == nomination).Value;

                comment = attrDict.FirstOrDefault(x => x.Key == commentAttr).Value;

                //transItem = attrDict.FirstOrDefault(x => x.Key == trItem).Value;
                objectNameEng = attrDict.FirstOrDefault(x => x.Key == trDocTitleEn).Value;
                objectNameRus = attrDict.FirstOrDefault(x => x.Key == trDocTitleRu).Value;
             }

            return dict;
        }


        public static Database CreateDatabaseFromTemplate(String templateName, String password)
        {
            if (templateName == null || templateName.Trim() == String.Empty) return null;
            Database templateDb = new Database(false, true);
            if (password == null) password = String.Empty;
            templateDb.ReadDwgFile(Environment.ExpandEnvironmentVariables(templateName),
                FileOpenMode.OpenForReadAndWriteNoShare, true, password);
            templateDb.CloseInput(true);
            Database result = templateDb.Wblock();
            return result;
        }


        /// <summary>
        /// find print area and pdf name by block id
        /// </summary>
        public static List<PrintModel> GetPrintParametersToPdf(Editor ed, List<PrintModel> printModels, Transaction tr,
            ObjectIdCollection objectIdCollection, string selAttrName)
        {
            foreach (ObjectId blkId in objectIdCollection)
            {
                BlockReference blkRef = (BlockReference)tr.GetObject(blkId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForRead);

                string docNumber = GetBlockAttributeValue(blkRef, selAttrName);
                //formatValue = attrDict.FirstOrDefault(x => x.Key == "ФОРМАТ").Value;
                //ed.WriteMessage("\nBlock:{0} - {1} габариты {2} -{3}", btr.Name, docNumber, posPoint2d.ToString(), formatValue);
                printModels.Add(new PrintModel(docNumber, blkId));
                btr.Dispose();
            }

            return printModels;
        }


        public static string GetBlockAttributeValue(BlockReference blkRef, string selAttrName)
        {
            string blockStamp = GetBlockAttributeVlueByAttrName(blkRef);
            var attrDict = AttributeExtensions.GetAttributesValues(blkRef);
            string docNumber = "";

            if (blockStamp == "Форма 3 ГОСТ Р 21.1101-2009 M25" || blockStamp == "Форма 3 ГОСТ Р 21.1101-2009")
                docNumber = attrDict.FirstOrDefault(x => x.Key == selAttrName).Value;
            else if (blockStamp == "Форма 6 ГОСТ Р 21.1101-2009")
            {
                selAttrName = "НОМЕР_ЛИСТА_2";
                docNumber = attrDict.FirstOrDefault(x => x.Key == selAttrName).Value;
                docNumber += "-page" + attrDict.FirstOrDefault(x => x.Key == "ЛИСТ2_СПЕЦ").Value;
            }
            return docNumber;
        }

        private static string GetBlockAttributeVlueByAttrName(BlockReference blkRef)
        {
            DynamicBlockReferencePropertyCollection props = blkRef.DynamicBlockReferencePropertyCollection;
            string blockStamp = "";
            foreach (DynamicBlockReferenceProperty prop in props)
            {
                if (prop.PropertyName == "Штамп")
                {
                    blockStamp = prop.Value.ToString();
                }
            }
            
            return blockStamp;
        }

        public void CreateJsonFile(List<Sheet> dict)
        {
            dict = dict.OrderBy(x => x.SheetNumber).ToList();
            List<Sheet> newSheets = new List<Sheet>();
            foreach (var sheet in dict)
            {
                if(sheet.ViewValue != "Форма 6 ГОСТ Р 21.1101-2009") newSheets.Add(sheet);
            }

            string json = JsonConvert.SerializeObject(newSheets, Formatting.Indented);
            string path = DrawingPath();
            string dirName = Path.GetDirectoryName(path);
            string pathExtension = Path.GetFileNameWithoutExtension(path) + ".json";
            string jsonFile = Path.Combine(dirName, pathExtension);
            File.WriteAllText(jsonFile, json);
        }

        private static string DrawingPath()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            HostApplicationServices hs = HostApplicationServices.Current;
            string path = hs.FindFile(doc.Name, doc.Database, FindFileHint.Default);
            return path;
        }
        public static Dictionary<string, string> GetAttribs(string blockName, string tag)
        {
            // create a new instance of Dictionary<string, string>
            var attribs = new Dictionary<string, string>();

            // get the documents collection
            var docs = Application.DocumentManager;

            // use an OpenCloseTransaction which is not related to a document or database
            using (var tr = new OpenCloseTransaction())
            {
                // iterate through the documents
                foreach (Document doc in docs)
                {
                    // get the document database
                    var db = doc.Database;

                    // get the database block table
                    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                    // if the block table contains a block definitions named 'blockName'...
                    if (bt.Has(blockName))
                    {
                        // open the block definition
                        var btr = (BlockTableRecord)tr.GetObject(bt[blockName], OpenMode.ForRead);

                        // get the inserted block references ObjectIds
                        var ids = btr.GetBlockReferenceIds(true, true);

                        // if any...
                        if (0 < ids.Count)
                        {
                            // open the first block reference
                            var br = (BlockReference)tr.GetObject(ids[0], OpenMode.ForRead);

                            // iterate through the attribute collection
                            foreach (ObjectId id in br.AttributeCollection)
                            {
                                // open the attribute reference
                                var attRef = (AttributeReference)tr.GetObject(id, OpenMode.ForRead);

                                // if the attribute tag is equal to 'tag'
                                if (attRef.Tag.Equals(tag, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    // add an entry to the dictionary
                                    attribs[doc.Name] = attRef.TextString;
                                    // break the loop
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            // return the dictionary
            return attribs;
        }


        public void CreateOnlyVed(List<Sheet> dict)
        {
            Editor ed = Active.Editor;
            Transaction tr = Active.Database.TransactionManager.StartTransaction();
            // Start the transaction
            try
            {
                //GetSheetsFromBlocks(ed, dict, tr);
                CreateTableFromList(dict);
                tr.Commit();
            }
            catch (Exception ex)
            {
                ed.WriteMessage(("Exception: " + ex.Message + " " + ex.InnerException));
            }
            finally
            {
                tr.Dispose();
            }
        }
        public void CreateOnlytrans(List<Sheet> dict)
        {
            Editor ed = Active.Editor;
            //Transaction tr = Active.Database.TransactionManager.StartTransaction();
            // Start the transaction
            try
            {
                //GetSheetsFromBlocks(ed, dict, tr);
                //tr.Commit();
                Sheet.WriteToExcel(dict);
            }
            catch (Exception ex)
            {
                ed.WriteMessage(("Exception: " + ex.Message + " " + ex.InnerException));
            }
        }

        public void CreateTableFromList(List<Sheet> dict)
        {
            dict = dict.OrderBy(x => x.SheetNumber).ToList();
            Database db = Active.Database;
            Editor ed = Active.Editor;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                PromptPointResult pr = ed.GetPoint("\nEnter table insertion point: ");
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                ObjectId msId = bt[BlockTableRecord.ModelSpace];
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(msId, OpenMode.ForWrite);
                Table tb = new Table();
                tb.TableStyle = db.Tablestyle;
                //tb.TableStyle = db.Tablestyle;
                btr.AppendEntity(tb);
                // Число строк
                int RowsNum = dict.Count;
                // Число столбцов
                int ColumnsNum = 3;
                // Высота строки
                double rowheight = 8;
                // Ширина столбца
                double columnwidth = 15;
                // Добавляем строки и колонки
                tb.InsertRows(0, rowheight, RowsNum + 1);
                tb.InsertColumns(0, columnwidth, ColumnsNum - 1);
                tb.SetRowHeight(rowheight);
                tb.SetColumnWidth(columnwidth);
                tb.Position = pr.Value;
                // Объединяем ячейки
                CellRange range = CellRange.Create(tb, 0, 0, 0, 2);
                tb.MergeCells(range);
                range.Borders.Top.IsVisible = false;
                range.Borders.Bottom.IsVisible = true;
                range.Borders.Left.IsVisible = false;
                range.Borders.Right.IsVisible = false;
                //var row = tb.Rows[RowsNum];
                range = CellRange.Create(tb, 1, 1, 1, 2);
                tb.UnmergeCells(range);
                tb.Columns[0].Width = 15;
                tb.Cells[0, 0].TextHeight = 5;
                tb.Cells[0, 0].Alignment = CellAlignment.MiddleCenter;
                tb.Cells[0, 0].TextString = "Ведомость рабочих чертежей основного комплекта";
                var row = tb.Rows[RowsNum + 1];
                tb.UnmergeCells(row);
                tb.Cells[1, 0].TextString = "Лист";
                tb.Columns[1].Width = 140;
                tb.Cells[1, 1].TextString = "Наименование";
                tb.Columns[2].Width = 30;
                tb.Cells[1, 2].TextString = "Примечание";
                //заполняем по одной все ячейки
                int curRow = 2;
                foreach (var item in dict)
                {
                    if (!string.IsNullOrWhiteSpace(item.DocNumber))
                    {
                        tb.Cells[curRow, 0].TextHeight = 3.5;
                        tb.Cells[curRow, 0].TextString = item.SheetNumber.ToString();
                        tb.Cells[curRow, 1].TextHeight = 3.5;
                        tb.Cells[curRow, 1].TextString = item.DocNumber + "_" + item.DocTitleRu;
                        tb.Cells[curRow, 1].Alignment = CellAlignment.MiddleLeft;
                        if (!string.IsNullOrEmpty(item.Comment))
                        {
                            tb.Cells[curRow, 2].TextHeight = 3.5;
                            tb.Cells[curRow, 2].TextString = item.Comment;
                            tb.Cells[curRow, 2].Alignment = CellAlignment.MiddleLeft;
                        }
                    }
                    //tb.Cells[curRow, 6].TextString = item.DocTitleEng;
                    //tb.Cells[curRow, 4].TextString = item.DocTitleRu;
                    curRow++;
                }
                range = CellRange.Create(tb, RowsNum, 0, RowsNum, RowsNum);
                tb.UnmergeCells(range);
                tb.GenerateLayout();
                tr.AddNewlyCreatedDBObject(tb, true);
                tr.Commit();
            }
        }


        [CommandMethod("SetDynamicBlkProperty")]
        static public void SetDynamicBlkProperty()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptEntityOptions prEntOptions = new PromptEntityOptions("Выберите вставку динамического блока...");

            PromptEntityResult prEntResult = ed.GetEntity(prEntOptions);

            if (prEntResult.Status != PromptStatus.OK)
            {
                ed.WriteMessage("Ошибка...");
                return;
            }

            using (Transaction Tx = db.TransactionManager.StartTransaction())
            {
                BlockReference bref = Tx.GetObject(prEntResult.ObjectId, OpenMode.ForWrite) as BlockReference;

                double blockWidth = 0;
                if (bref.IsDynamicBlock)
                {
                    DynamicBlockReferencePropertyCollection props = bref.DynamicBlockReferencePropertyCollection;

                    foreach (DynamicBlockReferenceProperty prop in props)
                    {
                        object[] values = prop.GetAllowedValues();

                        if (prop.PropertyName == "Штамп")
                        {
                            BlockTableRecord btr = (BlockTableRecord)Tx.GetObject(bref.BlockTableRecord, OpenMode.ForRead);
                            btr.Dispose();

                            AttributeCollection attCol = bref.AttributeCollection;
                            var attrDict = AttributeExtensions.GetAttributesValues(bref);
                            if (prop.Value.ToString() == "Форма 3 ГОСТ Р 21.1101 - 2009")
                                blockWidth = double.Parse(prop.Value.ToString(), CultureInfo.InvariantCulture);
                            ed.WriteMessage(blockWidth.ToString());
                        }
                        //if (prop.PropertyName == "Высота")
                        //{
                        //    blockHeidht = double.Parse(prop.Value.ToString(), System.Globalization.CultureInfo.InvariantCulture);
                        //    ed.WriteMessage("\n{0}", blockHeidht.ToString());
                        //}
                    }
                }

                Tx.Commit();
            }
        }

        /// <summary>
        /// select from model dynblocks byName
        /// </summary>
        /// <param name="blockName"></param>
        /// <returns></returns>
        [CommandMethod("selb")]
        public static ObjectIdCollection SelectDynamicBlockReferences(string blockName = "ФорматM25")
        {
            //TODO start here to search blocks in drawing
            Editor ed = Active.Editor;
            Database db = Active.Database;
            List<ObjectId> listObjectIds = new List<ObjectId>();
            ObjectIdCollection dynBlockRefs = new ObjectIdCollection();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                // получаем таблицу блоков и проходим по всем записям таблицы блоков
                BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                foreach (ObjectId btrId in bt)
                {
                    // получаем запись таблицы блоков и смотри анонимная ли она
                    BlockTableRecord btr = (BlockTableRecord)trans.GetObject(btrId, OpenMode.ForRead);
                    if (btr.IsDynamicBlock && btr.Name == blockName)
                    {
                        // получаем все анонимные блоки динамического блока
                        ObjectIdCollection anonymousIds = btr.GetAnonymousBlockIds();
                        // получаем все прямые вставки динамического блока
                        dynBlockRefs = btr.GetBlockReferenceIds(true, true);
                        ObjectId mSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
                        foreach (ObjectId anonymousBtrId in anonymousIds)
                        {
                            // open the model space BlockTableRecord
                            //var modelSpace = (BlockTableRecord)tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                            // получаем анонимный блок
                            BlockTableRecord anonymousBtr =
                                (BlockTableRecord)trans.GetObject(anonymousBtrId, OpenMode.ForRead);

                            // получаем все вставки этого блока
                            ObjectIdCollection blockRefIds = anonymousBtr.GetBlockReferenceIds(true, true);

                            SymbolTableRecord symTableRecord =
                                (SymbolTableRecord)trans.GetObject(anonymousBtrId, OpenMode.ForRead);

                            foreach (ObjectId id in blockRefIds)
                            {
                                //var blockReference = (BlockReference)tr.GetObject(id, OpenMode.ForRead);
                                var e = (BlockReference)trans.GetObject(id, OpenMode.ForRead);
                                string curBlockName = e.BlockName;

                                ObjectId ownerId = e.OwnerId;

                                if (ownerId == mSpaceId)
                                {
                                    ed.WriteMessage(e.ToString());
                                    listObjectIds.Add(id);
                                    dynBlockRefs.Add(id);
                                    ed.WriteMessage("\n \"{0}\" соответствуют {1} \n",
                                        btr.Name, id);
                                }
                            }
                        }

                        // Что-нибудь делаем с созданным нами набором
                        //ed.WriteMessage("\nДинамическому блоку \"{0}\" соответствуют {1} анонимных блоков и {2} вставок блока\n",
                        //    btr.Name, anonymousIds.Count, dynBlockRefs.Count);

                    }
                }
            }
            return dynBlockRefs;
        }

        /// <summary>
        /// get blocks by name
        /// </summary>
        /// <param name="blockName"></param>
        /// <returns></returns>
        public static IEnumerable<ObjectId> GetAllCurrentSpaceBlocksByName(string blockName)
        {
            var db = HostApplicationServices.WorkingDatabase;
            using (var tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                var curSpace = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead);
                return curSpace
                    .Cast<ObjectId>()
                    .Where(id => id.ObjectClass.DxfName == "INSERT")
                    .Select(id => (BlockReference)tr.GetObject(id, OpenMode.ForRead))
                    .Where(br => ((BlockTableRecord)tr.GetObject(br.DynamicBlockTableRecord, OpenMode.ForRead)).Name == blockName)
                    .Select(br => br.ObjectId);
            }
        }

        [CommandMethod("blockName")]
        static public void GetBlockName()
        {
            Document doc = Active.Document;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            PromptEntityOptions options =
                new PromptEntityOptions("\nSelect block reference");
            options.SetRejectMessage("\nSelect only block reference");
            options.AddAllowedClass(typeof(BlockReference), false);
            PromptEntityResult acSSPrompt = ed.GetEntity(options);
            using (Transaction tx =
                db.TransactionManager.StartTransaction())
            {
                BlockReference blockRef = tx.GetObject(acSSPrompt.ObjectId,
                    OpenMode.ForRead) as BlockReference;
                BlockTableRecord block = null;
                if (blockRef.IsDynamicBlock)
                {
                    //get the real dynamic block name.
                    block = tx.GetObject(blockRef.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                }
                else
                {
                    block = tx.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                }
                if (block != null)
                {
                    ed.WriteMessage("Block name is : "
                                    + block.Name + "\n");
                }
                tx.Commit();
            }
        }

        /// <summary>
        /// adding hvac table
        /// </summary>
        /// <param name="hvacTable"></param>
        /// <param name="columnsNum"></param>
        public void AddTable(HvacTable hvacTable, int columnsNum)
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                ObjectId msId = bt[BlockTableRecord.ModelSpace];
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(msId, OpenMode.ForWrite);

                PromptPointResult pr =
                    Active.Editor.GetPoint(
                        $"\nEnter table insertion point for:room #{hvacTable.RoomNumber}-{hvacTable.RoomName}");
                if (pr.Status == PromptStatus.OK)
                {
                    // create a table
                    Table tb = new Table();
                    //tb.TableStyle = db.Tablestyle;
                    tb.SetDatabaseDefaults();
                    // row height
                    double rowheight = 80;
                    // column width
                    double columnwidth = 150;
                    // insert rows and columns
                    tb.InsertRows(0, rowheight, 4);
                    tb.InsertColumns(0, columnwidth, 2);

                    tb.SetRowHeight(rowheight);
                    tb.SetColumnWidth(columnwidth);

                    tb.Position = pr.Value;
                    // fill in the cell one by one
                    //tb.Columns[0].Width = 150;
                    tb.Columns[1].Width = 340;
                    //tb.Columns[2].Width = 150;

                    tb.Rows[0].Height = 130;

                    SetCellPropsWithValue(tb, 0, 0, 50, 255, hvacTable.RoomNumber);
                    SetCellPropsWithValue(tb, 0, 1, 50, 255, hvacTable.RoomName);
                    SetCellPropsWithValue(tb, 0, 2, 50, 255, hvacTable.RoomTemp);


                    SetCellPropsWithValue(tb, 1, 0, 50, 30, "Qt");
                    SetCellPropsWithValue(tb, 1, 1, 50, 30,
                        GetRoundUpValue(hvacTable.Heating) + "-" + GetRoundUpValue(hvacTable.Heating, 120));
                    SetCellPropsWithValue(tb, 1, 2, 25, 30, "\\LВТ\\l\nсек.");

                    SetCellPropsWithValue(tb, 2, 0, 50, 130, "Qx");
                    SetCellPropsWithValue(tb, 2, 1, 50, 130,
                        GetRoundUpValue(hvacTable.Cooling) + "-" + GetRoundUpValue(hvacTable.Cooling, 293.07));
                    SetCellPropsWithValue(tb, 2, 2, 25, 130, "\\LВТ\\l\nBtu...");


                    SetCellPropsWithValue(tb, 3, 0, 50, 20, hvacTable.AirExchangeSupplyInd);
                    string airSupply = hvacTable.AirExchangeSupply;
                    if (!airSupply.Equals("–")) GetRoundUpValue(hvacTable.AirExchangeSupply).ToString();
                    SetCellPropsWithValue(tb, 3, 1, 50, 20, airSupply);
                    SetCellPropsWithValue(tb, 3, 2, 25, 20, "м3/ч");

                    string airExchangeExhaust = hvacTable.AirExchangeExhaust;
                    if (!airExchangeExhaust.Equals("–")) GetRoundUpValue(hvacTable.AirExchangeExhaust).ToString();
                    SetCellPropsWithValue(tb, 4, 0, 50, 150, hvacTable.AirExchangeExhaustInd);
                    SetCellPropsWithValue(tb, 4, 1, 50, 150, airExchangeExhaust);
                    SetCellPropsWithValue(tb, 4, 2, 25, 150, "м3/ч");

                    //CellRange range = CellRange.Create(tb, columnsNum - 2, 0, columnsNum - 1, 0);
                    //tb.UnmergeCells(range);
                    tb.SetDatabaseDefaults();
                    tb.GenerateLayout();
                    btr.AppendEntity(tb);
                    tr.AddNewlyCreatedDBObject(tb, true);
                    tr.Commit();
                }
            }
        }

        public static int GetRoundUpValue(string str, double divValue = 1)
        {
            return (int)Math.Ceiling(double.Parse(str) / divValue);
        }

        private void SetCellPropsWithValue(Table tb, int v1, int v2, int v3, int v4, string roomNumber)
        {
            throw new NotImplementedException();
        }

        
    }


}
