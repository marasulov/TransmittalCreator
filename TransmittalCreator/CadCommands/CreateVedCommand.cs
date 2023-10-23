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
					var blockNames = FileManager.GetBlockNames();
					if (blockNames.Contains(blockName)) idArray.Add(objectId);
				}
			}

			PrintUtils.GetSheetsFromBlocks(Active.Editor, dict, tr, idArray);
			const string selAttrName = "НОМЕР_ЛИСТА";

			var dictOrdered = dict.OrderBy(x => x.MinPointX).ToList();
			PrintUtils.GetPrintParametersToPdf(Active.Editor, printModels, tr, idArray, selAttrName);

			var scale = printModels[0].GetBlockScale();


			PrintUtils.CreateOnlyVed(dictOrdered, scale);
			tr.Commit();
		}
	}
}