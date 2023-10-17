using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Publishing;
using TransmittalCreator.Models;
using TransmittalCreator.Models.Layouts;

namespace TransmittalCreator.Services;

public class PrintPackageCreator
{
	private readonly string[] MainPageName = { "Форма 3 ГОСТ Р 21.1101-2009 M25", "Форма 3 ГОСТ Р 21.1101-2009" };

	//TODO finish refactoring from here
	public PrintPackageCreator(LayoutModelCollection layoutModels, string[] mainPageName)
	{
		LayoutModels = layoutModels;
		MainPageName = mainPageName;
		CreatePrintPackages();
	}

	public List<PrintPackageModel> PrintPackageModels { get; set; } = new();
	private LayoutModelCollection LayoutModels { get; }

	private void CreatePrintPackages()
	{
		var layoutModels = LayoutModels.LayoutModels;
		var mainPageDocNum = "";
		foreach (var layout in layoutModels)
		{
			var printModel = layout.PrintModel;

			if (printModel.StampViewName.Contains(MainPageName[0]) ||
			    printModel.StampViewName.Contains(MainPageName[1]))
			{
				var layouts = new List<LayoutModel> { layout };
				PrintPackageModels.Add(new PrintPackageModel(printModel.DocNumber, layouts));
				mainPageDocNum = printModel.DocNumber;
			}
			else
			{
				var printPackageModel = PrintPackageModels.FirstOrDefault(x => x.PdfFileName == mainPageDocNum);
				printPackageModel?.Layouts.Add(layout);
			}
		}
	}

	public void PublishAllPackages()
	{
		foreach (var package in PrintPackageModels) PublishPackage(package);
	}

	public void PublishPackage(PrintPackageModel package)
	{
		var doc = Application.DocumentManager.MdiActiveDocument;
		var db = doc.Database;

		var bgPlot = (short)Application.GetSystemVariable("BACKGROUNDPLOT");
		Application.SetSystemVariable("BACKGROUNDPLOT", 0);

		using (var tr = db.TransactionManager.StartTransaction())
		{
			var collection = new DsdEntryCollection();
			foreach (var layoutModel in package.Layouts)
			{
				var entry = new DsdEntry();
				entry.DwgName = package.DwgPath + package.DwgFileName;
				entry.Layout = layoutModel.LayoutName;
				entry.Title = "Layout_" + layoutModel.LayoutName;
				entry.NpsSourceDwg = entry.DwgName;
				entry.Nps = "Setup1";

				collection.Add(entry);
			}

			var dsdData = new DsdData();
			dsdData.SheetType = SheetType.MultiPdf; //SheetType.MultiPdf
			dsdData.ProjectPath = package.DwgPath;
			dsdData.DestinationName =
				$"{dsdData.ProjectPath}{package.PdfFileName}.pdf";
			if (File.Exists(dsdData.DestinationName))
				File.Delete(dsdData.DestinationName);
			dsdData.SetDsdEntryCollection(collection);
			var dsdFile = $"{dsdData.ProjectPath}{package.PdfFileName}.dsd";

			dsdData.WriteDsd(dsdFile);
			dsdData.ReadDsd(dsdFile);

			File.Delete(dsdFile);
			var plotConfig = PlotConfigManager.SetCurrentConfig("DWG_To_PDF_Uzle.pc3");
			var publisher = Application.Publisher;

			publisher.AboutToBeginPublishing +=
				Publisher_AboutToBeginPublishing;
			dsdData.PromptForDwfName = false;
			publisher.PublishExecute(dsdData, plotConfig);
			tr.Commit();
		}

		Application.SetSystemVariable("BACKGROUNDPLOT", bgPlot);
	}

	private static void Publisher_AboutToBeginPublishing(object sender,
		AboutToBeginPublishingEventArgs e)
	{
		Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nPublishing!!");
	}
}