using System;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Internal;
using TransmittalCreator.DBCad;
using TransmittalCreator.Services.Files;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace TransmittalCreator;

/// <summary>
///     Сервис слежения за командами
/// </summary>
public static class CommandsWatcher
{
	/// <summary>
	///     True - использовать редактор блоков. False - не использовать
	/// </summary>
	public static bool UseBedit { get; set; } = true;

	/// <summary>
	///     Возвращает true, если в данный момент выполняется команда MIRROR
	/// </summary>
	public static bool Mirroring { get; private set; }

	/// <summary>
	///     Возвращает true, если в данный момент выполняется команда ROTATE
	/// </summary>
	public static bool Rotation { get; private set; }

	/// <summary>
	///     Инициализация
	/// </summary>
	public static void Initialize()
	{
		AcApp.DocumentManager.DocumentLockModeChanged += DocumentManager_BlockChangingVeto;
	}

	/// <summary>
	///     Подписаться на события документа, связанные с командами
	/// </summary>
	/// <param name="doc">Документ</param>
	public static void SubscribeDocument(Document doc)
	{
		doc.CommandWillStart -= CommandWillStart;
		doc.CommandWillStart += CommandWillStart;
		doc.CommandEnded -= CommandEnded;
		doc.CommandEnded += CommandEnded;
		doc.CommandCancelled -= CommandCancelled;
		doc.CommandCancelled += CommandCancelled;
	}

	private static void DocumentManager_DocumentLockModeChanged(object sender, DocumentLockModeChangedEventArgs e)
	{
		try
		{
			if (!UseBedit)
				if (e.GlobalCommandName == "BEDIT")
					e.Veto();
		}
		catch (Exception exception)
		{
			Active.WriteMessageInDebug($"\nException {exception.Message}");
		}
	}

	private static void DocumentManager_BlockChangingVeto(object sender, DocumentLockModeChangedEventArgs e)
	{
		try
		{
			if ((e.GlobalCommandName.ToLower() == "#bedit") | (e.GlobalCommandName.ToLower() == "bedit") |
			    (e.GlobalCommandName.ToLower() == "_-bedit") |
			    (e.GlobalCommandName.ToLower() == "-bedit") | (e.GlobalCommandName.ToLower() == "_refedit") |
			    (e.GlobalCommandName.ToLower() == "refedit") | (e.GlobalCommandName.ToLower() == "refedit"))
			{
				var doc = AcApp.DocumentManager.MdiActiveDocument;
				var db = doc.Database;
				var ed = doc.Editor;
				var blockName = "";
				var selection = ed.SelectImplied();
				if (selection.Status == PromptStatus.OK)
					if (selection.Value.Count == 1)
					{
						var id = selection.Value[0].ObjectId;
						//AcApp.ShowAlertDialog(id.ObjectClass.Name);
						using (var tx =
						       db.TransactionManager.StartTransaction())
						{
							var blockRef = tx.GetObject(id,
								OpenMode.ForRead) as BlockReference;
							BlockTableRecord block = null;
							if (blockRef.IsDynamicBlock)
								//get the real dynamic block name.
								block = tx.GetObject(blockRef.DynamicBlockTableRecord,
									OpenMode.ForRead) as BlockTableRecord;
							else
								block = tx.GetObject(blockRef.BlockTableRecord,
									OpenMode.ForRead) as BlockTableRecord;

							if (block != null)
							{
								blockName = block.Name;
								var blockNames = FileManager.GetBlockNames();
								if (blockNames.Contains(blockName))
								{
									e.Veto();
									AcApp.ShowAlertDialog("Редактирование этого блока запрещено / Editing this block is prohibited");
								}
							}

							tx.Commit();
						}
					}
				//else
				//{
				//	AcApp.ShowAlertDialog("More than one object in pickfirst set");

				//}

				if (AcAeUtilities.IsInBlockEditor())
				{
					blockName = AcAeUtilities.GetBlockName();
					ed.WriteMessage("\nWe are in block editor of block \"{0}\"",
						blockName);
					if ((blockName == "Формат") | (blockName == "ФорматM25"))
					{
						doc.SendStringToExecute("(command \"_.bclose\" \"_d\")\n", false, true, false);
						doc.SendStringToExecute("_refclose\n", false, true, false);
						AcApp.ShowAlertDialog("Редактирование этого блока запрещено");
					}
				}
			}
		}
		catch (Exception exception)
		{
			Active.WriteMessageInDebug($"\nException {exception.Message}");
		}
	}

