using System.IO;
using System.Reflection;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Newtonsoft.Json;

namespace TransmittalCreator.Services.Files;

public class FileManager : IFileManager
{
	//public FileManager()
	//{
	//	FilePath = GetFilePath();
	//}

	public string FilePath { get; }
	public string[] BlockNames { get; set; }
	public string FileName { get; set; }
	public string DirPath { get; set; }

	//public static string GetFilePath()
	//{
	//	//return Path.Combine((string)Application.GetSystemVariable("DWGNAME"), (string)Application.GetSystemVariable("DWGPREFIX"));
	//	var fileName = (string)Application.GetSystemVariable("DWGNAME");
	//	var dirPath = (string)Application.GetSystemVariable("DWGPREFIX");

	//	return Path.Combine(dirPath, fileName);
	//}

	public static string[] GetBlockNames()
	{
		var confFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,
			"conf.json");

#if DEBUG
        var path = System.IO.Path.GetDirectoryName(
            System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
        path = path?.Substring(6);
        confFilePath = Path.Combine(path,"conf.json");
#endif
		var jsonFile = File.ReadAllText(confFilePath);
		var deserializeObject = JsonConvert.DeserializeObject<Params>(jsonFile);
		return deserializeObject.StampName;
	}
}