using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Newtonsoft.Json;
using TransmittalCreator.DBCad;
using TransmittalCreator.Models;
using TransmittalCreator.Services;
using TransmittalCreator.Services.Blocks;
using TransmittalCreator.Utils;

namespace TransmittalCreator.CadCommands;

public class CreatePdfWithRenumberingCommand
{
	//[CommandMethod("CreateTranspdf")]
	public static void CreatePdfWithRenumbering()
	{
		var dict = new List<Sheet>();
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
			var idArrayTemp = selSet.GetObjectIds();

			var idArray = new ObjectIdCollection();
			foreach (var objectId in idArrayTemp)
			{
				var blRef = (BlockReference)tr.GetObject(objectId, OpenMode.ForRead);
				var block =
					tr.GetObject(blRef.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
				if (!(block is null))
				{
					var blockName = block.Name;
					var jsonFile = File.ReadAllText(Path.Combine(
						Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "conf.json"));
					var deserializeObject = JsonConvert.DeserializeObject<Params>(jsonFile);

					if (deserializeObject.StampName.Contains(blockName)) idArray.Add(objectId);
				}
			}

			PrintUtils.GetSheetsFromBlocks(Active.Editor, dict, tr, idArray);
			var selAttrName = "НОМЕР_ЛИСТА";
			PrintUtils.GetPrintParametersToPdf(Active.Editor, printModels, tr, idArray, selAttrName);


			//utils.CreateOnlyVed(dict);
			PrintUtils.CreateJsonFile(dict);

			foreach (var printModel in printModels)
				//Active.Editor.WriteMessage("{0} печатаем ", printModel.DocNumber);
				PrintUtils.PlotCurrentLayout(printModel.DocNumber, printModel);

			tr.Commit();
		}
	}
}