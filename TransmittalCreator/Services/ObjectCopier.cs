using System;
using System.IO;
using System.Windows;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using TransmittalCreator.DBCad;
using TransmittalCreator.Utils;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TransmittalCreator.Services;

public class ObjectCopier
{
	public ObjectCopier(ObjectId objectId)
	{
		GetBlockLengths(objectId);
	}

	public Point3d MinPoint3d { get; set; }

	public Point3d MaxPoint3d { get; set; }

	public ObjectIdCollection ObjectIdColls { get; set; }

	private void GetBlockLengths(ObjectId objectId)
	{
		var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;

		var acObjIdColl = new ObjectIdCollection();
		var acCurDb = doc.Database;

		using (var acLckDocCur = doc.LockDocument())
		{
			using (var Tx = Active.Database.TransactionManager.StartTransaction())
			{
				var bref = Tx.GetObject(objectId, OpenMode.ForWrite) as BlockReference;

				var extents3d = bref.GeometryExtentsBestFit();
				MinPoint3d = extents3d.MinPoint;
				MaxPoint3d = extents3d.MaxPoint;
				//Point3d blockPos3d = Autodesk.AutoCAD.Internal.Utils.UcsToDisplay(new Point3d(bref.Position.X, bref.Position.Y, 0), false);
				//blockPosition = new Point2d(blockPos3d.X, blockPos3d.Y);
				//DynamicBlockReferencePropertyCollection props = bref.DynamicBlockReferencePropertyCollection;
				//dimPoint2d = new Point2d(blockPosition.X + blockWidth, blockPosition.Y + blockHeidht);

				Tx.Commit();
			}
		}
	}

	/// <summary>
	/// Выбор объектов по пересекающему полигону
	/// </summary>
	/// <returns></returns>
	public ObjectIdCollection SelectCrossingWindow()
	{
		var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;

		var acObjIdColl = new ObjectIdCollection();
		var acCurDb = doc.Database;

		using (var acLckDocCur = doc.LockDocument())
		{
			// Start a transaction
			using (var acTrans = doc.TransactionManager.StartTransaction())
			{
				var psr = doc.Editor.SelectCrossingWindow(MinPoint3d, MaxPoint3d);
				if (psr.Status == PromptStatus.OK)
				{
					var cnt = 0;
					using (doc.TransactionManager.StartTransaction())
					{
						foreach (var oID in psr.Value.GetObjectIds())
						{
							var ent = (Entity)oID.GetObject(OpenMode.ForRead);
							cnt += 1;
							//doc.Editor.WriteMessage(ent.ToString(), "SelectCrossingWindow Entity  " + cnt.ToString());
							acObjIdColl.Add(oID);
						}
					}
				}

				// Save the new objects to the database
				acTrans.Commit();
			}

			// Unlock the document
		}

		return acObjIdColl;
	}


	public void CopyObjectsNewDatabases(ObjectIdCollection acObjIdColl, string dwgFilename)
	{

		var acDoc = Application.DocumentManager.MdiActiveDocument;
		var acCurDb = acDoc.Database;

		// Lock the new document
		try
		{
			using var db = new Database();

			using (Transaction acTrans = db.TransactionManager.StartTransaction())
			{
				// Open the Block table for read
				BlockTable acBlkTblNewDoc = acTrans.GetObject(db.BlockTableId,
					OpenMode.ForRead) as BlockTable;
				// Open the Block table record Model space for read
				BlockTableRecord acBlkTblRecNewDoc = acTrans.GetObject(acBlkTblNewDoc[BlockTableRecord.ModelSpace],
					OpenMode.ForRead) as BlockTableRecord;
				// Clone the objects to the new database
				
				var acIdMap = new IdMapping();
				acCurDb.WblockCloneObjects(acObjIdColl, acBlkTblRecNewDoc.ObjectId, acIdMap,
					DuplicateRecordCloning.Ignore, false);
				// Save the copied objects to the database

				acTrans.Commit();
			}

			var version = AcadUtils.GetCurrentAcadVersion();;
			
			db.SaveAs(dwgFilename, version);

		}
		catch (Exception e)
		{
			MessageBox.Show($"невозможно распечатать {e.Message}");

		}
	}

}