using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TransmittalCreator;

public class Sheet
{
	public Sheet(string sheetNumber, string docNumber, string objectNameEng, string docTitleEng, string objectNameRu,
		string docTitleRu, string viewValue, double minPointX)
	{
		int.TryParse(sheetNumber, out var tempNumber);
		SheetNumber = tempNumber;
		DocNumber = docNumber;
		ObjectNameEng = objectNameEng;
		DocTitleEng = docTitleEng;
		ObjectNameRu = objectNameRu;
		DocTitleRu = docTitleRu;
		ViewValue = viewValue;
		MinPointX = minPointX;
	}

	public Sheet(string docNumber, string objectNameEng)
	{
		DocNumber = docNumber;
		ObjectNameEng = objectNameEng;
	}

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
	public string DocTitleEng { get; set; }

	/// <summary>
	/// documnet name in rus
	/// </summary>
	public string ObjectNameRu { get; set; }

	/// <summary>
	/// Document Title (Rus)
	/// </summary>
	public string DocTitleRu { get; set; }

	public string ViewValue { get; set; }

	public double MinPointX { get; }

	public static void WriteToExcelEplus(List<Sheet> sheetList, string dwgFilename)
	{
		var templatePath = @"\\uzliti-cloud\Users\GRP3\MarasulovYusuf\Transmittal\шаблон.xlsx";
		var dirPath = Path.GetDirectoryName(dwgFilename);
		var xlsFilename = Path.GetFileNameWithoutExtension(dwgFilename) + ".xlsx";
		var allTransFileName = Path.Combine(dirPath, xlsFilename);

		var existingFile = new FileInfo(templatePath);
		var fNewFile = new FileInfo(allTransFileName);
		ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
		using (var excelFile = new ExcelPackage(existingFile))
		{
			var ws = excelFile.Workbook.Worksheets["Transmittal"];
			double rowHeight = 65;
			ws.DefaultRowHeight = rowHeight;
			var startRow = 9;
			var i = 0;
			var countSheets = sheetList.Count + 9;

			ws.Cells[4, 17].Value = DateTime.Now.ToString("   dd-MM-yyyy");
			sheetList = sheetList.OrderBy(x => x.SheetNumber).ToList();

			foreach (var item in sheetList)
			{
				i++;

				ws.Row(startRow + i).Style.WrapText = true;
				ws.Cells[startRow + i, 2].Value = i;
				ws.Cells[startRow + i, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);

				ws.Cells[startRow + i, 3].Value = item.DocNumber;
				ws.Cells[startRow + i, 3, startRow + i, 7].Merge = true;
				ws.Cells[startRow + i, 3, startRow + i, 7].Style.Border.BorderAround(ExcelBorderStyle.Thin);

				ws.Cells[startRow + i, 8].Value = item.DocTitleEng + "\n" + item.ObjectNameEng;
				ws.Cells[startRow + i, 8, startRow + i, 14].Merge = true;
				ws.Cells[startRow + i, 8, startRow + i, 14].Style.Border.BorderAround(ExcelBorderStyle.Thin);

				ws.Cells[startRow + i, 15].Value = item.DocTitleRu + "\n" + item.ObjectNameRu;
				ws.Cells[startRow + i, 15, startRow + i, 17].Merge = true;
				ws.Cells[startRow + i, 15, startRow + i, 17].Style.Border.BorderAround(ExcelBorderStyle.Thin);

				var addHeight = 0;

				if (!string.IsNullOrWhiteSpace(item.ObjectNameRu))
				{
					var objectNameRuLen = item.ObjectNameRu.Length;
					if (objectNameRuLen > 100) addHeight = 35;
				}

				ws.Row(startRow + i).Height = rowHeight + addHeight;
			}

			var tableRange = ws.Cells[startRow, 2, countSheets, 17];
			tableRange.Style.Border.BorderAround(ExcelBorderStyle.Thick);
			tableRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
			excelFile.SaveAs(fNewFile);
		}
	}
}