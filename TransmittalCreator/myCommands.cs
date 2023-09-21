// (C) Copyright 2020 by HP Inc. 
//
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;
using LeaderPlacement;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using DocumentFormat.OpenXml.Office2010.PowerPoint;
using Newtonsoft.Json;
using TransmittalCreator.Models;
using TransmittalCreator.Models.Layouts;
using TransmittalCreator.Services;
using TransmittalCreator.Services.Blocks;
using TransmittalCreator.Services.Files;
using TransmittalCreator.Utils;
using TransmittalCreator.ViewModel;
using TransmittalCreator.Views;
using Active = TransmittalCreator.DBCad.Active;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Arc = Autodesk.AutoCAD.DatabaseServices.Arc;
using Document = Autodesk.AutoCAD.ApplicationServices.Document;
using Line = Autodesk.AutoCAD.DatabaseServices.Line;
using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;


// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(TransmittalCreator.MyCommands))]

namespace TransmittalCreator
{
	public class MyCommands : IExtensionApplication
	{
		const string path = @"C:\Users\yusufzhon.marasulov\Desktop\Test1\1\";
		//private string confFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "conf.json");
		private FileSystemWatcher _fsw = null;
		private bool _drawing = false;
		private int _linesPrevQty;
		int _arcPrevQty;


		#region XData

