using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using DV2177.Common;
using Exception = Autodesk.AutoCAD.Runtime.Exception;

namespace TransmittalCreator.Services
{
    public class Utils
    {
        /// <summary>
        /// get dict from attributes of block
        /// </summary>
        /// <param name="tr"></param>
        /// <param name="attrList"></param>
        /// <returns></returns>
        public static (string, Dictionary<string, string>) GetAttrList(Transaction tr, Dictionary<string, string> attrList)
        {
            string blockName = "";
            try
            {
                // Build a filter list so that only
                // block references are selected
                TypedValue[] filList = new TypedValue[1] { new TypedValue((int)DxfCode.Start, "INSERT") };
                SelectionFilter filter = new SelectionFilter(filList);
                PromptSelectionOptions opts = new PromptSelectionOptions();
                opts.MessageForAdding = "Select block references: ";
                PromptSelectionResult res = Active.Editor.GetSelection(opts, filter);
                // Do nothing if selection is unsuccessful
                if (res.Status != PromptStatus.OK)
                {
                    throw new InvalidOperationException("block not selected");
                }
                SelectionSet selSet = res.Value;
                ObjectId[] idArray = selSet.GetObjectIds();
                foreach (ObjectId blkId in idArray)
                {
                    BlockReference blkRef = (BlockReference)tr.GetObject(blkId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(blkRef.DynamicBlockTableRecord, OpenMode.ForRead);
                    blockName = btr.Name;
                    Active.Editor.WriteMessage("\nBlock: " + blockName);
                    btr.Dispose();
                    AttributeCollection attCol = blkRef.AttributeCollection;
                    foreach (ObjectId attId in attCol)
                    {
                        AttributeReference attRef = (AttributeReference)tr.GetObject(attId, OpenMode.ForRead);
                        string str = ("\n  Attribute Tag: " + attRef.Tag + "\n    Attribute String: " + attRef.TextString);
                        attrList.Add(attRef.Tag, attRef.TextString);
                        Active.Editor.WriteMessage(str);
                    }
                }

                tr.Commit();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                Active.Editor.WriteMessage(("Exception: " + ex.Message));
            }
            finally
            {
                tr.Dispose();
            }

            return (blockName, attrList);
        }



        /// <summary>
        /// создает колекцию листов из блоков штампа
        /// </summary>
        /// <param name="ed">editor</param>
        /// <param name="dict"> коллекция листов</param>
        /// <param name="tr"> транзакция </param>
        /// <returns>коллекция листов </returns>
        public static List<Sheet> GetSheetsFromBlocks(Editor ed, List<Sheet> dict, Transaction tr, ObjectIdCollection objectIdCollection, Models.AttributModel attributModel)
        {
            string objectNameEn = attributModel.ObjectNameEn;
            string objectNameRu = attributModel.ObjectNameRu;
            string listNumber = attributModel.Position;
            string nomination = attributModel.Nomination;
            string commentAttr = attributModel.Comment;
            string trItem = attributModel.TrItem;
            string trDocNumber = attributModel.TrDocNumber;
            string trDocTitleEn = attributModel.TrDocTitleEn;
            string trDocTitleRu = attributModel.TrDocTitleRu;

            // Build a filter list so that only
            // block references are selected
            //TypedValue[] filList = new TypedValue[1] { new TypedValue((int)DxfCode.Start, "INSERT") };
            //SelectionFilter filter = new SelectionFilter(filList);
            //PromptSelectionOptions opts = new PromptSelectionOptions();
            //opts.MessageForAdding = "Select block references: ";
            //PromptSelectionResult res = ed.GetSelection(opts, filter);

            //if (res.Status != PromptStatus.OK)
            //    throw new ArgumentException("Выберите блок");
            //SelectionSet selSet = res.Value;
            ////ObjectId[] idArray = selSet.GetObjectIds();
            //List<ObjectId> objectIds = objectIdCollection.ToList();

            // objectIdCollection.CopyTo(idArray, objectIdCollection.Count);
            string sheetNumber = "", docNumber = "", comment = "", objectNameEng = "", docTitleEng = "", objectNameRus = "", docTitleRu = "", transItem = "";

            foreach (ObjectId blkId in objectIdCollection)
            {
                BlockReference blkRef = (BlockReference)tr.GetObject(blkId, OpenMode.ForRead);

                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForRead);

                Extents3d extents3d = blkRef.GeometricExtents;
                ed.WriteMessage("\nBlock:{0} габариты {1} ", btr.Name, extents3d.MinPoint.ToString(), extents3d.MaxPoint.ToString());
                btr.Dispose();

                AttributeCollection attCol = blkRef.AttributeCollection;

                var attrDict = AttributeExtensions.GetAttributesValues(blkRef);
                
                docTitleEng = attrDict.FirstOrDefault(x => x.Key == objectNameEn).Value;
                docTitleRu = attrDict.FirstOrDefault(x => x.Key == objectNameRu).Value;


                sheetNumber = attrDict.FirstOrDefault(x => x.Key == listNumber).Value;
                docNumber = attrDict.FirstOrDefault(x => x.Key == nomination).Value;

                comment = attrDict.FirstOrDefault(x => x.Key == commentAttr).Value;

                //transItem = attrDict.FirstOrDefault(x => x.Key == trItem).Value;
                objectNameEng = attrDict.FirstOrDefault(x => x.Key == trDocTitleEn).Value;
                objectNameRus = attrDict.FirstOrDefault(x => x.Key == trDocTitleRu).Value;

                //foreach (var attribut in attrModelProps)
                //{
                //    foreach (ObjectId attId in attCol)
                //    {
                //        AttributeReference attRef = (AttributeReference)tr.GetObject(attId, OpenMode.ForRead);
                //        attribut.GetValue(attributModel);

                //    }

                //    //switch (attRef.Tag)
                //    //{
                //    //    case attributModel.Position:
                //    //        docNumber = attRef.TextString;
                //    //        break;
                //    //    case "НАЗВАНИЕEN":
                //    //        objectNameEng = attRef.TextString;
                //    //        break;
                //    //    case "ЛИСТEN":
                //    //        docTitleEng = attRef.TextString;
                //    //        break;
                //    //    case "НАЗВАНИЕRU":
                //    //        objectNameRu = attRef.TextString;
                //    //        break;
                //    //    case "НАЗВАНИЕ_ЛИСТАRU":
                //    //        docTitleRu = attRef.TextString;
                //    //        break;
                //    //    case "ЛИСТ":
                //    //        sheetNumber = attRef.TextString;
                //    //        break;
                //    //}
                //}

                dict.Add(new Sheet(sheetNumber, docNumber, comment, objectNameEng, docTitleEng, objectNameRus, docTitleRu));
            }

            return dict;
        }





