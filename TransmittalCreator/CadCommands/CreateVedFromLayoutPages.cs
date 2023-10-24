using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using TransmittalCreator.DBCad;
using TransmittalCreator.Models.Layouts;
using TransmittalCreator.Services.Blocks;
using TransmittalCreator.Services.Files;
using TransmittalCreator.Utils;

namespace TransmittalCreator.CadCommands;

public static class CreateVedFromLayoutPages
{

	[CommandMethod("selb")]
	public static void selectDynamicBlockReferences()
	{
		Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
		Database db = Application.DocumentManager.MdiActiveDocument.Database;
		using (Transaction trans = db.TransactionManager.StartTransaction())
		{
			// получаем таблицу блоков и проходим по всем записям таблицы блоков
			BlockTable bt =
				(BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
			foreach (ObjectId btrId in bt)
			{

				// получаем запись таблицы блоков и смотри анонимная ли она
				BlockTableRecord btr =
					(BlockTableRecord)trans.GetObject(btrId, OpenMode.ForRead);
				
				if (btr.IsDynamicBlock & btr.Name.Equals("Формат", StringComparison.InvariantCultureIgnoreCase))
				{
					Active.WriteMessage($"btr {btr.Name}\n");
					// получаем все анонимные блоки динамического блока
					ObjectIdCollection anonymousIds = btr.GetAnonymousBlockIds();
					// получаем все прямые вставки динамического блока
					ObjectIdCollection dynBlockRefs = btr.GetBlockReferenceIds(true, true);
					foreach (ObjectId anonymousBtrId in anonymousIds)
					{
						// получаем анонимный блок
						BlockTableRecord anonymousBtr =
							(BlockTableRecord)trans.GetObject(anonymousBtrId, OpenMode.ForRead);
						// получаем все вставки этого блока
						ObjectIdCollection blockRefIds =
							anonymousBtr.GetBlockReferenceIds(true, true);
						foreach (ObjectId id in blockRefIds)
						{
							var blRef = (BlockReference)trans.GetObject(id, OpenMode.ForRead);
							var btr1 = trans.GetObject(blRef.OwnerId, OpenMode.ForRead) as BlockTableRecord;
							if (btr1.IsLayout)
							{
								Active.WriteMessage($" {blRef.Name} in {btr1.Name}");
							}

							dynBlockRefs.Add(id);
						}
					}
					// Что-нибудь делаем с созданным нами набором
					ed.WriteMessage("\nДинамическому блоку \"{0}\" соответствуют {1} анонимных блоков и {2} вставок блока\n",
						btr.Name, anonymousIds.Count, dynBlockRefs.Count);
				}
			}
		}
	}


	[CommandMethod("ListarBloques")]
	public static void ListarBloques()
	{
		Document doc = Application.DocumentManager.MdiActiveDocument;
		Database db = doc.Database;
		Editor ed = doc.Editor;

		using (Transaction tr = db.TransactionManager.StartTransaction())
		{
			// open the block table which contains all the BlockTableRecords (block definitions and spaces)
			var blockTable = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

			// open the model space BlockTableRecord
			var modelSpace = (BlockTableRecord)tr.GetObject(blockTable[BlockTableRecord.PaperSpace], OpenMode.ForRead);

			// iterate through the model space 
			foreach (ObjectId id in modelSpace)
			{
				// check if the current ObjectId is a block reference one
				if (id.ObjectClass.DxfName == "INSERT")
				{
					// open the block reference
					var blockReference = (BlockReference)tr.GetObject(id, OpenMode.ForRead);
					

					// print the block name to the command line
					ed.WriteMessage("\n" + blockReference.Name);
				}
			}

			tr.Commit();
		}
	}

	//	[CommandMethod("Emp_RenumberPages")]
	//public void Emp_RenumberPages()
	//{


	//    try
	//    {
	//        // Start transaction
	//        Transaction Trans = Active.Database.TransactionManager.StartTransaction();
	//        Active.WriteMessage("Class1 | ServiceCommands | Emp_RenumberPages | ");
	//        // In each layout
	//        foreach (Layout Layt in g_Layouts)
	//        {
	//            DB.WriteLine("..." + Constants.vbTab + $"Layout: {Layt.LayoutName}");

	//            // Get the BTR of the layout.
	//            CadDBS.BlockTableRecord BTR = Trans.GetObject(Layt.BlockTableRecordId, CadDBS.OpenMode.ForRead);

	//            // Go through each entity id
	//            foreach (CadDBS.ObjectId ObjID in BTR)
	//            {
	//                // Try establishing this entity as a block reference. Skip if it isn't
	//                CadDBS.BlockReference MyBlockRef;
	//                try
	//                {
	//                    MyBlockRef = (CadDBS.BlockReference)Trans.GetObject(ObjID, CadDBS.OpenMode.ForRead); DB.WriteLine("..." + Constants.vbTab + $"Block Reference Name: {MyBlockRef.Name}");
	//                }
	//                catch (Exception ex)
	//                {
	//                    DB.WriteLine("..." + Constants.vbTab + $"Skipping: {ObjID}"); continue;
	//                }

	//                // Check if this block reference's name contains title
	//                if (MyBlockRef.Name.ToString.Contains("-title"))
	//                {
	//                    // Get the attribute collection of the title block
	//                    CadDBS.AttributeCollection AttCol = MyBlockRef.AttributeCollection;

	//                    // Go through each attribute
	//                    foreach (CadDBS.ObjectId AttObjId in AttCol)
	//                    {
	//                        // Make a dbobject from the object id
	//                        CadDBS.DBObject DbObj = Trans.GetObject(AttObjId, CadDBS.OpenMode.ForRead);

	//                        // Convert to atrribute reference
	//                        CadDBS.AttributeReference AttRef = DbObj;

	//                        // Display the tags and values of the attached attributes
	//                        DB.WriteLine("..." + Constants.vbTab + Constants.vbTab + "Tag: " + AttRef.Tag + "Value: " + AttRef.TextString);
	//                    }
	//                }
	//            }

	//            DB.WriteLine(Constants.vbNewLine);
	//        }

	//        // Save the new object to the database
	//        Trans.Commit();
	//    }
	//    catch (Exception ex)
	//    {
	//        return;
	//    }
	//}
	[CommandMethod("test")]
	public static void LoopThroughLayouts()
	{
		string blkName = "Формат";
		Document doc = Application.DocumentManager.MdiActiveDocument;
		Database db = doc.Database;
		Editor ed = doc.Editor;
		using (Transaction tr = db.TransactionManager.StartTransaction())
		{
			doc.TransactionManager.EnableGraphicsFlush(true);
			// Open dictionary for reading 
			DBDictionary layoutDict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
			// Loop through dictionary entries
			foreach (DictionaryEntry entry in layoutDict)
			{
				Layout ltr = (Layout)tr.GetObject((ObjectId)entry.Value, OpenMode.ForWrite);

				// Check for the tab number (zero-based , 0 is for ModelSpace) 

				if ((ltr.IsWriteEnabled && ltr.TabOrder > 0))
				{
					ed.WriteMessage("{0}" + "{1}\n", ltr.TabOrder, ltr.LayoutName);
					BlockTableRecord btr = (BlockTableRecord)tr.GetObject(ltr.BlockTableRecordId, OpenMode.ForWrite);

					if (btr.IsDynamicBlock)
					{
						// получаем все анонимные блоки динамического блока
						ObjectIdCollection anonymousIds = btr.GetAnonymousBlockIds();
						// получаем все прямые вставки динамического блока
						ObjectIdCollection dynBlockRefs = btr.GetBlockReferenceIds(true, true);
						foreach (ObjectId anonymousBtrId in anonymousIds)
						{
							// получаем анонимный блок
							BlockTableRecord anonymousBtr =
								(BlockTableRecord)tr.GetObject(anonymousBtrId, OpenMode.ForRead);
							// получаем все вставки этого блока
							ObjectIdCollection blockRefIds =
								anonymousBtr.GetBlockReferenceIds(true, true);
							foreach (ObjectId id in blockRefIds)
							{
								dynBlockRefs.Add(id);
							}
						}
						// Что-нибудь делаем с созданным нами набором
						ed.WriteMessage($"\nДинамическому блоку \"{0}\" соответствуют {1} анонимных блоков и {2} вставок блока в листе {ltr.LayoutName}\n",
							btr.Name, anonymousIds.Count, dynBlockRefs.Count);
					}

					foreach (ObjectId id in btr)
					{
						BlockReference bref = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
						if (bref != null && bref.Name.Equals(blkName, StringComparison.InvariantCultureIgnoreCase) & bref.IsWriteEnabled)
						{
							bref.UpgradeOpen();
							// ' <  Call your function to change attribute here  >
							bref.DowngradeOpen();
						}
					}
				}
			}

			tr.Commit();
		}
	}



	[CommandMethod("EntAddLayout")]
	static public void EntAddLayout()
	{
		Document doc = Application.DocumentManager.MdiActiveDocument;
		Database db = doc.Database;
		Editor ed = doc.Editor;
		// Спрашиваем имя листа
		PromptStringOptions opts = new PromptStringOptions("Укажите имя листа ");
		opts.AllowSpaces = true;
		PromptResult layoutName = ed.GetString(opts);
		if (layoutName.Status != PromptStatus.OK)
			return;
		using (Transaction tx = db.TransactionManager.StartTransaction())
		{
			DBDictionary dLayouts =
				tx.GetObject(db.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;
			// Проверяем имя листа
			if (dLayouts.Contains(layoutName.StringResult))
			{
				ObjectId id = dLayouts.GetAt(layoutName.StringResult);
				Layout layout = tx.GetObject(id, OpenMode.ForRead) as Layout;
				// layout.BlockTableRecordId – получаем id блока
				BlockTableRecord bt = tx.GetObject(
					layout.BlockTableRecordId,
					OpenMode.ForWrite) as BlockTableRecord;
				// в качестве текста добавим Окружность
				Circle circle = new Circle(Point3d.Origin, Vector3d.ZAxis, 10.0);
				bt.AppendEntity(circle);
				tx.AddNewlyCreatedDBObject(circle, true);
				// сделаем лист текущим
				//LayoutManager layoutMgr = LayoutManager.Current;
				//layoutMgr.CurrentLayout = layoutName.StringResult;
			}
			tx.Commit();
		}

		
	}



	[CommandMethod("ListLayouts")]
	public static void ListLayoutsMethod()
	{
		Document doc
			= Application.DocumentManager.MdiActiveDocument;
		Database db = doc.Database;
		Editor ed = doc.Editor;
		LayoutManager layoutMgr = LayoutManager.Current;
		ed.WriteMessage
		(
			String.Format
			(
				"{0}Active Layout is : {1}",
				Environment.NewLine,
				layoutMgr.CurrentLayout
			)
		);
		ed.WriteMessage
		(
			String.Format
			(
				"{0}Number of Layouts: {1}{0}List of all Layouts:",
				Environment.NewLine,
				layoutMgr.LayoutCount
			)
		);
		using (Transaction tr
			   = db.TransactionManager.StartTransaction())
		{
			DBDictionary layoutDic
				= tr.GetObject(
					db.LayoutDictionaryId,
					OpenMode.ForRead,
					false
				) as DBDictionary;
			foreach (DBDictionaryEntry entry in layoutDic)
			{
				ObjectId layoutId = entry.Value;
				Layout layout
					= tr.GetObject(
						layoutId,
						OpenMode.ForRead
					) as Layout;
				if (layout != null)
				{
					var btr = (BlockTableRecord)tr.GetObject(layout.BlockTableRecordId, OpenMode.ForRead);
					if (!btr.IsDynamicBlock) continue;
					Active.WriteMessage($"В листе {btr.Name} :\n");
					// получаем все анонимные блоки динамического блока
					var anonymousIds = btr.GetAnonymousBlockIds();
					var blockref = btr.GetBlockReferenceIds(true, true);

					foreach (ObjectId anonymousBtrId in anonymousIds)
					{
						// получаем анонимный блок
						var anonymousBtr = (BlockTableRecord)tr.GetObject(anonymousBtrId, OpenMode.ForRead);
						// получаем все вставки этого блока
						var blockRefIds = anonymousBtr.GetBlockReferenceIds(true, true);


					}
				}
				ed.WriteMessage(
					String.Format(
						"{0}--> {1}",
						Environment.NewLine,
						layout.LayoutName
					)
				);
			}
			tr.Commit();
		}
	}


	//[CommandMethod("CreateVedFromLayout")]
	public static void CreateVedFromLayout()
	{
		Active.Document.SendStringToExecute("_QSAVE ", true, false, false);
		var blockNames = FileManager.GetBlockNames();
		var dynamicBlocks = new DynamicBlockFinder(blockNames);

		using var trans = Active.Database.TransactionManager.StartTransaction();

		var layoutModelCollection = new LayoutModelCollection(dynamicBlocks);
		layoutModelCollection.ListLayouts("Model");

		layoutModelCollection.CreateCollectionFromBlockNames(trans);

		var blocksList = layoutModelCollection.BlockReferences.Select(x=>x.ObjectId).ToArray();
		if (blocksList.Length == 0) return;

		//TODO данные для таблицы
		var dict = new List<Sheet>();
		PrintUtils.GetSheetsFromBlocks(Active.Editor, dict, trans, new ObjectIdCollection(blocksList));
		PrintUtils.CreateOnlyVed(dict);
		trans.Commit();
	}


	//[CommandMethod("CreateCircleInPS")]
	public static void Test_CreateCircleInPS()
	{
		Database db = HostApplicationServices.WorkingDatabase;
		Editor ed = Active.Editor;
		Matrix3d ucs = ed.CurrentUserCoordinateSystem;
		try
		{
			PromptPointOptions prOpt = new PromptPointOptions("\nCircle center: ");
			PromptPointResult pr = ed.GetPoint(prOpt);
			if (pr.Status != PromptStatus.OK) return;
			PromptDistanceOptions prOpt1 = new PromptDistanceOptions("\nCircle radius: ");
			prOpt1.UseBasePoint = true;
			prOpt1.UseDashedLine = true;
			prOpt1.BasePoint = pr.Value;
			PromptDoubleResult pr1 = ed.GetDistance(prOpt1);
			if (pr1.Status != PromptStatus.OK) return;
			using (Circle cir = new Circle(pr.Value, Vector3d.ZAxis, pr1.Value))
			{
				using (Transaction tr = db.TransactionManager.StartTransaction())
				{
					BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
					BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.PaperSpace], OpenMode.ForWrite);
					cir.TransformBy(ed.CurrentUserCoordinateSystem);
					btr.AppendEntity(cir);
					tr.AddNewlyCreatedDBObject(cir, true);
					tr.Commit();
				}
			}
		}
		catch (System.Exception ex)
		{
			ed.WriteMessage(ex.ToString());
		}
	}