	private static void CommandCancelled(object sender, CommandEventArgs e)
	{
		Mirroring = false;
		Rotation = false;
	}

	private static void CommandEnded(object sender, CommandEventArgs e)
	{
		Mirroring = false;
		Rotation = false;
	}

	private static void CommandWillStart(object sender, CommandEventArgs e)
	{
		if (e.GlobalCommandName == "MIRROR")
		{
			Mirroring = true;
			Active.WriteMessage("зеркалирование запущено");
		}
		else if (e.GlobalCommandName == "ROTATE")
		{
			Rotation = true;
			Active.WriteMessage("поворот запущено");
		}
		//else if (e.GlobalCommandName.ToLower() == "bedit" | e.GlobalCommandName.ToLower() == "_-bedit" | e.GlobalCommandName.ToLower() == "-bedit" | e.GlobalCommandName.ToLower() == "_refedit")
		//{
		//	Document doc = Application.DocumentManager.MdiActiveDocument;
		//	Database db = doc.Database;
		//	Editor ed = doc.Editor;
		//	string blockName = "";
		//	var selection = ed.SelectImplied();
		//	if (selection.Status == PromptStatus.OK)
		//	{
		//		if (selection.Value.Count == 1)
		//		{
		//			var id = selection.Value[0].ObjectId;
		//			//AcApp.ShowAlertDialog(id.ObjectClass.Name);
		//			using (Transaction tx =
		//					   db.TransactionManager.StartTransaction())
		//			{
		//				BlockReference blockRef = tx.GetObject(id,
		//					OpenMode.ForRead) as BlockReference;
		//				BlockTableRecord block = null;
		//				if (blockRef.IsDynamicBlock)
		//				{
		//					//get the real dynamic block name.
		//					block = tx.GetObject(blockRef.DynamicBlockTableRecord,
		//						OpenMode.ForRead) as BlockTableRecord;
		//				}
		//				else
		//				{
		//					block = tx.GetObject(blockRef.BlockTableRecord,
		//						OpenMode.ForRead) as BlockTableRecord;
		//				}
		//				if (block != null)
		//				{
		//					blockName = block.Name;
		//					ed.WriteMessage("Block name is : "
		//									+ block.Name + "\n");
		//					if (blockName == "Opora2")
		//					{

		//						//doc.SendStringToExecute("\x03\x03", false, true, false);
		//						//acedPostCommand("CANCELCMD");
		//						doc.SendStringToExecute("BCLOSE\n", false, true, false);
		//						doc.SendStringToExecute("n\n", true, false, false);
		//						doc.SendStringToExecute("n\n", true, false, false);
		//						doc.SendStringToExecute("n\n", true, false, false);
		//						doc.SendStringToExecute("\x03\x03", false, true, false);
		//						doc.SendStringToExecute("\x03\x03", false, true, false);
		//						AcApp.ShowAlertDialog($"{blockName} редактирование запрещено");
		//					}
		//				}
		//				tx.Commit();
		//			}
		//		}
		//		else
		//		{
		//			AcApp.ShowAlertDialog("More than one object in pickfirst set");
		//		}
		//	}
		//	else
		//	{
		//		AcApp.ShowAlertDialog("None pickfirst set");
		//	}
		//	AcadUtils.Editor.WriteMessage($"редактирование  {blockName} ");
		//}
	}
}