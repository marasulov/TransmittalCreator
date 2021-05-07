using System.Collections.Generic;
using TransmittalCreator.Models.Layouts;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace TransmittalCreator.Models
{
    public class PrintPackageModel
    {
        public string PdfFileName { get; set; }
        public List<LayoutModel> Layouts { get; set; }
        public string DwgFileName { get; } = (string) Application.GetSystemVariable("DWGNAME");
        public string DwgPath { get; set; } = (string) Application.GetSystemVariable("DWGPREFIX");

        public PrintPackageModel(string pdfFileName, List<LayoutModel> layouts)
        {
            PdfFileName = pdfFileName;
            Layouts = layouts;
        }
    }
}