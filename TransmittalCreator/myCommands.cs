// (C) Copyright 2020 by HP Inc. 
//

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;
using DV2177.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using TransmittalCreator.Models;
using TransmittalCreator.Models.Layouts;
using TransmittalCreator.Services;
using TransmittalCreator.Services.Blocks;
using TransmittalCreator.ViewModel;
using TransmittalCreator.Views;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Table = Autodesk.AutoCAD.DatabaseServices.Table;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(TransmittalCreator.MyCommands))]

namespace TransmittalCreator
{
    public class MyCommands : Utils, IExtensionApplication
    {


        #region HvacTable

        // Modal Command with pickfirst selection
        [CommandMethod("hvac")]
        public void CreateHvacTable(Hvac hvac)
        {
            hvac.CreateHvacTable();
        }

        #endregion


        #region CreateTransPdf
        [CommandMethod("CreateTranspdf")]
        public static void CreateTransmittalAndPdf()
        {
            List<Sheet> dict = new List<Sheet>();
            List<PrintModel> printModels = new List<PrintModel>();
            Active.Document.SendStringToExecute("REGENALL ", true, false, true);
            using (Transaction tr = Active.Database.TransactionManager.StartTransaction())
            {
                //фильтр для выбора только блока
                BlockSelector blockSelector = new BlockSelector();
                blockSelector.GetFilterForSelectBlockId();
                var res = blockSelector.SelectionResult;

                if (res.Status != PromptStatus.OK)
                {
                    Active.Editor.WriteMessage("Надо выбрать блок");
                    return;
                }

                SelectionSet selSet = res.Value;
                ObjectId[] idArrayTemp = selSet.GetObjectIds();

                ObjectIdCollection idArray = new ObjectIdCollection();
                foreach (var objectId in idArrayTemp)
                {
                    BlockReference blRef = (BlockReference)tr.GetObject(objectId, OpenMode.ForRead);
                    BlockTableRecord block =
                        tr.GetObject(blRef.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                    if (!(block is null))
                    {
                        string blockName = block.Name;
                        if (blockName == "Формат" | blockName == "ФорматM25") idArray.Add(objectId);
                    }
                }

                GetSheetsFromBlocks(Active.Editor, dict, tr, idArray);
                string selAttrName = "НОМЕР_ЛИСТА";
                GetPrintParametersToPdf(Active.Editor, printModels, tr, idArray, selAttrName);

                Utils utils = new Utils();
                //utils.CreateOnlyVed(dict);
                utils.CreateJsonFile(dict);

                foreach (var printModel in printModels)
                {
                    //Active.Editor.WriteMessage("{0} печатаем ", printModel.DocNumber);
                    PlotCurrentLayout(printModel.DocNumber, printModel);
                }

                //utils.CreateOnlytrans(dict);

                tr.Commit();
            }
        }
        #endregion
        #region CreatePdfWithInterfaceSelecting

        [CommandMethod("CreatePdf")]
        public void CreatePdfName()
        {
            Active.Document.SendStringToExecute("REGENALL ", true, false, true);
            Dictionary<string, string> attrList = new Dictionary<string, string>();
            Window1 window = new Window1(new BlockViewModel(attrList));
            Application.ShowModalWindow(window);
            if (window.isClicked)
            {
                //ObjectIdCollection objectIds = Utils.SelectDynamicBlockReferences();
                List<Sheet> dict = new List<Sheet>();

                using (Transaction tr = Active.Database.TransactionManager.StartTransaction())
                {
                    BlockSelector blockSelector = new BlockSelector();
                    blockSelector.GetFilterForSelectBlockId(window.BlockName);
                    bool isExec = true;
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
                        //ObjectIdCollection idArray = new ObjectIdCollection(idArrayTemp);
                        //TODO printing X Y
                        //string selAttrName = attributeName.AttributeName;


                        FileNameCreator fileNameCreator = new FileNameCreator(window, idArrayTemp);

                        //GetPrintParametersToPdf(Active.Editor, printModels, tr, objectIds, selAttrName);

                        fileNameCreator.GetPrintParametersToPdf(tr);

                        Utils utils = new Utils();
                        utils.CreateJsonFile(dict);

                        foreach (var printModel in fileNameCreator.GetPrintModels())
                        {
                            PlotCurrentLayout(printModel.DocNumber, printModel);
                        }
                    }

                    tr.Commit();
                }
            }
        }

        #endregion

