using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System.IO;
using System.Linq;
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
				var idArray = new ObjectIdCollection();

				foreach (var objectId in idArrayTemp)
				{
					var blRef = (BlockReference)tr.GetObject(objectId, OpenMode.ForRead);
					var block =
						tr.GetObject(blRef.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
					var blockNames = FileManager.GetBlockNames();

					if (blockNames.Contains(block.Name)) idArray.Add(objectId);

				}

				foreach (ObjectId objectId in idArray)
				{
					var objectCopier = new ObjectCopier(objectId);
					var objectIds = objectCopier.SelectCrossingWindow();
					var blkRef = (BlockReference)tr.GetObject(objectId, OpenMode.ForRead);
					var selAttrName = "НАЗВАНИЕ_ЛИСТАRU";
					var fileName = PrintUtils.GetBlockAttributeValue(blkRef, selAttrName);


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

			AcadUtils.GetInfoBubble("Файлы успешно созданы", "Создание файлов завершено");
		}
	}
}