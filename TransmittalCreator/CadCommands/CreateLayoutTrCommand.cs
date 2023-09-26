using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TransmittalCreator.Models.Layouts;
using TransmittalCreator.Services.Blocks;
using TransmittalCreator.Services.Files;
using TransmittalCreator.Services;
using TransmittalCreator.Utils;
using TransmittalCreator.View;
using TransmittalCreator.DBCad;

namespace TransmittalCreator.CadCommands
{
	public class CreateLayoutTrCommand
	{
		[CommandMethod("CrLayTr")]
		public void CreateTransmitallFromLayout()
		{
			IFileManager fileManager = new FileManager();
			string allPath = fileManager.GetFilePath();
			Active.Database.SaveAs(allPath, true, DwgVersion.Current, Active.Database.SecurityParameters);
			string jsonFile = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "conf.json"));
			Params deserializeObject = JsonConvert.DeserializeObject<Params>(jsonFile);


			string[] blockNames = deserializeObject.StampName;
			DynamicBlockFinder dynamicBlocks = new DynamicBlockFinder(blockNames);
			LayoutModelCollection layoutModelCollection = new LayoutModelCollection(dynamicBlocks);
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
	}
}