		//[CommandMethod("AddXData")]
		public static void AddXData_Method()
		{
			const string TestAppName = "XDataAppName1";

			Database db = HostApplicationServices.WorkingDatabase;
			var dm = Application.DocumentManager;
			var doc = dm.MdiActiveDocument;
			Editor ed = doc.Editor;
			try
			{
				PromptEntityResult prEntRes = ed.GetEntity("Select an Entity to attach XDATA");
				if (prEntRes.Status == PromptStatus.OK)
				{
					using (Transaction tr = db.TransactionManager.StartTransaction())
					{
						RegAppTable regAppTable = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
						if (!regAppTable.Has(TestAppName))
						{
							using (RegAppTableRecord regAppRecord = new RegAppTableRecord())
							{
								regAppRecord.Name = TestAppName;
								regAppTable.UpgradeOpen();
								regAppTable.Add(regAppRecord);
								regAppTable.DowngradeOpen();
								tr.AddNewlyCreatedDBObject(regAppRecord, true);
							}
						}

						Entity ent = (Entity)tr.GetObject(prEntRes.ObjectId, OpenMode.ForWrite);
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
			}
			catch (System.Exception ex)
			{
				ed.WriteMessage(ex.ToString());
			}
		}

		//[CommandMethod("DeleteXData")]
		public static void DeleteXData_Method()
		{
			Database db = HostApplicationServices.WorkingDatabase;
			var dm = Application.DocumentManager;
			var doc = dm.MdiActiveDocument;
			Editor ed = doc.Editor;
			try
			{
				PromptEntityResult prEntRes = ed.GetEntity("\nSelect an Entity to delete XDATA from");
				if (prEntRes.Status == PromptStatus.OK)
				{
					using (Transaction tr = db.TransactionManager.StartTransaction())
					{
						Entity ent = (Entity)tr.GetObject(prEntRes.ObjectId, OpenMode.ForRead);
						if (ent.XData == null)
						{
							ed.WriteMessage("\nNo XData in the entity.");
							return;
						}

						string appName = ed.GetString("\nAppName for the XData to delete (Enter for ALL)").StringResult;
						if (string.IsNullOrEmpty(appName) &&
							MessageBox.Show("Woult you really like to delete all XData associated with the entity?",
								"Delete All XData", MessageBoxButtons.YesNo) == DialogResult.No)
							return;

						if (string.IsNullOrEmpty(appName))
						{
							foreach (TypedValue tv in ent.XData.AsArray().Where(e => e.TypeCode == 1001))
							{
								ent.UpgradeOpen();
								ent.XData = new ResultBuffer(new TypedValue(1001, tv.Value));
								ent.DowngradeOpen();
							}

							ed.WriteMessage("\nAll XData have been deleted.");
						}
						else
						{
							ResultBuffer rb = ent.GetXDataForApplication(appName);
							if (rb != null)
							{
								ent.UpgradeOpen();
								ent.XData = new ResultBuffer(new TypedValue(1001, appName));
								ent.DowngradeOpen();
								ed.WriteMessage("\nXData with AppName {0} have been deleted.", appName);
							}
							else
								ed.WriteMessage("\nIt doesn't have XData with AppName {0}.", appName);
						}

						tr.Commit();
					}
				}
			}
			catch (System.Exception ex)
			{
				ed.WriteMessage(ex.ToString());
			}
		}

		#endregion

		private static void AddBlock(Point3d blockPoint3d, Editor ed)
		{
			Database db = Application.DocumentManager.MdiActiveDocument.Database;
			using (Transaction myT = db.TransactionManager.StartTransaction())
			{
				//Get the block definition "Check".
				string blockName = "Опора 2";
				BlockTable bt =
					db.BlockTableId.GetObject(OpenMode.ForRead) as BlockTable;
				BlockTableRecord blockDef =
				  bt[blockName].GetObject(OpenMode.ForRead) as BlockTableRecord;

				//Also open modelspace - we'll be adding our BlockReference to it
				BlockTableRecord ms =
				  bt[BlockTableRecord.ModelSpace].GetObject(OpenMode.ForWrite)
														  as BlockTableRecord;
				//Create new BlockReference, and link it to our block definition

				using (BlockReference blockRef =
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
					BlockTableRecord btr = (BlockTableRecord)myT.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead);

					foreach (ObjectId id in btr)
					{
						ed.WriteMessage("{0} - {1}\n", id.ObjectClass.DxfName, id.ToString());
						if (id.ObjectClass.DxfName == "Circle")
						{
							circleId = id;
						}

					}

					foreach (ObjectId id in blockDef)
					{
						DBObject obj = id.GetObject(OpenMode.ForRead);
						AttributeDefinition attDef = obj as AttributeDefinition;

						if (obj is Circle circle)
						{
							ed.WriteMessage($"circle id {blockDef.Id}  \n");

							var str = blockDef.Id.ToString();
							char[] circleStr = { '(', ')' };

							circleX = circle.Center.X;
							circleY = circle.Center.Y;
							var newPt = new Point3d(circleX, circleY, 0);
							Matrix3d mWPlane = Matrix3d.WorldToPlane(Vector3d.ZAxis);
							pWCSPt = newPt.TransformBy(mWPlane);
							str.Trim(circleStr);

						}
						else if ((attDef != null) && (!attDef.Constant))
						{
							using (AttributeReference attRef = new AttributeReference())
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
			Database db = Application.DocumentManager.MdiActiveDocument.Database;
			Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
			string blkName = "Опора 2";
			using (Transaction acTrans = db.TransactionManager.StartTransaction())
			{
				BlockTable blkTbl = db.BlockTableId.GetObject(OpenMode.ForRead) as BlockTable;
				if (blkTbl.Has(blkName))
				{
					BlockTableRecord blkTblRec = blkTbl[blkName].GetObject(OpenMode.ForRead)
						as BlockTableRecord;
					using (BlockReference blkRef = new BlockReference(Point3d.Origin, blkTblRec.ObjectId))
					{
						BlockTableRecord ms = blkTbl[BlockTableRecord.ModelSpace].GetObject(OpenMode.ForWrite)
							as BlockTableRecord;
						ms.AppendEntity(blkRef);
						acTrans.AddNewlyCreatedDBObject(blkRef, true);
						if (blkTblRec.HasAttributeDefinitions)
						{
							Point3d cp = new Point3d(0, 0, 0);
							Circle circ = new Circle();
							ObjectId objID = new ObjectId();
							foreach (ObjectId id in blkTblRec)
							{
								DBObject obj = id.GetObject(OpenMode.ForRead);
								if ((obj) is Circle)
								{
									circ = (Circle)obj; // get the circle object
									cp = circ.Center;
									objID = circ.ObjectId;
								}
								if (obj is AttributeDefinition)
								{
									AttributeDefinition acAttDef = (AttributeDefinition)obj;
									//Create a new AttributeReference
									using (AttributeReference acAttRef = new AttributeReference())
									{
										acAttRef.SetAttributeFromBlock(acAttDef, blkRef.BlockTransform);
										acAttRef.Position = acAttDef.Position.TransformBy(blkRef.BlockTransform);
										acAttRef.TextString = acAttDef.TextString;
										acAttRef.Tag = acAttDef.Tag;
										//Add the AttributeReference to the BlockReference
										blkRef.AttributeCollection.AppendAttribute(acAttRef);
										//Assign the centerpoint of the circle to a field code string 
										string str1 = "%<\\AcObjProp.16.2 Object(%<\\_ObjId ";
										string strID = objID.OldIdPtr.ToString();
										string str2 = ">%,1).Center \\f \"%lu2\">%";
										string str = str1 + strID + str2;
										string fldstr = string.Format(str, cp.ToString());
										//crete a new field with field code parameter
										Field field = new Field(fldstr);
										field.Evaluate();
										FieldEvaluationStatusResult fieldEval = field.EvaluationStatus;
										if (fieldEval.Status != FieldEvaluationStatus.Success)
										{
											acTrans.Abort();
											ed.WriteMessage(string.Format("FieldEvaluationStatus Message: {0} - {1}",
												fieldEval.Status, fieldEval.ErrorMessage));
											return;
										}
										else
										{
											try
											{   //set the field to attribute reference 
												acAttRef.SetField(field);
												acTrans.AddNewlyCreatedDBObject(field, true);
												ed.WriteMessage(string.Format("Field value ({1}) is inseterd to Attribute '{0}' of the Block '{2}' ",
													acAttRef.Tag, field.Value, blkRef.Name));
											}
											catch
											{
												field.Dispose();
												ed.WriteMessage(string.Format("Failed to set attribute field '{0}' - {1}",
													acAttRef.Tag, acAttRef.Handle));
											}
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
			Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
			Database db = HostApplicationServices.WorkingDatabase;
			Transaction tr = db.TransactionManager.StartTransaction();
			var blockName = "Опора 2";
			// Start the transaction
			try
			{

				// Build a filter list so that only
				// block references are selected
				TypedValue[] filList = new TypedValue[2]
				{
					new TypedValue((int)DxfCode.Start, "INSERT"),
					new TypedValue((int)DxfCode.BlockName, blockName)
				};

				SelectionFilter filter = new SelectionFilter(filList);
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
					BlockReference blkRef = (BlockReference)tr.GetObject(blkId, OpenMode.ForWrite);
					BlockTableRecord btr = (BlockTableRecord)tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForWrite);
					var blockPostion = blkRef.Position;
					ed.WriteMessage("\nBlock: " + btr.Name);
					btr.Dispose();
					AttributeCollection attCol = blkRef.AttributeCollection;
					int circleId = default;
					double blockX = blockPostion.X;
					double blockY = blockPostion.Y;
					double blockZ = blockPostion.Z;
					foreach (ObjectId attId in attCol)
					{
						AttributeReference attRef = (AttributeReference)tr.GetObject(attId, OpenMode.ForRead);
						string str = ("\n  Attribute Tag: " + attRef.Tag + "\n    Attribute String: " + attRef.TextString);
						ed.WriteMessage(str);
						if (attRef.Tag == "X" && !string.IsNullOrEmpty(attRef.TextString))
						{
							blockX = Convert.ToDouble(attRef.TextString);
						}
						if (attRef.Tag == "Y" && !string.IsNullOrEmpty(attRef.TextString))
						{
							blockY = Convert.ToDouble(attRef.TextString);
						}
					}

					var newLocation = new Point3d(blockX, blockY, 0);
					ed.WriteMessage($"block pos {blockPostion} new pos {newLocation}");
					blkRef.TransformBy(Matrix3d.Displacement(blockPostion.GetVectorTo(newLocation)));
				}
				tr.Commit();

			}
			catch (Autodesk.AutoCAD.Runtime.Exception ex)
			{
				ed.WriteMessage(("Exception: " + ex.Message));
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
				Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
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
		public void EventCommand()
		{
			var dm = Application.DocumentManager;
			var doc = dm.MdiActiveDocument;
			if (doc == null)
				return;
			var ed = doc.Editor;
			// We'll start by creating one square for each file in the
			// specified location. The nSquares() function uses some
			// global state for the index and the total count

			nSquares(ed);
			// Create a FileSystemWatcher for the path, looking for
			// write changes and drawing more squares as needed
			if (_fsw == null)
			{
				_fsw = new FileSystemWatcher(path, "*.*");
				_fsw.Changed += (o, s) => nSquaresInContext(dm, ed, path);
				_fsw.NotifyFilter = NotifyFilters.LastWrite
									| NotifyFilters.FileName;
				_fsw.EnableRaisingEvents = true;
				ed.WriteMessage("\nWatching \"{0}\" for changes.", path);
			}
		}


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
					async (o) => nSquares(ed),
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

			string filePath = @"C:\Users\yusufzhon.marasulov\Desktop\Test1\1\6-10 кВ опоры.xlsx";

			// Create a Selection Filter which selects all Objects on a given Layer... in this case "0"
			TypedValue[] tvs = new TypedValue[] { new TypedValue((int)DxfCode.LayerName, "0") };
			SelectionFilter sf = new SelectionFilter(tvs);
			// Select all Objects on Layer
			PromptSelectionResult psr = ed.SelectAll(sf);
			SelectionSet ss = psr.Value;
			if (ss != null)
			{
				using (Transaction tr = Active.Database.TransactionManager.StartTransaction())
				{
					foreach (SelectedObject so in ss)
					{
						DBObject ob = tr.GetObject(so.ObjectId, OpenMode.ForWrite);
						ob.Erase();
					}
					tr.Commit();
				}
			}


			FileInfo fileInfo = new FileInfo(filePath);
			using (ExcelPackage package = new ExcelPackage(fileInfo))
			{
				ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
				//create an instance of the the first sheet in the loaded file
				ExcelWorksheet worksheet = package.Workbook.Worksheets[8];
				//TrimEmptyRows(worksheet);
				var rowCount = worksheet.Dimension.Rows;

				using (Active.Document.LockDocument())
				using (var tr = Active.Document.TransactionManager.StartTransaction())
				using (var pline = new Polyline())
				{
					List<Line> lines = new List<Line>();
					List<Arc> arcs = new List<Arc>();
					for (int i = 1; i < rowCount - 1; i++)
					{
						var xValText = worksheet.Cells[i, 3].Text;
						var yValText = worksheet.Cells[i, 4].Text;

						if (!string.IsNullOrEmpty(xValText))
						{
							var point3d = CreatePoint3d(xValText, yValText);
							pline.AddVertexAt(i - 1, new Point2d(point3d.X, point3d.Y), 0.0, 0.0, 0.0);
						}
						arcs.Add(CreateArcFromPoints(worksheet, i));
						Line acLine = CreateLineFromPoints(worksheet, i);
						acLine.SetDatabaseDefaults();
						lines.Add(acLine);
						Active.Editor.WriteMessage($"{i} - {xValText} \n");
					}

					var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(Active.Database), OpenMode.ForWrite);
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
					Active.Editor.WriteMessage($"\n геометрия обновлена \n " +
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

			CircularArc3d carc = new CircularArc3d(CreatePoint3d(x1ArcText, y1ArcText),
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

			Line acLine = new Line(CreatePoint3d(x1ValText, y1ValText), CreatePoint3d(x2ValText, y2ValText));

			return acLine;
		}

		static Point3d CreatePoint3d(string x1ValText, string y1ValText)
		{
			double x = !string.IsNullOrEmpty(x1ValText) ? double.Parse(x1ValText) : 0.0;
			double y = !string.IsNullOrEmpty(y1ValText) ? double.Parse(y1ValText) : 0.0;

			return new Point3d(x, y, 0);
		}

		private static Arc CircArc2Arc(CircularArc3d circArc)
		{
			Point3d center = circArc.Center;
			Vector3d normal = circArc.Normal;
			Vector3d refVec = circArc.ReferenceVector;
			Plane plane = new Plane(center, normal);
			double ang = refVec.AngleOnPlane(plane);
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
			for (int i = worksheet.Dimension.Start.Row; i <=
														worksheet.Dimension.End.Row; i++)
			{
				bool isRowEmpty = true;
				//loop all columns in a row
				for (int j = worksheet.Dimension.Start.Column; j <= worksheet.Dimension.End.Column; j++)
				{
					if (worksheet.Cells[i, j].Value != null)
					{
						isRowEmpty = false;
						break;
					}
				}
				if (isRowEmpty)
				{
					worksheet.DeleteRow(i);
				}
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

		//[CommandMethod("MYSL")]
		public static void SplinedLeader()
		{
			Document doc
				= Application.DocumentManager.MdiActiveDocument;
			Editor ed = doc.Editor;
			Database db = doc.Database;
			SplinedLeaderJig jig = new SplinedLeaderJig();
			bool bSuccess = true;
			bool bComplete = false;
			while (bSuccess && !bComplete)
			{
				SplinedLeaderJig._mIsJigStarted = false;
				PromptResult dragres = ed.Drag(jig);
				bSuccess = (dragres.Status == PromptStatus.OK);
				if (bSuccess)
					jig.addVertex();
				bComplete = (dragres.Status == PromptStatus.None);
				if (bComplete)
				{
					jig.removeLastVertex();
				}
			}
			if (bComplete)
			{
				// Append entity to DB
				Transaction tr = db.TransactionManager.StartTransaction();
				using (tr)
				{
					BlockTable bt = tr.GetObject(
						db.BlockTableId, OpenMode.ForRead
					) as BlockTable;
					BlockTableRecord ms =
						bt[BlockTableRecord.ModelSpace].GetObject
							(OpenMode.ForWrite) as BlockTableRecord;
					ms.AppendEntity(jig.getEntity());
					tr.AddNewlyCreatedDBObject(jig.getEntity(),
						true);
					tr.Commit();
				}
			}
		}

		//[CommandMethod("DL")]
		public void DirectionalLeader()
		{
			var doc = Application.DocumentManager.MdiActiveDocument;
			var ed = doc.Editor;
			var db = doc.Database;
			// Ask the user for the string and the start point of the leader
			var pso = new PromptStringOptions("\nEnter text");
			pso.AllowSpaces = true;
			var pr = ed.GetString(pso);
			if (pr.Status != PromptStatus.OK)
				return;
			var ppr = ed.GetPoint("\nStart point of leader");
			if (ppr.Status != PromptStatus.OK)
				return;
			// Start a transaction, as we'll be jigging a db-resident object
			using (var tr = db.TransactionManager.StartTransaction())
			{
				var bt =
					(BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead, false);
				var btr =
					(BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite, false);
				// Create and pass in an invisible MLeader
				// This helps avoid flickering when we start the jig
				var ml = new MLeader();
				ml.Visible = false;
				// Create jig
				var jig = new LeaderJig.DirectionalLeaderJig(pr.StringResult, ppr.Value, ml);
				// Add the MLeader to the drawing: this allows it to be displayed
				btr.AppendEntity(ml);
				tr.AddNewlyCreatedDBObject(ml, true);
				// Set end point in the jig
				var res = ed.Drag(jig);
				// If all is well, commit
				if (res.Status == PromptStatus.OK)
				{
					tr.Commit();
				}
			}
		}

		#region HvacTable

		[CommandMethod("JigLine")]
		public static void JigLine()
		{
			Document doc = Application.DocumentManager.MdiActiveDocument;
			if (LineJigger.Jig())
			{
				doc.Editor.WriteMessage("\nsuccess\n");
			}
			else
			{
				doc.Editor.WriteMessage("\nfailure\n");
			}
		}

		// Modal Command with pickfirst selection
		[CommandMethod("hvacTable")]
		public static void CreateHvac()
		{
			Document doc = Application.DocumentManager.MdiActiveDocument;

			if (Hvac.CreateHvacTable())
			{
				doc.Editor.WriteMessage("\nsuccess\n");
			}
			else
			{
				doc.Editor.WriteMessage("\nfailure\n");
			}


		}

		#endregion

		[CommandMethod("CreateVed")]
		public static void CreateVed()
		{
			List<Sheet> dict = new List<Sheet>();
			List<PrintModel> printModels = new List<PrintModel>();
			Active.Document.SendStringToExecute("REGENALL ", true, false, true);
			using (Transaction tr = DBCad.Active.Database.TransactionManager.StartTransaction())
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

						string jsonFile = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "conf.json"));
						Params deserializeObject = JsonConvert.DeserializeObject<Params>(jsonFile);

						//if (blockName == "Формат" | blockName == "ФорматM25") idArray.Add(objectId);
						if (deserializeObject.StampName.Contains(blockName)) idArray.Add(objectId);
					}
				}

				PrintUtils.GetSheetsFromBlocks(Active.Editor, dict, tr, idArray);
				string selAttrName = "НОМЕР_ЛИСТА";

				var dictOrdered = dict.OrderBy(x => x.MinPointX).ToList();
				PrintUtils.GetPrintParametersToPdf(Active.Editor, printModels, tr, idArray, selAttrName);

				var scale = printModels[0].GetBlockScale();


				PrintUtils.CreateOnlyVed(dictOrdered, scale);
				tr.Commit();
			}
		}


		#region CreateTransPdf

		[CommandMethod("CreateTranspdf")]
		public static void CreateTransmittalAndPdf()
		{
			List<Sheet> dict = new List<Sheet>();
			List<PrintModel> printModels = new List<PrintModel>();
			Active.Document.SendStringToExecute("REGENALL ", true, false, true);
			using (Transaction tr = DBCad.Active.Database.TransactionManager.StartTransaction())
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
						string jsonFile = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "conf.json"));
						Params deserializeObject = JsonConvert.DeserializeObject<Params>(jsonFile);

						if (deserializeObject.StampName.Contains(blockName)) idArray.Add(objectId);

					}
				}

				PrintUtils.GetSheetsFromBlocks(Active.Editor, dict, tr, idArray);
				string selAttrName = "НОМЕР_ЛИСТА";
				PrintUtils.GetPrintParametersToPdf(Active.Editor, printModels, tr, idArray, selAttrName);


				//utils.CreateOnlyVed(dict);
				PrintUtils.CreateJsonFile(dict);

				foreach (var printModel in printModels)
				{
					//Active.Editor.WriteMessage("{0} печатаем ", printModel.DocNumber);
					PrintUtils.PlotCurrentLayout(printModel.DocNumber, printModel);
				}

				tr.Commit();
			}
		}

		#endregion

		#region CreateCombinedTransmittalAndPdf

		[CommandMethod("CreateCombinedTransmittalAndPdf")]
		public static void CreateCombinedTransmittalAndPdf()
		{
			Document acDoc = Application.DocumentManager.MdiActiveDocument;

			List<Sheet> dict = new List<Sheet>();
			List<PrintModel> printModels = new List<PrintModel>();
			Active.Document.SendStringToExecute("REGENALL ", true, false, true);
			using (Transaction tr = DBCad.Active.Database.TransactionManager.StartTransaction())
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
						string jsonFile = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "conf.json"));
						Params deserializeObject = JsonConvert.DeserializeObject<Params>(jsonFile);

						//if (blockName == "Формат" | blockName == "ФорматM25") idArray.Add(objectId);
						if (deserializeObject.StampName.Contains(blockName)) idArray.Add(objectId);

					}
				}

				PrintUtils.GetSheetsFromBlocks(Active.Editor, dict, tr, idArray);
				string selAttrName = "НОМЕР_ЛИСТА";
				PrintUtils.GetPrintParametersToPdf(Active.Editor, printModels, tr, idArray, selAttrName);


				//utils.CreateOnlyVed(dict);
				PrintUtils.CreateJsonFile(dict);

				foreach (var printModel in printModels)
				{
					//Active.Editor.WriteMessage("{0} печатаем ", printModel.DocNumber);
					PrintUtils.PlotCurrentLayout(printModel.DocNumber, printModel);
				}

				//if (pKeyRes.StringResult.ToLower() == "yes")
				//{
				var combinedFileName = Path.Combine(Path.GetDirectoryName(Active.Document.Name), $"{Path.GetFileNameWithoutExtension(Active.Document.Name)}.pdf");
				if (File.Exists(combinedFileName))
				{
					try
					{
						File.Delete(combinedFileName);
					}
					catch (System.Exception e)
					{
						Active.WriteMessage(e.Message);

					}
				}

				var filenames = dict.Select(x => Path.Combine(Path.GetDirectoryName(Active.Document.Name), $"{x.DocNumber}.pdf")).ToList();

				var isSuccessPrinted = PrintUtils.UnitePages(filenames, combinedFileName);


				foreach (var file in filenames.Where(file => File.Exists(file)))
				{
					try
					{
						File.Delete(file);
					}
					catch (System.Exception e)
					{
						Active.WriteMessage(e.Message);

					}
				}
				Active.WriteMessage(isSuccessPrinted ? "Все файлы распечатаны\n" : "Произошла ошибка\n");

				//}

				tr.Commit();
			}
		}

		#endregion

		[CommandMethod("CIE1")]
		public void SelectObjectOnScreen()
		{
			var doc = Application.DocumentManager.MdiActiveDocument;
			var db = doc.Database;
			var ed = doc.Editor;

			var options = new PromptEntityOptions("\nSelect block: ");
			options.SetRejectMessage("\nSelected object is no a block.");
			options.AddAllowedClass(typeof(BlockReference), true);

			var result = ed.GetEntity(options);
			if (result.Status == PromptStatus.OK)
			{
				// at this point we know an entity have been selected and it is a Polyline
				using (var tr = db.TransactionManager.StartTransaction())
				{
					var blockRef = (BlockReference)tr.GetObject(result.ObjectId, OpenMode.ForWrite);

					BlockTableRecord block =
						tr.GetObject(blockRef.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
					string blockName = block.Name;
					ed.WriteMessage($"block id {blockRef.ObjectId} blockname {blockName}");
					tr.Commit();
				}
			}
		}


		#region CreatePdfWithInterfaceSelecting

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


						PrintUtils.CreateJsonFile(dict);

						foreach (var printModel in fileNameCreator.GetPrintModels())
						{
							PrintUtils.PlotCurrentLayout(printModel.DocNumber, printModel);
						}
					}

					tr.Commit();
				}
			}
		}

