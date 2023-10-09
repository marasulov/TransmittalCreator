using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace TransmittalCreator.Services.Files
{
	public class FileManager : IFileManager
	{
		public string FileName { get; set; }
		public string DirPath { get; set; }
		public string FilePath { get;}
		public string[] BlockNames { get; set; }
		

		public FileManager()
		{
			FilePath = GetFilePath();

			
		}

		public static string GetFilePath()
		{
			//return Path.Combine((string)Application.GetSystemVariable("DWGNAME"), (string)Application.GetSystemVariable("DWGPREFIX"));
			var fileName = (string) Application.GetSystemVariable("DWGNAME");
			var dirPath = (string) Application.GetSystemVariable("DWGPREFIX");

			return Path.Combine(dirPath, fileName);
		}

		public static string[] GetBlockNames()
		{
			var confFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "conf.json");

#if DEBUG
			confFilePath = "D:\\data\\source\\repos\\TransmittalCreator\\TransmittalCreator\\conf.json";
#endif
			string jsonFile = File.ReadAllText(confFilePath);
			Params deserializeObject = JsonConvert.DeserializeObject<Params>(jsonFile);
			return deserializeObject.StampName;
		}
	}
}