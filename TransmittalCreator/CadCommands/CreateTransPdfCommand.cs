using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TransmittalCreator.Models;
using TransmittalCreator.Services.Blocks;
using TransmittalCreator.Services;
using TransmittalCreator.Utils;
using TransmittalCreator.DBCad;

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
					BlockReference blRef = (BlockReference)tr.GetObject(objectId, OpenMode.ForRead);
					BlockTableRecord block =
						tr.GetObject(blRef.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
					if (!(block is null))
					{
						string blockName = block.Name;
						string jsonFile = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "conf.json"));
						Params deserializeObject = JsonConvert.DeserializeObject<Params>(jsonFile);

						if (deserializeObject.StampName.Contains(blockName)) idArray.Add(objectId);

					}
				}

				PrintUtils.GetSheetsFromBlocks(Active.Editor, dict, tr, idArray);
				string selAttrName = "НОМЕР_ЛИСТА";
				PrintUtils.GetPrintParametersToPdf(Active.Editor, printModels, tr, idArray, selAttrName);


				//utils.CreateOnlyVed(dict);
				PrintUtils.CreateJsonFile(dict);

				foreach (var printModel in printModels)
				{
					//Active.Editor.WriteMessage("{0} печатаем ", printModel.DocNumber);
					PrintUtils.PlotCurrentLayout(printModel.DocNumber, printModel);
				}

				tr.Commit();
			}
		}
	}
}
