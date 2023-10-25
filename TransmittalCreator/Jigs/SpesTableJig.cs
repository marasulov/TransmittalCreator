using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using TransmittalCreator.DBCad;
using TransmittalCreator.Utils;

namespace TransmittalCreator
{
	public class SpesTableJig : EntityJig
	{
		private Point3d _position;
		
		public SpesTableJig(List<Sheet> dict) : base(new Table())
		{
			Table table = Entity as Table;
			PrintUtils.CreateSpesTable(table, dict,1, _position);
		}

		protected override SamplerStatus Sampler(JigPrompts prompts)
		{
			var prOptions1 = new JigPromptPointOptions("\nEnter table insertion point: ");

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

			if (prResult1.Value.Equals(_position)) return SamplerStatus.NoChange;

			_position = prResult1.Value;
			return SamplerStatus.OK;
		}

		protected override bool Update()
		{

			try
			{
				Table table = Entity as Table;
				if (table.Position.DistanceTo(_position) > 1.0e-4)
				{
					table.Position = _position;
					return true;
				}
			}
			catch (Exception e)
			{
				Active.WriteMessage($"{e.ErrorStatus} ");
			}
			return false;
		}

		public Entity GetEntity()
		{
			return Entity;
		}
	}
}