        #region CreateDwg
        [CommandMethod("CreateDwg")]
        public static void CreateDwg()
        {
            Active.Document.SendStringToExecute("REGENALL ", true, false, true);
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            Dictionary<ObjectId, bool> layersDictionary = LayerManipulation.GetLayersIsBlockedCol();
            if (doc == null) return;
            doc.LockOrUnlockLayers(false, ignoreCurrent: false, lockZero: true);

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
                        string blockName = block.Name;

                        if (blockName == "Формат") idArray.Add(objectId);
                        else if (blockName == "ФорматM25") idArray.Add(objectId);

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
                        string selAttrName = "НОМЕР_ЛИСТА";
                        string fileName = Utils.GetBlockAttributeValue(blkRef, selAttrName);

                        //HostApplicationServices hs = HostApplicationServices.Current;
                        //string path = Application.GetSystemVariable("DWGPREFIX");
                        //hs.FindFile(doc.Name, doc.Database, FindFileHint.Default);
                        string createdwgFolder = Path.GetFileNameWithoutExtension(db.OriginalFileName);

                        string folderdwg = Path.GetDirectoryName(db.OriginalFileName);
                        string dwgFilename = Path.Combine(folderdwg, fileName + ".dwg");
                        objectCopier.CopyObjectsNewDatabases(objectIds, dwgFilename);
                        // objectCopier.CopyObjectsBetweenDatabases(objectIds, dwgFilename);
                        Active.Editor.WriteMessage("{0} сохранен", dwgFilename);
                        string newFileName = ZoomFilesAndSave(dwgFilename);
                        File.Delete(dwgFilename);
                        System.IO.File.Move(newFileName, dwgFilename);
                    }
                }