	[CommandMethod("BlockIterator")]
	public static void BlockIterator_Method()
	{
		Database database = HostApplicationServices.WorkingDatabase;
		using (Transaction transaction = database.TransactionManager.StartTransaction())
		{
			BlockTable blkTable = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);
			foreach (ObjectId id in blkTable)
			{
				BlockTableRecord btRecord = (BlockTableRecord)transaction.GetObject(id, OpenMode.ForRead);
				if (!btRecord.IsLayout)
				{
					//Access to the block (not model/paper space)
					Active.WriteMessage(string.Format("\nBlock name: {0}", btRecord.Name));
				}
			}

			transaction.Commit();
		}
	}

	//[CommandMethod("CreateCircleInCurrent")]
	public static void Test_CreateCircleInCurrent()
	{
		Database db = HostApplicationServices.WorkingDatabase;
		Editor ed = Active.Editor;
		Matrix3d ucs = ed.CurrentUserCoordinateSystem;
		try
		{
			PromptPointOptions prOpt = new PromptPointOptions("\nCircle center: ");
			PromptPointResult pr = ed.GetPoint(prOpt);
			if (pr.Status != PromptStatus.OK) return;
			PromptDistanceOptions prOpt1 = new PromptDistanceOptions("\nCircle radius: ");
			prOpt1.UseBasePoint = true;
			prOpt1.UseDashedLine = true;
			prOpt1.BasePoint = pr.Value;
			PromptDoubleResult pr1 = ed.GetDistance(prOpt1);
			if (pr1.Status != PromptStatus.OK) return;
			using (Circle cir = new Circle(pr.Value, Vector3d.ZAxis, pr1.Value))
			{
				using (Transaction tr = db.TransactionManager.StartTransaction())
				{
					BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
					cir.TransformBy(ed.CurrentUserCoordinateSystem);
					btr.AppendEntity(cir);
					tr.AddNewlyCreatedDBObject(cir, true);
					tr.Commit();
				}
			}
		}
		catch (System.Exception ex)
		{
			ed.WriteMessage(ex.ToString());
		}
	}
}