// (C) Copyright 2020 by HP Inc. 
//

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Internal;
using Autodesk.AutoCAD.Runtime;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using TransmittalCreator.DBCad;
using TransmittalCreator.Models;
using TransmittalCreator.Resources;
using TransmittalCreator.Services;
using TransmittalCreator.Utils;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using Exception = Autodesk.AutoCAD.Runtime.Exception;


// This line is not mandatory, but improves loading performances
//[assembly: CommandClass(typeof(TransmittalCreator.MyCommands))]

namespace TransmittalCreator;

public class MyCommands : IExtensionApplication
{
	public static bool UseBedit = true;
	private int _arcPrevQty;

	private bool _drawing;

	private FileSystemWatcher _fsw;
	private int _linesPrevQty;

	public void Initialize()
	{
		CommandsWatcher.Initialize();

		Application.DocumentManager.DocumentLockModeChanged += DocumentManager_DocumentLockModeChanged;
		var executablePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

		var pd = new ProxyDomain();
		var assembly = pd.GetAssembly(Path.Combine(executablePath, "MaterialDesignThemes.Wpf.dll"));

		var assembly1 = pd.GetAssembly(Path.Combine(executablePath, "MaterialDesignColors.dll"));

		if ((assembly != null) | (assembly1 != null)) Active.Editor.WriteMessage("style dlls not load");


		var standartCopier = new StandartCopier();
		var isConfFileCopied = standartCopier.CopyParamsFiles();

		if (!isConfFileCopied) Active.Editor.WriteMessage("файлы не скопированы");
	}

	public void Terminate()
	{
		throw new NotImplementedException();
	}

	private static void AddBlock(Point3d blockPoint3d, Editor ed)
	{
		var db = Application.DocumentManager.MdiActiveDocument.Database;
		using (var myT = db.TransactionManager.StartTransaction())
		{
			//Get the block definition "Check".
			var blockName = "Опора 2";
			var bt = db.BlockTableId.GetObject(OpenMode.ForRead) as BlockTable;
			var blockDef =
				bt[blockName].GetObject(OpenMode.ForRead) as BlockTableRecord;

			//Also open modelspace - we'll be adding our BlockReference to it
			var ms =
				bt[BlockTableRecord.ModelSpace].GetObject(OpenMode.ForWrite)
					as BlockTableRecord;
			//Create new BlockReference, and link it to our block definition

			using (var blockRef =
			       new BlockReference(blockPoint3d, blockDef.ObjectId))
			{
				//Add the block reference to modelspace
				ms.AppendEntity(blockRef);
				myT.AddNewlyCreatedDBObject(blockRef, true);
				//Iterate block definition to find all non-constant
				// AttributeDefinitions
				double circleX = default;
				double circleY = default;
				Point3d pWCSPt = default;
				ObjectId circleId = default;
				var btr = (BlockTableRecord)myT.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead);

				foreach (var id in btr)
				{
					ed.WriteMessage("{0} - {1}\n", id.ObjectClass.DxfName, id.ToString());
					if (id.ObjectClass.DxfName == "Circle") circleId = id;
				}

				foreach (var id in blockDef)
				{
					var obj = id.GetObject(OpenMode.ForRead);
					var attDef = obj as AttributeDefinition;

					if (obj is Circle circle)
					{
						ed.WriteMessage($"circle id {blockDef.Id}  \n");

						var str = blockDef.Id.ToString();
						char[] circleStr = { '(', ')' };

						circleX = circle.Center.X;
						circleY = circle.Center.Y;
						var newPt = new Point3d(circleX, circleY, 0);
						var mWPlane = Matrix3d.WorldToPlane(Vector3d.ZAxis);
						pWCSPt = newPt.TransformBy(mWPlane);
						str.Trim(circleStr);
					}
					else if (attDef != null && !attDef.Constant)
					{
						using (var attRef = new AttributeReference())
						{
							if (attDef.Tag == "№_Т_П")
							{
								attRef.SetAttributeFromBlock(attDef, blockRef.BlockTransform);
								attRef.TextString = "№3-";
							}
							else if (attDef.Tag == "ПРОЛЕТ")
							{
								attRef.SetAttributeFromBlock(attDef, blockRef.BlockTransform);
								attRef.TextString = "Пк-";
							}
							//else if (attDef.Tag == "X")
							//{
							//	attRef.SetAttributeFromBlock(attDef, blockRef.BlockTransform);
							//	Field custom = new Field($"X = %<\\AcObjProp.16.2 Object(%<\\_ObjId {circleId}>%,1).Center \\f \"%lu6%pt1\">%");
							//	custom.EvaluationOption = FieldEvaluationOptions.Automatic;
							//	custom.Evaluate((int)(FieldEvaluationOptions.Automatic), db);
							//	attRef.SetField(custom);

							//}
							//else if (attDef.Tag == "Y")
							//{
							//	attRef.SetAttributeFromBlock(attDef, blockRef.BlockTransform);
							//	attRef.TextString =
							//		$"Y = %<\\AcObjProp.16.2 Object(%<\\_ObjId {circleId}>%,1).Center \\f \"%lu6%pt2\">%";
							//}

							//attRef.SetAttributeFromBlock(attDef, blockRef.BlockTransform);
							//attRef.TextString = "Hello World";
							//Add the AttributeReference to the BlockReference
							blockRef.AttributeCollection.AppendAttribute(attRef);
							myT.AddNewlyCreatedDBObject(attRef, true);
						}
					}
				}
			}

			myT.Commit();
		}

