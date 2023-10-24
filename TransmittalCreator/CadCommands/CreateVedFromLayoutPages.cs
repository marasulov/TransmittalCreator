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

	[CommandMethod("CreateVedFromLayout")]
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