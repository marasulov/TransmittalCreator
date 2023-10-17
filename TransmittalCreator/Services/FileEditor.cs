using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace TransmittalCreator.Services;

internal class FileEditor
{
	public FileEditor(string fileName)
	{
		FileName = fileName;
	}

	public string FileName { get; set; }

	public void OpenDrawing()
	{
		var acDocMgr = Application.DocumentManager;
		if (File.Exists(FileName))
			acDocMgr.Open(FileName, false);
		else
			acDocMgr.MdiActiveDocument.Editor.WriteMessage("File " + FileName +
			                                               " does not exist.");
	}

	public void CloseDoc_Method()
	{
		var docToWorkOn = Application.DocumentManager.Open(FileName, false);
		var ed = Application.DocumentManager.MdiActiveDocument.Editor;
		ed.Command("_.zoom", "_extents");
		docToWorkOn.CloseAndSave(FileName);
	}

	public void ZoomExtCmd()
	{
		var ed = Application.DocumentManager.MdiActiveDocument.Editor;
		ed.Command("_.zoom", "_extents");
	}
}