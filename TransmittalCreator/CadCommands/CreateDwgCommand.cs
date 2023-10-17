using System.IO;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using TransmittalCreator.DBCad;
using TransmittalCreator.Services;
using TransmittalCreator.Services.Blocks;
using TransmittalCreator.Services.Files;
using TransmittalCreator.Utils;

namespace TransmittalCreator.CadCommands;

public class CreateDwgCommand
{
	[CommandMethod("CreateDwg")]
	public static void CreateDwg()
	{
		Active.Document.SendStringToExecute("REGENALL ", true, false, true);

		var layersDictionary = LayerManipulation.GetLayersIsBlockedCol();
		if (Active.Document == null) return;
		Active.Document.LockOrUnlockLayers(false, ignoreCurrent: false, lockZero: true);

		var isExec = true;
		using (var tr = Active.Database.TransactionManager.StartTransaction())
		{
			var blockSelector = new BlockSelector();
			blockSelector.GetFilterForSelectBlockId();
			var res = blockSelector.SelectionResult;
			if (res.Status != PromptStatus.OK)
			{
				isExec = false;
				Active.Editor.WriteMessage("Надо выбрать блок");
			}

			if (isExec)
			{
				var selSet = res.Value;
				var idArrayTemp = selSet.GetObjectIds();

				//idArray.Select(id => (BlockReference) tr.GetObject(id, OpenMode.ForRead))
				//    .Where(br =>
				//        ((BlockTableRecord) tr.GetObject(br.DynamicBlockTableRecord, OpenMode.ForRead)).Name ==
				//        "Формат")
				//    .Select(br => br.ObjectId);

				var idArray = new ObjectIdCollection();
				foreach (var objectId in idArrayTemp)
				{
					var blRef = (BlockReference)tr.GetObject(objectId, OpenMode.ForRead);
					var block =
						tr.GetObject(blRef.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
					var blockNames = FileManager.GetBlockNames();

					if (blockNames.Contains(block.Name)) idArray.Add(objectId);

					//Active.Document.Editor.WriteMessage(blockName);
				}

				//MyCommands.GetSheetsFromBlocks(Active.Editor, dict, tr, idArray);
				//MyCommands.GetPrintParametersToPdf(Active.Editor, printModels, tr, idArray);
				//Active.Editor.WriteMessage("печать {0} - {1}", printModels[0].DocNumber, printModels.Count);

				foreach (ObjectId objectId in idArray)
				{
					var objectCopier = new ObjectCopier(objectId);
					var objectIds = objectCopier.SelectCrossingWindow();
					var blkRef = (BlockReference)tr.GetObject(objectId, OpenMode.ForRead);
					var selAttrName = "НАЗВАНИЕ_ЛИСТА";
					var fileName = PrintUtils.GetBlockAttributeValue(blkRef, selAttrName);

					//HostApplicationServices hs = HostApplicationServices.Current;
					//string path = Application.GetSystemVariable("DWGPREFIX");
					//hs.FindFile(doc.Name, doc.Database, FindFileHint.Default);
					//string createdwgFolder = Path.GetFileNameWithoutExtension(db.OriginalFileName);

					var folderdwg = Path.GetDirectoryName(Active.Database.OriginalFileName);
					var dwgFilename = Path.Combine(folderdwg, fileName + ".dwg");
					objectCopier.CopyObjectsNewDatabases(objectIds, dwgFilename);
					// objectCopier.CopyObjectsBetweenDatabases(objectIds, dwgFilename);
					Active.Editor.WriteMessage("{0} сохранен", dwgFilename);
					var newFileName = AcadUtils.ZoomFilesAndSave(dwgFilename);
					File.Delete(dwgFilename);
					File.Move(newFileName, dwgFilename);
				}
			}

			Active.Document.LockLayers(layersDictionary);
			tr.Commit();
		}
	}
}