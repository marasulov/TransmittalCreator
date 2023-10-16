using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TransmittalCreator.DBCad;
using TransmittalCreator.Services;
using TransmittalCreator.Utils;
using TransmittalCreator.View;
using TransmittalCreator.ViewModels;
using BlockAttribute = TransmittalCreator.ViewModels.BlockAttribute;

namespace TransmittalCreator.CadCommands
{
	public class CreatePTestCommand
	{
		[CommandMethod("CreateP")]
		public void CreatePdfNameTest()
		{
			Active.Document.SendStringToExecute("REGENALL ", true, false, true);

			var options = new PromptEntityOptions("\nPlease select the block:");
			options.SetRejectMessage("\nSelected object isn't a block.\n");
			options.AddAllowedClass(typeof(BlockReference), true);

			var result = Active.Editor.GetEntity(options);
			if (result.Status == PromptStatus.OK)
			{
				using var tr = Active.Database.TransactionManager.StartTransaction();
				var blockRef = (BlockReference)tr.GetObject(result.ObjectId, OpenMode.ForWrite);

				BlockTableRecord btr =
					tr.GetObject(blockRef.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
					
				btr?.Dispose();
				var attributeCollection = blockRef.AttributeCollection;

				var attrList = GetDictionaryFromAttributeCollection(attributeCollection, tr);

				var blockAttributes = new ObservableCollection<BlockAttribute>();

				foreach (KeyValuePair<string, string> entry in attrList)
				{
					blockAttributes.Add(new BlockAttribute(entry.Key, entry.Value));
				}

				var pdfCreatorVm = new CreatePdfVm();
				pdfCreatorVm.Attributes = blockAttributes;

				var pdfCreatorView = new CreatePdfView(pdfCreatorVm);
				pdfCreatorVm.OnRequestClose += (s, e) => pdfCreatorView.Close();
				pdfCreatorView.ShowDialog();
				if (!pdfCreatorVm.ButtonClicked) return;

				Active.WriteMessageInDebug($"{pdfCreatorVm.Prefix}");

				var allSelectedBlocks = BlockUtils.SelectBlocksWithFilter();
				var fileNameCreator = new FileNameCreator(pdfCreatorVm, new ObjectIdCollection(allSelectedBlocks));

				fileNameCreator.GetPrintParametersToPdf(tr);
					
				foreach (var printModel in fileNameCreator.GetPrintModels())
				{
					PrintUtils.PlotCurrentLayout(printModel.DocNumber, printModel);
				}

				if (allSelectedBlocks == null)
				{
					return;
				}

				if (pdfCreatorVm.IsSelectedOrder)
				{
					Active.WriteMessageInDebug($"{pdfCreatorVm.SelectedAttr.AttributeName}");
				}

				tr.Commit();

				//var names = PrintUtils.GetAttrList(tr, attrList);

				//attrList = names.Item2;
				//_blockName = names.Item1;

				//foreach (KeyValuePair<string, string> entry in attrList)
				//{
				//	combolist.Add(new BlockAttribute(entry.Key, entry.Value));
				//}

				//Application.ShowModalWindow(window);
				//if (window.isClicked)
				//{
				//	//ObjectIdCollection objectIds = Utils.SelectDynamicBlockReferences();
				//	List<Sheet> dict = new List<Sheet>();

				//	using (Transaction tr = Active.Database.TransactionManager.StartTransaction())
				//	{
				//		var blockRef = (BlockReference)tr.GetObject(result.ObjectId, OpenMode.ForWrite);

				//		BlockTableRecord block = tr.GetObject(blockRef.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;

				//		ObjectId[] idArrayTemp = { block.Id };
				//		//ObjectIdCollection idArray = new ObjectIdCollection(idArrayTemp);
				//		//TODO printing X Y
				//		//string selAttrName = attributeName.AttributeName;


				//		FileNameCreator fileNameCreator = new FileNameCreator(window, idArrayTemp);

				//		//GetPrintParametersToPdf(Active.Editor, printModels, tr, objectIds, selAttrName);

				//		fileNameCreator.GetPrintParametersToPdf(tr);

				//		PrintUtils.CreateJsonFile(dict);

				//		foreach (var printModel in fileNameCreator.GetPrintModels())
				//		{
				//			PrintUtils.PlotCurrentLayout(printModel.DocNumber, printModel);
				//		}


				//		tr.Commit();
				//	}
				//}
			}
			else
			{
				Active.Editor.WriteMessage("Надо выбрать блок");
			}
		}

		private Dictionary<string, string> GetDictionaryFromAttributeCollection(AttributeCollection attributeCollection, Transaction tr)
		{
			var attributesDict = new Dictionary<string, string>();

			foreach (ObjectId attId in attributeCollection)
			{
				AttributeReference attRef = (AttributeReference)tr.GetObject(attId, OpenMode.ForRead);
				attributesDict.Add(attRef.Tag, attRef.TextString);
				//string str = ("\n  Attribute Tag: " + attRef.Tag + "\n    Attribute String: " + attRef.TextString);
				//Active.Editor.WriteMessage(str);
			}

			return attributesDict;
		}
	}
}
