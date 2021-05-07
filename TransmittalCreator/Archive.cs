using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Internal;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;
using DV2177.Common;
using TransmittalCreator.Models;
using TransmittalCreator.Models.Layouts;

namespace TransmittalCreator
{
    class Archive
    {
        [CommandMethod("LISTATT")]
        public static void ListAttributes1()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;
            var filter = new SelectionFilter(new[] { new TypedValue(0, "INSERT") });
            var opts = new PromptSelectionOptions();
            opts.MessageForAdding = "Select block references: ";

            var res = ed.GetSelection(opts, filter);
            if (res.Status != PromptStatus.OK)
                return;
            string str = "";

            string str1 = "";
            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject so in res.Value)
                {
                    var br = (BlockReference)tr.GetObject(so.ObjectId, OpenMode.ForRead);
                    var vargeom = br.GeometricExtents;
                    ed.WriteMessage(vargeom.MinPoint[0].ToString(CultureInfo.InvariantCulture));
                    str += $"{br.Name} {br.Position:0.00}\r\n";
                    if (br.AttributeCollection.Count > 0)
                    {
                        str += "Attributes:\r\n";
                        foreach (ObjectId id in br.AttributeCollection)
                        {
                            var att = (AttributeReference)id.GetObject(OpenMode.ForRead);
                            str += $"\tTag: {att.Tag} Text: {att.TextString}\r\n";
                        }
                    }

                    str += "\r\n";
                }