		#endregion

		#region CreatePdfWithInterfaceSelecting

		[CommandMethod("CreateP")]
		public void CreatePdfNameTest()
		{
			Active.Document.SendStringToExecute("REGENALL ", true, false, true);
			
			var options = new PromptEntityOptions("\nPlease select the block:");
			options.SetRejectMessage("\nSelected object isn't a block.\n");
			options.AddAllowedClass(typeof(BlockReference), true);

			var result = Active.Editor.GetEntity(options);
			if (result.Status == PromptStatus.OK)
			{
				using (var tr = Active.Database.TransactionManager.StartTransaction())
				{
					var blockRef = (BlockReference)tr.GetObject(result.ObjectId, OpenMode.ForWrite);

					BlockTableRecord btr =
						tr.GetObject(blockRef.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
					string blockName = btr.Name;
					btr.Dispose();
					 AttributeCollection attributeCollection = blockRef.AttributeCollection;

					 Dictionary<string, string> attrList = GetDictionaryFromAttributeCollection(attributeCollection, tr);

					var pdfCreatorView = new PdfCreatorView(attrList);
					pdfCreatorView.Show();

					Active.Editor.WriteMessage($"block id {blockRef.ObjectId} blockname {blockName}");

					tr.Commit();
				}

				//var names = PrintUtils.GetAttrList(tr, attrList);

				//attrList = names.Item2;
				//_blockName = names.Item1;

				//foreach (KeyValuePair<string, string> entry in attrList)
				//{
				//	combolist.Add(new BlockAttribute(entry.Key, entry.Value));
				//}

				//Application.ShowModalWindow(window);
				//if (window.isClicked)
				//{
				//	//ObjectIdCollection objectIds = Utils.SelectDynamicBlockReferences();
				//	List<Sheet> dict = new List<Sheet>();

				//	using (Transaction tr = Active.Database.TransactionManager.StartTransaction())
				//	{
				//		var blockRef = (BlockReference)tr.GetObject(result.ObjectId, OpenMode.ForWrite);

				//		BlockTableRecord block = tr.GetObject(blockRef.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;

				//		ObjectId[] idArrayTemp = { block.Id };
				//		//ObjectIdCollection idArray = new ObjectIdCollection(idArrayTemp);
				//		//TODO printing X Y
				//		//string selAttrName = attributeName.AttributeName;


				//		FileNameCreator fileNameCreator = new FileNameCreator(window, idArrayTemp);

				//		//GetPrintParametersToPdf(Active.Editor, printModels, tr, objectIds, selAttrName);

				//		fileNameCreator.GetPrintParametersToPdf(tr);

				//		PrintUtils.CreateJsonFile(dict);

				//		foreach (var printModel in fileNameCreator.GetPrintModels())
				//		{
				//			PrintUtils.PlotCurrentLayout(printModel.DocNumber, printModel);
				//		}


				//		tr.Commit();
				//	}
				//}



			}
			else
			{
				Active.Editor.WriteMessage("Надо выбрать блок");
			}


		}

		private Dictionary<string, string> GetDictionaryFromAttributeCollection(AttributeCollection attributeCollection, Transaction tr)
		{
			var attributesDict = new Dictionary<string, string>();

			foreach (ObjectId attId in attributeCollection)
			{
				AttributeReference attRef = (AttributeReference)tr.GetObject(attId, OpenMode.ForRead);
				attributesDict.Add(attRef.Tag, attRef.TextString);
				//string str = ("\n  Attribute Tag: " + attRef.Tag + "\n    Attribute String: " + attRef.TextString);
				//Active.Editor.WriteMessage(str);
			}

			return attributesDict;
		}

		#endregion

		#region CreateDwg

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

						//if (blockName == "Формат") idArray.Add(objectId);
						//else if (blockName == "ФорматM25") idArray.Add(objectId);
						var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
						string jsonFile = File.ReadAllText(Path.Combine(assemblyLocation ?? string.Empty, "conf.json"));
						Params deserializeObject = JsonConvert.DeserializeObject<Params>(jsonFile);

						if (deserializeObject.StampName.Contains(blockName)) idArray.Add(objectId);

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
						string selAttrName = "НАЗВАНИЕ_ЛИСТА";
						string fileName = PrintUtils.GetBlockAttributeValue(blkRef, selAttrName);

						//HostApplicationServices hs = HostApplicationServices.Current;
						//string path = Application.GetSystemVariable("DWGPREFIX");
						//hs.FindFile(doc.Name, doc.Database, FindFileHint.Default);
						//string createdwgFolder = Path.GetFileNameWithoutExtension(db.OriginalFileName);

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

		#endregion

		[CommandMethod("CrLayTr")]
		public void CreateTransmitallFromLayout()
		{
			IFileManager fileManager = new FileManager();
			string allPath = fileManager.GetFilePath();
			Active.Database.SaveAs(allPath, true, DwgVersion.Current, Active.Database.SecurityParameters);
			string jsonFile = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "conf.json"));
			Params deserializeObject = JsonConvert.DeserializeObject<Params>(jsonFile);


