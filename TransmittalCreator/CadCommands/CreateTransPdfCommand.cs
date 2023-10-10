using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TransmittalCreator.DBCad;
using TransmittalCreator.Models;
using TransmittalCreator.Services;
using TransmittalCreator.Services.Blocks;
using TransmittalCreator.Services.Files;
using TransmittalCreator.Utils;

namespace TransmittalCreator.CadCommands
{
	public class CreateTransPdfCommand
	{
		[CommandMethod("CreateTranspdf")]
		public static void CreateTransmittalAndPdf()
		{
			List<Sheet> dict = new List<Sheet>();
			List<PrintModel> printModels = new List<PrintModel>();
			Active.Document.SendStringToExecute("REGENALL ", true, false, true);
			using (Transaction tr = Active.Database.TransactionManager.StartTransaction())
			{
				//фильтр для выбора только блока
				BlockSelector blockSelector = new BlockSelector();
				blockSelector.GetFilterForSelectBlockId();
				var res = blockSelector.SelectionResult;

				if (res.Status != PromptStatus.OK)
				{
					Active.Editor.WriteMessage("Надо выбрать блок");
					return;
				}

				SelectionSet selSet = res.Value;
				ObjectId[] allSelectedBlocks = selSet.GetObjectIds();

				var blocksByName = BlockUtils.GetDynBlocksByName(allSelectedBlocks, tr);

				PrintUtils.GetSheetsFromBlocks(Active.Editor, dict, tr, blocksByName);
				var selAttrName = "НОМЕР_ЛИСТА";
				PrintUtils.GetPrintParametersToPdf(Active.Editor, printModels, tr, blocksByName, selAttrName);

				PrintUtils.CreateJsonFile(dict);

				foreach (var printModel in printModels)
				{
					PrintUtils.PlotCurrentLayout(printModel.DocNumber, printModel);
				}

				tr.Commit();
			}
		}
	}
}
