using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using TransmittalCreator.DBCad;
using TransmittalCreator.Models.Layouts;
using TransmittalCreator.Services.Blocks;
using TransmittalCreator.Services.Files;

namespace TransmittalCreator.CadCommands
{
	
	public static class CreateVedFromLayoutPages
	{
		[CommandMethod("CreateVedFromLayout")]
		public static void CreateVedFromLayout()
		{
			string allPath = FileManager.GetFilePath();
#if DEBUG
			allPath = "D:\\data\\source\\repos\\TransmittalCreator\\TransmittalCreator\\conf.json";
#endif
			Active.Database.SaveAs(allPath, true, DwgVersion.Current, Active.Database.SecurityParameters);
			string[] blockNames = FileManager.GetBlockNames();
			DynamicBlockFinder dynamicBlocks = new DynamicBlockFinder(blockNames);
			var layoutModelCollection = new LayoutModelCollection(dynamicBlocks);
					
			layoutModelCollection.ListLayouts("Model");
		}
	}
}
