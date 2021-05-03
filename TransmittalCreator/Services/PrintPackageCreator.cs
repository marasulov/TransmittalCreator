using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.PlottingServices;
using System.Collections.Generic;
using System.Linq;
using TransmittalCreator.Models;
using TransmittalCreator.Models.Layouts;

namespace TransmittalCreator.Services
{
    public class PrintPackageCreator
    {
        public List<PrintPackageModel> PrintPackageModels { get; set; } = new List<PrintPackageModel>();
        private LayoutModelCollection LayoutModels { get; set; }
        public string MainPageName { get; set; }

        public PrintPackageCreator(LayoutModelCollection layoutModels, string mainPageName)
        {
            LayoutModels = layoutModels;
            MainPageName = mainPageName;
            CreatePrintPackages();
        }

        private void CreatePrintPackages()
        {
            var layoutModels = LayoutModels.LayoutModels.ToArray();
            string mainPageDocNum = "";
            foreach (var layout in layoutModels)
            {
                var printModel = layout.PrintModel;

                if (printModel.StampViewName == MainPageName)
                {
                    List<LayoutModel> layouts = new List<LayoutModel> { layout };
                    PrintPackageModels.Add(new PrintPackageModel(printModel.DocNumber, layouts));
                    mainPageDocNum = printModel.DocNumber;
                }
                else
                {
                    var printPackageModel = PrintPackageModels.FirstOrDefault(x => x.PdfFileName == mainPageDocNum);
                    printPackageModel?.Layouts.Add(layout);
                }
            }
        }

        public void PublishAllPackages()
        {
            foreach (var package in PrintPackageModels)
            {
                PublishPackage(package);
            }
        }


        public void PublishPackage(PrintPackageModel package)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            short bgPlot = (short)Application.GetSystemVariable("BACKGROUNDPLOT");
            Application.SetSystemVariable("BACKGROUNDPLOT", 0);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DsdEntryCollection collection = new DsdEntryCollection();
                foreach (var layoutModel in package.Layouts)
                {
                    DsdEntry entry = new DsdEntry();
                    entry.DwgName = package.DwgPath + package.DwgFileName;
                    entry.Layout = layoutModel.LayoutName;
                    entry.Title = "Layout_" + layoutModel.LayoutName;
                    entry.NpsSourceDwg = entry.DwgName;
                    entry.Nps = "Setup1";

                    collection.Add(entry);
                }

                DsdData dsdData = new DsdData();
                dsdData.SheetType = SheetType.MultiPdf; //SheetType.MultiPdf
                dsdData.ProjectPath = package.DwgPath;
                dsdData.DestinationName =
                    $"{dsdData.ProjectPath}{package.PdfFileName}.pdf";
                if (System.IO.File.Exists(dsdData.DestinationName))
                    System.IO.File.Delete(dsdData.DestinationName);
                dsdData.SetDsdEntryCollection(collection);
                string dsdFile = $"{dsdData.ProjectPath}{package.PdfFileName}.dsd";

                dsdData.WriteDsd(dsdFile);
                dsdData.ReadDsd(dsdFile);

                System.IO.File.Delete(dsdFile);
                var plotConfig = PlotConfigManager.SetCurrentConfig("DWG_To_PDF_Uzle.pc3");
                var publisher = Application.Publisher;

                publisher.AboutToBeginPublishing +=
                    new Autodesk.AutoCAD.Publishing.
                        AboutToBeginPublishingEventHandler(
                            Publisher_AboutToBeginPublishing);
                dsdData.PromptForDwfName = false;
                publisher.PublishExecute(dsdData, plotConfig);
                tr.Commit();
            }

            Application.SetSystemVariable("BACKGROUNDPLOT", bgPlot);
        }

        static void Publisher_AboutToBeginPublishing(object sender,
            Autodesk.AutoCAD.Publishing.AboutToBeginPublishingEventArgs e)
        {
            Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nPublishing!!");
        }
    }
}