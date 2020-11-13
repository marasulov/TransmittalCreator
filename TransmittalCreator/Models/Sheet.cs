using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Excel = Microsoft.Office.Interop.Excel;
using OfficeOpenXml;
using System.IO;
using System.Runtime.CompilerServices;

namespace TransmittalCreator
{
    public class Sheet
    {
        /// <summary>
        /// Номер листа
        /// </summary>
        public int SheetNumber { get; set; }

        /// <summary>
        /// Document Number
        /// </summary>
        public string DocNumber { get; set; }

        /// <summary>
        /// Comment
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Document Name in English
        /// </summary>

        public string ObjectNameEng { get; set; }

        /// <summary>
        /// Document Title (English)
        /// </summary>
        public string DocTitleEng{ get; set; }

        /// <summary>
        /// documnet name in rus
        /// </summary>
        public string ObjectNameRu { get; set; }
        /// <summary>
        /// Document Title (Rus)
        /// </summary>
        public string DocTitleRu { get; set; }

        public Sheet(string sheetNumber, string docNumber, string objectNameEng, string docTitleEng, string objectNameRu,  string docTitleRu)
        {
            Int32.TryParse(sheetNumber, out int tempNumber);
            this.SheetNumber = tempNumber;
            this.DocNumber = docNumber;
            this.ObjectNameEng = objectNameEng;
            this.DocTitleEng= docTitleEng;
            this.ObjectNameRu = objectNameRu;
            this.DocTitleRu = docTitleRu;
        }

        public Sheet(string sheetNumber, string docNumber, string commment, string objectNameEng, string docTitleEng, string objectNameRu, string docTitleRu)
        {
            Int32.TryParse(sheetNumber, out int tempNumber);
            this.SheetNumber = tempNumber;
            this.DocNumber = docNumber;
            this.Comment = commment;
            this.ObjectNameEng = objectNameEng;
            this.DocTitleEng = docTitleEng;
            this.ObjectNameRu = objectNameRu;
            this.DocTitleRu = docTitleRu;
        }


        public Sheet(string docNumber, string objectNameEng)
        {
            DocNumber = docNumber;
            ObjectNameEng = objectNameEng;
        }

        public static void  WriteToExcel(List<Sheet> sheets)
        {
            string templatePath = @"\\uzliti-cloud\Users\GRP3\MarasulovYusuf\Transmittal\шаблон.xltx";
            var application = new Excel.Application();
            application.Visible = true;
            Excel.Workbook wb = application.Workbooks.Open(templatePath);
            Excel.Worksheet ws = application.ActiveSheet as Excel.Worksheet;

            Excel.Range usedRange = ws.UsedRange;
            int countRecords = usedRange.Rows.Count;
            int add = countRecords + 1;
            int i = 0;

            int countSheets = sheets.Count + 9;
            Excel.Range rangeX = ws.Range["B10", "q" + countSheets];
            rangeX.RowHeight = 63;
            rangeX.VerticalAlignment = Excel.XlVAlign.xlVAlignCenter;
            Excel.Borders borders = rangeX.Borders;
            borders.LineStyle = Excel.XlLineStyle.xlContinuous;
            borders.Weight = Excel.XlBorderWeight.xlMedium;

            ws.Cells[4,17] = DateTime.Now.ToString("   dd-MM-yyyy");

            sheets = sheets.OrderBy(x => x.SheetNumber).ToList();

            foreach (Sheet item in sheets)
            {
                ws.Cells[add + i, 2] = item.SheetNumber;

                ws.Cells[add + i, 3] = item.DocNumber;
                ws.Range[ws.Cells[add + i, 3], ws.Cells[add + i, 7]].Merge();

                ws.Cells[add + i, 8] = item.DocTitleEng + "\n"+ item.ObjectNameEng;
                ws.Range[ws.Cells[add + i, 8], ws.Cells[add + i, 14]].Merge();

                ws.Cells[add + i, 15] = item.DocTitleRu + "\n" + item.ObjectNameRu;
                ws.Range[ws.Cells[add + i, 15], ws.Cells[add + i, 17]].Merge();
                i++;
            }
        }