		ed.Regen();
	}

	//[CommandMethod("AddFieldToAttribute")]
	public void AddFieldToAttribute()
	{
		var db = Application.DocumentManager.MdiActiveDocument.Database;
		var ed = Application.DocumentManager.MdiActiveDocument.Editor;
		var blkName = "Опора 2";
		using (var acTrans = db.TransactionManager.StartTransaction())
		{
			var blkTbl = db.BlockTableId.GetObject(OpenMode.ForRead) as BlockTable;
			if (blkTbl.Has(blkName))
			{
				var blkTblRec = blkTbl[blkName].GetObject(OpenMode.ForRead)
					as BlockTableRecord;
				using (var blkRef = new BlockReference(Point3d.Origin, blkTblRec.ObjectId))
				{
					var ms = blkTbl[BlockTableRecord.ModelSpace].GetObject(OpenMode.ForWrite)
						as BlockTableRecord;
					ms.AppendEntity(blkRef);
					acTrans.AddNewlyCreatedDBObject(blkRef, true);
					if (blkTblRec.HasAttributeDefinitions)
					{
						var cp = new Point3d(0, 0, 0);
						var circ = new Circle();
						var objID = new ObjectId();
						foreach (var id in blkTblRec)
						{
							var obj = id.GetObject(OpenMode.ForRead);
							if (obj is Circle)
							{
								circ = (Circle)obj; // get the circle object
								cp = circ.Center;
								objID = circ.ObjectId;
							}

							if (obj is AttributeDefinition)
							{
								var acAttDef = (AttributeDefinition)obj;
								//Create a new AttributeReference
								using (var acAttRef = new AttributeReference())
								{
									acAttRef.SetAttributeFromBlock(acAttDef, blkRef.BlockTransform);
									acAttRef.Position = acAttDef.Position.TransformBy(blkRef.BlockTransform);
									acAttRef.TextString = acAttDef.TextString;
									acAttRef.Tag = acAttDef.Tag;
									//Add the AttributeReference to the BlockReference
									blkRef.AttributeCollection.AppendAttribute(acAttRef);
									//Assign the centerpoint of the circle to a field code string 
									var str1 = "%<\\AcObjProp.16.2 Object(%<\\_ObjId ";
									var strID = objID.OldIdPtr.ToString();
									var str2 = ">%,1).Center \\f \"%lu2\">%";
									var str = str1 + strID + str2;
									var fldstr = string.Format(str, cp.ToString());
									//crete a new field with field code parameter
									var field = new Field(fldstr);
									field.Evaluate();
									var fieldEval = field.EvaluationStatus;
									if (fieldEval.Status != FieldEvaluationStatus.Success)
									{
										acTrans.Abort();
										ed.WriteMessage(string.Format("FieldEvaluationStatus Message: {0} - {1}",
											fieldEval.Status, fieldEval.ErrorMessage));
										return;
									}

									try
									{
										//set the field to attribute reference 
										acAttRef.SetField(field);
										acTrans.AddNewlyCreatedDBObject(field, true);
										ed.WriteMessage(string.Format(
											"Field value ({1}) is inseterd to Attribute '{0}' of the Block '{2}' ",
											acAttRef.Tag, field.Value, blkRef.Name));
									}
									catch
									{
										field.Dispose();
										ed.WriteMessage(string.Format("Failed to set attribute field '{0}' - {1}",
											acAttRef.Tag, acAttRef.Handle));
									}

									acTrans.AddNewlyCreatedDBObject(acAttRef, true);
								}
							}
						}
					}
				}
			}

			acTrans.Commit();
		}
	}

	//[CommandMethod("ChangeBlockPos")]
	public static void ChangeBlockPosByAttrValue()
	{
		var ed = Application.DocumentManager.MdiActiveDocument.Editor;
		var db = HostApplicationServices.WorkingDatabase;
		var tr = db.TransactionManager.StartTransaction();
		var blockName = "Опора 2";
		// Start the transaction
		try
		{
			// Build a filter list so that only
			// block references are selected
			var filList = new TypedValue[2]
			{
				new((int)DxfCode.Start, "INSERT"),
				new((int)DxfCode.BlockName, blockName)
			};

			var filter = new SelectionFilter(filList);
			//PromptSelectionOptions opts = new PromptSelectionOptions();
			//opts.MessageForAdding = "Select block references: ";
			//PromptSelectionResult res = ed.GetSelection(opts, filter);
			//// Do nothing if selection is unsuccessful
			//if (res.Status != PromptStatus.OK) //(140686616716400)
			//	return;
			//SelectionSet selSet = res.Value;
			var idArray = ed.SelectAll(filter).Value.GetObjectIds();

			foreach (var blkId in idArray)
			{
				var blkRef = (BlockReference)tr.GetObject(blkId, OpenMode.ForWrite);
				var btr = (BlockTableRecord)tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForWrite);
				var blockPostion = blkRef.Position;
				ed.WriteMessage("\nBlock: " + btr.Name);
				btr.Dispose();
				var attCol = blkRef.AttributeCollection;
				var blockX = blockPostion.X;
				var blockY = blockPostion.Y;
				var blockZ = blockPostion.Z;
				foreach (ObjectId attId in attCol)
				{
					var attRef = (AttributeReference)tr.GetObject(attId, OpenMode.ForRead);
					var str = "\n  Attribute Tag: " + attRef.Tag + "\n    Attribute String: " + attRef.TextString;
					ed.WriteMessage(str);
					if (attRef.Tag == "X" && !string.IsNullOrEmpty(attRef.TextString))
						blockX = Convert.ToDouble(attRef.TextString);
					if (attRef.Tag == "Y" && !string.IsNullOrEmpty(attRef.TextString))
						blockY = Convert.ToDouble(attRef.TextString);
				}

				var newLocation = new Point3d(blockX, blockY, 0);
				ed.WriteMessage($"block pos {blockPostion} new pos {newLocation}");
				blkRef.TransformBy(Matrix3d.Displacement(blockPostion.GetVectorTo(newLocation)));
			}

			tr.Commit();
		}
		catch (Exception ex)
		{
			ed.WriteMessage("Exception: " + ex.Message);
		}
		finally
		{
			tr.Dispose();
		}
	}

	//[CommandMethod("InsertBlockByPline")]
	public static void InsertBlockByPline()
	{
		var doc =
			Application.DocumentManager.MdiActiveDocument;
		var ed = doc.Editor;
		var db = doc.Database;
		var per =
			ed.GetEntity("Select a polyline");
		if (per.Status == PromptStatus.OK)
		{
			var tr = db.TransactionManager.StartTransaction();

			using (tr)
			{
				var obj =
					tr.GetObject(per.ObjectId, OpenMode.ForRead);
				// If a "lightweight" (or optimized) polyline
				var lwp = obj as Polyline;
				//if (lwp != null)
				//{
				// Use a for loop to get each vertex, one by one
				var vn = lwp.NumberOfVertices;
				for (var i = 0; i < vn; i++)
				{
					// Could also get the 3D point here
					var pt = lwp.GetPoint2dAt(i);
					//CreateText(db, new Point3d(pt.X, pt.Y+10, 0));
					//CreateAttributeInBlock(db, new Point3d(pt.X, pt.Y, 0));
					AddBlock(new Point3d(pt.X, pt.Y, 0), ed);
					//BuildBlock(ed,"Опора 2",new Point3d(pt.X, pt.Y,0),dictionary,"Model");
					ed.WriteMessage("\n" + pt);
				}

				//ListAttributes2();
				//}
				//else
				//{
				//	// If an old-style, 2D polyline
				//	var p2d = obj as Polyline2d;
				//	if (p2d != null)
				//	{
				//		// Use foreach to get each contained vertex
				//		foreach (ObjectId vId in p2d)
				//		{
				//			var v2d =
				//				(Vertex2d)tr.GetObject(
				//					vId,
				//					OpenMode.ForRead
				//				);
				//			ed.WriteMessage(
				//				"\n" + v2d.Position
				//			);
				//		}
				//	}
				//	else
				//	{
				//		// If an old-style, 3D polyline
				//		var p3d = obj as Polyline3d;
				//		if (p3d != null)
				//			// Use foreach to get each contained vertex
				//			foreach (ObjectId vId in p3d)
				//			{
				//				var v3d =
				//					(PolylineVertex3d)tr.GetObject(
				//						vId,
				//						OpenMode.ForRead
				//					);
				//				ed.WriteMessage(
				//					"\n" + v3d.Position
				//				);
				//			}
				//	}
				//}
				// Committing is cheaper than aborting
				tr.Commit();
			}

			doc.SendStringToExecute("attsync\n", true, false, false);
			doc.SendStringToExecute("n\n", true, false, false);
			doc.SendStringToExecute("Опора 2\n", true, false, false);
		}
	}


	//[CommandMethod("EC")]
	//public void EventCommand()
	//{
	//	var dm = Application.DocumentManager;
	//	var doc = dm.MdiActiveDocument;
	//	if (doc == null)
	//		return;
	//	var ed = doc.Editor;
	//	// We'll start by creating one square for each file in the
	//	// specified location. The nSquares() function uses some
	//	// global state for the index and the total count

	//	nSquares(ed);
	//	// Create a FileSystemWatcher for the path, looking for
	//	// write changes and drawing more squares as needed
	//	if (_fsw == null)
	//	{
	//		_fsw = new FileSystemWatcher(path, "*.*");
	//		_fsw.Changed += (o, s) => nSquaresInContext(dm, ed, path);
	//		_fsw.NotifyFilter = NotifyFilters.LastWrite
	//		                    | NotifyFilters.FileName;
	//		_fsw.EnableRaisingEvents = true;
	//		ed.WriteMessage("\nWatching \"{0}\" for changes.", path);
	//	}
	//}


