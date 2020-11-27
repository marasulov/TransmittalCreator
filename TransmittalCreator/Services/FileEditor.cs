using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;

namespace TransmittalCreator.Services
{
    class FileEditor
    {
        public string FileName { get; set; }

        public FileEditor(string fileName)
        {
            this.FileName = fileName;
        }

        public void OpenDrawing()
        {
            DocumentCollection acDocMgr = Application.DocumentManager;
            if (File.Exists(this.FileName))
            {
                acDocMgr.Open(this.FileName, false);
            }
            else
            {
                acDocMgr.MdiActiveDocument.Editor.WriteMessage("File " + this.FileName +
                                                               " does not exist.");
            }
        }

        public void CloseDoc_Method()
        {
            Document docToWorkOn = Application.DocumentManager.Open(this.FileName, false);
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            ed.Command("_.zoom", "_extents");
            docToWorkOn.CloseAndSave(this.FileName);
        }
        public void ZoomExtCmd()
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            ed.Command("_.zoom", "_extents");
        }



    }
}
