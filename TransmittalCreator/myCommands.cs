// (C) Copyright 2020 by HP Inc. 
//

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using TransmittalCreator.DBCad;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Autodesk.AutoCAD.GraphicsInterface;
using LeaderPlacement;
using OfficeOpenXml;
using TransmittalCreator.Models;
using TransmittalCreator.Models.Layouts;
using TransmittalCreator.Services;
using TransmittalCreator.Services.Blocks;
using TransmittalCreator.Services.Files;
using TransmittalCreator.ViewModel;
using TransmittalCreator.Views;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(TransmittalCreator.MyCommands))]

namespace TransmittalCreator
{
    public class MyCommands : Utils, IExtensionApplication
    {
        const string path = @"C:\Users\yusufzhon.marasulov\Desktop\Test1\1\";
        private FileSystemWatcher _fsw = null;
        private bool _drawing = false;
        private int _linesPrevQty;
        int _arcPrevQty;


        [CommandMethod("EC")]
        public void EventCommand()
        {
            var dm = Application.DocumentManager;
            var doc = dm.MdiActiveDocument;
            if (doc == null)
                return;
            var ed = doc.Editor;
            // We'll start by creating one square for each file in the
            // specified location. The nSquares() function uses some
            // global state for the index and the total count

            nSquares(ed);
            // Create a FileSystemWatcher for the path, looking for
            // write changes and drawing more squares as needed
            if (_fsw == null)
            {
                _fsw = new FileSystemWatcher(path, "*.*");
                _fsw.Changed += (o, s) => nSquaresInContext(dm, ed, path);
                _fsw.NotifyFilter = NotifyFilters.LastWrite
                                    | NotifyFilters.FileName;
                _fsw.EnableRaisingEvents = true;
                ed.WriteMessage("\nWatching \"{0}\" for changes.", path);
            }
        }


#pragma warning disable 1998
        private async void nSquaresInContext(DocumentCollection dc, Editor ed, string path)
        {
            // Protect the command-calling function with a flag to avoid
            // eInvalidInput failures
            if (!_drawing)
            {
                _drawing = true;
                // Call our square creation function asynchronously
                await dc.ExecuteInCommandContextAsync(
                    async (o) => nSquares(ed),
                    null
                );
                _drawing = false;
            }
        }
#pragma warning restore 1998
        private void nSquares(Editor ed)
        {
            // Draw squares until we have enough (the total might
            // change, hence the need for global state)
            Active.Editor.WriteMessage("file changed");

            string filePath = @"C:\Users\yusufzhon.marasulov\Desktop\Test1\1\6-10 кВ опоры.xlsx";

            // Create a Selection Filter which selects all Objects on a given Layer... in this case "0"
            TypedValue[] tvs = new TypedValue[] { new TypedValue((int)DxfCode.LayerName, "0") };
            SelectionFilter sf = new SelectionFilter(tvs);
            // Select all Objects on Layer
            PromptSelectionResult psr = ed.SelectAll(sf);
            SelectionSet ss = psr.Value;
            if (ss != null)
            {
                using (Transaction tr = Active.Database.TransactionManager.StartTransaction())
                {
                    foreach (SelectedObject so in ss)
                    {
                        DBObject ob = tr.GetObject(so.ObjectId, OpenMode.ForWrite);
                        ob.Erase();
                    }
                    tr.Commit();
                }
            }


            FileInfo fileInfo = new FileInfo(filePath);
            using (ExcelPackage package = new ExcelPackage(fileInfo))
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                //create an instance of the the first sheet in the loaded file
                ExcelWorksheet worksheet = package.Workbook.Worksheets[8];
                //TrimEmptyRows(worksheet);
                var rowCount = worksheet.Dimension.Rows;

                using (Active.Document.LockDocument())
                using (var tr = Active.Document.TransactionManager.StartTransaction())
                using (var pline = new Polyline())
                {
                    List<Line> lines = new List<Line>();
                    List<Arc> arcs = new List<Arc>();
                    for (int i = 1; i < rowCount - 1; i++)
                    {
                        var xValText = worksheet.Cells[i, 3].Text;
                        var yValText = worksheet.Cells[i, 4].Text;

                        if (!string.IsNullOrEmpty(xValText))
                        {
                            var point3d = CreatePoint3d(xValText, yValText);
                            pline.AddVertexAt(i - 1, new Point2d(point3d.X, point3d.Y), 0.0, 0.0, 0.0);
                        }
                        arcs.Add(CreateArcFromPoints(worksheet, i));
                        Line acLine = CreateLineFromPoints(worksheet, i);
                        acLine.SetDatabaseDefaults();
                        lines.Add(acLine);
                        Active.Editor.WriteMessage($"{i} - {xValText} \n");
                    }

                    var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(Active.Database), OpenMode.ForWrite);
                    ms.AppendEntity(pline);
                    tr.AddNewlyCreatedDBObject(pline, true);
                    foreach (var line in lines)
                    {
                        ms.AppendEntity(line);
                        tr.AddNewlyCreatedDBObject(line, true);
                    }

                    foreach (var arc in arcs)
                    {
                        ms.AppendEntity(arc);
                        tr.AddNewlyCreatedDBObject(arc, true);
                    }

                    tr.Commit();
                    Active.Editor.WriteMessage($"\n геометрия обновлена \n " +
                    $"было {_linesPrevQty} создано {lines.Count} \n" +
                    $"было {_arcPrevQty} создано {arcs.Count} \n");
                    _linesPrevQty = lines.Count;
                    _arcPrevQty = arcs.Count;
                }
            }
            Active.Editor.Regen();
        }

        private static Arc CreateArcFromPoints(ExcelWorksheet worksheet, int i)
        {
            var x1ArcText = worksheet.Cells[i, 15].Text;
            var y1ArcText = worksheet.Cells[i, 16].Text;
            var x2ArcText = worksheet.Cells[i, 17].Text;
            var y2ArcText = worksheet.Cells[i, 18].Text;
            var x3ArcText = worksheet.Cells[i, 19].Text;
            var y3ArcText = worksheet.Cells[i, 20].Text;

            CircularArc3d carc = new CircularArc3d(CreatePoint3d(x1ArcText, y1ArcText),
                CreatePoint3d(x2ArcText, y2ArcText),
                CreatePoint3d(x3ArcText, y3ArcText));

            return CircArc2Arc(carc);
        }

        private static Line CreateLineFromPoints(ExcelWorksheet worksheet, int i)
        {
            var x1ValText = worksheet.Cells[i, 9].Text;
            var y1ValText = worksheet.Cells[i, 10].Text;
            var x2ValText = worksheet.Cells[i, 11].Text;
            var y2ValText = worksheet.Cells[i, 12].Text;

            Line acLine = new Line(CreatePoint3d(x1ValText, y1ValText), CreatePoint3d(x2ValText, y2ValText));

            return acLine;
        }

        static Point3d CreatePoint3d(string x1ValText, string y1ValText)
        {
            double x = !string.IsNullOrEmpty(x1ValText) ? double.Parse(x1ValText) : 0.0;
            double y = !string.IsNullOrEmpty(y1ValText) ? double.Parse(y1ValText) : 0.0;

            return new Point3d(x, y, 0);
        }

        private double GetIntFromCell(string pointVal)
        {
            return !string.IsNullOrEmpty(pointVal) ? int.Parse(pointVal) : 0;
        }

        private static Arc CircArc2Arc(CircularArc3d circArc)
        {
            Point3d center = circArc.Center;
            Vector3d normal = circArc.Normal;
            Vector3d refVec = circArc.ReferenceVector;
            Plane plane = new Plane(center, normal);
            double ang = refVec.AngleOnPlane(plane);
            return new Arc(
                center,
                normal,
                circArc.Radius,
                circArc.StartAngle + ang,
                circArc.EndAngle + ang
            );
        }

        private void TrimEmptyRows(ExcelWorksheet worksheet)
        {
            //loop all rows in a file
            for (int i = worksheet.Dimension.Start.Row; i <=
                                                        worksheet.Dimension.End.Row; i++)
            {
                bool isRowEmpty = true;
                //loop all columns in a row
                for (int j = worksheet.Dimension.Start.Column; j <= worksheet.Dimension.End.Column; j++)
                {
                    if (worksheet.Cells[i, j].Value != null)
                    {
                        isRowEmpty = false;
                        break;
                    }
                }
                if (isRowEmpty)
                {
                    worksheet.DeleteRow(i);
                }
            }
        }
        //worksheet.TrimLastEmptyRows();

        //public static void TrimLastEmptyRows(this ExcelWorksheet worksheet)
        //{
        //    while (worksheet.IsLastRowEmpty())
        //        worksheet.DeleteRow(worksheet.Dimension.End.Row);
        //}
        //public static bool IsLastRowEmpty(this ExcelWorksheet worksheet)
        //{
        //    var empties = new List<bool>();

        //    for (int i = 1; i <= worksheet.Dimension.End.Column; i++)
        //    {
        //        var rowEmpty = worksheet.Cells[worksheet.Dimension.End.Row, i].Value == null ? true : false;
        //        empties.Add(rowEmpty);
        //    }

        //    return empties.All(e => e);
        //}

        [CommandMethod("ECX")]
        public void StopEventCommand()
        {
            var dm = Application.DocumentManager;
            var doc = dm.MdiActiveDocument;
            if (doc == null)
                return;
            var ed = doc.Editor;
            if (_fsw != null)
            {
                _fsw.Dispose();
                _fsw = null;
            }
            ed.WriteMessage("\nNo longer watching folder.");
        }

        [CommandMethod("MYSL")]
        public static void SplinedLeader()
        {
            Document doc
                = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;
            SplinedLeaderJig jig = new SplinedLeaderJig();
            bool bSuccess = true;
            bool bComplete = false;
            while (bSuccess && !bComplete)
            {
                SplinedLeaderJig._mIsJigStarted = false;
                PromptResult dragres = ed.Drag(jig);
                bSuccess = (dragres.Status == PromptStatus.OK);
                if (bSuccess)
                    jig.addVertex();
                bComplete = (dragres.Status == PromptStatus.None);
                if (bComplete)
                {
                    jig.removeLastVertex();
                }
            }
            if (bComplete)
            {
                // Append entity to DB
                Transaction tr = db.TransactionManager.StartTransaction();
                using (tr)
                {
                    BlockTable bt = tr.GetObject(
                        db.BlockTableId, OpenMode.ForRead
                    ) as BlockTable;
                    BlockTableRecord ms =
                        bt[BlockTableRecord.ModelSpace].GetObject
                            (OpenMode.ForWrite) as BlockTableRecord;
                    ms.AppendEntity(jig.getEntity());
                    tr.AddNewlyCreatedDBObject(jig.getEntity(),
                        true);
                    tr.Commit();
                }
            }
        }

        [CommandMethod("DL")]
        public void DirectionalLeader()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;
            // Ask the user for the string and the start point of the leader
            var pso = new PromptStringOptions("\nEnter text");
            pso.AllowSpaces = true;
            var pr = ed.GetString(pso);
            if (pr.Status != PromptStatus.OK)
                return;
            var ppr = ed.GetPoint("\nStart point of leader");
            if (ppr.Status != PromptStatus.OK)
                return;
            // Start a transaction, as we'll be jigging a db-resident object
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt =
                    (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead, false);
                var btr =
                    (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite, false);
                // Create and pass in an invisible MLeader
                // This helps avoid flickering when we start the jig
                var ml = new MLeader();
                ml.Visible = false;
                // Create jig
                var jig = new LeaderJig.DirectionalLeaderJig(pr.StringResult, ppr.Value, ml);
                // Add the MLeader to the drawing: this allows it to be displayed
                btr.AppendEntity(ml);
                tr.AddNewlyCreatedDBObject(ml, true);
                // Set end point in the jig
                var res = ed.Drag(jig);
                // If all is well, commit
                if (res.Status == PromptStatus.OK)
                {
                    tr.Commit();
                }
            }
        }


        [CommandMethod("Dci", CommandFlags.NoPaperSpace)]
        public static void DrawEntities()
        {
            var dwg = Active.Document;
            var ed = dwg.Editor;

            try
            {
                CircleDrawJig jig = new CircleDrawJig();
                jig.DrawEntities();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nInitializing error:\n{ex.Message}\n");
            }
        }


        [CommandMethod("MyMoveJig")]
        public static void RunMyCommand()
        {
            var dwg = Active.Document;
            var ed = dwg.Editor;

            try
            {
                DimMoveJig jig = new DimMoveJig();
                jig.Move();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nInitializing error:\n{ex.Message}\n");
            }
        }


        [CommandMethod("TestEntityRotateJigger")]
        public static void TestEntityRotateJigger_Method()
        {
            Editor ed = Active.Editor;
            Database db = HostApplicationServices.WorkingDatabase;

            try
            {
                PromptEntityResult selRes = ed.GetEntity("\nPick an entity:");
                PromptPointResult ptRes = ed.GetPoint("\nBase Point: ");
                if (selRes.Status == PromptStatus.OK && ptRes.Status == PromptStatus.OK)
                {
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        Entity ent = tr.GetObject(selRes.ObjectId, OpenMode.ForWrite) as Entity;
                        if (ent != null)
                        {
                            using (Transaction tr1 = db.TransactionManager.StartTransaction())
                            {
                                ent.Highlight();
                                tr1.Commit();
                            }

                            if (EntityRotateJigger.Jig(ent, ptRes.Value))
                                tr.Commit();
                            else
                                tr.Abort();
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage(ex.ToString());
            }
        }


        #region HvacTable

        [CommandMethod("JigLine")]
        public static void JigLine()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (LineJigger.Jig())
            {
                doc.Editor.WriteMessage("\nsuccess\n");
            }
            else
            {
                doc.Editor.WriteMessage("\nfailure\n");
            }
        }

        // Modal Command with pickfirst selection
        [CommandMethod("hvacTable")]
        public static void CreateHvac()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;

            if (Hvac.CreateHvacTable())
            {
                doc.Editor.WriteMessage("\nsuccess\n");
            }
            else
            {
                doc.Editor.WriteMessage("\nfailure\n");
            }


        }

        #endregion


        #region CreateTransPdf

        [CommandMethod("CreateTranspdf")]
        public static void CreateTransmittalAndPdf()
        {
            List<Sheet> dict = new List<Sheet>();
            List<PrintModel> printModels = new List<PrintModel>();
            Active.Document.SendStringToExecute("REGENALL ", true, false, true);
            using (Transaction tr = DBCad.Active.Database.TransactionManager.StartTransaction())
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
                        const string selAttrName = "НОМЕР_ЛИСТА";
                        string fileName = Utils.GetBlockAttributeValue(blkRef, selAttrName);

                        //HostApplicationServices hs = HostApplicationServices.Current;
                        //string path = Application.GetSystemVariable("DWGPREFIX");
                        //hs.FindFile(doc.Name, doc.Database, FindFileHint.Default);
                        //string createdwgFolder = Path.GetFileNameWithoutExtension(db.OriginalFileName);

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
            IFileManager fileManager = new FileManager();
            string allPath = fileManager.GetFilePath();
            Active.SaveAs(allPath);
            string[] blockNames = { "ФорматM25", "Формат" };
            DynamicBlockFinder dynamicBlocks = new DynamicBlockFinder(blockNames);
            LayoutModelCollection layoutModelCollection = new LayoutModelCollection(dynamicBlocks);
            layoutModelCollection.ListLayouts("Model");
            layoutModelCollection.CreateLayoutCollection();

            string[] viewNames = { "Форма 3 ГОСТ Р 21.1101-2009 M25", "Форма 3 ГОСТ Р 21.1101-2009" };
            var packageCreator = new PrintPackageCreator(layoutModelCollection, viewNames);

            LayoutTreeView window = new LayoutTreeView(packageCreator.PrintPackageModels);
            Autodesk.AutoCAD.ApplicationServices.Core.Application.ShowModalWindow(window);

            if (!window.isClicked) return;

            var printPackages = window._printPackages;
            packageCreator.PrintPackageModels = printPackages;

            packageCreator.PublishAllPackages();

            var blockIds = printPackages.SelectMany(x => x.Layouts.Select(y => y.BlocksObjectId)).ToArray();

            Utils utils = new Utils();
            List<Sheet> sheets = new List<Sheet>();
            using (Transaction trans = Active.Database.TransactionManager.StartTransaction())
            {
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


        [CommandMethod("BlockExt")]
        public void BlockExt()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            PromptEntityOptions enOpt =
                new PromptEntityOptions("\nВыберите вставку блока:");
            enOpt.SetRejectMessage("Это не вставка блока");
            enOpt.AddAllowedClass(typeof(BlockReference), true);
            PromptEntityResult enRes = ed.GetEntity(enOpt);
            if (enRes.Status == PromptStatus.OK)
            {
                Extents3d blockExt = new Extents3d(Point3d.Origin, Point3d.Origin);
                using (BlockReference bref = enRes.ObjectId.Open(OpenMode.ForRead) as BlockReference)
                {
                    Matrix3d mat = bref.BlockTransform;
                    using (BlockTableRecord btr = bref.BlockTableRecord.Open(OpenMode.ForRead) as BlockTableRecord)
                    {
                        foreach (ObjectId id in btr)
                        {
                            using (DBObject obj = id.Open(OpenMode.ForRead) as DBObject)
                            {
                                Entity en = obj as Entity;
                                if (en != null && en.Visible == true)
                                {

                                    try
                                    {
                                        Extents3d ext = en.GeometricExtents;
                                        ext.TransformBy(mat);
                                        if (blockExt.MinPoint == Point3d.Origin && blockExt.MaxPoint == Point3d.Origin)
                                            blockExt = ext;
                                        else
                                            blockExt.AddExtents(ext);
                                    }
                                    catch (Autodesk.AutoCAD.Runtime.Exception e)
                                    {
                                        Active.Editor.WriteMessage(e.Message);

                                    }


                                }
                            }
                        }
                    }
                }
                string s =
                    "MinPoint: " + blockExt.MinPoint.ToString() + " " +
                    "MaxPoint: " + blockExt.MaxPoint.ToString();
                ed.WriteMessage(s);
            }
        }

        [CommandMethod("TestArc")]
        public void TestArc()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            PromptEntityOptions pr =
                new PromptEntityOptions("\nУкажите дугу (ARC) для получения начала и конца (ENTER - завершение): ");
            pr.SetRejectMessage("Это не дуга (ARC). Повторите!");
            pr.AddAllowedClass(typeof(Arc), true);
            PromptEntityResult res;
            while ((res = ed.GetEntity(pr)).Status == PromptStatus.OK)
            {
                ObjectId id = res.ObjectId;
                using (Arc arc = id.Open(OpenMode.ForRead) as Arc)
                {
                    // Первый вариант получения начала/конца дуги
                    ed.WriteMessage("\nStartPoint1 = ({0}) Endpoint1 = ({1}) ",
                        arc.StartPoint, arc.EndPoint);
                    // Второй вариант получения начала/конца дуги
                    ed.WriteMessage("\nStartPoint2 = ({0}) Endpoint2 = ({1}) ",
                        arc.GetPointAtParameter(arc.StartParam), arc.GetPointAtParameter(arc.EndParam));
                }
            }
        }

        [CommandMethod("TestArc111")]
        public void Test()
        {
            Document doc = Active.Document;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // prompt for first point
            var options = new PromptPointOptions("\nFirst point: ");
            var result = ed.GetPoint(options);
            if (result.Status != PromptStatus.OK)
                return;
            var pt1 = result.Value;

            // prompt for second point

            //options.BasePoint = new Point3d(pt1.X+5, pt1.Y+5, pt1.Z);
            //options.UseBasePoint = true;
            //result = ed.GetPoint(options);
            //if (result.Status != PromptStatus.OK)
            //    return;
            var pt2 = new Point3d(pt1.X + 5, pt1.Y + 5, pt1.Z);

            // prompt for third point
            options.Message = "\nThird point: ";
            options.BasePoint = pt1;
            result = ed.GetPoint(options);
            if (result.Status != PromptStatus.OK)
                return;
            var pt3 = result.Value;
            Arc acArc = new Arc(new Point3d(pt1.X, pt1.Y, pt1.Z),

                pt1.X + 5, pt1.Y + 5, pt1.Z);
            // convert points to 2d points
            var plane = new Plane(Point3d.Origin, Vector3d.ZAxis);
            var p1 = pt1.Convert2d(plane);
            var p2 = pt2.Convert2d(plane);
            var p3 = pt3.Convert2d(plane);

            // compute the bulge of the second segment
            var angle1 = p1.GetVectorTo(p2).Angle;
            var angle2 = p2.GetVectorTo(p3).Angle;
            var bulge = Math.Tan((angle2 - angle1) / 2.0);

            // add the polyline to the current space
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var curSpace = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                using (var pline = new Polyline())
                {
                    pline.AddVertexAt(0, p1, 0.0, 0.0, 0.0);
                    pline.AddVertexAt(1, p1, 0.0, 0.0, 0.0);
                    pline.AddVertexAt(2, p1, 5.0, 0.0, 0.0);
                    pline.AddVertexAt(3, p2, bulge, 0.0, 0.0);
                    pline.AddVertexAt(4, p3, 0.0, 0.0, 0.0);


                    pline.AddVertexAt(0, new Point2d(0, 0), -0.4141, 0, 0);
                    pline.AddVertexAt(1, new Point2d(5, 5), 0, 0, 0);
                    pline.AddVertexAt(2, new Point2d(10, 5), 0.4141, 0, 0);
                    pline.AddVertexAt(3, new Point2d(15, 10), 0, 0, 0);

                    pline.TransformBy(ed.CurrentUserCoordinateSystem);
                    curSpace.AppendEntity(pline);
                    tr.AddNewlyCreatedDBObject(pline, true);
                }
                tr.Commit();
            }
        }

        //TODO fragment
        private Polyline CreateFragmentPolyline(Point3d p1, Point3d p2)
        {
            var pline = new Polyline();
            pline.AddVertexAt(0, new Point2d(p1.X, p1.Y), -0.4141, 0, 0);
            pline.AddVertexAt(1, new Point2d(p1.X + 5, p1.Y + 5), 0, 0, 0);
            pline.AddVertexAt(2, new Point2d(10, 5), 0.4141, 0, 0);
            pline.AddVertexAt(3, new Point2d(15, 10), 0, 0, 0);

            return pline;
        }

        static Vector3d GetArbPerpVector(Vector3d zAxis)
        {
            Vector3d xAxis;
            const double kArbBound = 0.015625;    //  1/64th
            if ((Math.Abs(zAxis.X) < kArbBound) && (Math.Abs(zAxis.Y) < kArbBound))
                xAxis = Vector3d.YAxis.CrossProduct(zAxis);
            else
                xAxis = Vector3d.ZAxis.CrossProduct(zAxis);
            return xAxis.GetNormal();
        }
        [CommandMethod("TestVector")]
        public void TestVector()
        {
            //Document doc = Application.DocumentManager.MdiActiveDocument;
            //Editor ed = doc.Editor;
            //PromptPointOptions pr =
            //    new PromptPointOptions("\nУкажите вектор для получения перпендикулярного (ENTER - завершение): ");
            //pr.AllowNone = true;
            //PromptPointResult res;
            //while ((res = ed.GetPoint(pr)).Status == PromptStatus.OK)
            //{
            //    Vector3d v = res.Value.GetAsVector();
            //    v = v.GetNormal();
            //    Vector3d vPerp1 = v.GetPerpendicularVector(); // Стандартный алгоритм
            //    Vector3d vPerp2 = GetArbPerpVector(v); // Arbitrary алгоритм
            //    ed.WriteMessage("\nPerp1 = ({0}) Perp2=({1})", vPerp1, vPerp2);
            //}

            PromptEntityOptions promptOptions = new PromptEntityOptions("\nSelect Polyline: ");
            PromptEntityResult entity = Application.DocumentManager.MdiActiveDocument.Editor.GetEntity(promptOptions);

            // Exit if the user presses ESC or cancels the command
            if (entity.Status != PromptStatus.OK)
            {
                return;
            }

            PromptPointOptions promptPointOptions = new PromptPointOptions("\nSelect Point: ");
            PromptPointResult userPoint = Application.DocumentManager.MdiActiveDocument.Editor.GetPoint(promptPointOptions);

            // Exit if the user presses ESC or cancels the command
            if (userPoint.Status != PromptStatus.OK)
            {
                return;
            }

            // Get the current document and database
            Document document = Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;

            // Start a transaction
            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                using (Curve curve = (Curve)transaction.GetObject(entity.ObjectId, OpenMode.ForRead))
                {
                    Point3d pointOnCurve = curve.GetClosestPointTo(userPoint.Value, true);
                    Vector3d tangentVector = curve.GetFirstDerivative(pointOnCurve);
                    Vector3d perpendicularVector = tangentVector.GetPerpendicularVector();
                    perpendicularVector = perpendicularVector.GetNormal();

                    double distance = 5.0;

                    Point3d startPoint = userPoint.Value;
                    Point3d endPoint = pointOnCurve - perpendicularVector * distance;

                    using (BlockTable blockTable = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead))
                    {
                        using (BlockTableRecord blockTableRecord = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite))
                        {
                            Polyline perpendicularPolyline = new Polyline();
                            perpendicularPolyline.SetDatabaseDefaults();
                            perpendicularPolyline.ColorIndex = 2;
                            perpendicularPolyline.AddVertexAt(0, new Point2d(startPoint.X, startPoint.Y), 0.4141, 0, 0);

                            perpendicularPolyline.AddVertexAt(1, new Point2d(endPoint.X, endPoint.Y), 0, 0, 0);

                            blockTableRecord.AppendEntity(perpendicularPolyline);
                            transaction.AddNewlyCreatedDBObject(perpendicularPolyline, true);
                        }
                    }
                }

                transaction.Commit();
            }

            Application.DocumentManager.MdiActiveDocument.Editor.UpdateScreen();
        }



        [CommandMethod("TestVector1")]
        public void TestVector1()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = doc.Database;
            Editor ed = doc.Editor;
            PromptPointOptions pr =
                new PromptPointOptions("\nУкажите вектор для получения перпендикулярного (ENTER - завершение): ");
            pr.AllowNone = true;
            PromptPointResult res;

            Point3d p1 = new Point3d(50, 0, 0);
            Point3d p2 = new Point3d(70, 20, 0);

            var vector3d = p1 - p2;
            Point3d pt3 = p1 - vector3d.GetNormal();

            var perpendicular = vector3d.GetPerpendicularVector();
            var endPoint3d = pt3 - perpendicular * 10;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {

                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                BlockTableRecord acBlkTblRec;
                acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                Polyline acPoly = new Polyline();
                acPoly.AddVertexAt(0, new Point2d(p1.X, p1.Y), -0.4141, 0, 0);
                acPoly.AddVertexAt(1, new Point2d(endPoint3d.X, endPoint3d.Y), 0, 0, 0);
                acBlkTblRec.AppendEntity(acPoly);
                acTrans.AddNewlyCreatedDBObject(acPoly, true);

                acTrans.Commit();
            }

        }


        [CommandMethod("EditPolyline1111")]
        public static void EditPolyline()
        {
            // Get the current document and database
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            // Start a transaction

            var options = new PromptPointOptions("\nFirst point: ");
            var result = Active.Editor.GetPoint(options);
            if (result.Status != PromptStatus.OK)
                return;
            var pt1 = result.Value;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
                    OpenMode.ForRead) as BlockTable;
                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                    OpenMode.ForWrite) as BlockTableRecord;
                // Create a lightweight polyline
                Polyline acPoly = new Polyline();
                //acPoly.SetDatabaseDefaults();

                acPoly.AddVertexAt(0, new Point2d(0, 0), -0.4141, 0, 0);
                acPoly.AddVertexAt(1, new Point2d(5, 5), 0, 0, 0);
                acPoly.AddVertexAt(2, new Point2d(10, 5), 0.4141, 0, 0);
                acPoly.AddVertexAt(3, new Point2d(15, 10), 0, 0, 0);
                //acPoly.AddVertexAt(4, new Point2d(10, 10), 0, 0, 0);
                // Add the new object to the block table record and the transaction
                acBlkTblRec.AppendEntity(acPoly);
                acTrans.AddNewlyCreatedDBObject(acPoly, true);
                // Sets the bulge at index 3
                //acPoly.SetBulgeAt(3, -0.6);
                // Add a new vertex
                //acPoly.AddVertexAt(5, new Point2d(4, 1), 0, 0, 0);
                // Sets the start and end width at index 4
                //acPoly.SetStartWidthAt(0, 0);
                //acPoly.SetEndWidthAt(4, 0);
                // Close the polyline
                //acPoly.Closed = false;
                // Save the new objects to the database
                acTrans.Commit();
            }
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

        [CommandMethod("BPJIG")]
        public static void RunBulgePolyJig()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            BulgePolyJig jig =
                new BulgePolyJig(ed.CurrentUserCoordinateSystem);
            while (true)
            {
                PromptResult res = ed.Drag(jig);
                switch (res.Status)
                {
                    // New point was added, keep going
                    case PromptStatus.OK:
                        jig.AddDummyVertex();
                        break;
                    // Keyword was entered
                    case PromptStatus.Keyword:
                        if (jig.IsUndoing)
                            jig.RemoveLastVertex();
                        break;
                    // If the jig completed successfully, add the polyline
                    case PromptStatus.None:
                        jig.RemoveLastVertex();
                        jig.Append();
                        return;
                    // User cancelled the command, get out of here
                    // and don't forget to dispose the jigged entity
                    default:
                        //jig.Entity.Dispose();
                        return;
                }
            }
        }
    }

    class JigUtils
    {
        // Custom ArcTangent method, as the Math.Atan
        // doesn't handle specific cases
        public static double Atan(double y, double x)
        {
            if (x > 0)
                return Math.Atan(y / x);
            else if (x < 0)
                return Math.Atan(y / x) - Math.PI;
            else  // x == 0
            {
                if (y > 0)
                    return Math.PI;
                else if (y < 0)
                    return -Math.PI;
                else // if (y == 0) theta is undefined
                    return 0.0;
            }
        }
        // Computes Angle between current direction
        // (vector from last vertex to current vertex)
        // and the last pline segment
        public static double ComputeAngle(
          Point3d startPoint, Point3d endPoint,
          Vector3d xdir, Matrix3d ucs
        )
        {
            Vector3d v =
              new Vector3d(
                (endPoint.X - startPoint.X) / 2,
                (endPoint.Y - startPoint.Y) / 2,
                (endPoint.Z - startPoint.Z) / 2
              );
            double cos = v.DotProduct(xdir);
            double sin =
              v.DotProduct(
                Vector3d.ZAxis.TransformBy(ucs).CrossProduct(xdir)
              );
            return Atan(sin, cos);
        }
    }
    public class BulgePolyJig : EntityJig
    {
        Point3d _tempPoint;
        Plane _plane;
        bool _isArcSeg = false;
        bool _isUndoing = false;
        Matrix3d _ucs;
        public BulgePolyJig(Matrix3d ucs) : base(new Polyline())
        {
            _ucs = ucs;
            // Get the coordinate system for the UCS passed in, and
            // create a plane with the same normal (but we won't use
            // the same origin)
            CoordinateSystem3d cs = ucs.CoordinateSystem3d;
            Vector3d normal = cs.Zaxis;
            _plane = new Plane(Point3d.Origin, normal);
            // Access our polyline and set its normal
            Polyline pline = Entity as Polyline;
            pline.SetDatabaseDefaults();
            pline.Normal = normal;
            // Check the distance from the plane to the coordinate
            // system's origin (wwe could use Plane.DistanceTo(), but
            // then we also need the vector to determine whether it is
            // co-directional with the normal)
            Point3d closest = cs.Origin.Project(_plane, normal);
            Vector3d disp = closest - cs.Origin;
            // Set the elevation based on the direction of the vector
            pline.Elevation =
              disp.IsCodirectionalTo(normal) ? -disp.Length : disp.Length;
            AddDummyVertex();
        }
        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            JigPromptPointOptions jigOpts = new JigPromptPointOptions();
            jigOpts.UserInputControls =
              (UserInputControls.Accept3dCoordinates |
               UserInputControls.NullResponseAccepted |
               UserInputControls.NoNegativeResponseAccepted |
               UserInputControls.GovernedByOrthoMode);
            _isUndoing = false;
            Polyline pline = Entity as Polyline;
            if (pline.NumberOfVertices == 1)
            {
                // For the first vertex, just ask for the point
                jigOpts.Message = "\nSpecify start point: ";
            }
            else if (pline.NumberOfVertices > 1)
            {
                string msgAndKwds =
                  (_isArcSeg ?
                    "\nSpecify endpoint of arc or [Line/Undo]: " :
                    "\nSpecify next point or [Arc/Undo]: "
                  );
                string kwds = (_isArcSeg ? "Line Undo" : "Arc Undo");
                jigOpts.SetMessageAndKeywords(msgAndKwds, kwds);
            }
            else
                return SamplerStatus.Cancel; // Should never happen
            // Get the point itself
            PromptPointResult res = prompts.AcquirePoint(jigOpts);
            if (res.Status == PromptStatus.Keyword)
            {
                if (res.StringResult.ToUpper() == "ARC")
                    _isArcSeg = true;
                else if (res.StringResult.ToUpper() == "LINE")
                    _isArcSeg = false;
                else if (res.StringResult.ToUpper() == "UNDO")
                    _isUndoing = true;
                return SamplerStatus.OK;
            }
            else if (res.Status == PromptStatus.OK)
            {
                // Check if it has changed or not (reduces flicker)
                if (_tempPoint == res.Value)
                    return SamplerStatus.NoChange;
                else
                {
                    _tempPoint = res.Value;
                    return SamplerStatus.OK;
                }
            }
            return SamplerStatus.Cancel;
        }
        protected override bool Update()
        {
            // Update the dummy vertex to be our 3D point
            // projected onto our plane
            Polyline pl = Entity as Polyline;
            if (_isArcSeg)
            {
                Point3d lastVertex =
                  pl.GetPoint3dAt(pl.NumberOfVertices - 2);
                Vector3d refDir;
                if (pl.NumberOfVertices < 3)
                    refDir = new Vector3d(1.0, 1.0, 0.0);
                else
                {
                    // Check bulge to see if last segment was an arc or a line
                    if (pl.GetBulgeAt(pl.NumberOfVertices - 3) != 0)
                    {
                        CircularArc3d arcSegment =
                          pl.GetArcSegmentAt(pl.NumberOfVertices - 3);
                        Line3d tangent = arcSegment.GetTangent(lastVertex);
                        // Reference direction is the invert of the arc tangent
                        // at last vertex
                        refDir = tangent.Direction.MultiplyBy(-1.0);
                    }
                    else
                    {
                        Point3d pt =
                          pl.GetPoint3dAt(pl.NumberOfVertices - 3);
                        refDir =
                          new Vector3d(
                            lastVertex.X - pt.X,
                            lastVertex.Y - pt.Y,
                            lastVertex.Z - pt.Z
                          );
                    }
                }
                double angle =
                  JigUtils.ComputeAngle(
                    lastVertex, _tempPoint, refDir, _ucs
                  );
                // Bulge is defined as tan of one fourth of included angle
                // Need to double the angle since it represents the included
                // angle of the arc
                // So formula is: bulge = Tan(angle * 2 * 0.25)
                double bulge = Math.Tan(angle * 0.5);
                pl.SetBulgeAt(pl.NumberOfVertices - 2, bulge);
            }
            else
            {
                // Line mode. Need to remove last bulge if there was one
                if (pl.NumberOfVertices > 1)
                    pl.SetBulgeAt(pl.NumberOfVertices - 2, 0);
            }
            pl.SetPointAt(
              pl.NumberOfVertices - 1, _tempPoint.Convert2d(_plane)
            );
            return true;
        }
        public bool IsUndoing
        {
            get
            {
                return _isUndoing;
            }
        }
        public void AddDummyVertex()
        {
            // Create a new dummy vertex... can have any initial value
            Polyline pline = Entity as Polyline;
            pline.AddVertexAt(
              pline.NumberOfVertices, new Point2d(0, 0), 0, 0, 0
            );
        }
        public void RemoveLastVertex()
        {
            Polyline pline = Entity as Polyline;
            // Let's first remove our dummy vertex   
            if (pline.NumberOfVertices > 0)
                pline.RemoveVertexAt(pline.NumberOfVertices - 1);
            // And then check the type of the last segment
            if (pline.NumberOfVertices >= 2)
            {
                double blg = pline.GetBulgeAt(pline.NumberOfVertices - 2);
                _isArcSeg = (blg != 0);
            }
        }
        public void Append()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            Transaction tr =
              db.TransactionManager.StartTransaction();
            using (tr)
            {
                BlockTable bt =
                  tr.GetObject(
                    db.BlockTableId, OpenMode.ForRead
                  ) as BlockTable;
                BlockTableRecord btr =
                  tr.GetObject(
                    bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite
                  ) as BlockTableRecord;
                btr.AppendEntity(this.Entity);
                tr.AddNewlyCreatedDBObject(this.Entity, true);
                tr.Commit();
            }
        }

    }
}