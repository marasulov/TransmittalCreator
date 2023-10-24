using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Newtonsoft.Json;
using TransmittalCreator.DBCad;
using TransmittalCreator.Models;
using TransmittalCreator.Services;
using TransmittalCreator.Services.Blocks;
using TransmittalCreator.Services.Files;
using TransmittalCreator.Utils;

namespace TransmittalCreator.CadCommands;

public class CreateVedCommand
{
	[CommandMethod("CreateVed")]
	public static void CreateVed()
	{
		List<Sheet> dict = new List<Sheet>();
		List<PrintModel> printModels = new List<PrintModel>();
		Active.Document.SendStringToExecute("REGENALL ", true, false, true);
		using (Transaction tr = DBCad.Active.Database.TransactionManager.StartTransaction())
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
				BlockReference blRef = (BlockReference)tr.GetObject(objectId, OpenMode.ForRead);
				BlockTableRecord block =
					tr.GetObject(blRef.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
				if (!(block is null))
				{
					string blockName = block.Name;

					string jsonFile = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "conf.json"));
					Params deserializeObject = JsonConvert.DeserializeObject<Params>(jsonFile);

					//if (blockName == "Формат" | blockName == "ФорматM25") idArray.Add(objectId);
					if (deserializeObject.StampName.Contains(blockName)) idArray.Add(objectId);
				}
			}

			PrintUtils.GetSheetsFromBlocks(Active.Editor, dict, tr, idArray);
			string selAttrName = "НОМЕР_ЛИСТА";

			var dictOrdered = dict.OrderBy(x => x.MinPointX).ToList();
			PrintUtils.GetPrintParametersToPdf(Active.Editor, printModels, tr, idArray, selAttrName);

			var scale = printModels[0].GetBlockScale();


			PrintUtils.CreateOnlyVed(dictOrdered, scale);
			tr.Commit();
		}
	}
}