                doc.LockLayers(layersDictionary);
                tr.Commit();
            }
        }



        public static string ZoomFilesAndSave(string fileName)
        {
            string newFileName = "";
            using (Database db = new Database(false, false))
            {
                db.ReadDwgFile(fileName, FileOpenMode.OpenForReadAndReadShare, true, null);
                Database prevDb = HostApplicationServices.WorkingDatabase;
                HostApplicationServices.WorkingDatabase = db;
                db.UpdateExt(true);
                using (ViewportTable vTab = db.ViewportTableId.GetObject(OpenMode.ForRead) as ViewportTable)
                {
                    ObjectId acVptId = vTab["*Active"];
                    using (ViewportTableRecord vpTabRec = acVptId.GetObject(OpenMode.ForWrite) as ViewportTableRecord)
                    {
                        double scrRatio = (vpTabRec.Width / vpTabRec.Height);
                        Matrix3d matWCS2DCS = Matrix3d.PlaneToWorld(vpTabRec.ViewDirection);
                        matWCS2DCS = Matrix3d.Displacement(vpTabRec.Target - Point3d.Origin) * matWCS2DCS;
                        matWCS2DCS = Matrix3d.Rotation(-vpTabRec.ViewTwist,
                                         vpTabRec.ViewDirection,
                                         vpTabRec.Target)
                                     * matWCS2DCS;
                        matWCS2DCS = matWCS2DCS.Inverse();
                        Extents3d extents = new Extents3d(db.Extmin, db.Extmax);
                        extents.TransformBy(matWCS2DCS);
                        double width = (extents.MaxPoint.X - extents.MinPoint.X);
                        double height = (extents.MaxPoint.Y - extents.MinPoint.Y);
                        Point2d center = new Point2d((extents.MaxPoint.X + extents.MinPoint.X) * 0.5,
                            (extents.MaxPoint.Y + extents.MinPoint.Y) * 0.5);
                        if (width > (height * scrRatio))
                            height = width / scrRatio;
                        vpTabRec.Height = height;
                        vpTabRec.Width = height * scrRatio;
                        vpTabRec.CenterPoint = center;
                    }
                }

                HostApplicationServices.WorkingDatabase = prevDb;
                newFileName = fileName.Substring(0, fileName.Length - 4) + "z.dwg";
                db.SaveAs(newFileName, DwgVersion.Current);
            }

            return newFileName;
        }
        #endregion




        [CommandMethod("CrLayTr")]
        public void CreateTransmitallFromLayout()
        {
            string fileName = (string)Application.GetSystemVariable("DWGNAME");
            string path = (string)Application.GetSystemVariable("DWGPREFIX");
            string allPath = Path.Combine(path, fileName);
            Active.Database.SaveAs(allPath, true, DwgVersion.Current, Active.Database.SecurityParameters);

            List<Sheet> sheets = new List<Sheet>();
            LayoutModelCollection layoutModelCollection = new LayoutModelCollection();
            layoutModelCollection.ListLayouts("Model");
            string[] blockNames = { "ФорматM25", "Формат" };
            using (Transaction trans = Active.Database.TransactionManager.StartTransaction())
            {
                using (Transaction tr = Active.Database.TransactionManager.StartTransaction())
                {

                }
                DynamicBlockFinder dynamicBlocks = new DynamicBlockFinder(layoutModelCollection)
                {
                    BlockNameToSearch = blockNames
                };

                dynamicBlocks.GetLayoutsWithDynBlocks(trans);

                layoutModelCollection.DeleteEmptyLayout();

                var blocksList = layoutModelCollection.LayoutModels.Select(x => x.BlocksObjectId).ToArray();
                if (blocksList.Length == 0) return;

                ObjectIdCollection blockIdCollection = new ObjectIdCollection(blocksList);

                layoutModelCollection.SetPrintModels(trans);
                layoutModelCollection.SetLayoutPlotSetting();
                trans.Commit();
            }

            using (Transaction trans = Active.Database.TransactionManager.StartTransaction())
            {
                var packageCreator = new PrintPackageCreator(layoutModelCollection, "Форма 3 ГОСТ Р 21.1101-2009 M25");


                var layoutPackages = packageCreator.PrintPackageModels;
                var layoutTree = new LayoutTreeViewModel();


                LayoutTreeView window = new LayoutTreeView(packageCreator.PrintPackageModels);
                Application.ShowModalWindow(window);

                if (!window.isClicked) return;

                var printPackages = window._printPackages;
                packageCreator.PrintPackageModels = printPackages;

                packageCreator.PublishAllPackages();

                var blockIds = printPackages.SelectMany(x => x.Layouts.Select(y => y.BlocksObjectId)).ToArray();

                Utils utils = new Utils();
                GetSheetsFromBlocks(Active.Editor, sheets, trans, new ObjectIdCollection(blockIds));

                utils.CreateJsonFile(sheets);
            }


        }



        [CommandMethod("CtTransm")]
        public void ListAttributes()
        {
            Dictionary<string, string> attrList = new Dictionary<string, string>();

            MainWindow window = new MainWindow(new BlockViewModel(attrList));

            Application.ShowModalWindow(window);

            if (window.isClicked == true)
            {
                //var objectIds = Utils.GetAllCurrentSpaceBlocksByName(window.NameBlock.Text);
                ObjectId mSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(Active.Database);
                ObjectIdCollection objectIds = Utils.SelectDynamicBlockReferences(mSpaceId);

                List<Sheet> dict = new List<Sheet>();
                List<PrintModel> printModels = new List<PrintModel>();

                BlockAttribute objectNameEn = window.ComboObjectNameEn.SelectedItem as BlockAttribute;
                BlockAttribute objectNameRu = window.ComboObjectNameRu.SelectedItem as BlockAttribute;

                BlockAttribute position = window.ComboBoxPosition.SelectedItem as BlockAttribute;
                BlockAttribute nomination = window.ComboBoxNomination.SelectedItem as BlockAttribute;
                BlockAttribute comment = window.ComboBoxComment.SelectedItem as BlockAttribute;
                BlockAttribute trItem = window.ComboBoxTrItem.SelectedItem as BlockAttribute;
                BlockAttribute trDocNumber = window.ComboBoxTrDocNumber.SelectedItem as BlockAttribute;
                BlockAttribute trDocTitleEn = window.ComboBoxTrDocTitleEn.SelectedItem as BlockAttribute;
                BlockAttribute trDocTitleRu = window.ComboBoxTrDocTitleRu.SelectedItem as BlockAttribute;

                AttributModel attributModel = new AttributModel(objectNameEn, objectNameRu, position, nomination,
                    comment, trItem, trDocNumber, trDocTitleEn, trDocTitleRu);

                using (Transaction tr = Active.Database.TransactionManager.StartTransaction())
                {
                    MyCommands.GetSheetsFromBlocks(Active.Editor, dict, tr, objectIds);
                    string selAttrName = "НОМЕР_ЛИСТА";
                    MyCommands.GetPrintParametersToPdf(Active.Editor, printModels, tr, objectIds, selAttrName);

                    if (window.transmittalCheckBox.IsChecked == true)
                    {
                        Utils utils = new Utils();
                        utils.CreateOnlyVed(dict);
                        utils.CreateOnlytrans(dict);
                        foreach (var printModel in printModels)
                        {
                            PlotCurrentLayout(printModel.DocNumber, printModel);
                        }
                    }
                    else
                    {
                        //Utils utils = new Utils();
                        //utils.CreateOnlyVed(dict);
                        foreach (var printModel in printModels)
                        {
                            //PlotCurrentLayout(printModel.DocNumber, printModel.BlockExtents3d, printModel.StampViewName);
                        }
                    }
                    tr.Commit();
                }
            }
        }

        public void Initialize()
        {
            string executablePath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            ProxyDomain pd = new ProxyDomain();
            Assembly assembly = pd.GetAssembly(System.IO.Path.Combine(executablePath, "MaterialDesignThemes.Wpf.dll"));

            Assembly assembly1 = pd.GetAssembly(Path.Combine(executablePath, "MaterialDesignColors.dll"));

            if (assembly != null | assembly1 != null)
            {
                Active.Editor.WriteMessage("style dlls not load");
            }


            StandartCopier standartCopier = new StandartCopier();
            if (!File.Exists(standartCopier.Pc3Location) & !File.Exists(standartCopier.PmpLocation))
            {
                bool isCopied = standartCopier.CopyParamsFiles();
                if (isCopied)
                    Active.Editor.WriteMessage("Файлы {0}, {1} скопированы", standartCopier.Pc3Location,
                        standartCopier.PmpLocation);
                else
                {
                    Active.Editor.WriteMessage(
                        "Не удалось скопировать файлы настройки, скопируйте с сервера \\\\uz-fs\\install\\CAD\\Blocks файлы {0}  в {1} и {2} ",
                        standartCopier.Pc3Dest, standartCopier.Pc3Location, standartCopier.PmpLocation);
                }
            }
            else
            {
                Active.Editor.WriteMessage("Файлы настройки присутствуют, для перевода в pdf наберите CreateTranspdf");
            }

            Active.Editor.WriteMessage("Файлы настройки присутствуют, для перевода в pdf наберите CreateTranspdf");
        }

        public void Terminate()
        {
            throw new NotImplementedException();
        }


        [CommandMethod("OPSV")]
        public static void OpenSaveDwgFiles()
        {
            List<Sheet> dict = new List<Sheet>();
            try
            {
                var path = @"C:\Users\yusufzhon.marasulov\Desktop\test";
                DirectoryInfo d = new DirectoryInfo(path);
                FileInfo[] Files = d.GetFiles("*.dwg");
                foreach (FileInfo file in Files)
                {
                    var fileName = Path.GetFileName(file.FullName);
                    string dwgFlpath = file.FullName;
                    using (Database db = new Database(false, true))
                    {
                        db.ReadDwgFile(dwgFlpath, FileOpenMode.OpenForReadAndAllShare, false, null);
                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            ObjectId mSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
                            ObjectIdCollection idArray = Utils.SelectDynamicBlockReferences(mSpaceId);
                            GetSheetsFromBlocks(Active.Editor, dict, tr, idArray);
                            tr.Commit();
                        }
                        db.SaveAs(dwgFlpath, DwgVersion.Current);
                    }
                }
                Application.ShowAlertDialog("All files processed");
            }
            catch (System.Exception ex)
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(ex.ToString());
            }
        }

        static public void BatchTransmittal(List<string> docsToPlot)
        {
            List<Sheet> dict = new List<Sheet>();
            List<PrintModel> printModels = new List<PrintModel>();
            Active.Document.SendStringToExecute("REGENALL ", true, false, true);
            Document doc = Active.Document;
            foreach (string filename in docsToPlot)
            {
                using (DocumentLock doclock = doc.LockDocument())
                {
                    Database db = new Database(false, true);
                    db.ReadDwgFile(filename, System.IO.FileShare.Read, true, "");

                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        ObjectId mSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
                        ObjectIdCollection idArray = Utils.SelectDynamicBlockReferences(mSpaceId);


                        //TODO надо проверить предыдущий и нижние методы на поиск по Id
                        GetSheetsFromBlocks(Active.Editor, dict, tr, idArray);
                        string selAttrName = "НОМЕР_ЛИСТА";
                        GetPrintParametersToPdf(Active.Editor, printModels, tr, idArray, selAttrName);

                        Utils utils = new Utils();
                        //utils.CreateOnlyVed(dict);
                        utils.CreateJsonFile(dict);

                        foreach (var printModel in printModels)
                        {
                            //Active.Editor.WriteMessage("{0} печатаем ", printModel.DocNumber);
                            PlotCurrentLayout(printModel.DocNumber, printModel);
                        }
                        tr.Commit();
                    }
                }
            }

        }
    }


}