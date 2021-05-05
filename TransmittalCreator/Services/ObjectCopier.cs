using System;
using System.IO;
using System.Reflection;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DV2177.Common;

namespace TransmittalCreator.Services
{
    public class ObjectCopier
    {
        private Point3d _minPoint3d;
        public Point3d MinPoint3d
        {
            get => _minPoint3d;
            set => _minPoint3d = value;
        }
        private Point3d _maxPoint3d;
        public Point3d MaxPoint3d
        {
            get => _maxPoint3d;
            set => _maxPoint3d = value;
        }

        public ObjectIdCollection ObjectIdColls { get; set; }

        public ObjectCopier(ObjectId objectId)
        {
            GetBlockLengths(objectId);
        }

        private void GetBlockLengths(ObjectId objectId)
        {

            Document doc = Application.DocumentManager.MdiActiveDocument;

            ObjectIdCollection acObjIdColl = new ObjectIdCollection();
            Database acCurDb = doc.Database;

            using (DocumentLock acLckDocCur = doc.LockDocument())
            {
                using (Transaction Tx = Active.Database.TransactionManager.StartTransaction())
                {
                    BlockReference bref = Tx.GetObject(objectId, OpenMode.ForWrite) as BlockReference;

                    Extents3d extents3d = bref.GeometryExtentsBestFit();
                    this.MinPoint3d = extents3d.MinPoint;
                    this.MaxPoint3d = extents3d.MaxPoint;
                    //Point3d blockPos3d = Autodesk.AutoCAD.Internal.Utils.UcsToDisplay(new Point3d(bref.Position.X, bref.Position.Y, 0), false);
                    //blockPosition = new Point2d(blockPos3d.X, blockPos3d.Y);
                    //DynamicBlockReferencePropertyCollection props = bref.DynamicBlockReferencePropertyCollection;
                    //dimPoint2d = new Point2d(blockPosition.X + blockWidth, blockPosition.Y + blockHeidht);

                    Tx.Commit();
                }

            }
        }

        public ObjectIdCollection SelectCrossingWindow()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;

            ObjectIdCollection acObjIdColl = new ObjectIdCollection();
            Database acCurDb = doc.Database;

            using (DocumentLock acLckDocCur = doc.LockDocument())
            {
                // Start a transaction
                using (Transaction acTrans = doc.TransactionManager.StartTransaction())
                {

                    PromptSelectionResult psr = doc.Editor.SelectCrossingWindow(this.MinPoint3d, this.MaxPoint3d);
                    if (psr.Status == PromptStatus.OK)
                    {
                        int cnt = 0;
                        using (doc.TransactionManager.StartTransaction())
                        {
                            foreach (ObjectId oID in psr.Value.GetObjectIds())
                            {
                                Entity ent = (Entity)oID.GetObject(Autodesk.AutoCAD.DatabaseServices.OpenMode.ForRead);
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
            //ObjectIdCollection acObjIdColl = new ObjectIdCollection();
            // Get the current document and database
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            // Lock the new document
            try
            {
                using (Database db = new Database())
                {
                    using (Transaction acTrans = db.TransactionManager.StartTransaction())
                    {
                        // Open the Block table for read
                        BlockTable acBlkTblNewDoc;
                        acBlkTblNewDoc = acTrans.GetObject(db.BlockTableId,
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
                        //ZoomObjects(acObjIdColl);
                        acTrans.Commit();
                    }
                    db.SaveAs(dwgFilename, db.OriginalFileVersion);
                    FileInfo fi = new FileInfo(dwgFilename);
                    Active.Editor.WriteMessage("\nSize of {0}: {1}-{2}", dwgFilename, fi.Length, db.OriginalFileVersion);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