#pragma warning disable 1998
	private async void nSquaresInContext(DocumentCollection dc, Editor ed, string path)
	{
		// Protect the command-calling function with a flag to avoid
		// eInvalidInput failures
		if (!_drawing)
		{
			_drawing = true;
			// Call our square creation function asynchronously
			await dc.ExecuteInCommandContextAsync(
				async o => nSquares(ed),
				null
			);
			_drawing = false;
		}
	}
#pragma warning restore 1998
	private void nSquares(Editor ed)
	{
		// Draw squares until we have enough (the total might
		// change, hence the need for global state)
		Active.Editor.WriteMessage("file changed");

		var filePath = @"C:\Users\yusufzhon.marasulov\Desktop\Test1\1\6-10 кВ опоры.xlsx";

		// Create a Selection Filter which selects all Objects on a given Layer... in this case "0"
		TypedValue[] tvs = { new((int)DxfCode.LayerName, "0") };
		var sf = new SelectionFilter(tvs);
		// Select all Objects on Layer
		var psr = ed.SelectAll(sf);
		var ss = psr.Value;
		if (ss != null)
			using (var tr = Active.Database.TransactionManager.StartTransaction())
			{
				foreach (SelectedObject so in ss)
				{
					var ob = tr.GetObject(so.ObjectId, OpenMode.ForWrite);
					ob.Erase();
				}

				tr.Commit();
			}


		var fileInfo = new FileInfo(filePath);
		using (var package = new ExcelPackage(fileInfo))
		{
			ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
			//create an instance of the the first sheet in the loaded file
			var worksheet = package.Workbook.Worksheets[8];
			//TrimEmptyRows(worksheet);
			var rowCount = worksheet.Dimension.Rows;

			using (Active.Document.LockDocument())
			using (var tr = Active.Document.TransactionManager.StartTransaction())
			using (var pline = new Polyline())
			{
				var lines = new List<Line>();
				var arcs = new List<Arc>();
				for (var i = 1; i < rowCount - 1; i++)
				{
					var xValText = worksheet.Cells[i, 3].Text;
					var yValText = worksheet.Cells[i, 4].Text;

					if (!string.IsNullOrEmpty(xValText))
					{
						var point3d = CreatePoint3d(xValText, yValText);
						pline.AddVertexAt(i - 1, new Point2d(point3d.X, point3d.Y), 0.0, 0.0, 0.0);
					}

					arcs.Add(CreateArcFromPoints(worksheet, i));
					var acLine = CreateLineFromPoints(worksheet, i);
					acLine.SetDatabaseDefaults();
					lines.Add(acLine);
					Active.Editor.WriteMessage($"{i} - {xValText} \n");
				}

				var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(Active.Database),
					OpenMode.ForWrite);
				ms.AppendEntity(pline);
				tr.AddNewlyCreatedDBObject(pline, true);
				foreach (var line in lines)
				{
					ms.AppendEntity(line);
					tr.AddNewlyCreatedDBObject(line, true);
				}

				foreach (var arc in arcs)
				{
					ms.AppendEntity(arc);
					tr.AddNewlyCreatedDBObject(arc, true);
				}

				tr.Commit();
				Active.Editor.WriteMessage("\n геометрия обновлена \n " +
				                           $"было {_linesPrevQty} создано {lines.Count} \n" +
				                           $"было {_arcPrevQty} создано {arcs.Count} \n");
				_linesPrevQty = lines.Count;
				_arcPrevQty = arcs.Count;
			}
		}

		Active.Editor.Regen();
	}

	private static Arc CreateArcFromPoints(ExcelWorksheet worksheet, int i)
	{
		var x1ArcText = worksheet.Cells[i, 15].Text;
		var y1ArcText = worksheet.Cells[i, 16].Text;
		var x2ArcText = worksheet.Cells[i, 17].Text;
		var y2ArcText = worksheet.Cells[i, 18].Text;
		var x3ArcText = worksheet.Cells[i, 19].Text;
		var y3ArcText = worksheet.Cells[i, 20].Text;

		var carc = new CircularArc3d(CreatePoint3d(x1ArcText, y1ArcText),
			CreatePoint3d(x2ArcText, y2ArcText),
			CreatePoint3d(x3ArcText, y3ArcText));

		return CircArc2Arc(carc);
	}

	private static Line CreateLineFromPoints(ExcelWorksheet worksheet, int i)
	{
		var x1ValText = worksheet.Cells[i, 9].Text;
		var y1ValText = worksheet.Cells[i, 10].Text;
		var x2ValText = worksheet.Cells[i, 11].Text;
		var y2ValText = worksheet.Cells[i, 12].Text;

		var acLine = new Line(CreatePoint3d(x1ValText, y1ValText), CreatePoint3d(x2ValText, y2ValText));

		return acLine;
	}

	private static Point3d CreatePoint3d(string x1ValText, string y1ValText)
	{
		var x = !string.IsNullOrEmpty(x1ValText) ? double.Parse(x1ValText) : 0.0;
		var y = !string.IsNullOrEmpty(y1ValText) ? double.Parse(y1ValText) : 0.0;

		return new Point3d(x, y, 0);
	}

	private static Arc CircArc2Arc(CircularArc3d circArc)
	{
		var center = circArc.Center;
		var normal = circArc.Normal;
		var refVec = circArc.ReferenceVector;
		var plane = new Plane(center, normal);
		var ang = refVec.AngleOnPlane(plane);
		return new Arc(
			center,
			normal,
			circArc.Radius,
			circArc.StartAngle + ang,
			circArc.EndAngle + ang
		);
	}

	private void TrimEmptyRows(ExcelWorksheet worksheet)
	{
		//loop all rows in a file
		for (var i = worksheet.Dimension.Start.Row;
		     i <=
		     worksheet.Dimension.End.Row;
		     i++)
		{
			var isRowEmpty = true;
			//loop all columns in a row
			for (var j = worksheet.Dimension.Start.Column; j <= worksheet.Dimension.End.Column; j++)
				if (worksheet.Cells[i, j].Value != null)
				{
					isRowEmpty = false;
					break;
				}

			if (isRowEmpty) worksheet.DeleteRow(i);
		}
	}
	//worksheet.TrimLastEmptyRows();

	//public static void TrimLastEmptyRows(this ExcelWorksheet worksheet)
	//{
	//    while (worksheet.IsLastRowEmpty())
	//        worksheet.DeleteRow(worksheet.Dimension.End.Row);
	//}
	//public static bool IsLastRowEmpty(this ExcelWorksheet worksheet)
	//{
	//    var empties = new List<bool>();

	//    for (int i = 1; i <= worksheet.Dimension.End.Column; i++)
	//    {
	//        var rowEmpty = worksheet.Cells[worksheet.Dimension.End.Row, i].Value == null ? true : false;
	//        empties.Add(rowEmpty);
	//    }

	//    return empties.All(e => e);
	//}

	//[CommandMethod("ECX")]
	public void StopEventCommand()
	{
		var dm = Application.DocumentManager;
		var doc = dm.MdiActiveDocument;
		if (doc == null)
			return;
		var ed = doc.Editor;
		if (_fsw != null)
		{
			_fsw.Dispose();
			_fsw = null;
		}

		ed.WriteMessage("\nNo longer watching folder.");
	}

	private static void DocumentManager_DocumentLockModeChanged(
		object sender, DocumentLockModeChangedEventArgs e)
	{
		try
		{
			if (!UseBedit)
				if (e.GlobalCommandName == "BEDIT")
				{
					e.Veto();
					var blockName = AcAeUtilities.GetBlockName();
					Active.WriteMessageInDebug(blockName);
				}
		}
		catch (System.Exception exception)
		{
			Active.WriteMessageInDebug($"\nException {exception.Message}");
		}
	}

	private static void Documents_DocumentActivated(object sender, DocumentCollectionEventArgs e)
	{
		if (e.Document == null)
			return;

		//SubscribeDocumentEvents(e.Document);
	}

	private static void SubscribeDocumentEvents(Document doc)
	{
		CommandsWatcher.SubscribeDocument(doc);
	}

	//[CommandMethod("OPSV")]
	public static void OpenSaveDwgFiles()
	{
		var dict = new List<Sheet>();
		try
		{
			var path = @"C:\Users\yusufzhon.marasulov\Desktop\test";
			var d = new DirectoryInfo(path);
			var Files = d.GetFiles("*.dwg");
			foreach (var file in Files)
			{
				var fileName = Path.GetFileName(file.FullName);
				var dwgFlpath = file.FullName;
				using (var db = new Database(false, true))
				{
					db.ReadDwgFile(dwgFlpath, FileOpenMode.OpenForReadAndAllShare, false, null);
					using (var tr = db.TransactionManager.StartTransaction())
					{
						var mSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
						var idArray = PrintUtils.SelectDynamicBlockReferences(mSpaceId);
						PrintUtils.GetSheetsFromBlocks(Active.Editor, dict, tr, idArray);
						tr.Commit();
					}

					db.SaveAs(dwgFlpath, DwgVersion.Current);
				}
			}

			Application.ShowAlertDialog("All files processed");
		}
		catch (System.Exception ex)
		{
			Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(ex.ToString());
		}
	}

	public static void BatchTransmittal(List<string> docsToPlot)
	{
		var dict = new List<Sheet>();
		var printModels = new List<PrintModel>();
		Active.Document.SendStringToExecute("REGENALL ", true, false, true);
		var doc = Active.Document;
		foreach (var filename in docsToPlot)
			using (var doclock = doc.LockDocument())
			{
				var db = new Database(false, true);
				db.ReadDwgFile(filename, FileShare.Read, true, "");

				using (var tr = db.TransactionManager.StartTransaction())
				{
					var mSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
					var idArray = PrintUtils.SelectDynamicBlockReferences(mSpaceId);


					//TODO надо проверить предыдущий и нижние методы на поиск по Id
					PrintUtils.GetSheetsFromBlocks(Active.Editor, dict, tr, idArray);
					var selAttrName = "НОМЕР_ЛИСТА";
					PrintUtils.GetPrintParametersToPdf(Active.Editor, printModels, tr, idArray, selAttrName);


					//utils.CreateOnlyVed(dict);
					PrintUtils.CreateJsonFile(dict);

					foreach (var printModel in printModels)
						//Active.Editor.WriteMessage("{0} печатаем ", printModel.DocNumber);
						PrintUtils.PlotCurrentLayout(printModel.DocNumber, printModel);

					tr.Commit();
				}
			}
	}


	#region XData

	//[CommandMethod("AddXData")]
	public static void AddXData_Method()
	{
		const string TestAppName = "XDataAppName1";

		var db = HostApplicationServices.WorkingDatabase;
		var dm = Application.DocumentManager;
		var doc = dm.MdiActiveDocument;
		var ed = doc.Editor;
		try
		{
			var prEntRes = ed.GetEntity("Select an Entity to attach XDATA");
			if (prEntRes.Status == PromptStatus.OK)
				using (var tr = db.TransactionManager.StartTransaction())
				{
					var regAppTable = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
					if (!regAppTable.Has(TestAppName))
						using (var regAppRecord = new RegAppTableRecord())
						{
							regAppRecord.Name = TestAppName;
							regAppTable.UpgradeOpen();
							regAppTable.Add(regAppRecord);
							regAppTable.DowngradeOpen();
							tr.AddNewlyCreatedDBObject(regAppRecord, true);
						}

					var ent = (Entity)tr.GetObject(prEntRes.ObjectId, OpenMode.ForWrite);
					ent.XData = new ResultBuffer
					(
						new TypedValue((int)DxfCode.ExtendedDataRegAppName, TestAppName), //1001
						new TypedValue((int)DxfCode.ExtendedDataAsciiString, "String1"), //1000
						new TypedValue((int)DxfCode.ExtendedDataLayerName, "0"), //1003
						new TypedValue((int)DxfCode.ExtendedDataBinaryChunk,
							Encoding.ASCII.GetBytes("BinaryChunk")), //1004
						new TypedValue((int)DxfCode.ExtendedDataHandle, ent.Handle), //1005
						new TypedValue((int)DxfCode.ExtendedDataXCoordinate, new Point3d(1.1, 2.1, 3.1)), //1010
						//new TypedValue((int)DxfCode.ExtendedDataYCoordinate, 2.1),          //1020
						//new TypedValue((int)DxfCode.ExtendedDataZCoordinate, 3.1),          //1030
						new TypedValue((int)DxfCode.ExtendedDataWorldXCoordinate,
							new Point3d(1.2, 2.2, 3.2)), //1011
						//new TypedValue((int)DxfCode.ExtendedDataWorldYCoordinate, 2.2),   //1021
						//new TypedValue((int)DxfCode.ExtendedDataWorldZCoordinate, 3.2),   //1031
						new TypedValue((int)DxfCode.ExtendedDataWorldXDisp, new Point3d(1.3, 2.3, 3.3)), //1012
						//new TypedValue((int)DxfCode.ExtendedDataWorldYDisp, 2.3),           //1022
						//new TypedValue((int)DxfCode.ExtendedDataWorldZDisp, 3.3),           //1032
						new TypedValue((int)DxfCode.ExtendedDataWorldXDir, new Point3d(1.4, 2.4, 3.4)), //1013
						//new TypedValue((int)DxfCode.ExtendedDataWorldYDir, 2.4),            //1023
						//new TypedValue((int)DxfCode.ExtendedDataWorldZDir, 3.4),            //1033
						new TypedValue((int)DxfCode.ExtendedDataControlString, "{"), //1002
						new TypedValue((int)DxfCode.ExtendedDataReal, 12345.6789), //1040
						new TypedValue((int)DxfCode.ExtendedDataDist, 25.25), //1041
						new TypedValue((int)DxfCode.ExtendedDataScale, 0.2), //1042
						new TypedValue((int)DxfCode.ExtendedDataControlString, "}"), //1002
						new TypedValue((int)DxfCode.ExtendedDataInteger16, 16), //1070
						new TypedValue((int)DxfCode.ExtendedDataInteger32, 32) //1071
					);

					tr.Commit();
				}
		}
		catch (System.Exception ex)
		{
			ed.WriteMessage(ex.ToString());
		}
	}

	//[CommandMethod("DeleteXData")]
	public static void DeleteXData_Method()
	{
		var db = HostApplicationServices.WorkingDatabase;
		var dm = Application.DocumentManager;
		var doc = dm.MdiActiveDocument;
		var ed = doc.Editor;
		try
		{
			var prEntRes = ed.GetEntity("\nSelect an Entity to delete XDATA from");
			if (prEntRes.Status == PromptStatus.OK)
				using (var tr = db.TransactionManager.StartTransaction())
				{
					var ent = (Entity)tr.GetObject(prEntRes.ObjectId, OpenMode.ForRead);
					if (ent.XData == null)
					{
						ed.WriteMessage("\nNo XData in the entity.");
						return;
					}

					var appName = ed.GetString("\nAppName for the XData to delete (Enter for ALL)").StringResult;
					if (string.IsNullOrEmpty(appName) &&
					    MessageBox.Show("Woult you really like to delete all XData associated with the entity?",
						    "Delete All XData", MessageBoxButtons.YesNo) == DialogResult.No)
						return;

					if (string.IsNullOrEmpty(appName))
					{
						foreach (var tv in ent.XData.AsArray().Where(e => e.TypeCode == 1001))
						{
							ent.UpgradeOpen();
							ent.XData = new ResultBuffer(new TypedValue(1001, tv.Value));
							ent.DowngradeOpen();
						}

						ed.WriteMessage("\nAll XData have been deleted.");
					}
					else
					{
						var rb = ent.GetXDataForApplication(appName);
						if (rb != null)
						{
							ent.UpgradeOpen();
							ent.XData = new ResultBuffer(new TypedValue(1001, appName));
							ent.DowngradeOpen();
							ed.WriteMessage("\nXData with AppName {0} have been deleted.", appName);
						}
						else
						{
							ed.WriteMessage("\nIt doesn't have XData with AppName {0}.", appName);
						}
					}

					tr.Commit();
				}
		}
		catch (System.Exception ex)
		{
			ed.WriteMessage(ex.ToString());
		}
	}

	#endregion

	#region 

	

	// Modal Command with pickfirst selection
	//[CommandMethod("hvacTable")]
	public static void CreateHvac()
	{
		var doc = Application.DocumentManager.MdiActiveDocument;

		if (Hvac.CreateHvacTable())
			doc.Editor.WriteMessage("\nsuccess\n");
		else
			doc.Editor.WriteMessage("\nfailure\n");
	}

	#endregion
}