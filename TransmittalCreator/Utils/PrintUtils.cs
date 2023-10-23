using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;

using iTextSharp.text.pdf;
using Newtonsoft.Json;
using TransmittalCreator.DBCad;
using TransmittalCreator.Models;
using TransmittalCreator.Services;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using Db = Autodesk.AutoCAD.DatabaseServices;
using Exception = Autodesk.AutoCAD.Runtime.Exception;
using Active = TransmittalCreator.DBCad.Active;

namespace TransmittalCreator.Utils;

public static class PrintUtils
{
	/// <summary>
	///     plotting method
	/// </summary>
	/// <param name="pdfFileName"> name</param>
	/// <param name="printModel">print param model</param>
	public static void PlotCurrentLayout(string pdfFileName, PrintModel printModel)
	{
		// Get the current document and database, and start a transaction
		var acDoc = Application.DocumentManager.MdiActiveDocument;
		var acCurDb = acDoc.Database;
		//short bgPlot = (short)Application.GetSystemVariable("BACKGROUNDPLOT");
		Application.SetSystemVariable("BACKGROUNDPLOT", 0);
		try
		{
			using (var acTrans = acCurDb.TransactionManager.StartTransaction())
			{
				// Reference the Layout Manager
				Db.LayoutManager acLayoutMgr;
				acLayoutMgr = Db.LayoutManager.Current;
				// Get the current layout and output its name in the Command Line window
				var acLayout = acTrans.GetObject(acLayoutMgr.GetLayoutId(acLayoutMgr.CurrentLayout),
					Db.OpenMode.ForRead) as Db.Layout;

				// Get the PlotInfo from the layout
				var acPlInfo = new PlotInfo();
				acPlInfo.Layout = acLayout.ObjectId;

				// Get a copy of the PlotSettings from the layout
				var acPlSet = new Db.PlotSettings(acLayout.ModelType);

				acPlSet.CopyFrom(acLayout);
				// Update the PlotSettings object
				var acPlSetVdr = Db.PlotSettingsValidator.Current;
				var sheetList = acPlSetVdr.GetPlotStyleSheetList();
				//acPlSetVdr.SetCurrentStyleSheet(acPlSet, "monochrome.ctb");

				var points = new Db.Extents2d(printModel.BlockPosition, printModel.BlockDimensions);

				var isHor = printModel.IsFormatHorizontal();
				//pdfCreator.GetBlockDimensions();
				var canonName = printModel.GetCanonNameByWidthAndHeight();

				//acDoc.Utility.TranslateCoordinates(point1, acWorld, acDisplayDCS, False);
				acPlSetVdr.SetPlotWindowArea(acPlSet, points);
				acPlSetVdr.SetPlotType(acPlSet, Db.PlotType.Window);
				if (!isHor)
					acPlSetVdr.SetPlotRotation(acPlSet, Db.PlotRotation.Degrees090);
				//else if(canonName =="ISO_full_bleed_A4_(297.00_x_210.00_MM)")
				//    acPlSetVdr.SetPlotRotation(acPlSet, PlotRotation.Degrees090);
				else
					acPlSetVdr.SetPlotRotation(acPlSet, Db.PlotRotation.Degrees000);

				// Set the plot scale
				acPlSetVdr.SetUseStandardScale(acPlSet, false);
				acPlSetVdr.SetStdScaleType(acPlSet, Db.StdScaleType.ScaleToFit);
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
				var acPlInfoVdr = new PlotInfoValidator();
				acPlInfoVdr.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
				acPlInfoVdr.Validate(acPlInfo);

				// Check to see if a plot is already in progress
				if (PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting)
					using (var acPlEng = PlotFactory.CreatePublishEngine())
					{
						// Track the plot progress with a Progress dialog
						var acPlProgDlg = new PlotProgressDialog(false, 1, true);
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
							var filename = Path.Combine(Path.GetDirectoryName(acDoc.Name), pdfFileName);
							//if (filename.Contains("."))
							//{
							//    filename.Replace(".", "_");
							//}

							filename += ".pdf";
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
							var acPlPageInfo = new PlotPageInfo();
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
		catch (Exception e)
		{
			if (e.ErrorStatus == ErrorStatus.InvalidInput)
				Application.ShowAlertDialog(
					$"{e.Message} : {printModel.Height} - {printModel.Width} не найден в настройках принтера");
		}
	}


	/// <summary>
	///     get dict from attributes of block
	/// </summary>
	/// <param name="tr"></param>
	/// <param name="attrList"></param>
	/// <returns></returns>
	public static (string, Dictionary<string, string>) GetAttrList(Db.Transaction tr,
		Dictionary<string, string> attrList)
	{
		var blockName = "";
		try
		{
			// Build a filter list so that only
			// block references are selected
			var filList = new Db.TypedValue[1] { new((int)Db.DxfCode.Start, "INSERT") };
			var filter = new SelectionFilter(filList);
			var opts = new PromptSelectionOptions();
			opts.SingleOnly = true;
			opts.SinglePickInSpace = true;
			opts.MessageForAdding = "Select block references: ";
			var res = Active.Editor.GetSelection(opts, filter);

			// Do nothing if selection is unsuccessful
			if (res.Status != PromptStatus.OK) MessageBox.Show("block not selected");
			if (res.Value.Count > 1) MessageBox.Show("For selecting block attributes, please select only one block");
			var selSet = res.Value;
			var idArray = selSet.GetObjectIds();
			foreach (var blkId in idArray)
			{
				var blkRef = (Db.BlockReference)tr.GetObject(blkId, Db.OpenMode.ForRead);
				var btr = (Db.BlockTableRecord)tr.GetObject(blkRef.DynamicBlockTableRecord, Db.OpenMode.ForRead);
				blockName = btr.Name;
				Active.Editor.WriteMessage("\nBlock: " + blockName);
				btr.Dispose();
				var attCol = blkRef.AttributeCollection;
				foreach (Db.ObjectId attId in attCol)
				{
					var attRef = (Db.AttributeReference)tr.GetObject(attId, Db.OpenMode.ForRead);
					var str = "\n  Attribute Tag: " + attRef.Tag + "\n    Attribute String: " + attRef.TextString;
					attrList.Add(attRef.Tag, attRef.TextString);
					Active.Editor.WriteMessage(str);
				}
			}

			tr.Commit();
		}
		catch (Exception ex)
		{
			Active.Editor.WriteMessage("Exception: " + ex.Message);
		}
		finally
		{
			tr.Dispose();
		}

		return (blockName, attrList);
	}

	private static Dictionary<string, string> GetAttributes(Db.BlockReference blkRef, Db.Transaction tr)
	{
		var attrList = new Dictionary<string, string>();
		var attCol = blkRef.AttributeCollection;
		foreach (Db.ObjectId attId in attCol)
		{
			var attRef = (Db.AttributeReference)tr.GetObject(attId, Db.OpenMode.ForRead);
			var str = "\n  Attribute Tag: " + attRef.Tag + "\n    Attribute String: " + attRef.TextString;
			attrList.Add(attRef.Tag, attRef.TextString);
			Active.Editor.WriteMessage(str);
		}

		return attrList;
	}

	public static void GetSheetsFromBlocks(Editor ed, List<Sheet> dict, Db.Transaction tr,
		Db.ObjectIdCollection idArray)
	{
		// Build a filter list so that only
		// block references are selected

		string sheetNumber = "",
			docNumber = "",
			objectNameEng = "",
			docTitleEng = "",
			objectNameRu = "",
			docTitleRu = "";

		foreach (Db.ObjectId blkId in idArray)
			//TODO Check this point
			if (blkId != Db.ObjectId.Null)
			{
				var blkRef = (Db.BlockReference)tr.GetObject(blkId, Db.OpenMode.ForRead);

				var blockPos3d =
					Autodesk.AutoCAD.Internal.Utils.UcsToDisplay(new Point3d(blkRef.Position.X, blkRef.Position.Y, 0),
						false);
				var minPointX = blockPos3d.X;

				var stampViewValue = GetBlockAttributeValueByAttrName(blkRef);
				var btr = (Db.BlockTableRecord)tr.GetObject(blkRef.BlockTableRecord, Db.OpenMode.ForRead);

				btr.Dispose();
				var attCol = blkRef.AttributeCollection;

				dict.Add(CreateSheetFromAttributs(attCol, tr, stampViewValue.Trim(), minPointX));
			}
	}

	private static Sheet CreateSheetFromAttributs(Db.AttributeCollection attCol, Db.Transaction tr, string stampViewValue, double minPointX)
	{
		string sheetNumber = "",
			docNumber = "",
			objectNameEng = "",
			docTitleEng = "",
			objectNameRu = "",
			docTitleRu = "";

		foreach (Db.ObjectId attId in attCol)
		{
			var attRef = (Db.AttributeReference)tr.GetObject(attId, Db.OpenMode.ForRead);

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
				case "НАЗВАНИЕ_ЛИСТА":
					docTitleRu = attRef.TextString;
					break;
				case "ЛИСТ":
					sheetNumber = attRef.TextString;
					break;
			}
		}

		return new Sheet(sheetNumber.Trim(), docNumber.Trim(), objectNameEng.Trim(), docTitleEng.Trim(),
			objectNameRu.Trim(), docTitleRu.Trim(), stampViewValue.Trim(), minPointX);
	}

	/// <summary>
	///     создает колекцию листов из блоков штампа
	/// </summary>
	/// <param name="ed">editor</param>
	/// <param name="dict"> коллекция листов</param>
	/// <param name="tr"> транзакция </param>
	/// <returns>коллекция листов </returns>
	public static List<Sheet> GetSheetsFromBlocks(Editor ed, List<Sheet> dict, Db.Transaction tr,
		Db.ObjectIdCollection objectIdCollection, AttributModel attributModel)
	{
		var objectNameEn = attributModel.ObjectNameEn;
		var objectNameRu = attributModel.ObjectNameRu;
		var listNumber = attributModel.Position;
		var nomination = attributModel.Nomination;
		var commentAttr = attributModel.Comment;
		var trDocTitleEn = attributModel.TrDocTitleEn;
		var trDocTitleRu = attributModel.TrDocTitleRu;

		string sheetNumber = "",
			docNumber = "",
			comment = "",
			objectNameEng = "",
			docTitleEng = "",
			objectNameRus = "",
			docTitleRu = "";

		foreach (Db.ObjectId blkId in objectIdCollection)
		{
			var blkRef = (Db.BlockReference)tr.GetObject(blkId, Db.OpenMode.ForRead);

			var btr = (Db.BlockTableRecord)tr.GetObject(blkRef.BlockTableRecord, Db.OpenMode.ForRead);

			var extents3d = blkRef.GeometricExtents;
			ed.WriteMessage("\nBlock:{0} габариты {1} ", btr.Name, extents3d.MinPoint.ToString(),
				extents3d.MaxPoint.ToString());
			btr.Dispose();

			var attCol = blkRef.AttributeCollection;


			var attrDict = blkRef.GetAttributesValues();

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


	public static Db.Database CreateDatabaseFromTemplate(string templateName, string password)
	{
		if (templateName == null || templateName.Trim() == string.Empty) return null;
		var templateDb = new Db.Database(false, true);
		if (password == null) password = string.Empty;
		templateDb.ReadDwgFile(Environment.ExpandEnvironmentVariables(templateName),
			Db.FileOpenMode.OpenForReadAndWriteNoShare, true, password);
		templateDb.CloseInput(true);
		var result = templateDb.Wblock();
		return result;
	}


	/// <summary>
	///     find print area and pdf name by block id
	/// </summary>
	public static List<PrintModel> GetPrintParametersToPdf(Editor ed, List<PrintModel> printModels, Db.Transaction tr,
		Db.ObjectIdCollection objectIdCollection, string selAttrName)
	{
		foreach (Db.ObjectId blkId in objectIdCollection)
			printModels.Add(GetPrintModelByBlockId(tr, selAttrName, blkId));

		return printModels;
	}

	public static PrintModel GetPrintModelByBlockId(Db.Transaction tr, string selAttrName, Db.ObjectId blkId)
	{
		var blkRef = (Db.BlockReference)tr.GetObject(blkId, Db.OpenMode.ForRead);
		var btr = (Db.BlockTableRecord)tr.GetObject(blkRef.BlockTableRecord, Db.OpenMode.ForRead);

		var docNumber = GetBlockAttributeValue(blkRef, selAttrName);
		//formatValue = attrDict.FirstOrDefault(x => x.Key == "ФОРМАТ").Value;
		//ed.WriteMessage("\nBlock:{0} - {1} габариты {2} -{3}", btr.Name, docNumber, posPoint2d.ToString(), formatValue);

		btr.Dispose();
		return new PrintModel(docNumber, blkId);
	}

	public static string GetBlockAttributeValue(Db.BlockReference blkRef, string selAttrName)
	{
		var blockStamp = GetBlockAttributeValueByAttrName(blkRef);
		var attrDict = blkRef.GetAttributesValues();
		var docNumber = "";

		if (blockStamp == "Форма 3 ГОСТ Р 21.1101-2009 M25" || blockStamp == "Форма 3 ГОСТ Р 21.1101-2009")
		{
			docNumber = attrDict.FirstOrDefault(x => x.Key == selAttrName).Value;
		}
		else if (blockStamp == "Форма 6 ГОСТ Р 21.1101-2009")
		{
			selAttrName = "НОМЕР_ЛИСТА_2";
			docNumber = attrDict.FirstOrDefault(x => x.Key == selAttrName).Value;
			docNumber += "-page" + attrDict.FirstOrDefault(x => x.Key == "ЛИСТ2_СПЕЦ").Value;
		}

		return docNumber;
	}

	private static string GetBlockAttributeValueByAttrName(Db.BlockReference blkRef)
	{
		var props = blkRef.DynamicBlockReferencePropertyCollection;
		var blockStamp = "";
		foreach (Db.DynamicBlockReferenceProperty prop in props)
			if (prop.PropertyName == "Штамп")
				blockStamp = prop.Value.ToString();

		return blockStamp;
	}

	public static void CreateJsonFile(List<Sheet> dict)
	{
		dict = dict.OrderBy(x => x.SheetNumber).ToList();
		var newSheets = new List<Sheet>();
		foreach (var sheet in dict)
			if (sheet.ViewValue != "Форма 6 ГОСТ Р 21.1101-2009")
				newSheets.Add(sheet);

		var json = JsonConvert.SerializeObject(newSheets, Formatting.Indented);
		var path = DrawingPath();
		var dirName = Path.GetDirectoryName(path);
		var pathExtension = Path.GetFileNameWithoutExtension(path) + ".json";
		var jsonFile = Path.Combine(dirName, pathExtension);
		File.WriteAllText(jsonFile, json);
	}

	private static string DrawingPath()
	{
		var doc = Application.DocumentManager.MdiActiveDocument;
		var hs = Db.HostApplicationServices.Current;
		var path = hs.FindFile(doc.Name, doc.Database, Db.FindFileHint.Default);
		return path;
	}

	public static Dictionary<string, string> GetAttribs(string blockName, string tag)
	{
		// create a new instance of Dictionary<string, string>
		var attribs = new Dictionary<string, string>();

		// get the documents collection
		var docs = Application.DocumentManager;

		// use an OpenCloseTransaction which is not related to a document or database
		using (var tr = new Db.OpenCloseTransaction())
		{
			// iterate through the documents
			foreach (Document doc in docs)
			{
				// get the document database
				var db = doc.Database;

				// get the database block table
				var bt = (Db.BlockTable)tr.GetObject(db.BlockTableId, Db.OpenMode.ForRead);

				// if the block table contains a block definitions named 'blockName'...
				if (bt.Has(blockName))
				{
					// open the block definition
					var btr = (Db.BlockTableRecord)tr.GetObject(bt[blockName], Db.OpenMode.ForRead);

					// get the inserted block references ObjectIds
					var ids = btr.GetBlockReferenceIds(true, true);

					// if any...
					if (0 < ids.Count)
					{
						// open the first block reference
						var br = (Db.BlockReference)tr.GetObject(ids[0], Db.OpenMode.ForRead);

						// iterate through the attribute collection
						foreach (Db.ObjectId id in br.AttributeCollection)
						{
							// open the attribute reference
							var attRef = (Db.AttributeReference)tr.GetObject(id, Db.OpenMode.ForRead);

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

	public static void CreateOnlyVed(List<Sheet> dict, double scale = 1)
	{
		var ed = Active.Editor;
		
		// Start the transaction
		try
		{
			//GetSheetsFromBlocks(ed, dict, tr);
			CreateTableFromList(dict, scale);
			
		}
		catch (Exception ex)
		{
			ed.WriteMessage("Exception: " + ex.Message + " " + ex.InnerException);
		}
		//finally
		//{
		//	tr.Dispose();
		//}
	}

	public static void CreateOnlytrans(List<Sheet> dict)
	{
		var ed = Active.Editor;
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
			ed.WriteMessage("Exception: " + ex.Message + " " + ex.InnerException);
		}
	}

	public static void CreateTableFromList(List<Sheet> dict, double scale = 1)
	{
		//dict = dict.OrderBy(x => x.SheetNumber).ToList();
		
		var db = HostApplicationServices.WorkingDatabase;
		var ed = Active.Editor;
		using (var tr = db.TransactionManager.StartTransaction())
		{
			PromptPointOptions prOpt = new PromptPointOptions("\nEnter table insertion point: ");
			PromptPointResult pr = ed.GetPoint(prOpt);

			if (pr.Status != PromptStatus.OK) return;
			//Application.SetSystemVariable("TILEMODE", 0);
			//Active.Editor.SwitchToPaperSpace();
			
			var bt = (Db.BlockTable)tr.GetObject(db.BlockTableId, Db.OpenMode.ForRead);
			var btr = (Db.BlockTableRecord)tr.GetObject(bt[BlockTableRecord.PaperSpace], Db.OpenMode.ForWrite);
			var tb = new Db.Table();
			tb.TableStyle = db.Tablestyle;
			
			btr.AppendEntity(tb);
			
			var RowsNum = dict.Count;
			var ColumnsNum = 3;
			var rowheight = 8 * scale;
			var columnwidth = 15 * scale;
			
			tb.InsertRows(0, rowheight, RowsNum + 1);
			tb.InsertColumns(0, columnwidth, ColumnsNum - 1);
			tb.SetRowHeight(rowheight);
			tb.SetColumnWidth(columnwidth);
			tb.Position = pr.Value;
			// Объединяем ячейки
			var range = Db.CellRange.Create(tb, 0, 0, 0, 2);
			tb.MergeCells(range);
			range.Borders.Top.IsVisible = false;
			range.Borders.Bottom.IsVisible = true;
			range.Borders.Left.IsVisible = false;
			range.Borders.Right.IsVisible = false;
			
			range = Db.CellRange.Create(tb, 1, 1, 1, 2);
			tb.UnmergeCells(range);
			tb.Columns[0].Width = 15 * scale;
			tb.Cells[0, 0].TextHeight = 5 * scale;
			tb.Cells[0, 0].Alignment = Db.CellAlignment.MiddleCenter;
			tb.Cells[0, 0].TextString = "Ведомость рабочих чертежей основного комплекта";
			var row = tb.Rows[RowsNum + 1];
			tb.UnmergeCells(row);
			tb.Rows[1].Alignment = Db.CellAlignment.MiddleCenter;
			tb.Rows[1].TextHeight = 3.5 * scale;
			tb.Cells[1, 0].TextString = "Лист";

			tb.Columns[1].Width = 140 * scale;
						tb.Cells[1, 1].TextString = "Наименование";

			tb.Columns[2].Width = 30 * scale;
			tb.Cells[1, 2].TextString = "Примечание";
			
			var curRow = 2;
			var i = 0;
			foreach (var item in dict)
			{
				if (!string.IsNullOrWhiteSpace(item.DocNumber))
				{
					i++;
					tb.Rows[curRow].TextHeight = 3.0 * scale;
					tb.Cells[curRow, 0].TextString = i.ToString();
					tb.Cells[curRow, 0].Alignment = Db.CellAlignment.MiddleCenter;


					tb.Cells[curRow, 1].TextString = item.DocNumber + " " + item.DocTitleRu;
					tb.Cells[curRow, 1].Alignment = Db.CellAlignment.MiddleLeft;
					if (!string.IsNullOrEmpty(item.Comment))
					{
						tb.Cells[curRow, 2].TextString = item.Comment;
						tb.Cells[curRow, 2].Alignment = Db.CellAlignment.MiddleLeft;
					}
				}

				curRow++;
			}

			range = Db.CellRange.Create(tb, RowsNum, 0, RowsNum, RowsNum);
			tb.UnmergeCells(range);
			tb.GenerateLayout();
			tr.AddNewlyCreatedDBObject(tb, true);
			tr.Commit();
		}
	}


	//[CommandMethod("SetDynamicBlkProperty")]
	public static void SetDynamicBlkProperty()
	{
		var doc = Application.DocumentManager.MdiActiveDocument;
		var db = doc.Database;
		var ed = doc.Editor;

		var prEntOptions = new PromptEntityOptions("Выберите вставку динамического блока...");

		var prEntResult = ed.GetEntity(prEntOptions);

		if (prEntResult.Status != PromptStatus.OK)
		{
			ed.WriteMessage("Ошибка...");
			return;
		}

		using (var Tx = db.TransactionManager.StartTransaction())
		{
			var bref = Tx.GetObject(prEntResult.ObjectId, Db.OpenMode.ForWrite) as Db.BlockReference;

			double blockWidth = 0;
			if (bref.IsDynamicBlock)
			{
				var props = bref.DynamicBlockReferencePropertyCollection;

				foreach (Db.DynamicBlockReferenceProperty prop in props)
				{
					var values = prop.GetAllowedValues();

					if (prop.PropertyName == "Штамп")
					{
						var btr = (Db.BlockTableRecord)Tx.GetObject(bref.BlockTableRecord, Db.OpenMode.ForRead);
						btr.Dispose();

						var attCol = bref.AttributeCollection;
						var attrDict = bref.GetAttributesValues();
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
	///     select from model dynblocks byName
	/// </summary>
	/// <param name="blockName"></param>
	/// <returns></returns>
	//[CommandMethod("selb")]
	public static Db.ObjectIdCollection SelectDynamicBlockReferences(Db.ObjectId mSpaceId,
		string blockName = "ФорматM25")
	{
		var ed = Active.Editor;
		var db = Active.Database;

		var dynBlockRefs = new Db.ObjectIdCollection();
		using (var trans = db.TransactionManager.StartTransaction())
		{
			// получаем таблицу блоков и проходим по всем записям таблицы блоков
			var bt = (Db.BlockTable)trans.GetObject(db.BlockTableId, Db.OpenMode.ForRead);
			foreach (var btrId in bt)
			{
				// получаем запись таблицы блоков и смотри анонимная ли она
				var btr = (Db.BlockTableRecord)trans.GetObject(btrId, Db.OpenMode.ForRead);
				if (btr.IsDynamicBlock && btr.Name == blockName)
				{
					// получаем все анонимные блоки динамического блока
					var anonymousIds = btr.GetAnonymousBlockIds();
					// получаем все прямые вставки динамического блока
					dynBlockRefs = btr.GetBlockReferenceIds(true, true);

					foreach (Db.ObjectId anonymousBtrId in anonymousIds)
					{
						// open the model space BlockTableRecord
						//var modelSpace = (BlockTableRecord)tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead);

						// получаем анонимный блок
						var anonymousBtr =
							(Db.BlockTableRecord)trans.GetObject(anonymousBtrId, Db.OpenMode.ForRead);

						// получаем все вставки этого блока
						var blockRefIds = anonymousBtr.GetBlockReferenceIds(true, true);

						foreach (Db.ObjectId id in blockRefIds)
						{
							//var blockReference = (BlockReference)tr.GetObject(id, OpenMode.ForRead);
							var e = (Db.BlockReference)trans.GetObject(id, Db.OpenMode.ForRead);

							var ownerId = e.OwnerId;

							if (ownerId == mSpaceId)
							{
								ed.WriteMessage(e.ToString());

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
	///     get blocks by name in current space
	/// </summary>
	/// <param name="blockName"></param>
	/// <returns></returns>
	public static IEnumerable<Db.ObjectId> GetAllCurrentSpaceBlocksByName(string blockName)
	{
		var db = Db.HostApplicationServices.WorkingDatabase;
		using (var tr = db.TransactionManager.StartOpenCloseTransaction())
		{
			var curSpace = (Db.BlockTableRecord)tr.GetObject(db.CurrentSpaceId, Db.OpenMode.ForRead);
			return curSpace
				.Cast<Db.ObjectId>()
				.Where(id => id.ObjectClass.DxfName == "INSERT")
				.Select(id => (Db.BlockReference)tr.GetObject(id, Db.OpenMode.ForRead))
				.Where(br =>
					((Db.BlockTableRecord)tr.GetObject(br.DynamicBlockTableRecord, Db.OpenMode.ForRead)).Name ==
					blockName)
				.Select(br => br.ObjectId);
		}
	}

	//[CommandMethod("blockName")]
	public static void GetBlockName()
	{
		var doc = Active.Document;
		var db = doc.Database;
		var ed = doc.Editor;
		var options =
			new PromptEntityOptions("\nSelect block reference");
		options.SetRejectMessage("\nSelect only block reference");
		options.AddAllowedClass(typeof(Db.BlockReference), false);
		var acSSPrompt = ed.GetEntity(options);
		using (var tx =
			   db.TransactionManager.StartTransaction())
		{
			var blockRef = tx.GetObject(acSSPrompt.ObjectId,
				Db.OpenMode.ForRead) as Db.BlockReference;
			Db.BlockTableRecord block = null;
			if (blockRef.IsDynamicBlock)
				//get the real dynamic block name.
				block = tx.GetObject(blockRef.DynamicBlockTableRecord, Db.OpenMode.ForRead) as Db.BlockTableRecord;
			else
				block = tx.GetObject(blockRef.BlockTableRecord, Db.OpenMode.ForRead) as Db.BlockTableRecord;
			if (block != null)
				ed.WriteMessage("Block name is : "
								+ block.Name + "\n");
			tx.Commit();
		}
	}

	/// <summary>
	///     adding hvac table
	/// </summary>
	/// <param name="hvacTable"></param>
	/// <param name="columnsNum"></param>
	public static void AddTable(HvacTable hvacTable)
	{
		var ed = Active.Editor;
		var db = Db.HostApplicationServices.WorkingDatabase;
		using (var tr = db.TransactionManager.StartTransaction())
		{
			var bt = (Db.BlockTable)tr.GetObject(db.BlockTableId, Db.OpenMode.ForRead);
			var msId = bt[Db.BlockTableRecord.ModelSpace];
			var btr = (Db.BlockTableRecord)tr.GetObject(msId, Db.OpenMode.ForWrite);

			var pr =
				Active.Editor.GetPoint(
					$"\nEnter table insertion point for:room #{hvacTable.RoomNumber}-{hvacTable.RoomName}");
			if (pr.Status == PromptStatus.OK)
			{
				// create a table
				var tb = new Db.Table();
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
				var airSupply = hvacTable.AirExchangeSupply;
				if (!airSupply.Equals("–")) GetRoundUpValue(hvacTable.AirExchangeSupply).ToString();
				SetCellPropsWithValue(tb, 3, 1, 50, 20, airSupply);
				SetCellPropsWithValue(tb, 3, 2, 25, 20, "м3/ч");

				var airExchangeExhaust = hvacTable.AirExchangeExhaust;
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

	private static void SetCellPropsWithValue(Db.Table tb, int curRow, int curCol, int textHeight, short colorNumber,
		string stringValue)
	{
		var curPos = tb.Cells[curRow, curCol];
		curPos.TextHeight = textHeight;
		curPos.TextString = stringValue;
		curPos.Alignment = Db.CellAlignment.MiddleCenter;
		curPos.ContentColor = Color.FromColorIndex(ColorMethod.ByAci, colorNumber);
	}

	/// <summary>
	///     принимает список пдф документов для объединения и объединяет их в выходном файле
	/// </summary>
	/// <param name="pages">список пдф документов в виде полного названия файла</param>
	/// <param name="fileName">выходной документ в виде полного названия</param>
	/// <returns></returns>
	public static bool UnitePages(List<string> pages, string fileName)
	{
		if (pages.Count == 0) return false;
		try
		{
			//создаем экземпляр ридера из первого пдф документа
			using (var pdfReader = new PdfReader(pages[0]))
			{
				//получаем документ из первого ридера
				using (var document = new iTextSharp.text.Document(pdfReader.GetPageSizeWithRotation(1)))
				{
					//создаем выходной документ как копию первого открытого
					using (var pdfCopy = new PdfCopy(document, new FileStream(fileName, FileMode.CreateNew)))
					{
						//открываем первый документ
						document.Open();
						//получаем содержимое первой страницы открытого документа
						var pdfImportedPage = pdfCopy.GetImportedPage(pdfReader, 1);
						//добавляем полученную страницу в выходной документ
						pdfCopy.AddPage(pdfImportedPage);
						//проходим по оставшимся документам
						for (var i = 1; i < pages.Count; i++)
							//создаем новый экземпляр ридера
							using (var pdfReader2 = new PdfReader(pages[i]))
							{
								//получаем первую страницу
								using (var document2 =
									   new iTextSharp.text.Document(pdfReader.GetPageSizeWithRotation(1)))
								{
									//открываем страницу
									document2.Open();
									//записываем содержимое этой страницы
									pdfImportedPage = pdfCopy.GetImportedPage(pdfReader2, 1);
									//добавляем эту страницу в выходной документ
									pdfCopy.AddPage(pdfImportedPage);
								}
							}
					}
				}
			}

			return true;
		}
		catch
		{
			return false;
		}
	}
}