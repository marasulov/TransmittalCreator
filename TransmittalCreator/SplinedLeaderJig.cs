using System;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace TransmittalCreator;

public class SplinedLeaderJig : EntityJig
{
	public static bool _mIsJigStarted;
	private readonly Point3dCollection _mPts;
	private Point3d _mTempPoint;

	public SplinedLeaderJig()
		: base(new Leader())
	{
		_mPts = new Point3dCollection();
		var leader = Entity as Leader;
		leader.SetDatabaseDefaults();
		_mIsJigStarted = false;
		leader.IsSplined = true;
	}

	protected override SamplerStatus Sampler(JigPrompts prompts)
	{
		var ed = Application.DocumentManager.MdiActiveDocument.Editor;
		var opts = new JigPromptPointOptions();
		opts.UserInputControls = UserInputControls.Accept3dCoordinates |
		                         UserInputControls.NoNegativeResponseAccepted |
		                         UserInputControls.NullResponseAccepted;
		if (_mPts.Count >= 1)
		{
			opts.BasePoint = _mPts[_mPts.Count - 1];
			opts.UseBasePoint = true;
		}

		opts.Message = "\nSpecify leader vertex: ";
		var res = prompts.AcquirePoint(opts);
		if (_mTempPoint == res.Value) return SamplerStatus.NoChange;

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
			var leader = Entity as Leader;
			var ed = Application.DocumentManager.MdiActiveDocument.Editor;
			if (_mIsJigStarted)
				// Remove the last vertex since we are only jigging
				leader.RemoveLastVertex();
			var lastVertex
				= leader.VertexAt(leader.NumVertices - 1);
			if (!_mTempPoint.Equals(lastVertex))
			{
				// Temporarily append the acquired point as a vertex
				leader.AppendVertex(_mTempPoint);
				_mIsJigStarted = true;
			}
		}
		catch (Exception ex)
		{
			var doc
				= Application.DocumentManager.MdiActiveDocument;
			doc.Editor.WriteMessage("\nException: " + ex.Message);
			return false;
		}

		return true;
	}

	public void addVertex()
	{
		var leader = Entity as Leader;
		leader.AppendVertex(_mTempPoint);
		_mPts.Add(_mTempPoint);
	}

	public void removeLastVertex()
	{
		var leader = Entity as Leader;
		if (_mPts.Count >= 1) leader.RemoveLastVertex();
	}

	public Entity getEntity()
	{
		return Entity;
	}
}