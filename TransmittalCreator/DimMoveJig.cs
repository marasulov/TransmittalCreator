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
  public class DimMoveJig : DrawJig
    {
        private ObjectId _entityId;
        private Point3d _basePoint = Point3d.Origin;
        private Point3d _prevPoint = Point3d.Origin;
        private Point3d _movingPoint = Point3d.Origin;
 
        private Document _dwg;
        private Editor _ed;
 
        private Entity _entity;
        private AlignedDimension _dim = null;
 
        public DimMoveJig() : base()
        {
            _dwg = Active.Document;
            _ed = _dwg.Editor;
        }
 
        public void Move()
        {
            if (!SelectEntity(out ObjectId entId, out Point3d basePt)) return;
 
            _entityId = entId;
            _basePoint = basePt;
            _prevPoint = basePt;
            _movingPoint = basePt;
 
            var offset = GetDimLineOffset(_entityId);
            var dimTextPt = GetMidOffsetPoint(_basePoint, _movingPoint, offset);
 
            try
            {
                _dim = new AlignedDimension(
                    _basePoint, 
                    _movingPoint, 
                    dimTextPt, 
                    "0000", 
                    _dwg.Database.DimStyleTableId);
 
                using (var tran = _dwg.TransactionManager.StartTransaction())
                {
                    _entity = (Entity)tran.GetObject(_entityId, OpenMode.ForWrite);
 
                    var res = _ed.Drag(this);
                    if (res.Status == PromptStatus.OK)
                    {
                        tran.Commit();
                    }
                    else
                    {
                        tran.Abort();
                    }
                }
            }
            finally
            {
                if (_dim != null) _dim.Dispose();
            }
 
            _ed.WriteMessage("\n");
        }
 
        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            var status = SamplerStatus.NoChange;
 
            var opt = new JigPromptPointOptions(
                "\nMove to:");
            opt.UseBasePoint = true;
            opt.BasePoint = _basePoint;
            opt.Cursor = CursorType.RubberBand;
 
            var res = prompts.AcquirePoint(opt);
            if (res.Status== PromptStatus.OK)
            {
                if (!res.Value.Equals(_movingPoint))
                {
                    var mt = Matrix3d.Displacement(_movingPoint.GetVectorTo(res.Value));
                    _entity.TransformBy(mt);
 
                    _dim.XLine2Point = res.Value;
                    var dist = _basePoint.DistanceTo(res.Value).ToString("######0.000");
                    _dim.DimensionText = dist.ToString();
 
                    _movingPoint = res.Value;
 
                    status = SamplerStatus.OK;
                }
            }
            else
            {
                status = SamplerStatus.Cancel;
            }
 
            return status;
        }
 
        protected override bool WorldDraw(WorldDraw draw)
        {
            _dim.RecomputeDimensionBlock(true);
            draw.Geometry.Draw(_dim);
            
            return draw.Geometry.Draw(_entity);
        }
 
        #region private methods
 
        private bool SelectEntity(out ObjectId entId, out Point3d basePoint)
        {
            entId = ObjectId.Null;
            basePoint = Point3d.Origin;
 
            var ok = true;
            var eRes = _ed.GetEntity("\nSelect entity to move:");
            if (eRes.Status== PromptStatus.OK)
            {
                entId = eRes.ObjectId;
 
                var pRes = _ed.GetPoint("\nSelect base point:");
                if (pRes.Status== PromptStatus.OK)
                {
                    basePoint=pRes.Value;
                }
                else
                {
                    ok = false;
                }
            }
            else
            {
                ok = false;
            }
 
            if (!ok) _ed.WriteMessage("\n*Cancel*\n");
 
            return ok;
        }
 
        private double GetDimLineOffset(ObjectId entId)
        {
            Extents3d ext;
 
            using (var tran = entId.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var ent = (Entity)tran.GetObject(entId, OpenMode.ForRead);
                ext = ent.GeometricExtents;
            }
 
            var w = ext.MaxPoint.X - ext.MinPoint.X;
            var h = ext.MaxPoint.Y - ext.MinPoint.Y;
 
            return (w > h ? w : h) / 5.0;
        }
 
        private Point3d GetMidOffsetPoint(Point3d startPt, Point3d endPt, double offset)
        {
            double ang;
            using (var line = new Line(startPt, endPt))
            {
                ang = line.Angle;
            }
 
            if (startPt.X < endPt.X)
                ang = ang + Math.PI / 2.0;
            else
                ang = ang - Math.PI / 2.0;
 
            var x = Math.Cos(ang) * offset;
            var y = Math.Sin(ang) * offset;
 
            var midX = (startPt.X + endPt.X) / 2.0;
            var midY = (startPt.Y + endPt.Y) / 2.0;
            var midZ = (startPt.Z + endPt.Z) / 2.0;
            var midPt = new Point3d(midX, midY, midZ);
 
            return new Point3d(midPt.X + x, midPt.Y + y, midPt.Z);
        }
 
        #endregion
    }
}
