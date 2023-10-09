using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TransmittalCreator.DBCad;
using TransmittalCreator.Models.Layouts;
using TransmittalCreator.Services;
using TransmittalCreator.Services.Blocks;
using TransmittalCreator.Services.Files;
using TransmittalCreator.Utils;
using TransmittalCreator.View;
using Exception = Autodesk.AutoCAD.Runtime.Exception;

namespace TransmittalCreator.CadCommands
{
	public class CreateLayoutTrCommand
	{
		[CommandMethod("CrLayTr")]
		public void CreateTransmitallFromLayout()
		{
			//IFileManager fileManager = new FileManager();
			
			//Active.Database.SaveAs(allPath, true, DwgVersion.Current, Active.Database.SecurityParameters);
			//string jsonFile = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "conf.json"));
			//Params deserializeObject = JsonConvert.DeserializeObject<Params>(jsonFile);
			try
			{
				string allPath = FileManager.GetFilePath();
				//Active.Database.SaveAs(allPath, true, DwgVersion.Current, Active.Database.SecurityParameters);
				string[] blockNames = FileManager.GetBlockNames();
				//DynamicBlockFinder dynamicBlocks = new DynamicBlockFinder(blockNames);
				LayoutModelCollection layoutModelCollection = new LayoutModelCollection(blockNames);
				layoutModelCollection.ListLayouts("Model");
				layoutModelCollection.CreateLayoutCollection();

				string[] viewNames = { "Форма 3 ГОСТ Р 21.1101-2009 M25", "Форма 3 ГОСТ Р 21.1101-2009" };
				var packageCreator = new PrintPackageCreator(layoutModelCollection, viewNames);

				LayoutTreeView window = new LayoutTreeView(packageCreator.PrintPackageModels);
				Autodesk.AutoCAD.ApplicationServices.Core.Application.ShowModalWindow(window);

				if (!window.isClicked) return;

				var printPackages = window._printPackages;
				packageCreator.PrintPackageModels = printPackages;
				var sheetType = SheetType.MultiPdf;

				packageCreator.PublishAllPackages();

				var blockIds = printPackages.SelectMany(x => x.Layouts.Select(y => y.BlocksObjectId)).ToArray();

				List<Sheet> sheets = new List<Sheet>();
				using (Transaction trans = Active.Database.TransactionManager.StartTransaction())
				{
					PrintUtils.GetSheetsFromBlocks(Active.Editor, sheets, trans, new ObjectIdCollection(blockIds));
					PrintUtils.CreateJsonFile(sheets);
				}
			}
			catch (Exception e)
			{
				Active.WriteMessage($"Произошла ошибка {e.ErrorStatus}");
			}
			
		}
	}
}
