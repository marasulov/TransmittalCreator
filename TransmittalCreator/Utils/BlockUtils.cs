using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Gile.AutoCAD.Extension;
using TransmittalCreator.Services.Files;

namespace TransmittalCreator.Utils;

public static class BlockUtils
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="blocks"></param>
    /// <param name="attributeNames"></param>
    /// <param name="startFrom"></param>
    /// <param name="allPages"></param>
    /// <param name="prefix"></param>
    /// <param name="suffix"></param>
    public static void BlockAttributeEnumerator(List<BlockReference> blocks, string[] attributeNames, int startFrom = 1,
        int allPages = 0, string prefix = "", string suffix = "")
    {
        foreach (var attributeReferences in blocks.Select(block => block.AttributeCollection.GetObjects()))
        {
            foreach (var attribute in attributeReferences)
            {
                foreach (var attributeName in attributeNames)
                {
                    attribute.OpenForWrite();
                    if (attributeName.Equals(attribute.Tag, StringComparison.InvariantCultureIgnoreCase))
                        attribute.TextString = prefix + startFrom + suffix;
                }

                if (allPages == 0) continue;
                string[] allPagesAttributes = { "PAGES", "ЛИСТОВ" };
                foreach (var allPage in allPagesAttributes)
                    if (attribute.Tag.Equals(allPage, StringComparison.InvariantCultureIgnoreCase))
                        attribute.TextString = allPages.ToString();
            }

            startFrom++;
        }
    }


    /// <summary>
    /// Возвращает блоки по имени из выбранного списка блоков
    /// </summary>
    /// <param name="allSelectedBlocks">все блоки</param>
    /// <param name="tr">Транзакция</param>
    /// <param name="blockNames">массив из имени блоков</param>
    /// <returns></returns>
    public static ObjectIdCollection GetDynBlocksByName(ObjectId[] allSelectedBlocks, Transaction tr, string[] blockNames = default)
    {
        var blocksByName = new ObjectIdCollection();
        foreach (var objectId in allSelectedBlocks)
        {
            var blRef = (BlockReference)tr.GetObject(objectId, OpenMode.ForRead);
            var block = tr.GetObject(blRef.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
            if (block is null) continue;
            if (blockNames == default)
                blockNames = FileManager.GetBlockNames();

            if (blockNames.Contains(block.Name)) blocksByName.Add(objectId);
        }

        return blocksByName;
    }

    public static ObjectId[] SelectBlocksWithFilter(bool onlySelectedPages
        = false)
    {
        var sFilter = new SelectionFilter(
            new TypedValue[2]
            {
                new(0, "INSERT"),
                new(66, 1)
            }
        );
        var selResult = Active.Editor.SelectAll(sFilter);

        if (onlySelectedPages)
        {
            var psOptions = new PromptSelectionOptions();
            psOptions.MessageForAdding = "\nSelect stamp block : ";
            psOptions.MessageForRemoval = "\nRemove from selection : ";

            selResult = Active.Editor.GetSelection(psOptions, sFilter);
        }


        if (selResult.Status != PromptStatus.OK) return null;

        var selSet = selResult.Value;
        return selSet.GetObjectIds();
    }

    //public static ObjectId[] SelectAllBlocksWithFilter()
    //{
    //	SelectionFilter sFilter = new SelectionFilter(
    //		new TypedValue[2]
    //		{
    //			new TypedValue(0, "INSERT"),
    //			new TypedValue(66,1)
    //		}
    //	);


    //	if (selResult.Status != PromptStatus.OK)
    //	{
    //		return null;
    //	}

    //	SelectionSet selSet = selResult.Value;
    //	return selSet.GetObjectIds();
    //}
}