using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace TransmittalCreator
{
    public class SplinedLeaderJig : EntityJig
    {
        Point3dCollection _mPts;
        Point3d _mTempPoint;
        public static bool _mIsJigStarted;
        public SplinedLeaderJig()
            : base(new Leader())
        {
            _mPts = new Point3dCollection();
            Leader leader = Entity as Leader;
            leader.SetDatabaseDefaults();
            _mIsJigStarted = false;
            leader.IsSplined = true;
        }
        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            JigPromptPointOptions opts = new JigPromptPointOptions();
            opts.UserInputControls = (
                UserInputControls.Accept3dCoordinates |
                UserInputControls.NoNegativeResponseAccepted |
                UserInputControls.NullResponseAccepted);
            if (_mPts.Count >= 1)
            {
                opts.BasePoint = _mPts[_mPts.Count - 1];
                opts.UseBasePoint = true;
            }
            opts.Message = "\nSpecify leader vertex: ";
            PromptPointResult res = prompts.AcquirePoint(opts);
            if (_mTempPoint == res.Value)
            {
                return SamplerStatus.NoChange;
            }

            if (res.Status == PromptStatus.OK)
            {
                _mTempPoint = res.Value;
                return SamplerStatus.OK;
            }
            return SamplerStatus.Cancel;
        }
        protected override bool Update()
        {
            try
            {
                Leader leader = Entity as Leader;
                Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
                if (_mIsJigStarted)
                {
                    // Remove the last vertex since we are only jigging
                    leader.RemoveLastVertex();
                }
                Point3d lastVertex
                    = leader.VertexAt(leader.NumVertices - 1);
                if (!_mTempPoint.Equals(lastVertex))
                {
                    // Temporarily append the acquired point as a vertex
                    leader.AppendVertex(_mTempPoint);
                    _mIsJigStarted = true;
                }
            }
            catch (System.Exception ex)
            {
                Document doc
                    = Application.DocumentManager.MdiActiveDocument;
                doc.Editor.WriteMessage("\nException: " + ex.Message);
                return false;
            }
            return true;
        }
        public void addVertex()
        {
            Leader leader = Entity as Leader;
            leader.AppendVertex(_mTempPoint);
            _mPts.Add(_mTempPoint);
        }
        public void removeLastVertex()
        {
            Leader leader = Entity as Leader;
            if (_mPts.Count >= 1)
            {
                leader.RemoveLastVertex();
            }
        }
        public Entity getEntity()
        {
            return Entity;
        }
        
    }
}
