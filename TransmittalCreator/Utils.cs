using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using DV2177.Common;

namespace TransmittalCreator
{
    public class Utils
    {
        /// <summary>
        /// get dict from attributes of block
        /// </summary>
        /// <param name="tr"></param>
        /// <param name="attrList"></param>
        /// <returns></returns>
        public static (string, Dictionary<string,string>) GetAttrList(Transaction tr, Dictionary<string, string> attrList)
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
                    throw new ArgumentException("Выберите блок");
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
                        attrList.Add(attRef.Tag,attRef.TextString);
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
        public static List<Sheet> GetSheetsFromBlocks(Editor ed, List<Sheet> dict, Transaction tr, ObjectId[] idArray)
        {

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
            
            string sheetNumber = "", docNumber = "", objectNameEng = "", docTitleEng = "", objectNameRu = "", docTitleRu = "";

            foreach (ObjectId blkId in idArray)
            {
                BlockReference blkRef = (BlockReference)tr.GetObject(blkId, OpenMode.ForRead);

                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForRead);
                ed.WriteMessage("\nBlock: " + btr.Name);
                btr.Dispose();
                AttributeCollection attCol = blkRef.AttributeCollection;
                string str = "";

                foreach (ObjectId attId in attCol)
                {
                    AttributeReference attRef = (AttributeReference)tr.GetObject(attId, OpenMode.ForRead);

                    switch (attRef.Tag)
                    {
                        case "НОМЕР_ЛИСТА":
                            docNumber = attRef.TextString;
                            break;
                        case "НАЗВАНИЕEN":
                            objectNameEng = attRef.TextString;
                            break;
                        case "ЛИСТEN":
                            docTitleEng = attRef.TextString;
                            break;
                        case "НАЗВАНИЕRU":
                            objectNameRu = attRef.TextString;
                            break;
                        case "НАЗВАНИЕ_ЛИСТАRU":
                            docTitleRu = attRef.TextString;
                            break;
                        case "ЛИСТ":
                            sheetNumber = attRef.TextString;
                            break;
                    }
                }

                dict.Add(new Sheet(sheetNumber, docNumber, objectNameEng, docTitleEng, objectNameRu, docTitleRu));
            }

            return dict;
        }

        public void CreateOnlyVed()
        {
            Editor ed = Active.Editor;
            Transaction tr = Active.Database.TransactionManager.StartTransaction();

            List<Sheet> dict = new List<Sheet>();
            // Start the transaction

            try
            {

                //GetSheetsFromBlocks(ed, dict, tr);

                tr.Commit();

                CreateTableFromList(dict);


            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage(("Exception: " + ex.Message));
            }
            finally
            {
                tr.Dispose();
            }
        }

        public void CreateOnlytrans()
        {
            Editor ed = Active.Editor;
            Transaction tr = Active.Database.TransactionManager.StartTransaction();

            List<Sheet> dict = new List<Sheet>();
            // Start the transaction

            try
            {
                //GetSheetsFromBlocks(ed, dict, tr);

                tr.Commit();

                Sheet.WriteToExcel(dict);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage(("Exception: " + ex.Message));
            }
            finally
            {
                tr.Dispose();
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
    }
}
