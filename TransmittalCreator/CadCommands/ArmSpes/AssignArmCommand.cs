using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TransmittalCreator.DBCad;
using TransmittalCreator.Models;
using TransmittalCreator.Services.Blocks;
using TransmittalCreator.Services;
using TransmittalCreator.Utils;

//[assembly: CommandClass(typeof(BlockExtCommand))]
namespace TransmittalCreator.CadCommands.ArmSpes;

public static class AssignArmCommand
{
	[CommandMethod("AssignArm")]
	public static void AssignArm()
	{
        List<Sheet> dict = new List<Sheet>();
        List<PrintModel> printModels = new List<PrintModel>();
        Active.Document.SendStringToExecute("REGENALL ", true, false, true);

        using var tr = Active.Database.TransactionManager.StartTransaction();

        var allSelectedBlocks = BlockUtils.SelectBlocksWithFilter(true);

        if (allSelectedBlocks == null) return;
        var blocksByName = BlockUtils.GetDynBlocksByName(allSelectedBlocks, tr, new []{ "sn_Армирование" });

        if (blocksByName.Count <= 0) return;
    }
}