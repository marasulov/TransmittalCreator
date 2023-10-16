using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TransmittalCreator.DBCad;
using TransmittalCreator.Services;
using TransmittalCreator.Services.Blocks;
using TransmittalCreator.Utils;
using TransmittalCreator.View;
using TransmittalCreator.ViewModel;
using TransmittalCreator.ViewModels;

namespace TransmittalCreator.CadCommands
{
	public static class CreatePdfCommand
	{
		[CommandMethod("CreatePdf")]
		public static void CreatePdfName()
		{
			Active.Document.SendStringToExecute("REGENALL ", true, false, true);
			using var tr = Active.Database.TransactionManager.StartTransaction();
			var selectAllBlocks = BlockUtils.SelectBlocksWithFilter();

			var selectedBlocksByName = BlockUtils.GetDynBlocksByName(selectAllBlocks, tr);

			var blockRef = (BlockReference)tr.GetObject(selectedBlocksByName[0], OpenMode.ForWrite);

			var attributeCollection = blockRef.AttributeCollection;
			//var blockAttributes = new ObservableCollection<BlockAttribute> { new("", "") };
			var blockAttributes = GetListBlockAttributeFromAttributeCollection(attributeCollection, tr);
			blockAttributes.Insert(0,new("", ""));

			//var blockAttributes = new ObservableCollection<BlockAttribute>();

			//foreach (KeyValuePair<string, string> entry in attrList)
			//{
			//	blockAttributes.Add(new BlockAttribute(entry.Key, entry.Value));
			//}

			var pdfCreatorVm = new CreatePdfVm();
			pdfCreatorVm.Attributes = blockAttributes;

			var pdfCreatorView = new CreatePdfView(pdfCreatorVm);
			pdfCreatorVm.OnRequestClose += (s, e) => pdfCreatorView.Close();
			pdfCreatorView.ShowDialog();
			if (!pdfCreatorVm.ButtonClicked) return;

			Active.WriteMessageInDebug($"{pdfCreatorVm.Prefix}");

			if (!pdfCreatorVm.IsPrintAll)
			{
				selectAllBlocks = BlockUtils.SelectBlocksWithFilter(true);
				selectedBlocksByName = BlockUtils.GetDynBlocksByName(selectAllBlocks, tr);
			}

			var fileNameCreator = new FileNameCreator(pdfCreatorVm, selectedBlocksByName);


			fileNameCreator.GetPrintParametersToPdf(tr);

			var printModels = fileNameCreator.GetPrintModels();
			if (pdfCreatorVm.IsX)
			{
				printModels.Sort((x, y) => x.MinPointX.CompareTo(y.MinPointX));
			}

			if (pdfCreatorVm.IsY)
			{
				printModels.Sort((x, y) => x.MinPointY.CompareTo(y.MinPointY));
			}


			foreach (var printModel in printModels)
			{
				PrintUtils.PlotCurrentLayout(printModel.DocNumber, printModel);
			}

			tr.Commit();


			//	Active.Document.SendStringToExecute("REGENALL ", true, false, true);
			//	Dictionary<string, string> attrList = new Dictionary<string, string>();
			//	var window = new Window1(new BlockViewModel(attrList));
			//	Application.ShowModalWindow(window);
			//	if (!window.isClicked) return;
			//	//ObjectIdCollection objectIds = Utils.SelectDynamicBlockReferences();
			//	List<Sheet> dict = new List<Sheet>();

			//	using Transaction tr = Active.Database.TransactionManager.StartTransaction();
			//	BlockSelector blockSelector = new BlockSelector();
			//	blockSelector.GetFilterForSelectBlockId(window.BlockName);
			//	bool isExec = true;
			//	var res = blockSelector.SelectionResult;

			//	if (res.Status != PromptStatus.OK)
			//	{
			//		isExec = false;
			//		Active.Editor.WriteMessage("Надо выбрать блок");
			//	}

			//	if (isExec)
			//	{
			//		SelectionSet selSet = res.Value;
			//		ObjectId[] idArrayTemp = selSet.GetObjectIds();

			//		//TODO printing X Y

			//		var fileNameCreator = new FileNameCreator(window, idArrayTemp);

			//		fileNameCreator.GetPrintParametersToPdf(tr);

			//		PrintUtils.CreateJsonFile(dict);

			//		foreach (var printModel in fileNameCreator.GetPrintModels())
			//		{
			//			PrintUtils.PlotCurrentLayout(printModel.DocNumber, printModel);
			//		}
			//	}

			//	tr.Commit();
		}


		private static ObservableCollection<BlockAttribute> GetListBlockAttributeFromAttributeCollection(AttributeCollection attributeCollection, Transaction tr)
		{
			var attributesDict = new ObservableCollection<BlockAttribute>();

			foreach (ObjectId attId in attributeCollection)
			{
				AttributeReference attRef = (AttributeReference)tr.GetObject(attId, OpenMode.ForRead);
				attributesDict.Add(new BlockAttribute(attRef.Tag, attRef.TextString));
			}

			return attributesDict;
		}
	}


}