using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using TransmittalCreator.DBCad;
using TransmittalCreator.Services;
using TransmittalCreator.Services.Blocks;
using PlotType = Autodesk.AutoCAD.DatabaseServices.PlotType;

namespace TransmittalCreator.Models.Layouts
{
    public class LayoutModelCollection
    {
        public List<LayoutModel> LayoutModels { get; set; } = new List<LayoutModel>();
        public DynamicBlockFinder DynamicBlocks { get; set; }

        public LayoutModelCollection(DynamicBlockFinder dynamicBlocks)
        {
            DynamicBlocks = dynamicBlocks;
        }

        public void ListLayouts(string notIncludeSpace = "")
        {
            Database db = Active.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DBDictionary layoutDic = tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead, false) as DBDictionary;

                if (!(layoutDic is null))
                    foreach (DBDictionaryEntry entry in layoutDic)
                    {
                        ObjectId layoutPlotId = entry.Value;
                        Layout layout = tr.GetObject(layoutPlotId, OpenMode.ForRead) as Layout;
                        if (layout is null) continue;
                        var blockTr = tr.GetObject(layout.BlockTableRecordId, OpenMode.ForRead) as BlockTableRecord;
                        if (blockTr is null) continue;
                        var layoutOwnerId = blockTr.ObjectId;
                        if (!string.IsNullOrWhiteSpace(notIncludeSpace))
                        {
                            if (layout.LayoutName != notIncludeSpace)
                            {
                                LayoutModels.Add(new LayoutModel(layoutOwnerId, layoutPlotId, layout));
                            }
                        }
                        else LayoutModels.Add(new LayoutModel(layoutOwnerId, layoutPlotId, layout));
                    }

                tr.Commit();
            }
        }

        public void DeleteEmptyLayout()
        {
            LayoutModels = LayoutModels.Where(b => b.BlocksObjectId != ObjectId.Null).Distinct().ToList();
        }

        public void SetPrintModels(Transaction trans)
        {
            foreach (var layout in LayoutModels)
            {
                string selAttrName = "НОМЕР_ЛИСТА";
                layout.PrintModel = Utils.GetPrintModelByBlockId(trans, selAttrName, layout.BlocksObjectId);
                layout.CanonicalName = layout.PrintModel.GetCanonNameByWidthAndHeight();
            }
        }

        public void SetLayoutPlotSetting()
        {

            StandartCopier standartCopier = new StandartCopier();
            PlotConfigManager.SetCurrentConfig(standartCopier.Pc3Location);

            using (Transaction acTrans = Active.Database.TransactionManager.StartTransaction())
            {
                Layout acLayout;

                LayoutModels = LayoutModels.OrderBy(x => x.Layout.TabOrder).ToList();

                foreach (var layout in LayoutModels)
                {
                    acLayout = acTrans.GetObject(layout.LayoutPlotId,
                        OpenMode.ForRead) as Layout;
                    
                    if (acLayout == null) continue;
                    
                    LayoutManager lm = LayoutManager.Current;
                    lm.CurrentLayout = acLayout.LayoutName;
                    
                    var plotArea = acLayout.Extents;
                    // Output the name of the current layout and its device
                    Active.Editor.WriteMessage("\nCurrent layout: " +
                                              acLayout.LayoutName);

                    Active.Editor.WriteMessage("\nCurrent device name: " +
                                              acLayout.PlotConfigurationName);

                    // Get the PlotInfo from the layout
                    PlotInfo acPlInfo = new PlotInfo();
                    acPlInfo.Layout = acLayout.ObjectId;

                    // Get a copy of the PlotSettings from the layout
                    PlotSettings acPlSet = new PlotSettings(acLayout.ModelType);
                    acPlSet.CopyFrom(acLayout);

                    // Update the PlotConfigurationName property of the PlotSettings object
                    PlotSettingsValidator acPlSetVdr = PlotSettingsValidator.Current;
                    //acPlSetVdr.SetCurrentStyleSheet(acPlSet, "monochrome.ctb");
                    bool isHor = layout.PrintModel.IsFormatHorizontal();

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
        
        private void CreateCollectionFromBlockNames(Transaction trans)
        {
            DynamicBlocks.GetLayoutsWithDynBlocks(trans, LayoutModels);

            DeleteEmptyLayout();

            var blocksList = this.LayoutModels.Select(x => x.BlocksObjectId).ToArray();
            if (blocksList.Length == 0) return;

            this.SetPrintModels(trans);
            this.SetLayoutPlotSetting();
        }

        public void CreateLayoutCollection()
        {
            Active.UsingTransaction(CreateCollectionFromBlockNames);
        }
        
        
    }
}