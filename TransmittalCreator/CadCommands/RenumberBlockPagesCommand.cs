using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using TransmittalCreator.DBCad;
using TransmittalCreator.Utils;

namespace TransmittalCreator.CadCommands
{
	public class RenumberBlockPagesCommand
	{
		[CommandMethod("RenumberBlocksInModel")]
		public static void RenumberBlockPagesInModel()
		{
			SelectionFilter sFilter = new SelectionFilter(
				new TypedValue[2]
				{
					new TypedValue(0, "INSERT"),
					new TypedValue(66,1)
				}
			);

			PromptSelectionOptions psOptions = new PromptSelectionOptions();
			psOptions.MessageForAdding = "\nВыберите блок с атрибутом : ";
			psOptions.MessageForRemoval = "\nУдалить из выбора : ";

			PromptSelectionResult selResult = Active.Editor.GetSelection(psOptions, sFilter);

			if (selResult.Status != PromptStatus.OK)
			{
				return;
			}

			SelectionSet selSet = selResult.Value;

			List<BlockReference> blockReferences = new List<BlockReference>();
			using (Transaction transaction = Active.Database.TransactionManager.StartTransaction())
			{
				foreach (var blockRefId in selSet.GetObjectIds())
				{
					var blockRef = blockRefId.GetObject(OpenMode.ForRead) as BlockReference;
					if (blockRef != null)
					{
						blockReferences.Add(blockRef);
					}
				}



				BlockUtils.BlockAttributeEnumerator(blockReferences, new[] { "Page", "Лист" });
				transaction.Commit();
			}

		}
	}
}
