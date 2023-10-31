using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Windows;
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

	private void GetBlockLengths(ObjectId objectId)
	{
		using (var acLckDocCur = Active.Document.LockDocument())
		{
			using (var tx = Active.Database.TransactionManager.StartTransaction())
			{
				var bref = tx.GetObject(objectId, OpenMode.ForWrite) as BlockReference;

				var extents3d = bref.GeometryExtentsBestFit();
				MinPoint3d = extents3d.MinPoint;
				MaxPoint3d = extents3d.MaxPoint;

				tx.Commit();
			}
		}
	}

	/// <summary>
	/// Выбор объектов по пересекающему полигону
	/// </summary>
	/// <returns></returns>
	public ObjectIdCollection SelectCrossingWindow()
	{
		var acObjIdColl = new ObjectIdCollection();
		
		using (var acLckDocCur = Active.Document.LockDocument())
		{
			// Start a transaction
			using (var acTrans = Active.Document.TransactionManager.StartTransaction())
			{
				var psr = Active.Editor.SelectCrossingWindow(MinPoint3d, MaxPoint3d);
				if (psr.Status == PromptStatus.OK)
				{
					var cnt = 0;
					using (Active.Document.TransactionManager.StartTransaction())
					{
						foreach (var oID in psr.Value.GetObjectIds())
						{
							var ent = (Entity)oID.GetObject(OpenMode.ForRead);
							cnt += 1;
							
							acObjIdColl.Add(oID);
						}
					}
				}
				acTrans.Commit();
			}
		}

		return acObjIdColl;
	}


	public void CopyObjectsNewDatabases(ObjectIdCollection acObjIdColl, string dwgFilename)
	{ 
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
				Active.Database.WblockCloneObjects(acObjIdColl, acBlkTblRecNewDoc.ObjectId, acIdMap,
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