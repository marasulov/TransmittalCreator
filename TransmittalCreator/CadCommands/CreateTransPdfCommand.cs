using System.Collections.Generic;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using TransmittalCreator.DBCad;
using TransmittalCreator.Models;
using TransmittalCreator.Services.Blocks;
using TransmittalCreator.Utils;

namespace TransmittalCreator.CadCommands;

public class CreateTransPdfCommand
{
	[CommandMethod("CreateTranspdf")]
	public static void CreateTransmittalAndPdf()
	{
		var dict = new List<Sheet>();
		var printedPages = new List<string>();
		var printModels = new List<PrintModel>();
		Active.Document.SendStringToExecute("REGENALL ", true, false, true);
		using (var tr = Active.Database.TransactionManager.StartTransaction())
		{
			//фильтр для выбора только блока
			var blockSelector = new BlockSelector();
			blockSelector.GetFilterForSelectBlockId();
			var res = blockSelector.SelectionResult;

			if (res.Status != PromptStatus.OK)
			{
				Active.Editor.WriteMessage("Надо выбрать блок");
				return;
			}

			var selSet = res.Value;
			var allSelectedBlocks = selSet.GetObjectIds();

			var blocksByName = BlockUtils.GetDynBlocksByName(allSelectedBlocks, tr);

			PrintUtils.GetSheetsFromBlocks(Active.Editor, dict, tr, blocksByName);
			var selAttrName = "НОМЕР_ЛИСТА";
			PrintUtils.GetPrintParametersToPdf(Active.Editor, printModels, tr, blocksByName, selAttrName);

			PrintUtils.CreateJsonFile(dict);

			foreach (var printModel in printModels)
			{
				PrintUtils.PlotCurrentLayout(printModel.DocNumber, printModel);
				printedPages.Add(printModel.DocNumber + ".pdf создан");
			}

			tr.Commit();
		}

		var joinedBubbleTexts = string.Join("\n", printedPages);
		string drawingPath = Path.GetDirectoryName(Application.DocumentManager.CurrentDocument.Database.Filename);
		AcadUtils.GetInfoBubble("Создание файлов успешно завершено", joinedBubbleTexts, drawingPath);
	}
}