        public static void WriteToExcelEplus(List<Sheet> sheetList, string dwgFilename)
        {
            string templatePath = @"\\uzliti-cloud\Users\GRP3\MarasulovYusuf\Transmittal\шаблон.xlsx";
            string dirPath = Path.GetDirectoryName(dwgFilename);
            string xlsFilename = Path.GetFileNameWithoutExtension(dwgFilename) + ".xlsx";
            string allTransFileName = Path.Combine(dirPath, xlsFilename);
            
            FileInfo existingFile = new FileInfo(templatePath);
            FileInfo fNewFile = new FileInfo(allTransFileName);
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (ExcelPackage excelFile = new ExcelPackage(existingFile))
            {

                ExcelWorksheet ws = excelFile.Workbook.Worksheets["Transmittal"];
                double rowHeight = 65;
                ws.DefaultRowHeight = rowHeight;
                int startRow = 9;
                int i = 0;
                int countSheets = sheetList.Count + 9;

                //rangeX.VerticalAlignment = XlVAlign.xlVAlignCenter;
                //Excel.Borders borders = rangeX.Borders;
                //borders.LineStyle = Excel.XlLineStyle.xlContinuous;
                //borders.Weight = XlBorderWeight.xlMedium;

                ws.Cells[4, 17].Value = DateTime.Now.ToString("   dd-MM-yyyy");
                sheetList = sheetList.OrderBy(x => x.SheetNumber).ToList();
                
                foreach (var item in sheetList)
                {
                    i++;

                    ws.Row(startRow + i).Style.WrapText = true;
                    ws.Cells[startRow + i, 2].Value = i;
                    ws.Cells[startRow + i, 2].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

                    ws.Cells[startRow + i, 3].Value = item.DocNumber;
                    ws.Cells[startRow + i, 3, startRow + i, 7].Merge = true;
                    ws.Cells[startRow + i, 3, startRow + i, 7].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

                    ws.Cells[startRow + i, 8].Value = item.DocTitleEng + "\n" + item.ObjectNameEng;
                    ws.Cells[startRow + i, 8, startRow + i, 14].Merge = true;
                    ws.Cells[startRow + i, 8, startRow + i, 14].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

                    ws.Cells[startRow + i, 15].Value = item.DocTitleRu + "\n" + item.ObjectNameRu;
                    ws.Cells[startRow + i, 15, startRow + i, 17].Merge = true;
                    ws.Cells[startRow + i, 15, startRow + i, 17].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

                    int addHeight = 0;

                    if (!string.IsNullOrWhiteSpace(item.ObjectNameRu))
                    {
                        int objectNameRuLen = item.ObjectNameRu.Length;
                        if (objectNameRuLen > 100) addHeight = 35;
                    }
                    ws.Row(startRow + i).Height = rowHeight + addHeight;

                }

                var tableRange = ws.Cells[startRow, 2, countSheets, 17];
                tableRange.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thick);
                tableRange.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                excelFile.SaveAs(fNewFile);
            }

            

            //foreach (Sheet item in sheets)
            //{
            //    ws.Cells[add + i, 2] = item.SheetNumber;

            //    ws.Cells[add + i, 3] = item.DocNumber;
            //    ws.Range[ws.Cells[add + i, 3], ws.Cells[add + i, 7]].Merge();

            //    ws.Cells[add + i, 8] = item.DocTitleEng + "\n" + item.ObjectNameEng;
            //    ws.Range[ws.Cells[add + i, 8], ws.Cells[add + i, 14]].Merge();

            //    ws.Cells[add + i, 15] = item.DocTitleRu + "\n" + item.ObjectNameRu;
            //    ws.Range[ws.Cells[add + i, 15], ws.Cells[add + i, 17]].Merge();
            //    i++;
            //}
        }
    }

}