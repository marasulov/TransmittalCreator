using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Gile.AutoCAD.Extension;
using TransmittalCreator.DBCad;

namespace TransmittalCreator.Utils
{
	public static class BlockUtils
	{
		public static void BlockAttributeEnumerator(List<BlockReference> blocks, string[] attributeNames, int startFrom = 1, int allPages = 0)
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
							attribute.TextString = startFrom.ToString();
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
	}
}