        /// <summary>
        /// find print area and pdf name by block id
        /// </summary>
        public static Dictionary<string, Extents3d> GetExtentsNamePdf(Editor ed, Dictionary<string, Extents3d> dict, Transaction tr,
            ObjectIdCollection objectIdCollection)
        {
            string sheetNumber = "";
            foreach (ObjectId blkId in objectIdCollection)
            {
                BlockReference blkRef = (BlockReference) tr.GetObject(blkId, OpenMode.ForRead);

                BlockTableRecord btr = (BlockTableRecord) tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForRead);

                Extents3d extents3d = blkRef.GeometricExtents;
                AttributeCollection attCol = blkRef.AttributeCollection;
                var attrDict = AttributeExtensions.GetAttributesValues(blkRef);
                sheetNumber = attrDict.FirstOrDefault(x => x.Key == "НОМЕР_ЛИСТА").Value;

                ed.WriteMessage("\nBlock:{0} - {1} габариты {2} ", btr.Name, sheetNumber,extents3d.MinPoint.ToString());
                btr.Dispose();

                dict[sheetNumber] = extents3d;
            }

            return dict;
        }
        //private string GetAttributeValue(string attrTag, AttributeCollection attCol)
        //{
        //    foreach (var attr in attCol)
        //    {
        //        if (attr == attrTag)
        //        {
        //            return 
        //        }
        //    }
        //}


