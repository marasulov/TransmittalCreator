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
				ObjectId[] idArrayTemp = selSet.GetObjectIds();

				ObjectIdCollection idArray = new ObjectIdCollection();
				foreach (var objectId in idArrayTemp)
				{
					var blRef = (BlockReference)tr.GetObject(objectId, OpenMode.ForRead);
					var block = tr.GetObject(blRef.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
					if (block is null) continue;
					
					string[] blockNames = FileManager.GetBlockNames();

					if (blockNames.Contains(block.Name)) idArray.Add(objectId);
				}

				PrintUtils.GetSheetsFromBlocks(Active.Editor, dict, tr, idArray);
				var selAttrName = "НОМЕР_ЛИСТА";
				PrintUtils.GetPrintParametersToPdf(Active.Editor, printModels, tr, idArray, selAttrName);

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
