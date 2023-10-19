using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using TransmittalCreator.DBCad;
using TransmittalCreator.Models.Layouts;
using TransmittalCreator.Services.Blocks;
using TransmittalCreator.Services.Files;

namespace TransmittalCreator.CadCommands;

public static class CreateVedFromLayoutPages
{
	//[CommandMethod("CreateVedFromLayout")]
	public static void CreateVedFromLayout()
	{
		var allPath = FileManager.GetFilePath();
#if DEBUG
		allPath = "D:\\data\\source\\repos\\TransmittalCreator\\TransmittalCreator\\conf.json";
#endif
		Active.Database.SaveAs(allPath, true, DwgVersion.Current, Active.Database.SecurityParameters);
		var blockNames = FileManager.GetBlockNames();
		var dynamicBlocks = new DynamicBlockFinder(blockNames);
		var layoutModelCollection = new LayoutModelCollection(dynamicBlocks);

		layoutModelCollection.ListLayouts("Model");
	}
}