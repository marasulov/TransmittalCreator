using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;
using TransmittalCreator.DBCad;
using TransmittalCreator.Models.Layouts;
using TransmittalCreator.Services;
using TransmittalCreator.Services.Blocks;
using TransmittalCreator.Services.Files;
using TransmittalCreator.Utils;
using TransmittalCreator.View;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace TransmittalCreator.CadCommands;

public class CreateLayoutTrCommand
{
	[CommandMethod("CrLayTr")]
	public void CreateTransmitallFromLayout()
	{
		try
		{
			using (var trans = Active.Database.TransactionManager.StartTransaction())
			{
				Active.Document.SendStringToExecute("_QSAVE ", true, false, false);
				var blockNames = FileManager.GetBlockNames();
				var dynamicBlocks = new DynamicBlockFinder(blockNames);
				var layoutModelCollection = new LayoutModelCollection(dynamicBlocks);

				layoutModelCollection.ListLayouts("Model");
				layoutModelCollection.CreateLayoutCollection();
				layoutModelCollection.SetPrintModels(trans);
				layoutModelCollection.SetLayoutPlotSetting();
				string[] viewNames = { "Форма 3 ГОСТ Р 21.1101-2009 M25", "Форма 3 ГОСТ Р 21.1101-2009" };

				var packageCreator = new PrintPackageCreator(layoutModelCollection, viewNames);

				var window = new LayoutTreeView(packageCreator.PrintPackageModels);
				Application.ShowModalWindow(window);

				if (!window.isClicked) return;

				var printPackages = window._printPackages;
				packageCreator.PrintPackageModels = printPackages;
				var sheetType = SheetType.MultiPdf;

				packageCreator.PublishAllPackages();

				var blockIds = printPackages.SelectMany(x => x.Layouts.Select(y => y.BlocksObjectId)).ToArray();

				var sheets = new List<Sheet>();

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