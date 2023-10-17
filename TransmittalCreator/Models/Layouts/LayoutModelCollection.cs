using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using TransmittalCreator.DBCad;
using TransmittalCreator.Services;
using TransmittalCreator.Services.Blocks;
using TransmittalCreator.Utils;
using PlotType = Autodesk.AutoCAD.DatabaseServices.PlotType;

namespace TransmittalCreator.Models.Layouts;

/// <summary>
///     Создает коллекцию Layout
/// </summary>
public class LayoutModelCollection
{
	public LayoutModelCollection(DynamicBlockFinder dynamicBlocks)
	{
		DynamicBlocks = dynamicBlocks;
	}

	public List<LayoutModel> LayoutModels { get; set; } = new();
	public DynamicBlockFinder DynamicBlocks { get; set; }

	public List<BlockReference> BlockReferences { get; set; }


	/// <summary>
	///     Создает коллекцию Layout
	/// </summary>
	/// <param name="notIncludeSpace">не искать на указанном виде</param>
	public void ListLayouts(string notIncludeSpace = "")
	{
		var db = Active.Database;

		using (var tr = db.TransactionManager.StartTransaction())
		{
			var layoutDic = tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead, false) as DBDictionary;

			if (!(layoutDic is null))
				foreach (var entry in layoutDic)
				{
					var layoutPlotId = entry.Value;
					var layout = tr.GetObject(layoutPlotId, OpenMode.ForRead) as Layout;
					if (layout is null) continue;
					var blockTr = tr.GetObject(layout.BlockTableRecordId, OpenMode.ForRead) as BlockTableRecord;
					if (blockTr is null) continue;
					var layoutOwnerId = blockTr.ObjectId;
					if (!string.IsNullOrWhiteSpace(notIncludeSpace))
					{
						if (layout.LayoutName != notIncludeSpace)
							LayoutModels.Add(new LayoutModel(layoutOwnerId, layoutPlotId, layout));
					}
					else
					{
						LayoutModels.Add(new LayoutModel(layoutOwnerId, layoutPlotId, layout));
					}
				}

			tr.Commit();
		}
	}

	public void ExcludeEmptyLayout()
	{
		LayoutModels = LayoutModels.Where(b => b.BlocksObjectId != ObjectId.Null).Distinct().ToList();
	}

	public void SetPrintModels(Transaction trans)
	{
		foreach (var layout in LayoutModels)
		{
			var selAttrName = "НОМЕР_ЛИСТА";
			layout.PrintModel = PrintUtils.GetPrintModelByBlockId(trans, selAttrName, layout.BlocksObjectId);
			layout.CanonicalName = layout.PrintModel.GetCanonNameByWidthAndHeight();
		}
	}

	public void SetLayoutPlotSetting()
	{
		var standartCopier = new StandartCopier();
		PlotConfigManager.SetCurrentConfig(standartCopier.Pc3Source);

		using (var acTrans = Active.Database.TransactionManager.StartTransaction())
		{
			Layout acLayout;

			LayoutModels = LayoutModels.OrderBy(x => x.Layout.TabOrder).ToList();

			foreach (var layout in LayoutModels)
			{
				acLayout = acTrans.GetObject(layout.LayoutPlotId,
					OpenMode.ForRead) as Layout;

				if (acLayout == null) continue;

				var lm = LayoutManager.Current;
				lm.CurrentLayout = acLayout.LayoutName;

				var plotArea = acLayout.Extents;
				// Output the name of the current layout and its device
				Active.Editor.WriteMessage("\nCurrent layout: " +
				                           acLayout.LayoutName);

				Active.Editor.WriteMessage("\nCurrent device name: " +
				                           acLayout.PlotConfigurationName);

				// Get the PlotInfo from the layout
				var acPlInfo = new PlotInfo();
				acPlInfo.Layout = acLayout.ObjectId;

				// Get a copy of the PlotSettings from the layout
				var acPlSet = new PlotSettings(acLayout.ModelType);
				acPlSet.CopyFrom(acLayout);

				// Update the PlotConfigurationName property of the PlotSettings object
				var acPlSetVdr = PlotSettingsValidator.Current;
				//acPlSetVdr.SetCurrentStyleSheet(acPlSet, "monochrome.ctb");
				var isHor = layout.PrintModel.IsFormatHorizontal();

				acPlSetVdr.SetPlotType(acPlSet, PlotType.Extents);
				acPlSetVdr.SetPlotRotation(acPlSet, isHor ? PlotRotation.Degrees000 : PlotRotation.Degrees090);
				acPlSetVdr.SetPlotWindowArea(acPlSet, Get2dExtentsFrom3d(plotArea));
				acPlSetVdr.SetStdScaleType(acPlSet, StdScaleType.ScaleToFit);

				// Center the plot
				acPlSetVdr.SetPlotCentered(acPlSet, true);

				acPlSetVdr.SetPlotConfigurationName(acPlSet, "DWG_To_PDF_Uzle.pc3",
					layout.CanonicalName);
				acPlSetVdr.SetZoomToPaperOnUpdate(acPlSet, true);
				// Update the layout
				acLayout.UpgradeOpen();
				acLayout.CopyFrom(acPlSet);

				// Output the name of the new device assigned to the layout
				Active.Editor.WriteMessage("\nNew device name: " +
				                           acLayout.PlotConfigurationName);

				Active.Editor.Regen();
			}

			// Save the new objects to the database
			acTrans.Commit();
		}
	}

	private Extents2d Get2dExtentsFrom3d(Extents3d plotArea)
	{
		return new Extents2d(new Point2d(plotArea.MinPoint.X,
			plotArea.MinPoint.X), new Point2d(plotArea.MaxPoint.X, plotArea.MaxPoint.X));
	}

	public void CreateCollectionFromBlockNames(Transaction trans)
	{
		DynamicBlocks.GetLayoutsWithDynBlocks(trans, LayoutModels);

		ExcludeEmptyLayout();

		var blocksList = LayoutModels.Select(x => x.BlocksObjectId).ToList();
		if (blocksList.Count == 0) return;

		//TODO сюда вставить нумерацию листов
		BlockReferences = new List<BlockReference>();
		BlockReferences.AddRange(blocksList.Select(blockRefId => blockRefId.GetObject(OpenMode.ForWrite))
			.OfType<BlockReference>());

		//BlockUtils.BlockAttributeEnumerator(blockReferences,new []{"Page", "Лист"});
		//SetPrintModels(trans);
		//SetLayoutPlotSetting();
	}

	public void CreateLayoutCollection()
	{
		Active.UsingTransaction(CreateCollectionFromBlockNames);
	}
}