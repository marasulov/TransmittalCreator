using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using TransmittalCreator.DBCad;

namespace TransmittalCreator
{
    public class LineJigger : EntityJig
    {
        public Point3d endPnt = new Point3d();

        public LineJigger(Line line)
            : base(line)
        {
        }

        protected override bool Update()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;

            (Entity as Line).EndPoint = endPnt;
            return true;
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;

            JigPromptPointOptions prOptions1 = new JigPromptPointOptions("\nNext point:");
            prOptions1.BasePoint = (Entity as Line).StartPoint;
            prOptions1.UseBasePoint = true;
            prOptions1.UserInputControls = UserInputControls.Accept3dCoordinates
                | UserInputControls.AnyBlankTerminatesInput
                | UserInputControls.GovernedByOrthoMode
                | UserInputControls.GovernedByUCSDetect
                | UserInputControls.UseBasePointElevation
                | UserInputControls.InitialBlankTerminatesInput
                | UserInputControls.NullResponseAccepted;
            PromptPointResult prResult1 = prompts.AcquirePoint(prOptions1);
            if (prResult1.Status == PromptStatus.Cancel)
                return SamplerStatus.Cancel;

            if (prResult1.Value.Equals(endPnt))
            {
                return SamplerStatus.NoChange;
            }

            endPnt = prResult1.Value;
            return SamplerStatus.OK;
        }



        public static bool Jig()
        {
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;

                PromptPointResult ppr = doc.Editor.GetPoint("\nStart point:");
                if (ppr.Status != PromptStatus.OK)
                    return false;
                Point3d pt = ppr.Value;
                Line line = new Line(pt, pt);
                line.TransformBy(doc.Editor.CurrentUserCoordinateSystem);

                LineJigger jigger = new LineJigger(line);
                PromptResult pr = doc.Editor.Drag(jigger);
                if (pr.Status == PromptStatus.OK)
                {
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = tr.GetObject(db.BlockTableId,
                            OpenMode.ForRead) as BlockTable;
                        BlockTableRecord modelSpace = tr.GetObject(
                            bt[BlockTableRecord.ModelSpace],
                            OpenMode.ForWrite) as BlockTableRecord;

                        modelSpace.AppendEntity(jigger.Entity);
                        tr.AddNewlyCreatedDBObject(jigger.Entity, true);
                        tr.Commit();
                    }
                }
                else
                {
                    line.Dispose();
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