                tr.Commit();
            }

            ed.WriteMessage(str);
            ed.WriteMessage(str1);
        }

        //TODO refactor have to delete below methods if no need


        [CommandMethod("CreateTrPdfFromFiles")]
        public static void CreateTransmittalAndPdfFromFiles()
        {
            List<Sheet> dict = new List<Sheet>();
            List<PrintModel> printModels = new List<PrintModel>();

            Active.Document.SendStringToExecute("REGENALL ", true, false, true);

            short bgPlot = (short)Application.GetSystemVariable("BACKGROUNDPLOT");
            Application.SetSystemVariable("BACKGROUNDPLOT", 0);
            List<string> docsToPlot = new List<string>();
            docsToPlot.Add(@"C:\Users\yusufzhon.marasulov\Desktop\test\test.dwg");
            docsToPlot.Add(@"C:\Users\yusufzhon.marasulov\Desktop\test\test1.dwg");
            //BatchTransmittal(docsToPlot);
        }

       
        private static void DisplayDynBlockProperties(Editor ed, BlockReference br, string name)
        {
            // Only continue is we have a valid dynamic block
            if (br != null && br.IsDynamicBlock)
            {
                ed.WriteMessage("\nDynamic properties for \"{0}\"\n", name);

                // Get the dynamic block's property collection
                DynamicBlockReferencePropertyCollection pc =
                    br.DynamicBlockReferencePropertyCollection;
                // Loop through, getting the info for each property
                foreach (DynamicBlockReferenceProperty prop in pc)
                {
                    // Start with the property name, type and description
                    ed.WriteMessage("\nProperty: \"{0}\" : {1}", prop.PropertyName, prop.UnitsType);

                    if (prop.Description != "")
                        ed.WriteMessage("\n  Description: {0}", prop.Description);
                    // Is it read-only?
                    if (prop.ReadOnly)
                        ed.WriteMessage(" (Read Only)");
                    // Get the allowed values, if it's constrained
                    bool first = true;
                    foreach (object value in prop.GetAllowedValues())
                    {
                        ed.WriteMessage((first ? "\n  Allowed values: [" : ", "));
                        ed.WriteMessage("\"{0}\"", value);
                        first = false;
                    }

                    if (!first) ed.WriteMessage("]");
                    // And finally the current value
                    ed.WriteMessage("\n  Current value: \"{0}\"\n", prop.Value);
                }
            }
        }

        [CommandMethod("ChangePlotSetting")]
        public static void ChangePlotSetting()
        {
            // Get the current document and database, and start a transaction
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Reference the Layout Manager
                LayoutManager acLayoutMgr;
                acLayoutMgr = LayoutManager.Current;

                // Get the current layout and output its name in the Command Line window
                Layout acLayout;

                LayoutModelCollection layoutModelCollection = new LayoutModelCollection();
                layoutModelCollection.ListLayouts("Model");
                var layouts = layoutModelCollection.LayoutModels.OrderBy(x => x.Layout.TabOrder).ToList();

                foreach (var layout in layouts)
                {

                    acLayout = acTrans.GetObject(layout.LayoutPlotId,
                        OpenMode.ForRead) as Layout;
                    if (acLayout != null)
                    {
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
                        acPlSetVdr.SetPlotConfigurationName(acPlSet, "DWG_To_PDF_Uzle.pc3",
                            "UserDefinedMetric (891.00 x 420.00мм)");

                        acPlSetVdr.SetStdScaleType(acPlSet, StdScaleType.ScaleToFit);
                        // Center the plot
                        //acPlSetVdr.SetPlotCentered(acPlSet, true);

                        // Update the layout
                        acLayout.UpgradeOpen();
                        acLayout.CopyFrom(acPlSet);

                        // Output the name of the new device assigned to the layout
                        acDoc.Editor.WriteMessage("\nNew device name: " +
                                                  acLayout.PlotConfigurationName);
                    }
                }

                // Save the new objects to the database
                acTrans.Commit();
            }
        }


        //////////////////////////////////////////////////////////////////////////

        //Use: This command will publish all the layouts in the current

        //      drawing to a multi sheet dwf.

        //////////////////////////////////////////////////////////////////////////

        [CommandMethod("PublishAllLayouts")]
        static public void PublishAllLayouts()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            //put the plot in foreground
            short bgPlot = (short)Application.GetSystemVariable("BACKGROUNDPLOT");
            Application.SetSystemVariable("BACKGROUNDPLOT", 0);
            //get the layout ObjectId List
            List<ObjectId> layoutIds = GetLayoutIds(db);
            string dwgFileName = (string)Application.GetSystemVariable("DWGNAME");
            string dwgPath = (string)Application.GetSystemVariable("DWGPREFIX");
            using (Transaction Tx = db.TransactionManager.StartTransaction())
            {
                DsdEntryCollection collection = new DsdEntryCollection();
                foreach (ObjectId layoutId in layoutIds)
                {
                    Layout layout = Tx.GetObject(layoutId, OpenMode.ForRead)
                        as Layout;
                    if (layout.LayoutName != "Model")
                    {
                        DsdEntry entry = new DsdEntry();
                        entry.DwgName = dwgPath + dwgFileName;
                        entry.Layout = layout.LayoutName;
                        entry.Title = "Layout_" + layout.LayoutName;
                        entry.NpsSourceDwg = entry.DwgName;
                        entry.Nps = "Setup1";

                        collection.Add(entry);
                    }

                    //TODO have to think about creating collection depend of block view
                }
                dwgFileName = dwgFileName.Substring(0, dwgFileName.Length - 4);
                DsdData dsdData = new DsdData();
                dsdData.SheetType = SheetType.MultiPdf; //SheetType.MultiPdf
                dsdData.ProjectPath = dwgPath;
                dsdData.DestinationName =
                    $"{dsdData.ProjectPath}{dwgFileName}.pdf";
                if (System.IO.File.Exists(dsdData.DestinationName))
                    System.IO.File.Delete(dsdData.DestinationName);
                dsdData.SetDsdEntryCollection(collection);
                string dsdFile = $"{dsdData.ProjectPath}{dwgFileName}.dsd";
                //Workaround to avoid promp for dwf file name
                //set PromptForDwfName=FALSE in dsdData using
                //StreamReader/StreamWriter
                dsdData.WriteDsd(dsdFile);
                //System.IO.StreamReader sr =
                //    new System.IO.StreamReader(dsdFile, Encoding.Default);
                //string str = sr.ReadToEnd();
                //sr.Close();
                //str = str.Replace(
                //    "PromptForDwfName=TRUE", "PromptForDwfName=FALSE");
                //System.IO.StreamWriter sw =
                //    new System.IO.StreamWriter(dsdFile, true, Encoding.Default);
                //sw.Write(str);
                //sw.Close();
                dsdData.ReadDsd(dsdFile);

                System.IO.File.Delete(dsdFile);
                PlotConfig plotConfig =
                    Autodesk.AutoCAD.PlottingServices.PlotConfigManager.SetCurrentConfig("DWG_To_PDF_Uzle.pc3");
                //PlotConfig pc = Autodesk.AutoCAD.PlottingServices.
                //  PlotConfigManager.SetCurrentConfig("DWG To PDF.pc3");
                Autodesk.AutoCAD.Publishing.Publisher publisher =
                    Autodesk.AutoCAD.ApplicationServices.Application.Publisher;
                publisher.AboutToBeginPublishing +=
                    new Autodesk.AutoCAD.Publishing.
                        AboutToBeginPublishingEventHandler(
                            Publisher_AboutToBeginPublishing);
                dsdData.PromptForDwfName = false;
                publisher.PublishExecute(dsdData, plotConfig);
                Tx.Commit();
            }
            //reset the background plot value
            Application.SetSystemVariable("BACKGROUNDPLOT", bgPlot);
        }
        private static System.Collections.Generic.List<ObjectId> GetLayoutIds(Database db)
        {
            System.Collections.Generic.List<ObjectId> layoutIds =
                new System.Collections.Generic.List<ObjectId>();
            using (Transaction Tx = db.TransactionManager.StartTransaction())
            {
                DBDictionary layoutDic = Tx.GetObject(
                        db.LayoutDictionaryId,
                        OpenMode.ForRead, false)
                    as DBDictionary;
                foreach (DBDictionaryEntry entry in layoutDic)
                {
                    layoutIds.Add(entry.Value);
                }
            }
            return layoutIds;
        }
        static void Publisher_AboutToBeginPublishing(object sender,
            Autodesk.AutoCAD.Publishing.AboutToBeginPublishingEventArgs e)
        {
            Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(
                "\nAboutToBeginPublishing!!");
        }

        //////////////////////////////////////////////////////////////////////////

        //Use: Will batch publish to single dwf every layout of

        //      each document provided as input

        //     Make sure each of your drawing contains a page setup

        //      named Setup1 before running.

        //          

        //////////////////////////////////////////////////////////////////////////

        [CommandMethod("BatchPublishCmd", CommandFlags.Session)]
        static public void BatchPublishCmd()
        {
            short bgPlot = (short)Application.GetSystemVariable("BACKGROUNDPLOT");
            Application.SetSystemVariable("BACKGROUNDPLOT", 0);
            System.Collections.Generic.List<string> docsToPlot =
                new System.Collections.Generic.List<string>();
            docsToPlot.Add(@"C:\Users\yusufzhon.marasulov\Desktop\test\test.dwg");
            docsToPlot.Add(@"C:\Users\yusufzhon.marasulov\Desktop\test\test1.dwg");
            BatchPublish(docsToPlot);
        }


        static public void BatchPublish(System.Collections.Generic.List<string> docsToPlot)
        {
            DsdEntryCollection collection = new DsdEntryCollection();
            Document doc = Application.DocumentManager.MdiActiveDocument;
            foreach (string filename in docsToPlot)
            {
                using (DocumentLock doclock = doc.LockDocument())
                {
                    Database db = new Database(false, true);
                    db.ReadDwgFile(
                        filename, System.IO.FileShare.Read, true, "");
                    System.IO.FileInfo fi = new System.IO.FileInfo(filename);
                    string docName =
                        fi.Name.Substring(0, fi.Name.Length - 4);
                    using (Transaction Tx =
                        db.TransactionManager.StartTransaction())
                    {
                        foreach (ObjectId layoutId in getLayoutIds(db))
                        {
                            Layout layout = Tx.GetObject(layoutId, OpenMode.ForRead) as Layout;

                            DsdEntry entry = new DsdEntry
                            {
                                DwgName = filename,
                                Layout = layout.LayoutName,
                                Title = docName + "_" + layout.LayoutName
                            };
                            entry.NpsSourceDwg = entry.DwgName;
                            entry.Nps = "Setup1";
                            collection.Add(entry);
                        }
                        Tx.Commit();
                    }
                }
            }
            DsdData dsdData = new DsdData();
            dsdData.SheetType = SheetType.MultiPdf;
            dsdData.ProjectPath = @"C:\Users\yusufzhon.marasulov\Desktop\test";
            //Not used for "SheetType.SingleDwf"
            //dsdData.DestinationName = dsdData.ProjectPath + "\\output.dwf";
            dsdData.SetDsdEntryCollection(collection);
            string dsdFile = dsdData.ProjectPath + "\\dsdData.dsd";
            //Workaround to avoid promp for dwf file name
            //set PromptForDwfName=FALSE in dsdData
            //using StreamReader/StreamWriter
            if (System.IO.File.Exists(dsdFile))
                System.IO.File.Delete(dsdFile);
            dsdData.WriteDsd(dsdFile);
            System.IO.StreamReader sr = new System.IO.StreamReader(dsdFile);
            string str = sr.ReadToEnd();
            sr.Close();
            str = str.Replace(
                "PromptForDwfName=TRUE", "PromptForDwfName=FALSE");
            System.IO.StreamWriter sw = new System.IO.StreamWriter(dsdFile);
            sw.Write(str);
            sw.Close();
            dsdData.ReadDsd(dsdFile);
            System.IO.File.Delete(dsdFile);
            PlotConfig plotConfig =
                Autodesk.AutoCAD.PlottingServices.PlotConfigManager.SetCurrentConfig("DWG_To_PDF_Uzle.pc3");
            Autodesk.AutoCAD.Publishing.Publisher publisher =
                Autodesk.AutoCAD.ApplicationServices.Application.Publisher;
            publisher.PublishExecute(dsdData, plotConfig);
        }


        private static List<ObjectId> getLayoutIds(Database db)
        {
            List<ObjectId> layoutIds =
                new List<ObjectId>();
            using (Transaction Tx = db.TransactionManager.StartTransaction())
            {
                DBDictionary layoutDic = Tx.GetObject(
                        db.LayoutDictionaryId,
                        OpenMode.ForRead, false)
                    as DBDictionary;
                foreach (DBDictionaryEntry entry in layoutDic)
                {
                    layoutIds.Add(entry.Value);
                }
            }
            return layoutIds;
        }

        [CommandMethod("BlockExt")]
        public void BlockExt()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            PromptEntityOptions enOpt =
                new PromptEntityOptions("\nВыберите примитив: ");

            PromptEntityResult enRes = ed.GetEntity(enOpt);
            if (enRes.Status == PromptStatus.OK)
            {
                Extents3d blockExt =
                    new Extents3d(Point3d.Origin, Point3d.Origin);
                Matrix3d mat = Matrix3d.Identity;
                using (Entity en =
                    enRes.ObjectId.GetObject(OpenMode.ForRead) as Entity)
                {
                    GetBlockExtents(en, ref blockExt, ref mat);
                }

                string s =
                    "MinPoint: " + blockExt.MinPoint.ToString() + " " +
                    "MaxPoint: " + blockExt.MaxPoint.ToString();
                ed.WriteMessage(s);
                //------------------------------------------------------------
                // Только для тестирования полученного габаритного контейнера
                //------------------------------------------------------------

                #region TestinExts

                using (BlockTableRecord curSpace =
                    doc.Database.CurrentSpaceId.GetObject(OpenMode.ForWrite) as BlockTableRecord)
                {
                    Point3dCollection pts = new Point3dCollection();
                    pts.Add(blockExt.MinPoint);
                    pts.Add(new Point3d(blockExt.MinPoint.X,
                        blockExt.MaxPoint.Y, blockExt.MinPoint.Z));
                    pts.Add(blockExt.MaxPoint);
                    pts.Add(new Point3d(blockExt.MaxPoint.X,
                        blockExt.MinPoint.Y, blockExt.MinPoint.Z));
                    using (Polyline3d poly =
                        new Polyline3d(Poly3dType.SimplePoly, pts, true))
                    {
                        curSpace.AppendEntity(poly);
                    }
                }

                #endregion
            }
        }

        /// <summary>
        /// Рекурсивное получение габаритного контейнера для выбранного примитива.
        /// </summary>
        /// <param name="en">Имя примитива</param>
        /// <param name="ext">Габаритный контейнер</param>
        /// <param name="mat">Матрица преобразования из системы координат блока в МСК.</param>
        void GetBlockExtents(Entity en, ref Extents3d ext, ref Matrix3d mat)
        {
            if (!IsLayerOn(en.LayerId))
                return;
            if (en is BlockReference)
            {
                BlockReference bref = en as BlockReference;
                Matrix3d matIns = mat * bref.BlockTransform;
                using (BlockTableRecord btr =
                    bref.BlockTableRecord.GetObject(OpenMode.ForRead) as BlockTableRecord)
                {
                    foreach (ObjectId id in btr)
                    {
                        using (DBObject obj = id.GetObject(OpenMode.ForRead) as DBObject)
                        {
                            Entity enCur = obj as Entity;
                            if (enCur == null || enCur.Visible != true)
                                continue;
                            // Пропускаем неконстантные и невидимые определения атрибутов
                            AttributeDefinition attDef = enCur as AttributeDefinition;
                            if (attDef != null && (!attDef.Constant || attDef.Invisible))
                                continue;
                            GetBlockExtents(enCur, ref ext, ref matIns);
                        }
                    }
                }

                // Отдельно обрабатываем атрибуты блока
                if (bref.AttributeCollection.Count > 0)
                {
                    foreach (ObjectId idAtt in bref.AttributeCollection)
                    {
                        using (AttributeReference attRef =
                            idAtt.GetObject(OpenMode.ForRead) as AttributeReference)
                        {
                            if (!attRef.Invisible && attRef.Visible)
                                GetBlockExtents(attRef, ref ext, ref mat);
                        }
                    }
                }
            }
            else
            {
                if (mat.IsUniscaledOrtho())
                {
                    using (Entity enTr = en.GetTransformedCopy(mat))
                    {
                        if (enTr is Dimension)
                            (enTr as Dimension).RecomputeDimensionBlock(true);
                        if (enTr is Table)
                            (enTr as Table).RecomputeTableBlock(true);
                        if (IsEmptyExt(ref ext))
                            try
                            {
                                ext = enTr.GeometricExtents;
                            }
                            catch
                            {
                                // ignored
                            }
                        else
                        {
                            try
                            {
                                ext.AddExtents(enTr.GeometricExtents);
                            }
                            catch
                            {
                                // ignored
                            }
                        }

                        return;
                    }
                }
                else
                {
                    try
                    {
                        Extents3d curExt = en.GeometricExtents;
                        curExt.TransformBy(mat);
                        if (IsEmptyExt(ref ext))
                            ext = curExt;
                        else
                            ext.AddExtents(curExt);
                    }
                    catch
                    {
                    }

                    return;
                }
            }
        }




        /// <summary>
        /// Определяет видны ли объекты на слое, указанном его ObjectId.
        /// </summary>
        /// <param name="layerId">ObjectId слоя.</param>
        /// <returns></returns>
        bool IsLayerOn(ObjectId layerId)
        {
            using (LayerTableRecord ltr = layerId.GetObject(OpenMode.ForRead) as LayerTableRecord)
            {
                if (ltr.IsFrozen) return false;
                if (ltr.IsOff) return false;
            }

            return true;
        }


        /// <summary>
        /// Определят не пустой ли габаритный контейнер.
        /// </summary>
        /// <param name="ext">Габаритный контейнер.</param>
        /// <returns></returns>

        bool IsEmptyExt(ref Extents3d ext)
        {
            if (ext.MinPoint.DistanceTo(ext.MaxPoint) < Tolerance.Global.EqualPoint)
                return true;
            else
                return false;
        }

        [CommandMethod("CalcBlocks1")]
        static public void CalcBlocks1()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            Database db = HostApplicationServices.WorkingDatabase;
            PromptResult res = ed.GetString("\nType name of block: ");

            if (res.Status == PromptStatus.OK)
            {
                TypedValue[] flt = { new TypedValue(0, "INSERT"), new TypedValue(2, res.StringResult), new TypedValue(410, "Model") };

                PromptSelectionResult rs = ed.SelectAll(new SelectionFilter(flt));
                if (rs.Status == PromptStatus.OK && rs.Value.Count > 0)
                {
                    ed.WriteMessage("\nNumber of blocks with name <{0}> in Model Space is {1}", res.StringResult, rs.Value.Count);
                }
                else
                {
                    ed.WriteMessage("\nNo blocks with name <{0}>", res.StringResult);
                }
            }
        }

        [CommandMethod("filterblocks")]
        public void FilterBlocks()
        {
            Document doc = Active.Document;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptEntityOptions opts = new PromptEntityOptions("\nSelect a block: ");
            opts.SetRejectMessage("Only a block.");
            opts.AddAllowedClass(typeof(BlockReference), true);
            PromptEntityResult per = ed.GetEntity(opts);
            if (per.Status != PromptStatus.OK)
                return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockReference br = (BlockReference)tr.GetObject(per.ObjectId, OpenMode.ForRead);
                blockName = GetEffectiveName(br);
                TypedValue[] filterList = new TypedValue[3]
                {
                        new TypedValue(0, "INSERT"),
                        new TypedValue(2, "`*U*," + blockName),
                        new TypedValue(8, br.Layer)
                };
                ed.SelectionAdded += onSelectionAdded;
                PromptSelectionResult psr = ed.SelectAll(new SelectionFilter(filterList));
                ed.SelectionAdded -= onSelectionAdded;
                ed.SetImpliedSelection(psr.Value);
                ed.WriteMessage("\nNumber of selected objects: {0}", psr.Value.Count);
                tr.Commit();
            }
        }

        private string blockName;

        private string GetEffectiveName(BlockReference br)
        {
            return br.IsDynamicBlock ?
                ((BlockTableRecord)br.DynamicBlockTableRecord.GetObject(OpenMode.ForRead)).Name :
                br.Name;
        }

        void onSelectionAdded(object sender, SelectionAddedEventArgs e)
        {
            for (int i = 0; i < e.AddedObjects.Count; i++)
            {
                BlockReference br = (BlockReference)e.AddedObjects[i].ObjectId.GetObject(OpenMode.ForRead);
                if (GetEffectiveName(br) != blockName)
                    e.Remove(i);
            }
        }

        /// <summary>
        /// Summary description for Class.
        /// </summary>

        // Define Command "CalcBlocks"
        [CommandMethod("CalcBlocks")]
        static public void CalcBlocks()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            Database db = HostApplicationServices.WorkingDatabase;
            var pso = new PromptStringOptions("\nName of dynamic block to search for");
            pso.AllowSpaces = true;
            var res = ed.GetString(pso);
            if (res.Status == PromptStatus.OK)
            {

                ObjectIdCollection ids = GetAllBlockReferenceByName(res.StringResult, db);
                if (ids != null)
                {
                    ed.WriteMessage("\nNumber of blocks with name <{0}> in Model Space is {1}", res.StringResult, ids.Count);
                }
                else
                {
                    ed.WriteMessage("\nNo blocks with name <{0}>", res.StringResult);
                }
            }
        }

        public static ObjectIdCollection GetAllBlockReferenceByName(string name, Database db)
        {
            ObjectIdCollection ids_temp = null, ids = new ObjectIdCollection();
            List<string> blkNames = new List<string>();
            blkNames.Add(name);
            List<ObjectId> objectIds = new List<ObjectId>();
            Transaction tr = db.TransactionManager.StartTransaction();
            try
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[name], OpenMode.ForRead);
                BlockTableRecord btr_model = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                SelectionFilter sf = new SelectionFilter(CreateFilterListForBlocks(blkNames));
                PromptSelectionResult psr = Active.Editor.SelectAll(sf);

                ObjectId id_model = btr_model.ObjectId;
                ids_temp = btr.GetBlockReferenceIds(true, true);
                foreach (ObjectId id in ids_temp)
                {
                    DBObject obj = (DBObject)tr.GetObject(id, OpenMode.ForRead);
                    // If BlockReference owned with Model Space - add it to collection
                    if (obj.OwnerId == id_model)
                    {
                        ids.Add(id);
                        objectIds.Add(id);
                    }
                }
                tr.Commit();
            }
            finally
            {
                tr.Dispose();
            }

            return ids;
        }

        public static ObjectIdCollection GetBlockByName(string blkName)
        {
            ObjectIdCollection ids = new ObjectIdCollection();

            Editor ed = Active.Editor;
            Document doc = Active.Document;
            List<string> blkNames = new List<string>();
            blkNames.Add(blkName);
            var tr = doc.TransactionManager.StartTransaction();
            using (tr)
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);

                // Start by getting access to our block, if it exists
                if (!bt.Has(blkName))
                {
                    ed.WriteMessage(
                        "\nCannot find block called \"{0}\".", blkName
                    );

                }
                // Get the anonymous block names
                var btr = (BlockTableRecord)tr.GetObject(bt[blkName], OpenMode.ForRead);
                if (!btr.IsDynamicBlock)
                {
                    ed.WriteMessage(
                        "\nCannot find a dynamic block called \"{0}\".", blkName
                    );

                }
                // Get the anonymous blocks and add them to our list
                var anonBlks = btr.GetAnonymousBlockIds();
                foreach (ObjectId bid in anonBlks)
                {
                    var btr2 =
                        (BlockTableRecord)tr.GetObject(bid, OpenMode.ForRead);
                    blkNames.Add(btr2.Name);
                }
                tr.Commit();
            }
            // Build a conditional filter list so that only
            // entities with the specified properties are
            // selected
            SelectionFilter sf =
                new SelectionFilter(CreateFilterListForBlocks(blkNames));
            PromptSelectionResult psr = ed.SelectAll(sf);
            ed.WriteMessage(
                "\nFound {0} entit{1}.",
                psr.Value.Count,
                (psr.Value.Count == 1 ? "y" : "ies"));
            return ids;
        }

        [CommandMethod("SDB")]
        static public void SelectDynamicBlocks()
        {
            var doc = Active.Document;
            var ed = doc.Editor;
            var pso = new PromptStringOptions("\nName of dynamic block to search for");
            pso.AllowSpaces = true;
            var pr = ed.GetString(pso);
            if (pr.Status != PromptStatus.OK)
                return;
            string blkName = pr.StringResult;
            List<string> blkNames = new List<string>();
            blkNames.Add(blkName);
            var tr = doc.TransactionManager.StartTransaction();
            try
            {
                using (tr)
                {

                    var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                    // Start by getting access to our block, if it exists
                    if (!bt.Has(blkName))
                    {
                        ed.WriteMessage("\nCannot find block called \"{0}\".", blkName);
                        return;
                    }
                    // Get the anonymous block names
                    var btr = (BlockTableRecord)tr.GetObject(bt[blkName], OpenMode.ForRead);
                    if (!btr.IsDynamicBlock)
                    {
                        ed.WriteMessage("\nCannot find a dynamic block called \"{0}\".", blkName);
                        return;
                    }
                    // Get the anonymous blocks and add them to our list
                    var anonBlks = btr.GetAnonymousBlockIds();
                    foreach (ObjectId bid in anonBlks)
                    {
                        var btr2 = (BlockTableRecord)tr.GetObject(bid, OpenMode.ForRead);
                        blkNames.Add(btr2.Name);
                    }
                    tr.Commit();
                }

                // Build a conditional filter list so that only
                // entities with the specified properties are
                // selected
                SelectionFilter sf = new SelectionFilter(CreateFilterListForBlocks(blkNames));
                PromptSelectionResult psr = ed.SelectAll(sf);
                ed.WriteMessage("\nFound {0} entit{1}.", psr.Value.Count, (psr.Value.Count == 1 ? "y" : "ies"));
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }

        private static TypedValue[] CreateFilterListForBlocks(List<string> blkNames)
        {
            // If we don't have any block names, return null
            if (blkNames.Count == 0)
                return null;
            // If we only have one, return an array of a single value
            if (blkNames.Count == 1)
                return new TypedValue[]
                {
                    new TypedValue((int)DxfCode.BlockName, blkNames[0])
                };
            // We have more than one block names to search for...
            // Create a list big enough for our block names plus
            // the containing "or" operators
            List<TypedValue> tvl = new List<TypedValue>(blkNames.Count + 2);
            // Add the initial operator
            tvl.Add(new TypedValue((int)DxfCode.Operator, "<or"));
            // Add an entry for each block name, prefixing the
            // anonymous block names with a reverse apostrophe
            foreach (var blkName in blkNames)
            {
                tvl.Add(
                    new TypedValue(
                        (int)DxfCode.BlockName,
                        (blkName.StartsWith("*") ? "`" + blkName : blkName)
                    )
                );
            }
            // Add the final operator
            tvl.Add(
                new TypedValue(
                    (int)DxfCode.Operator,
                    "or>"
                )
            );
            // Return an array from the list
            return tvl.ToArray();
        }
        private void ZoomObjects(ObjectIdCollection idCol)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            using (ViewTableRecord view = ed.GetCurrentView())
            {
                Matrix3d WCS2DCS = Matrix3d.PlaneToWorld(view.ViewDirection);
                WCS2DCS = Matrix3d.Displacement(view.Target - Point3d.Origin) * WCS2DCS;
                WCS2DCS = Matrix3d.Rotation(-view.ViewTwist, view.ViewDirection, view.Target) * WCS2DCS;
                WCS2DCS = WCS2DCS.Inverse();
                Entity ent = (Entity)tr.GetObject(idCol[0], OpenMode.ForRead);
                Extents3d ext = ent.GeometricExtents;
                ext.TransformBy(WCS2DCS);
                for (int i = 1; i < idCol.Count; i++)
                {
                    ent = (Entity)tr.GetObject(idCol[i], OpenMode.ForRead);
                    Extents3d tmp = ent.GeometricExtents;
                    tmp.TransformBy(WCS2DCS);
                    ext.AddExtents(tmp);
                }
                double ratio = view.Width / view.Height;
                double width = ext.MaxPoint.X - ext.MinPoint.X;
                double height = ext.MaxPoint.Y - ext.MinPoint.Y;
                if (width > (height * ratio))
                    height = width / ratio;
                Point2d center =
                    new Point2d((ext.MaxPoint.X + ext.MinPoint.X) / 2.0, (ext.MaxPoint.Y + ext.MinPoint.Y) / 2.0);
                view.Height = height;
                view.Width = width;
                view.CenterPoint = center;
                ed.SetCurrentView(view);
                tr.Commit();
            }
        }

        public void CopyObjectsBetweenDatabases(ObjectIdCollection acObjIdColl, string dwgFilename)
        {
            //ObjectIdCollection acObjIdColl = new ObjectIdCollection();
            // Get the current document and database
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            // Change the file and path to match a drawing template on your workstation
            string sLocalRoot = Application.GetSystemVariable("LOCALROOTPREFIX") as string;
            string sTemplatePath = sLocalRoot + "Template\\acadiso.dwt";
            // Create a new drawing to copy the objects to
            DocumentCollection acDocMgr = Application.DocumentManager;
            Document acNewDoc = acDocMgr.Add(sTemplatePath);
            Database acDbNewDoc = acNewDoc.Database;


            acDocMgr.MdiActiveDocument = acNewDoc;
            // Lock the new document
            using (DocumentLock acLckDoc = acNewDoc.LockDocument())
            {
                // Start a transaction in the new database
                using (Transaction acTrans = acDbNewDoc.TransactionManager.StartTransaction())
                {
                    // Open the Block table for read
                    BlockTable acBlkTblNewDoc;
                    acBlkTblNewDoc = acTrans.GetObject(acDbNewDoc.BlockTableId,
                                                       OpenMode.ForRead) as BlockTable;
                    // Open the Block table record Model space for read
                    BlockTableRecord acBlkTblRecNewDoc;
                    acBlkTblRecNewDoc = acTrans.GetObject(acBlkTblNewDoc[BlockTableRecord.ModelSpace],
                                                        OpenMode.ForRead) as BlockTableRecord;
                    //acTrans.TransactionManager.QueueForGraphicsFlush();
                    // Clone the objects to the new database
                    IdMapping acIdMap = new IdMapping();
                    acCurDb.WblockCloneObjects(acObjIdColl, acBlkTblRecNewDoc.ObjectId, acIdMap,
                                               DuplicateRecordCloning.Ignore, false);
                    // Save the copied objects to the database
                    acTrans.Commit();
                }
                // Unlock the document
            }
            // Set the new document current

            dynamic acadApp = Autodesk.AutoCAD.ApplicationServices.Application.AcadApplication;
            acadApp.ZoomExtents();
            //Active.Document.SendStringToExecute("_.zoom _all ", true, true, false);

            acDbNewDoc.SaveAs(dwgFilename, DwgVersion.Current);
            acDocMgr.MdiActiveDocument = acNewDoc;
            //acNewDoc.CloseAndDiscard();

            acDocMgr.MdiActiveDocument = acDoc;

        }
    }

}
