using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace TransmittalCreator.CadCommands;

public class PrintStampCommand
{
	//[CommandMethod("CreatePdf")]
	//public void PrintStamp()
	//{
	//	Active.Document.SendStringToExecute("REGENALL ", true, false, true);
	//	using var tr = Active.Database.TransactionManager.StartTransaction();
	//	var selectAllBlocks = BlockUtils.SelectBlocksWithFilter();

	//	var selectedBlocksByName = BlockUtils.GetDynBlocksByName(selectAllBlocks, tr);

	//	var blockRef = (BlockReference)tr.GetObject(selectedBlocksByName[0], OpenMode.ForWrite);

	//	var attributeCollection =  blockRef.AttributeCollection;

	//	var attrList = GetDictionaryFromAttributeCollection(attributeCollection, tr);

	//	var blockAttributes = new ObservableCollection<BlockAttribute>();

	//	foreach (KeyValuePair<string, string> entry in attrList)
	//	{
	//		blockAttributes.Add(new BlockAttribute(entry.Key, entry.Value));
	//	}

	//	var pdfCreatorVm = new CreatePdfVm();
	//	pdfCreatorVm.Attributes = blockAttributes;

	//	var pdfCreatorView = new CreatePdfView(pdfCreatorVm);
	//	pdfCreatorVm.OnRequestClose += (s, e) => pdfCreatorView.Close();
	//	pdfCreatorView.ShowDialog();
	//	if (!pdfCreatorVm.ButtonClicked) return;

	//	Active.WriteMessageInDebug($"{pdfCreatorVm.Prefix}");

	//	if (!pdfCreatorVm.IsPrintAll)
	//	{
	//		selectAllBlocks = BlockUtils.SelectBlocksWithFilter(true);
	//		selectedBlocksByName = BlockUtils.GetDynBlocksByName(selectAllBlocks, tr);
	//	}

	//	var fileNameCreator = new FileNameCreator(pdfCreatorVm, selectedBlocksByName);


	//	fileNameCreator.GetPrintParametersToPdf(tr);

	//	var printModels = fileNameCreator.GetPrintModels();
	//	if (pdfCreatorVm.IsX)
	//	{
	//		printModels.Sort((x, y) =>x.MinPointX.CompareTo(y.MinPointX));
	//	}

	//	if (pdfCreatorVm.IsY)
	//	{
	//		printModels.Sort((x, y) => x.MinPointY.CompareTo(y.MinPointY));
	//	}


	//	foreach (var printModel in printModels)
	//	{
	//		PrintUtils.PlotCurrentLayout(printModel.DocNumber, printModel);
	//	}

	//	tr.Commit();

	//}

	private Dictionary<string, string> GetDictionaryFromAttributeCollection(AttributeCollection attributeCollection,
		Transaction tr)
	{
		var attributesDict = new Dictionary<string, string>();

		foreach (ObjectId attId in attributeCollection)
		{
			var attRef = (AttributeReference)tr.GetObject(attId, OpenMode.ForRead);
			attributesDict.Add(attRef.Tag, attRef.TextString);
		}

		return attributesDict;
	}
}