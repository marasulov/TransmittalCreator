using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;
using System.IO;
using TransmittalCreator.DBCad;
using TransmittalCreator.Informers;
using TransmittalCreator.Services;
using TransmittalCreator.Utils;
using TransmittalCreator.ViewModels;
using TransmittalCreator.ViewModels.Models;
using TransmittalCreator.ViewModels.View;

namespace TransmittalCreator.CadCommands;

public static class CreatePdfCommand
{
	[CommandMethod("CreatePdf")]
	public static void CreatePdfName()
	{
		var printedPages = new List<string>();
		Active.Document.SendStringToExecute("REGENALL ", true, false, true);
		using var tr = Active.Database.TransactionManager.StartTransaction();

		var selectAllBlocks = BlockUtils.SelectBlocksWithFilter();

		var selectedBlocksByName = BlockUtils.GetDynBlocksByName(selectAllBlocks, tr);

		var blockRef = (BlockReference)tr.GetObject(selectedBlocksByName[0], OpenMode.ForWrite);

		var attributeCollection = blockRef.AttributeCollection;
		//var blockAttributes = new ObservableCollection<BlockAttribute> { new("", "") };
		var blockAttributes = GetListBlockAttributeFromAttributeCollection(attributeCollection, tr);

		var pdfCreatorVm = new CreatePdfVm();
		pdfCreatorVm.Attributes = blockAttributes;

		var pdfCreatorView = new CreatePdfView(pdfCreatorVm);
		pdfCreatorVm.OnRequestClose += (s, e) => pdfCreatorView.Close();
		pdfCreatorView.ShowDialog();
		if (!pdfCreatorVm.ButtonClicked) return;

		selectAllBlocks = BlockUtils.SelectBlocksWithFilter(true);
		if (selectAllBlocks == null) return;

		selectedBlocksByName = BlockUtils.GetDynBlocksByName(selectAllBlocks, tr);
		if (selectedBlocksByName.Count <= 0) return;

		var fileNameCreator = new FileNameCreator(pdfCreatorVm, selectedBlocksByName);

		fileNameCreator.GetPrintParametersToPdf(tr);

		var printModels = fileNameCreator.GetPrintModels();
		if (pdfCreatorVm.IsX) printModels.Sort((x, y) => x.MinPointX.CompareTo(y.MinPointX));

		if (pdfCreatorVm.IsY) printModels.Sort((x, y) => x.MinPointY.CompareTo(y.MinPointY));


		foreach (var printModel in printModels)
		{
			PrintUtils.PlotCurrentLayout(printModel.DocNumber, printModel);
			printedPages.Add(printModel.DocNumber + ".pdf создан");
		}


		var joinedBubbleTexts = string.Join("\n", printedPages);
		string drawingPath = Path.GetDirectoryName(Application.DocumentManager.CurrentDocument.Database.Filename);

		BalloonNotifier notifier = new BalloonNotifier("Создание файлов успешно завершено", "Создание файлов успешно завершено", drawingPath);
		notifier.Notify(joinedBubbleTexts);
	}

	private static List<BlockAttribute> GetListBlockAttributeFromAttributeCollection(
		AttributeCollection attributeCollection, Transaction tr)
	{
		var attributesDict = new List<BlockAttribute>();

		foreach (ObjectId attId in attributeCollection)
		{
			var attRef = (AttributeReference)tr.GetObject(attId, OpenMode.ForRead);
			attributesDict.Add(new BlockAttribute(attRef.Tag, attRef.TextString));
		}

		return attributesDict;
	}
}