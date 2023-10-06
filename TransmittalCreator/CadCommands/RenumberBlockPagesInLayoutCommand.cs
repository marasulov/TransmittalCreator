using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Windows;
using Gile.AutoCAD.Extension;
using TransmittalCreator.Models.Layouts;
using TransmittalCreator.Services;
using TransmittalCreator.Services.Blocks;
using TransmittalCreator.Services.Files;
using TransmittalCreator.Utils;
using TransmittalCreator.View;
using TransmittalCreator.ViewModels;
using Active = TransmittalCreator.DBCad.Active;
using DocumentFormat.OpenXml.Drawing.Diagrams;


namespace TransmittalCreator.CadCommands
{

	public class RenumberBlockPagesInLayoutCommand
	{
		public static void DeleteEmptyLayouts(Database db, Editor ed, List<string> notEmptyList)
		{
			var layoutManager = LayoutManager.Current;
			var layoutNamed = GetTabOrderedLayoutNames(db);
			if (layoutNamed.Count == notEmptyList.Count) return;
			foreach (string layoutName in GetTabOrderedLayoutNames(db))
			{
				if (layoutName != "Model" & !notEmptyList.Contains(layoutName))
					layoutManager.DeleteLayout(layoutName);
			}
			ed.Regen();
		}

		public static List<string> GetTabOrderedLayoutNames(Database db)
		{
			if (db == null)
				return null;

			var tabOrderedLayouts = new Dictionary<int, string>();
			using (Transaction tr = db.TransactionManager.StartTransaction())
			{
				DBDictionary layoutDict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
				foreach (DBDictionaryEntry layoutEntry in layoutDict)
				{
					Layout layout = (Layout)tr.GetObject(layoutEntry.Value, OpenMode.ForRead);
					if (!layout.ModelType)
						tabOrderedLayouts[layout.TabOrder] = layout.LayoutName;
				}
				tr.Commit();
			}
			return tabOrderedLayouts.OrderBy(n => n.Key).Select(n => n.Value).ToList();
		}

		[CommandMethod("RenumberBlocksInLayout")]
		public static void RenumberBlockPagesInLayout()
		{
			RenumberPagesViewModel renumberVm = new RenumberPagesViewModel();

			var renumberPagesView = new RenumberPagesInLayoutSpaceView(renumberVm);

			renumberVm.OnRequestClose += (s, e) => renumberPagesView.Close();
			renumberPagesView.ShowDialog();

			if (!renumberVm.ButtonClicked) return;

			Active.WriteMessageInDebug($"InsertTotalPages = {renumberVm.InsertTotalPages}, RenumberLayouts - {renumberVm.RenumberLayouts}, DeleteEmptyLayouts - {renumberVm.DeleteEmptyLayouts} ");

			IFileManager fileManager = new FileManager();
			string allPath = fileManager.GetFilePath();
			//Active.Database.SaveAs(allPath, true, DwgVersion.Current, Active.Database.SecurityParameters);
			var confFilePath = Path.Combine(Path.GetDirectoryName(
				Assembly.GetExecutingAssembly().Location) ?? string.Empty, "conf.json");

#if DEBUG
			confFilePath = "D:\\data\\source\\repos\\TransmittalCreator\\TransmittalCreator\\conf.json";
#endif
			string jsonFile = File.ReadAllText(confFilePath);
			Params deserializeObject = JsonConvert.DeserializeObject<Params>(jsonFile);
			using (Transaction trans = Active.Database.TransactionManager.StartTransaction())
			{
				string[] blockNames = deserializeObject.StampName;
				var dynamicBlocks = new DynamicBlockFinder(blockNames);
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
					DeleteEmptyLayouts(db,ed,notEmptyLayoutsList);

				}

				if (renumberVm.RenumberLayouts)
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
								layout.LayoutName = attributeValue;
							}
						}

						//var blockRef = layoutModel.BlocksObjectId.GetObject(OpenMode.ForWrite) as BlockReference;
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

