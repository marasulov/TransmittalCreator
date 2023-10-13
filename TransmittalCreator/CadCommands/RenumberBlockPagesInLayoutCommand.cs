using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using TransmittalCreator.Models.Layouts;
using TransmittalCreator.Services.Blocks;
using TransmittalCreator.Services.Files;
using TransmittalCreator.Utils;
using TransmittalCreator.View;
using TransmittalCreator.ViewModels;
using Active = TransmittalCreator.DBCad.Active;


namespace TransmittalCreator.CadCommands
{
	public class RenumberBlockPagesInLayoutCommand
	{
		[CommandMethod("RenumberBlocksInLayout")]
		public static void RenumberBlockPagesInLayout()
		{
			var renumberVm = new RenumberPagesViewModel();

			var renumberPagesView = new RenumberPagesInLayoutSpaceView(renumberVm);

			renumberVm.OnRequestClose += (s, e) => renumberPagesView.Close();
			renumberPagesView.ShowDialog();

			if (!renumberVm.ButtonClicked) return;

			Active.WriteMessageInDebug($"InsertTotalPages = {renumberVm.InsertTotalPages}, RenumberLayouts - {renumberVm.RenumberLayouts}, DeleteEmptyLayouts - {renumberVm.DeleteEmptyLayouts} ");

			//Active.Database.SaveAs(allPath, true, DwgVersion.Current, Active.Database.SecurityParameters);

			var blockNames = FileManager.GetBlockNames();
			var dynamicBlocks = new DynamicBlockFinder(blockNames);
			using (Transaction trans = Active.Database.TransactionManager.StartTransaction())
			{
				var layoutModelCollection = new LayoutModelCollection(dynamicBlocks);
				layoutModelCollection.ListLayouts("Model");
				Active.UsingTransaction(layoutModelCollection.CreateCollectionFromBlockNames);

				List<BlockReference> blockReferences = new List<BlockReference>();
				//BlockUtils.BlockAttributeEnumerator(blockReferences, new[] { "Page", "Лист" });
				layoutModelCollection.SetPrintModels(trans);
				layoutModelCollection.SetLayoutPlotSetting();
				layoutModelCollection.DynamicBlocks.GetLayoutsWithDynBlocks(trans, layoutModelCollection.LayoutModels);

				//layoutModelCollection.ExcludeEmptyLayout();

				var blocksList = layoutModelCollection.LayoutModels.Select(x => x.BlocksObjectId).ToList();
				if (blocksList.Count == 0) return;
				blockReferences.AddRange(blocksList.Select(blockRefId => blockRefId.GetObject(OpenMode.ForRead)).OfType<BlockReference>());

				var totalPages = 0;
				var pageStartNumber = 1;

				if (renumberVm.InsertTotalPages)
				{
					totalPages = blockReferences.Count;
				}

				if (renumberVm.RenumberStartValue != 1)
				{
					pageStartNumber = renumberVm.RenumberStartValue;
				}

				BlockUtils.BlockAttributeEnumerator(blockReferences, new[] { "page", "лист", "ЛИСТ2_СПЕЦ" }, pageStartNumber, totalPages, renumberVm.Prefix, renumberVm.Suffix);

				if (renumberVm.DeleteEmptyLayouts)
				{
					var notEmptyLayoutsList = layoutModelCollection.LayoutModels.Select(x => x.LayoutName).ToList();
					var db = Active.Database;
					var ed = Active.Editor;
					LayoutUtils.DeleteEmptyLayouts(db, ed, notEmptyLayoutsList);

				}

				if (renumberVm.RenumberLayouts)
				{
					string errorValue = string.Empty;
					try
					{
						//var layouts = DatabaseExtension.GetLayouts(Active.Database, mode:OpenMode.ForWrite);
						foreach (var layoutModel in layoutModelCollection.LayoutModels)
						{
							var attributeValue = GetAttributeValueByTag(layoutModel.BlocksObjectId, "page");
							var btr = trans.GetObject(layoutModel.Layout.BlockTableRecordId, OpenMode.ForRead) as BlockTableRecord;
							if (btr != null & btr.IsLayout)
							{
								var layout = trans.GetObject(btr.LayoutId, OpenMode.ForWrite) as Layout;
								if (layout != null & !layout.LayoutName.Equals("Model") & attributeValue != layoutModel.LayoutName)
								{
									errorValue = attributeValue;
									layout.LayoutName = attributeValue;
								}
							}
						}
					}
					catch (Autodesk.AutoCAD.Runtime.Exception aex)
					{
						if (aex.ErrorStatus == ErrorStatus.RenameLayoutAlreadyExists)
						{
							MessageBox.Show($"Перенумерование отменено Лист {errorValue} существуют, попробуйте сначала изменить с добавлением префикса");	
							return;
						}
						
					}

				}

				trans.Commit();
				Active.Editor.Regen();

				TrayItemBubbleWindow trayItemBubble = new TrayItemBubbleWindow();
				trayItemBubble.Text = "Листы перенумерованы";
				trayItemBubble.Title = "Перенумерование завершено";

				TrayItem trayItem = new TrayItem();

				Autodesk.AutoCAD.ApplicationServices.Application.StatusBar.TrayItems.Add(trayItem);
				trayItem.ShowBubbleWindow(trayItemBubble);
				Autodesk.AutoCAD.ApplicationServices.Application.StatusBar.Update();
			}

		}

		private static string GetAttributeValueByTag(ObjectId blockRefId, string tag)
		{
			var blockRef = blockRefId.GetObject(OpenMode.ForWrite) as BlockReference;

			foreach (ObjectId attRefId in blockRef.AttributeCollection)
			{
				AttributeReference attRef = attRefId.GetObject(OpenMode.ForRead) as AttributeReference;
				if (attRef != null && attRef.Tag.Equals(tag, StringComparison.InvariantCultureIgnoreCase))
				{
					return attRef.TextString;
				}
			}
			return null;
		}
	}
}

