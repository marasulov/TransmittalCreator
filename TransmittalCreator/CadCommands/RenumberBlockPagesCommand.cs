using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Autodesk.AutoCAD.Geometry;
using DocumentFormat.OpenXml.Spreadsheet;
using TransmittalCreator.DBCad;
using TransmittalCreator.Utils;
using TransmittalCreator.View;
using TransmittalCreator.ViewModels.ViewModels;
using AcadUtils = Autodesk.AutoCAD.Internal.Utils;

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

			if (!renumberVm.ButtonClicked) return;
			using Transaction tr = Active.Database.TransactionManager.StartTransaction();

			SelectionFilter sFilter = new SelectionFilter(
				new TypedValue[2]
				{
					new TypedValue(0, "INSERT"),
					new TypedValue(66,1)
				}
			);

			PromptSelectionOptions psOptions = new PromptSelectionOptions();
			psOptions.MessageForAdding = "\nSelect stamp block : ";
			psOptions.MessageForRemoval = "\nRemove from selection : ";

			var selResult = Active.Editor.GetSelection(psOptions, sFilter);

			if (selResult.Status != PromptStatus.OK)
			{
				return;
			}

			SelectionSet selSet = selResult.Value;
			ObjectId[] allSelectedBlocks = selSet.GetObjectIds();
			
			var blocksByName = BlockUtils.GetDynBlocksByName(allSelectedBlocks, tr);
			ObjectId[] selectedBlocksId = new ObjectId[blocksByName.Count];

			blocksByName.CopyTo(selectedBlocksId, 0);

			var totalPages = 0;
			var pageStartNumber = 1;
			if (renumberVm.InsertTotalPages)
			{
				totalPages = blocksByName.Count;
			}

			if (renumberVm.RenumberStartValue != 1)
			{
				pageStartNumber = renumberVm.RenumberStartValue;
			}

			List<BlockReference> blockReferences = new List<BlockReference>();
			blockReferences.AddRange(selectedBlocksId.Select(blockRefId => blockRefId.GetObject(OpenMode.ForRead)).OfType<BlockReference>());

			if (renumberVm.IsX)
			{
				blockReferences.Sort((x,y) => GetUcsPoint3d(x).X.CompareTo(GetUcsPoint3d(y).X));
			}

			if (renumberVm.IsY)
			{
				blockReferences.Sort((x,y) => GetUcsPoint3d(x).Y.CompareTo(GetUcsPoint3d(y).Y));
			}
			
			BlockUtils.BlockAttributeEnumerator(blockReferences, new[] { "page", "лист", "ЛИСТ2_СПЕЦ" }, pageStartNumber, totalPages, renumberVm.Prefix, renumberVm.Suffix);

			tr.Commit();
		}

		private static Point3d GetUcsPoint3d(BlockReference blockReference)
		{
			return AcadUtils.UcsToDisplay(new Point3d(blockReference.Position.X, blockReference.Position.Y, 0), false);
		}

	}
}
