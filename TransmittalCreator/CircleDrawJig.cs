using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using TransmittalCreator.DBCad;

namespace TransmittalCreator;

public class CircleDrawJig : DrawJig
{
	private readonly Document _dwg;
	private Editor _ed;
	private float factor = 1f;
	private JigPromptDistanceOptions jigDis;
	private Point3d p3d;
	private PromptDoubleResult ppr;

	public CircleDrawJig()
	{
		_dwg = Active.Document;
		_ed = _dwg.Editor;
	}

	public void DrawEntities()
	{
		var ed = Active.Editor;
		var db = HostApplicationServices.WorkingDatabase;

		using (var transaction = db.TransactionManager.StartTransaction())
		{
			try
			{
				var blockTable = transaction.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
				var bkTRec =
					transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as
						BlockTableRecord;
				var cPointOptions = new PromptPointOptions("Specify circle center point");
				var cPointResult = ed.GetPoint(cPointOptions);

				if (cPointResult.Status == PromptStatus.OK)
				{
					var circle = new Circle();
					p3d = cPointResult.Value;
					circle.Center = p3d;
					jigDis = new JigPromptDistanceOptions(Environment.NewLine + "Specify radius");
					jigDis.BasePoint = p3d;
					jigDis.UseBasePoint = true;
					jigDis.UserInputControls = UserInputControls.NoNegativeResponseAccepted;
					jigDis.Keywords.Add("Diameter");
					var jigResult = ed.Drag(this);
					//PromptDistanceOptions circleRadius = new PromptDistanceOptions(System.Environment.NewLine + "Specify radius");
					//circleRadius.AllowNegative = false;
					//circleRadius.Keywords.Add("Diameter");
					//circleRadius.BasePoint = p3d;
					//circleRadius.UseBasePoint = true;
					//PromptDoubleResult cRadiusResult = ed.GetDistance(circleRadius);

					if (jigResult.Status == PromptStatus.Keyword)
					{
						factor = 0.5f;
						jigDis.Message = Environment.NewLine + "Specify diameter";
						jigDis.Keywords.Clear();
						jigResult = ed.Drag(this);
					}

					if (jigResult.Status == PromptStatus.OK)
					{
						circle.Radius = ppr.Value * factor;
						var line = new Line();
						line.StartPoint = p3d;
						line.EndPoint = new Point3d(p3d.X + ppr.Value * factor, p3d.Y, p3d.Z);
						bkTRec.AppendEntity(circle);
						bkTRec.AppendEntity(line);
						transaction.AddNewlyCreatedDBObject(circle, true);
						transaction.AddNewlyCreatedDBObject(line, true);
						transaction.Commit();
						ed.WriteMessage("shape created");
					}

					//if (cRadiusResult.Status == PromptStatus.OK)
					//{
					//    circle.Radius = cRadiusResult.Value * factor;
					//    Line line = new Line();
					//    line.StartPoint = p3d;
					//    line.EndPoint = new Point3d(p3d.X + cRadiusResult.Value * factor, p3d.Y, p3d.Z);
					//    bkTRec.AppendEntity(circle);
					//    bkTRec.AppendEntity(line);
					//    transaction.AddNewlyCreatedDBObject(circle, true);
					//    transaction.AddNewlyCreatedDBObject(line, true);
					//    transaction.Commit();
					//    ed.WriteMessage("shape created");
					//}
				}
			}
			catch (SystemException e)
			{
				Console.WriteLine(e.Message);
				transaction.Abort();
			}
		}
	}

	protected override SamplerStatus Sampler(JigPrompts prompts)
	{
		ppr = prompts.AcquireDistance(jigDis);
		if (ppr.Value < 0.001) return SamplerStatus.NoChange;

		return SamplerStatus.OK;
	}

	protected override bool WorldDraw(WorldDraw draw)
	{
		draw.Geometry.Circle(p3d, ppr.Value * factor, Vector3d.ZAxis);
		draw.Geometry.WorldLine(p3d, new Point3d(p3d.X + ppr.Value * factor, p3d.Y, p3d.Z));
		return true;
	}
}