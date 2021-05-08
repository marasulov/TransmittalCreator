using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace TransmittalCreator.Services.Files
{
    public class FileManager : IFileManager
    {
        public string FileName { get; set; }
        public string DirPath { get; set; }

        public FileManager()
        {
            
        }

        public string GetFilePath()
        {
            FileName = (string) Application.GetSystemVariable("DWGNAME");
            DirPath = (string) Application.GetSystemVariable("DWGPREFIX");

            return Path.Combine(DirPath, FileName);
        }
    }
}