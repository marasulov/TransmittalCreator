using Autodesk.AutoCAD.DatabaseServices;
using Gile.AutoCAD.Extension;
using System;
using System.Collections.Generic;
using TransmittalCreator.Services.Files;
using System.Linq;

namespace TransmittalCreator.Utils
{
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
		public static void BlockAttributeEnumerator(List<BlockReference> blocks, string[] attributeNames, int startFrom = 1, int allPages = 0, string prefix = "", string suffix = "")
		{
			foreach (var block in blocks)
			{

				var attributeReferences = block.AttributeCollection.GetObjects();

				foreach (var attribute in attributeReferences)
				{
					foreach (var attributeName in attributeNames)
					{
						attribute.OpenForWrite();
						if (attributeName.Equals(attribute.Tag, StringComparison.InvariantCultureIgnoreCase))
						{
							attribute.TextString = prefix + startFrom + suffix;
						}
					}

					if (allPages == 0) continue;
					string[] allPagesAttributes = { "PAGES", "ЛИСТОВ" };
					foreach (var allPage in allPagesAttributes)
					{
						if (attribute.Tag.Equals(allPage, StringComparison.InvariantCultureIgnoreCase))
							attribute.TextString = allPages.ToString();
					}
				}

				//foreach (ObjectId attRefId in block.AttributeCollection)
				//{
				//	AttributeReference attRef = attRefId.GetObject(OpenMode.ForWrite) as AttributeReference;
				//	DBCad.Active.Editor.WriteMessage($"{attRef.Tag}\n");
				//	if (attRef == null) continue;

				//	foreach (var attr in attributeNames)
				//	{
				//		if (attr.Equals(attRef.Tag, StringComparison.InvariantCultureIgnoreCase))
				//		{
				//			attRef.TextString = startFrom.ToString();
				//		}
				//	}
				//	if (allPages == 0) continue;
				//	string[] allPagesAttributes = { "PAGES", "ЛИСТОВ" };
				//	foreach (var allPage in allPagesAttributes)
				//	{
				//		if (attRef.Tag.Equals(allPage, StringComparison.InvariantCultureIgnoreCase))
				//			attRef.TextString = allPages.ToString();
				//	}

				//}
				startFrom++;
			}
		}



		/// <summary>
		/// Возвращает блоки по имени из выбранного списка блоков 
		/// </summary>
		/// <param name="allSelectedBlocks"></param>
		/// <param name="tr"></param>
		/// <returns></returns>
		public static ObjectIdCollection GetDynBlocksByName(ObjectId[] allSelectedBlocks, Transaction tr)
		{
			ObjectIdCollection blocksByName = new ObjectIdCollection();
			foreach (var objectId in allSelectedBlocks)
			{
				var blRef = (BlockReference)tr.GetObject(objectId, OpenMode.ForRead);
				var block = tr.GetObject(blRef.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
				if (block is null) continue;

				string[] blockNames = FileManager.GetBlockNames();

				if (blockNames.Contains(block.Name)) blocksByName.Add(objectId);
			}
			return blocksByName;
		}
	}
}
