using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TransmittalCreator.DBCad;
using TransmittalCreator.Services;
using TransmittalCreator.Services.Blocks;
using TransmittalCreator.Services.Files;
using TransmittalCreator.Utils;

namespace TransmittalCreator.CadCommands
{
	public class CreateDwgCommand
	{
		[CommandMethod("CreateDwg")]
		public static void CreateDwg()
		{
			Active.Document.SendStringToExecute("REGENALL ", true, false, true);
			
			Dictionary<ObjectId, bool> layersDictionary = LayerManipulation.GetLayersIsBlockedCol();
			if (Active.Document == null) return;
			Active.Document.LockOrUnlockLayers(false, ignoreCurrent: false, lockZero: true);

			bool isExec = true;
			using (Transaction tr = Active.Database.TransactionManager.StartTransaction())
			{
				BlockSelector blockSelector = new BlockSelector();
				blockSelector.GetFilterForSelectBlockId();
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

					//idArray.Select(id => (BlockReference) tr.GetObject(id, OpenMode.ForRead))
					//    .Where(br =>
					//        ((BlockTableRecord) tr.GetObject(br.DynamicBlockTableRecord, OpenMode.ForRead)).Name ==
					//        "Формат")
					//    .Select(br => br.ObjectId);

					ObjectIdCollection idArray = new ObjectIdCollection();
					foreach (var objectId in idArrayTemp)
					{
						BlockReference blRef = (BlockReference)tr.GetObject(objectId, OpenMode.ForRead);
						BlockTableRecord block =
							tr.GetObject(blRef.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
						string[] blockNames = FileManager.GetBlockNames();
						
						if (blockNames.Contains(block.Name)) idArray.Add(objectId);

						//Active.Document.Editor.WriteMessage(blockName);
					}

					//MyCommands.GetSheetsFromBlocks(Active.Editor, dict, tr, idArray);
					//MyCommands.GetPrintParametersToPdf(Active.Editor, printModels, tr, idArray);
					//Active.Editor.WriteMessage("печать {0} - {1}", printModels[0].DocNumber, printModels.Count);

					foreach (ObjectId objectId in idArray)
					{
						ObjectCopier objectCopier = new ObjectCopier(objectId);
						ObjectIdCollection objectIds = objectCopier.SelectCrossingWindow();
						BlockReference blkRef = (BlockReference)tr.GetObject(objectId, OpenMode.ForRead);
						string selAttrName = "НАЗВАНИЕ_ЛИСТА";
						string fileName = PrintUtils.GetBlockAttributeValue(blkRef, selAttrName);

						//HostApplicationServices hs = HostApplicationServices.Current;
						//string path = Application.GetSystemVariable("DWGPREFIX");
						//hs.FindFile(doc.Name, doc.Database, FindFileHint.Default);
						//string createdwgFolder = Path.GetFileNameWithoutExtension(db.OriginalFileName);

						string folderdwg = Path.GetDirectoryName(Active.Database.OriginalFileName);
						string dwgFilename = Path.Combine(folderdwg, fileName + ".dwg");
						objectCopier.CopyObjectsNewDatabases(objectIds, dwgFilename);
						// objectCopier.CopyObjectsBetweenDatabases(objectIds, dwgFilename);
						Active.Editor.WriteMessage("{0} сохранен", dwgFilename);
						string newFileName = AcadUtils.ZoomFilesAndSave(dwgFilename);
						File.Delete(dwgFilename);
						System.IO.File.Move(newFileName, dwgFilename);
					}
				}

				Active.Document.LockLayers(layersDictionary);
				tr.Commit();
			}
		}
	}
}
