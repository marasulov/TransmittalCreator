using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using TransmittalCreator.DBCad;
using TransmittalCreator.Utils;
using TransmittalCreator.View;
using TransmittalCreator.ViewModels.ViewModels;

namespace TransmittalCreator.CadCommands
{
	public class RenumberBlockPagesCommand
	{
		[CommandMethod("RenumberBlocksInModel")]
		public static void RenumberBlockPagesInModel()
		{
			var renumberVm = new RenumberPagesInModelSpaceViewModel();
			var renumberPagesView = new RenumberPagesInModelSpaceView(renumberVm);
			renumberVm.OnRequestClose += (s, e) => renumberPagesView.Close();
			renumberPagesView.ShowDialog();

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
				blockReferences.AddRange(selSet.GetObjectIds().Select(blockRefId => blockRefId.GetObject(OpenMode.ForRead)).OfType<BlockReference>());

				BlockUtils.BlockAttributeEnumerator(blockReferences, new[] { "Page", "Лист" });
				transaction.Commit();
			}

		}
	}
}