        public static Dictionary<string, string> GetAttribs(string blockName, string tag)
        {
            // create a new instance of Dictionary<string, string>
            var attribs = new Dictionary<string, string>();

            // get the documents collection
            var docs = Application.DocumentManager;

            // use an OpenCloseTransaction which is not related to a document or database
            using (var tr = new OpenCloseTransaction())
            {
                // iterate through the documents
                foreach (Document doc in docs)
                {
                    // get the document database
                    var db = doc.Database;

                    // get the database block table
                    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                    // if the block table contains a block definitions named 'blockName'...
                    if (bt.Has(blockName))
                    {
                        // open the block definition
                        var btr = (BlockTableRecord)tr.GetObject(bt[blockName], OpenMode.ForRead);

                        // get the inserted block references ObjectIds
                        var ids = btr.GetBlockReferenceIds(true, true);

                        // if any...
                        if (0 < ids.Count)
                        {
                            // open the first block reference
                            var br = (BlockReference)tr.GetObject(ids[0], OpenMode.ForRead);

                            // iterate through the attribute collection
                            foreach (ObjectId id in br.AttributeCollection)
                            {
                                // open the attribute reference
                                var attRef = (AttributeReference)tr.GetObject(id, OpenMode.ForRead);

                                // if the attribute tag is equal to 'tag'
                                if (attRef.Tag.Equals(tag, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    // add an entry to the dictionary
                                    attribs[doc.Name] = attRef.TextString;
                                    // break the loop
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            // return the dictionary
            return attribs;
        }


        public void CreateOnlyVed(List<Sheet> dict)
        {
            Editor ed = Active.Editor;
            Transaction tr = Active.Database.TransactionManager.StartTransaction();


            // Start the transaction

            try
            {

                //GetSheetsFromBlocks(ed, dict, tr);


                CreateTableFromList(dict);

                tr.Commit();

            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage(("Exception: " + ex.Message + " " + ex.InnerException));
            }
            finally
            {
                tr.Dispose();
            }
        }

        public void CreateOnlytrans(List<Sheet> dict)
        {
            Editor ed = Active.Editor;
            //Transaction tr = Active.Database.TransactionManager.StartTransaction();

            // Start the transaction

            try
            {
                //GetSheetsFromBlocks(ed, dict, tr);

                //tr.Commit();

                Sheet.WriteToExcel(dict);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage(("Exception: " + ex.Message + " " + ex.InnerException));
            }

        }

        public void CreateTableFromList(List<Sheet> dict)
        {
            dict = dict.OrderBy(x => x.SheetNumber).ToList();
            Database db = Active.Database;
            Editor ed = Active.Editor;


            using (Transaction tr = db.TransactionManager.StartTransaction())
            {

                PromptPointResult pr = ed.GetPoint("\nEnter table insertion point: ");
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                ObjectId msId = bt[BlockTableRecord.ModelSpace];

                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(msId, OpenMode.ForWrite);

                Table tb = new Table();
                tb.TableStyle = db.Tablestyle;

                //tb.TableStyle = db.Tablestyle;
                btr.AppendEntity(tb);

                // Число строк
                int RowsNum = dict.Count;
                // Число столбцов
                int ColumnsNum = 3;

                // Высота строки
                double rowheight = 8;
                // Ширина столбца
                double columnwidth = 15;

                // Добавляем строки и колонки
                tb.InsertRows(0, rowheight, RowsNum + 1);
                tb.InsertColumns(0, columnwidth, ColumnsNum - 1);

                tb.SetRowHeight(rowheight);
                tb.SetColumnWidth(columnwidth);

                tb.Position = pr.Value;

                // Объединяем ячейки
                CellRange range = CellRange.Create(tb, 0, 0, 0, 2);
                tb.MergeCells(range);

                range.Borders.Top.IsVisible = false;
                range.Borders.Bottom.IsVisible = true;
                range.Borders.Left.IsVisible = false;
                range.Borders.Right.IsVisible = false;

                //var row = tb.Rows[RowsNum];


                range = CellRange.Create(tb, 1, 1, 1, 2);

                tb.UnmergeCells(range);
                tb.Columns[0].Width = 15;
                tb.Cells[0, 0].TextHeight = 5;

                tb.Cells[0, 0].Alignment = CellAlignment.MiddleCenter;
                tb.Cells[0, 0].TextString = "Ведомость рабочих чертежей основного комплекта";


                var row = tb.Rows[RowsNum + 1];
                tb.UnmergeCells(row);
                tb.Cells[1, 0].TextString = "Лист";

                tb.Columns[1].Width = 140;
                tb.Cells[1, 1].TextString = "Наименование";

                tb.Columns[2].Width = 30;
                tb.Cells[1, 2].TextString = "Примечание";


                //заполняем по одной все ячейки
                int curRow = 2;
                foreach (var item in dict)
                {
                    if (!string.IsNullOrWhiteSpace(item.DocNumber))
                    {
                        tb.Cells[curRow, 0].TextHeight = 3.5;
                        tb.Cells[curRow, 0].TextString = item.SheetNumber.ToString();
                        tb.Cells[curRow, 1].TextHeight = 3.5;
                        tb.Cells[curRow, 1].TextString = item.DocNumber + "_" + item.DocTitleRu;
                        tb.Cells[curRow, 1].Alignment = CellAlignment.MiddleLeft;

                        if (!string.IsNullOrEmpty(item.Comment))
                        {
                            tb.Cells[curRow, 2].TextHeight = 3.5;
                            tb.Cells[curRow, 2].TextString = item.Comment;
                            tb.Cells[curRow, 2].Alignment = CellAlignment.MiddleLeft;
                        }


                    }
                    //tb.Cells[curRow, 6].TextString = item.DocTitleEng;
                    //tb.Cells[curRow, 4].TextString = item.DocTitleRu;
                    curRow++;
                }
                range = CellRange.Create(tb, RowsNum, 0, RowsNum, RowsNum);
                tb.UnmergeCells(range);

                tb.GenerateLayout();
                tr.AddNewlyCreatedDBObject(tb, true);
                tr.Commit();
            }
        }



        /// <summary>
        /// select from model dynblocks byName
        /// </summary>
        /// <param name="blockName"></param>
        /// <returns></returns>
        [CommandMethod("selb")]
        public static ObjectIdCollection SelectDynamicBlockReferences(string blockName="Формат")
        {
            Editor ed = Active.Editor;
            Database db = Active.Database;
            List<ObjectId> listObjectIds = new List<ObjectId>();
            ObjectIdCollection dynBlockRefs = new ObjectIdCollection();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                // получаем таблицу блоков и проходим по всем записям таблицы блоков
                BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                foreach (ObjectId btrId in bt)
                {
                    // получаем запись таблицы блоков и смотри анонимная ли она
                    BlockTableRecord btr = (BlockTableRecord)trans.GetObject(btrId, OpenMode.ForRead);
                    if (btr.IsDynamicBlock && btr.Name == blockName)
                    {
                        // получаем все анонимные блоки динамического блока
                        ObjectIdCollection anonymousIds = btr.GetAnonymousBlockIds();
                        // получаем все прямые вставки динамического блока
                        dynBlockRefs = btr.GetBlockReferenceIds(true, true);
                        ObjectId mSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
                        foreach (ObjectId anonymousBtrId in anonymousIds)
                        {
                            // open the model space BlockTableRecord
                            //var modelSpace = (BlockTableRecord)tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                            // получаем анонимный блок
                            BlockTableRecord anonymousBtr =
                                (BlockTableRecord)trans.GetObject(anonymousBtrId, OpenMode.ForRead);

                            // получаем все вставки этого блока
                            ObjectIdCollection blockRefIds = anonymousBtr.GetBlockReferenceIds(true, true);

                            SymbolTableRecord symTableRecord =
                                (SymbolTableRecord)trans.GetObject(anonymousBtrId, OpenMode.ForRead);

                            foreach (ObjectId id in blockRefIds)
                            {
                                //var blockReference = (BlockReference)tr.GetObject(id, OpenMode.ForRead);
                                var e = (BlockReference)trans.GetObject(id, OpenMode.ForRead);
                                string curBlockName = e.BlockName;

                                ObjectId ownerId = e.OwnerId;

                                if (ownerId == mSpaceId)
                                {
                                    ed.WriteMessage(e.ToString());
                                    listObjectIds.Add(id);
                                    dynBlockRefs.Add(id);
                                    ed.WriteMessage("\n \"{0}\" соответствуют {1} \n",
                                        btr.Name, id);
                                }
                            }
                        }

                        // Что-нибудь делаем с созданным нами набором
                        //ed.WriteMessage("\nДинамическому блоку \"{0}\" соответствуют {1} анонимных блоков и {2} вставок блока\n",
                        //    btr.Name, anonymousIds.Count, dynBlockRefs.Count);

                    }
                }
            }
            return dynBlockRefs;
        }

        /// <summary>
        /// get blocks by name
        /// </summary>
        /// <param name="blockName"></param>
        /// <returns></returns>
        public static IEnumerable<ObjectId> GetAllCurrentSpaceBlocksByName(string blockName)
        {
            var db = HostApplicationServices.WorkingDatabase;
            using (var tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                var curSpace = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead);
                return curSpace
                    .Cast<ObjectId>()
                    .Where(id => id.ObjectClass.DxfName == "INSERT")
                    .Select(id => (BlockReference)tr.GetObject(id, OpenMode.ForRead))
                    .Where(br => ((BlockTableRecord)tr.GetObject(br.DynamicBlockTableRecord, OpenMode.ForRead)).Name == blockName)
                    .Select(br => br.ObjectId);
            }
        }



        [CommandMethod("blockName")]
        static public void GetBlockName()
        {
            Document doc = Active.Document;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            PromptEntityOptions options =
                new PromptEntityOptions("\nSelect block reference");
            options.SetRejectMessage("\nSelect only block reference");
            options.AddAllowedClass(typeof(BlockReference), false);
            PromptEntityResult acSSPrompt = ed.GetEntity(options);
            using (Transaction tx =
                db.TransactionManager.StartTransaction())
            {
                BlockReference blockRef = tx.GetObject(acSSPrompt.ObjectId,
                    OpenMode.ForRead) as BlockReference;
                BlockTableRecord block = null;
                if (blockRef.IsDynamicBlock)
                {
                    //get the real dynamic block name.
                    block = tx.GetObject(blockRef.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                }
                else
                {
                    block = tx.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                }
                if (block != null)
                {
                    ed.WriteMessage("Block name is : "
                                    + block.Name + "\n");
                }
                tx.Commit();
            }
        }
    }

    public static class AttributeExtensions
    {
        public static IEnumerable<AttributeReference> GetAttributes(this AttributeCollection attribs)
        {
            foreach (ObjectId id in attribs)
            {
                yield return (AttributeReference)id.GetObject(OpenMode.ForRead, false, false);
            }
        }

        public static Dictionary<string, string> GetAttributesValues(this BlockReference br)
        {
            return br.AttributeCollection
                .GetAttributes()
                .ToDictionary(att => att.Tag, att => att.TextString);
        }

        public static void SetAttributesValues(this BlockReference br, Dictionary<string, string> atts)
        {
            foreach (AttributeReference attRef in br.AttributeCollection.GetAttributes())
            {
                if (atts.ContainsKey(attRef.Tag))
                {
                    attRef.UpgradeOpen();
                    attRef.TextString = atts[attRef.Tag];
                }
            }
        }
    }
}
