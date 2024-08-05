using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TransmittalCreator.Models;
using TransmittalCreator.Services.Blocks;
using TransmittalCreator.Services;
using TransmittalCreator.Utils;
using Gile.AutoCAD.Extension;
using System;
using Org.BouncyCastle.Asn1.X509;
using DocumentFormat.OpenXml.Presentation;
using System.Windows.Documents;

//[assembly: CommandClass(typeof(BlockExtCommand))]
namespace TransmittalCreator.CadCommands.ArmSpes;

public static class AssignArmCommand
{
    [CommandMethod("AssignArm")]
    public static void AssignArm()
    {


        Active.Document.SendStringToExecute("REGENALL ", true, false, true);

        using var tr = Active.Database.TransactionManager.StartTransaction();

        var allSelectedBlocks = BlockUtils.SelectBlocksWithFilter(true);

        if (allSelectedBlocks == null) return;
        var armBlocks = BlockUtils.GetDynBlocksByName(allSelectedBlocks, tr, "sn_Армирование");
        var nodeBlocks = BlockUtils.GetDynBlocksByName(allSelectedBlocks, tr, "sn_Узел");

        if (armBlocks.Count <= 0) return;
        var linkText = string.Empty;
        foreach (var block in armBlocks)
        {
            var blRef = (BlockReference)tr.GetObject((ObjectId)block, OpenMode.ForWrite);
            if (blRef != null)
            {
                linkText += blRef.Handle + ";";
            }
        }

        if (linkText.Length > 0)
        {
            var tempString = linkText.Remove(linkText.Length - 1);
            linkText = tempString;
        }

        if (nodeBlocks.Count <= 0) return;
        foreach (ObjectId nodeBlockId in nodeBlocks)
        {
            var blRef = (BlockReference)tr.GetObject(nodeBlockId, OpenMode.ForWrite);
            var attributes = blRef.AttributeCollection.GetObjects();
            Active.Editor.WriteMessage($"attrs {attributes.Count()}");
            //var attributes = BlockReferenceExtension.GetAttributesByTag(blRef);
            foreach (var attribute in attributes)
            {

                if (attribute.Tag == "HANDLE")
                {
                    attribute.OpenForWrite();
                    attribute.TextString = linkText;
                    Active.Editor.WriteMessage($"attribure {attribute} writen {linkText} ");
                }
            }
        }

        //foreach (var selectedBlockId in allSelectedBlocks)
        //{
        //    var blRef = (BlockReference)tr.GetObject(selectedBlockId, OpenMode.ForWrite);
        //    if (blRef.GetEffectiveName() == "sn_Узел")
        //    {
        //        var attributes = blRef.AttributeCollection.GetObjects();

        //        //var attributes = BlockReferenceExtension.GetAttributesByTag(blRef);
        //        //if (attributes == null) return;


        //        //foreach (var attribute in attributes)
        //        //{

        //        //    attribute.Value.OpenForWrite();
        //        //    blRef.SetAttributeValue("HANDLE", linkText);
        //        //}


        //        foreach (var attribute in attributes)
        //        {
        //            attribute.OpenForWrite();

        //            blRef.SetAttributeValue("HANDLE", linkText);


        //        }

        //    }

        //}
        tr.Commit();



    }
}