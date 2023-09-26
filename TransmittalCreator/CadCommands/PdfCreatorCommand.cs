using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System.Collections.ObjectModel;
using TransmittalCreator.DBCad;
using TransmittalCreator.Models;
using TransmittalCreator.ViewModel;
using TransmittalCreator.View;


namespace TransmittalCreator.CadCommands
{
	public static class PdfCreatorCommand
	{
		[CommandMethod("CreateP")]
		public static void CreatePdfNameTest()
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
					
					btr?.Dispose();

					AttributeCollection attributeCollection = blockRef.AttributeCollection;

					//Dictionary<string, string> attrList =
					//	GetDictionaryFromAttributeCollection(attributeCollection, tr);

					ObservableCollection<BlockAttribute> blockAttributes =
						new ObservableCollection<BlockAttribute>();

					foreach (ObjectId attId in attributeCollection)
					{
						AttributeReference attRef = (AttributeReference)tr.GetObject(attId, OpenMode.ForRead);
						blockAttributes.Add(new BlockAttribute(attRef.Tag, attRef.TextString));

					}

					var pdfCreatorViewModel = new PdfCreatorViewModel(blockAttributes);

					var pdfCreatorView = new PdfCreatorView(pdfCreatorViewModel);
					pdfCreatorView.Show();



					//Active.Editor.WriteMessage($"block id {blockRef.ObjectId} blockname {blockName} selected attr {pdfCreatorViewModel.SelectedAttr.AttributeName}");

					tr.Commit();
				}
			}
		}

	}
}