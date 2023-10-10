using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;
using TransmittalCreator.DBCad;
using TransmittalCreator.Services;
using TransmittalCreator.Services.Blocks;
using TransmittalCreator.Utils;
using TransmittalCreator.ViewModel;

namespace TransmittalCreator.CadCommands
{
	public static class PdfCreatorCommand
	{
		[CommandMethod("CreatePdf")]
		public static void CreatePdfName()
		{
			Active.Document.SendStringToExecute("REGENALL ", true, false, true);
			Dictionary<string, string> attrList = new Dictionary<string, string>();
			Window1 window = new Window1(new BlockViewModel(attrList));
			Application.ShowModalWindow(window);
			if (!window.isClicked) return;
			//ObjectIdCollection objectIds = Utils.SelectDynamicBlockReferences();
			List<Sheet> dict = new List<Sheet>();

			using Transaction tr = Active.Database.TransactionManager.StartTransaction();
			BlockSelector blockSelector = new BlockSelector();
			blockSelector.GetFilterForSelectBlockId(window.BlockName);
			bool isExec = true;
			var res = blockSelector.SelectionResult;

			if (res.Status != PromptStatus.OK)
			{
				isExec = false;
				Active.Editor.WriteMessage("Надо выбрать блок");
			}

			if (isExec)
			{
				SelectionSet selSet = res.Value;
				ObjectId[] idArrayTemp = selSet.GetObjectIds();
						
				//TODO printing X Y
						
				FileNameCreator fileNameCreator = new FileNameCreator(window, idArrayTemp);

				fileNameCreator.GetPrintParametersToPdf(tr);
						
				PrintUtils.CreateJsonFile(dict);

				foreach (var printModel in fileNameCreator.GetPrintModels())
				{
					PrintUtils.PlotCurrentLayout(printModel.DocNumber, printModel);
				}
			}

			tr.Commit();
		}

	}
}