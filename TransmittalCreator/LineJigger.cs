using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace TransmittalCreator;

public class LineJigger : EntityJig
{
	public Point3d endPnt;

	public LineJigger(Line line)
		: base(line)
	{
	}

	protected override bool Update()
	{
		var doc = Application.DocumentManager.MdiActiveDocument;

		(Entity as Line).EndPoint = endPnt;
		return true;
	}

	protected override SamplerStatus Sampler(JigPrompts prompts)
	{
		var doc = Application.DocumentManager.MdiActiveDocument;

		var prOptions1 = new JigPromptPointOptions("\nNext point:");
		prOptions1.BasePoint = (Entity as Line).StartPoint;
		prOptions1.UseBasePoint = true;
		prOptions1.UserInputControls = UserInputControls.Accept3dCoordinates
		                               | UserInputControls.AnyBlankTerminatesInput
		                               | UserInputControls.GovernedByOrthoMode
		                               | UserInputControls.GovernedByUCSDetect
		                               | UserInputControls.UseBasePointElevation
		                               | UserInputControls.InitialBlankTerminatesInput
		                               | UserInputControls.NullResponseAccepted;
		var prResult1 = prompts.AcquirePoint(prOptions1);
		if (prResult1.Status == PromptStatus.Cancel)
			return SamplerStatus.Cancel;

		if (prResult1.Value.Equals(endPnt)) return SamplerStatus.NoChange;

		endPnt = prResult1.Value;
		return SamplerStatus.OK;
	}


	public static bool Jig()
	{
		try
		{
			var doc = Application.DocumentManager.MdiActiveDocument;
			var db = doc.Database;

			var ppr = doc.Editor.GetPoint("\nStart point:");
			if (ppr.Status != PromptStatus.OK)
				return false;
			var pt = ppr.Value;
			var line = new Line(pt, pt);
			line.TransformBy(doc.Editor.CurrentUserCoordinateSystem);

			var jigger = new LineJigger(line);
			var pr = doc.Editor.Drag(jigger);
			if (pr.Status == PromptStatus.OK)
			{
				using (var tr = db.TransactionManager.StartTransaction())
				{
					var bt = tr.GetObject(db.BlockTableId,
						OpenMode.ForRead) as BlockTable;
					var modelSpace = tr.GetObject(
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