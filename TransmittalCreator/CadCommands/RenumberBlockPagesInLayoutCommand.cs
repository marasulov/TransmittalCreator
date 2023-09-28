using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using TransmittalCreator.DBCad;
using TransmittalCreator.Models.Layouts;
using TransmittalCreator.Services;
using TransmittalCreator.Services.Blocks;
using TransmittalCreator.Services.Files;

namespace TransmittalCreator.CadCommands
{

	public class RenumberBlockPagesInLayoutCommand
	{
		[CommandMethod("ListLayouts")]
		public static void ListLayoutsMethod()
		{
			LayoutManager layoutMgr = LayoutManager.Current;
			Active.WriteMessage(
				String.Format
				(
					"{0}Active Layout is : {1}",
					Environment.NewLine,
					layoutMgr.CurrentLayout
				)
			);
			Active.WriteMessage
			(
				String.Format
				(
					"{0}Number of Layouts: {1}{0}List of all Layouts:",
					Environment.NewLine,
					layoutMgr.LayoutCount
				)
			);
			using (Transaction tr
				= Active.Database.TransactionManager.StartTransaction())
			{
				DBDictionary layoutDic
					= tr.GetObject(
						Active.Database.LayoutDictionaryId,
						OpenMode.ForRead,
						false
					) as DBDictionary;
				foreach (DBDictionaryEntry entry in layoutDic)
				{
					ObjectId layoutId = entry.Value;
					Layout layout
						= tr.GetObject(
							layoutId,
							OpenMode.ForRead
						) as Layout;
					Active.WriteMessage(
						String.Format(
							"{0}--> {1}",
							Environment.NewLine,
							layout.LayoutName
						)
					);
				}
				tr.Commit();
			}
		}

		[CommandMethod("RenumberBlocksInLayout")]
		public static void RenumberBlockPagesInLayout()
		{


			IFileManager fileManager = new FileManager();
			string allPath = fileManager.GetFilePath();
			//Active.Database.SaveAs(allPath, true, DwgVersion.Current, Active.Database.SecurityParameters);
			var confFilePath =
				Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,
					"conf.json");

#if DEBUG
			confFilePath = "D:\\data\\source\\repos\\TransmittalCreator\\TransmittalCreator\\conf.json";
#endif
			string jsonFile = File.ReadAllText(confFilePath);
			Params deserializeObject = JsonConvert.DeserializeObject<Params>(jsonFile);
			
			string[] blockNames = deserializeObject.StampName;
			DynamicBlockFinder dynamicBlocks = new DynamicBlockFinder(blockNames);
			LayoutModelCollection layoutModelCollection = new LayoutModelCollection(dynamicBlocks);
			layoutModelCollection.ListLayouts("Model");
			layoutModelCollection.CreateLayoutCollection();

			string[] viewNames = { "Форма 3 ГОСТ Р 21.1101-2009 M25", "Форма 3 ГОСТ Р 21.1101-2009" };
			var packageCreator = new PrintPackageCreator(layoutModelCollection, viewNames);

			//LayoutTreeView window = new LayoutTreeView(packageCreator.PrintPackageModels);
			//Autodesk.AutoCAD.ApplicationServices.Core.Application.ShowModalWindow(window);

			//if (!window.isClicked) return;

			//var printPackages = window._printPackages;
			//packageCreator.PrintPackageModels = printPackages;
			//var sheetType = SheetType.MultiPdf;

			//packageCreator.PublishAllPackages();

			//var blockIds = printPackages.SelectMany(x => x.Layouts.Select(y => y.BlocksObjectId)).ToArray();


			//List<Sheet> sheets = new List<Sheet>();
			//using (Transaction trans = Active.Database.TransactionManager.StartTransaction())
			//{
			//	PrintUtils.GetSheetsFromBlocks(Active.Editor, sheets, trans, new ObjectIdCollection(blockIds));
			//	PrintUtils.CreateJsonFile(sheets);
			//}

		}
	}
}
