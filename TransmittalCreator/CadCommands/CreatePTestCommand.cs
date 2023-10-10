using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TransmittalCreator.DBCad;
using TransmittalCreator.Models;
using TransmittalCreator.View;
using TransmittalCreator.ViewModel;

namespace TransmittalCreator.CadCommands
{
	public class CreatePTestCommand
	{
		//[CommandMethod("CreateP")]
		public void CreatePdfNameTest()
		{
			Active.Document.SendStringToExecute("REGENALL ", true, false, true);

			var options = new PromptEntityOptions("\nPlease select the block:");
			options.SetRejectMessage("\nSelected object isn't a block.\n");
			options.AddAllowedClass(typeof(BlockReference), true);

			var result = Active.Editor.GetEntity(options);
			if (result.Status == PromptStatus.OK)
			{
				using (var tr = Active.Database.TransactionManager.StartTransaction())
				{
					var blockRef = (BlockReference)tr.GetObject(result.ObjectId, OpenMode.ForWrite);

					BlockTableRecord btr =
						tr.GetObject(blockRef.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
					string blockName = btr.Name;
					btr.Dispose();
					AttributeCollection attributeCollection = blockRef.AttributeCollection;

					Dictionary<string, string> attrList = GetDictionaryFromAttributeCollection(attributeCollection, tr);

					ObservableCollection<BlockAttribute> blockAttributes = new ObservableCollection<BlockAttribute>();

					foreach (KeyValuePair<string, string> entry in attrList)
					{
						blockAttributes.Add(new BlockAttribute(entry.Key, entry.Value));
					}
					//var filenameModel = new FilenameModel(blockAttributes);

					var pdfCreatorViewModel = new PdfCreatorViewModel(blockAttributes);

					var pdfCreatorView = new PdfCreatorView(pdfCreatorViewModel);
					pdfCreatorView.Show();

					tr.Commit();
				}

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
