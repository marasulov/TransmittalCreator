using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using DV2177.Common;
using TransmittalCreator.Services;
using PlotType = Autodesk.AutoCAD.DatabaseServices.PlotType;

namespace TransmittalCreator.Models.Layouts
{
    public class LayoutModelCollection
    {
        public List<LayoutModel> LayoutModels { get; set; } = new List<LayoutModel>();

        public void ListLayouts(string notIncludeSpace = "")
        {
            Database db = Active.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DBDictionary layoutDic = tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead, false) as DBDictionary;

                foreach (DBDictionaryEntry entry in layoutDic)
                {
                    ObjectId layoutPlotId = entry.Value;
                    Layout layout = tr.GetObject(layoutPlotId, OpenMode.ForRead) as Layout;
                    var blockTR = tr.GetObject(layout.BlockTableRecordId, OpenMode.ForRead) as BlockTableRecord;
                    var layoutOwnerId = blockTR.ObjectId;
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
            //List<LayoutModel> layoutModels = new List<LayoutModel>();
            //LayoutModel layoutModel = GetOne(x => x.BlocksObjectId == ObjectId.Null);
            //LayoutModels.Remove(layoutModel);
        }

        private LayoutModel GetOne(Func<LayoutModel, bool> predicate)
        {
            LayoutModel account = LayoutModels.FirstOrDefault(predicate);
            return account;
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
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            StandartCopier standartCopier = new StandartCopier();
            PlotConfig pConfig = PlotConfigManager.SetCurrentConfig(standartCopier.Pc3Location);

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                Layout acLayout;

                var layouts = LayoutModels.OrderBy(x => x.Layout.TabOrder).ToList();

                foreach (var layout in layouts)
                {
                    acLayout = acTrans.GetObject(layout.LayoutPlotId,
                        OpenMode.ForRead) as Layout;

                    if (acLayout == null) continue;
                    var plotArea = acLayout.Extents;
                    // Output the name of the current layout and its device
                    acDoc.Editor.WriteMessage("\nCurrent layout: " +
                                              acLayout.LayoutName);

                    acDoc.Editor.WriteMessage("\nCurrent device name: " +
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
                    acPlSetVdr.SetPlotWindowArea(acPlSet, new Extents2d(new Point2d(plotArea.MinPoint.X,
                        plotArea.MinPoint.X), new Point2d(plotArea.MaxPoint.X, plotArea.MaxPoint.X)));
                    acPlSetVdr.SetStdScaleType(acPlSet, StdScaleType.ScaleToFit);

                    // Center the plot
                    acPlSetVdr.SetPlotCentered(acPlSet, true);

                    acPlSetVdr.SetPlotConfigurationName(acPlSet, "DWG_To_PDF_Uzle.pc3",
                        layout.CanonicalName);
                    acPlSetVdr.SetZoomToPaperOnUpdate(acPlSet, true);
                    //acPlInfo.OverrideSettings = acPlSet;
                    // Validate the plot info
                    //PlotInfoValidator acPlInfoVdr = new PlotInfoValidator();
                    //acPlInfoVdr.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
                    //acPlInfoVdr.Validate(acPlInfo);
                    // Update the layout
                    acLayout.UpgradeOpen();
                    acLayout.CopyFrom(acPlSet);
//TODO refresh layout

                    // Output the name of the new device assigned to the layout
                    acDoc.Editor.WriteMessage("\nNew device name: " +
                                              acLayout.PlotConfigurationName);
                }

                // Save the new objects to the database
                acTrans.Commit();
            }
        }
    }
}