			string[] blockNames = deserializeObject.StampName;
			DynamicBlockFinder dynamicBlocks = new DynamicBlockFinder(blockNames);
			LayoutModelCollection layoutModelCollection = new LayoutModelCollection(dynamicBlocks);
			layoutModelCollection.ListLayouts("Model");
			layoutModelCollection.CreateLayoutCollection();

			string[] viewNames = { "Форма 3 ГОСТ Р 21.1101-2009 M25", "Форма 3 ГОСТ Р 21.1101-2009" };
			var packageCreator = new PrintPackageCreator(layoutModelCollection, viewNames);

			LayoutTreeView window = new LayoutTreeView(packageCreator.PrintPackageModels);
			Autodesk.AutoCAD.ApplicationServices.Core.Application.ShowModalWindow(window);

			if (!window.isClicked) return;

			var printPackages = window._printPackages;
			packageCreator.PrintPackageModels = printPackages;
			var sheetType = SheetType.MultiPdf;

			packageCreator.PublishAllPackages();

			var blockIds = printPackages.SelectMany(x => x.Layouts.Select(y => y.BlocksObjectId)).ToArray();


			List<Sheet> sheets = new List<Sheet>();
			using (Transaction trans = Active.Database.TransactionManager.StartTransaction())
			{
				PrintUtils.GetSheetsFromBlocks(Active.Editor, sheets, trans, new ObjectIdCollection(blockIds));
				PrintUtils.CreateJsonFile(sheets);
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
				ObjectId mSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(Active.Database);
				ObjectIdCollection objectIds = PrintUtils.SelectDynamicBlockReferences(mSpaceId);

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
					PrintUtils.GetSheetsFromBlocks(Active.Editor, dict, tr, objectIds);
					string selAttrName = "НОМЕР_ЛИСТА";
					PrintUtils.GetPrintParametersToPdf(Active.Editor, printModels, tr, objectIds, selAttrName);

					if (window.transmittalCheckBox.IsChecked == true)
					{

						PrintUtils.CreateOnlyVed(dict);
						PrintUtils.CreateOnlytrans(dict);
						foreach (var printModel in printModels)
						{
							PrintUtils.PlotCurrentLayout(printModel.DocNumber, printModel);
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

		public static bool UseBedit = true;
		private static void DocumentManager_DocumentLockModeChanged(
			object sender, Autodesk.AutoCAD.ApplicationServices.DocumentLockModeChangedEventArgs e)
		{
			try
			{
				if (!UseBedit)
				{
					if (e.GlobalCommandName == "BEDIT")
					{
						e.Veto();
						var blockName = Autodesk.AutoCAD.Internal.AcAeUtilities.GetBlockName();
						Active.WriteMessageInDebug(blockName);
					}
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


		public void Initialize()
		{
			CommandsWatcher.Initialize();
			//AcadUtils.Documents.DocumentActivated += Documents_DocumentActivated;


			//if (AcadUtils.Document != null) 
			//	SubscribeDocumentEvents(AcadUtils.Document);

			Application.DocumentManager.DocumentLockModeChanged += DocumentManager_DocumentLockModeChanged;
			string executablePath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

			ProxyDomain pd = new ProxyDomain();
			Assembly assembly = pd.GetAssembly(System.IO.Path.Combine(executablePath, "MaterialDesignThemes.Wpf.dll"));

			Assembly assembly1 = pd.GetAssembly(Path.Combine(executablePath, "MaterialDesignColors.dll"));

			if (assembly != null | assembly1 != null)
			{
				Active.Editor.WriteMessage("style dlls not load");
			}


			StandartCopier standartCopier = new StandartCopier();
			var isConfFileCopied = standartCopier.CopyParamsFiles();

			if (!isConfFileCopied)
			{
				Active.Editor.WriteMessage($"файлы не скопированы");
			}
		}

		public void Terminate()
		{
			throw new NotImplementedException();
		}

		[CommandMethod("BlockExt")]
		public void BlockExt()
		{
			Document doc = Application.DocumentManager.MdiActiveDocument;
			if (doc == null) return;
			Editor ed = doc.Editor;
			PromptEntityOptions enOpt =
				new PromptEntityOptions("\nВыберите вставку блока:");
			enOpt.SetRejectMessage("Это не вставка блока");
			enOpt.AddAllowedClass(typeof(BlockReference), true);
			PromptEntityResult enRes = ed.GetEntity(enOpt);
			if (enRes.Status == PromptStatus.OK)
			{
				Extents3d blockExt = new Extents3d(Point3d.Origin, Point3d.Origin);
				using (BlockReference bref = enRes.ObjectId.Open(OpenMode.ForRead) as BlockReference)
				{
					Matrix3d mat = bref.BlockTransform;
					using (BlockTableRecord btr = bref.BlockTableRecord.Open(OpenMode.ForRead) as BlockTableRecord)
					{
						foreach (ObjectId id in btr)
						{
							using (DBObject obj = id.Open(OpenMode.ForRead) as DBObject)
							{
								Entity en = obj as Entity;
								if (en != null && en.Visible == true)
								{

									try
									{
										Extents3d ext = en.GeometricExtents;
										ext.TransformBy(mat);
										if (blockExt.MinPoint == Point3d.Origin && blockExt.MaxPoint == Point3d.Origin)
											blockExt = ext;
										else
											blockExt.AddExtents(ext);
									}
									catch (Autodesk.AutoCAD.Runtime.Exception e)
									{
										Active.Editor.WriteMessage(e.Message);

									}


								}
							}
						}
					}
				}
				string s =
					"MinPoint: " + blockExt.MinPoint.ToString() + " " +
					"MaxPoint: " + blockExt.MaxPoint.ToString();
				ed.WriteMessage(s);
			}
		}




		//[CommandMethod("OPSV")]
		public static void OpenSaveDwgFiles()
		{
			List<Sheet> dict = new List<Sheet>();
			try
			{
				var path = @"C:\Users\yusufzhon.marasulov\Desktop\test";
				DirectoryInfo d = new DirectoryInfo(path);
				FileInfo[] Files = d.GetFiles("*.dwg");
				foreach (FileInfo file in Files)
				{
					var fileName = Path.GetFileName(file.FullName);
					string dwgFlpath = file.FullName;
					using (Database db = new Database(false, true))
					{
						db.ReadDwgFile(dwgFlpath, FileOpenMode.OpenForReadAndAllShare, false, null);
						using (Transaction tr = db.TransactionManager.StartTransaction())
						{
							ObjectId mSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
							ObjectIdCollection idArray = PrintUtils.SelectDynamicBlockReferences(mSpaceId);
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

					using (Transaction tr = db.TransactionManager.StartTransaction())
					{
						ObjectId mSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
						ObjectIdCollection idArray = PrintUtils.SelectDynamicBlockReferences(mSpaceId);


						//TODO надо проверить предыдущий и нижние методы на поиск по Id
						PrintUtils.GetSheetsFromBlocks(Active.Editor, dict, tr, idArray);
						string selAttrName = "НОМЕР_ЛИСТА";
						PrintUtils.GetPrintParametersToPdf(Active.Editor, printModels, tr, idArray, selAttrName);


						//utils.CreateOnlyVed(dict);
						PrintUtils.CreateJsonFile(dict);

						foreach (var printModel in printModels)
						{
							//Active.Editor.WriteMessage("{0} печатаем ", printModel.DocNumber);
							PrintUtils.PlotCurrentLayout(printModel.DocNumber, printModel);
						}

						tr.Commit();
					}
				}
			}
		}

	}
}