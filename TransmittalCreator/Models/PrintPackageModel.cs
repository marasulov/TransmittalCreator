using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices.Core;
using TransmittalCreator.Models.Layouts;

namespace TransmittalCreator.Models;

public class PrintPackageModel
{
	public PrintPackageModel(string pdfFileName, List<LayoutModel> layouts)
	{
		PdfFileName = pdfFileName;
		Layouts = layouts;
	}

	public string PdfFileName { get; set; }
	public List<LayoutModel> Layouts { get; set; }
	public string DwgFileName { get; } = (string)Application.GetSystemVariable("DWGNAME");
	public string DwgPath { get; set; } = (string)Application.GetSystemVariable("DWGPREFIX");
}