using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using TransmittalCreator.DBCad;

namespace TransmittalCreator;

public class EntityRotateJigger : EntityJig
{
	#region Constructors

	public EntityRotateJigger(Entity ent, Point3d basePoint) : base(ent)
	{
		mBasePoint = basePoint;
	}

	#endregion


	public static bool Jig(Entity ent, Point3d basePt)
	{
		EntityRotateJigger jigger = null;
		try
		{
			jigger = new EntityRotateJigger(ent, basePt);
			PromptResult pr;
			do
			{
				pr = Active.Editor.Drag(jigger);
				if (pr.Status == PromptStatus.Keyword)
				{
					// Add keyword handling code below
				}
				else
				{
					jigger.mCurJigFactorIndex++;
				}
			} while (pr.Status != PromptStatus.Cancel && pr.Status != PromptStatus.Error &&
			         jigger.mCurJigFactorIndex <= 1);

			if (pr.Status == PromptStatus.Cancel || pr.Status == PromptStatus.Error)
			{
				if (jigger != null && jigger.Entity != null)
					jigger.Entity.Dispose();

				return false;
			}

			return true;
		}
		catch
		{
			if (jigger != null && jigger.Entity != null)
				jigger.Entity.Dispose();

			return false;
		}
	}

	#region Fields

	public int mCurJigFactorIndex = 1;

	private double mRotation; // Factor #1

	private Point3d mBasePoint;
	private Matrix3d mLastMatrix = new();
	private double mLastAngle;

	#endregion

	#region Properties

	private Editor Editor => Active.Editor;

	private Matrix3d UCS => Editor.CurrentUserCoordinateSystem;

	#endregion

	#region Overrides

	protected override bool Update()
	{
		var basePt = new Point3d(mBasePoint.X, mBasePoint.Y, mBasePoint.Z);
		var mat = Matrix3d.Rotation(mRotation - mLastAngle, Vector3d.ZAxis.TransformBy(UCS),
			basePt.TransformBy(UCS));
		Entity.TransformBy(mat);

		mLastAngle = mRotation;

		return true;
	}

	protected override SamplerStatus Sampler(JigPrompts prompts)
	{
		switch (mCurJigFactorIndex)
		{
			case 1:
				var prOptions1 = new JigPromptAngleOptions("\nRotation angle:");
				prOptions1.BasePoint = mBasePoint;
				prOptions1.UseBasePoint = true;
				var prResult1 = prompts.AcquireAngle(prOptions1);

				if (prResult1.Status == PromptStatus.Cancel) return SamplerStatus.Cancel;

				if (prResult1.Value.Equals(mRotation))
				{
					return SamplerStatus.NoChange;
				}

				mRotation = prResult1.Value;
				return SamplerStatus.OK;
		}

		return SamplerStatus.OK;
	}

	#endregion
}