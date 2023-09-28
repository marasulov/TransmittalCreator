using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using TransmittalCreator.DBCad;

namespace TransmittalCreator.Utils
{
	public static class BlockUtils
	{
		public static void BlockAttributeEnumerator(List<BlockReference> blocks, string[] attributeNames, int startFrom = 0, bool allPages = false)
		{
			//string tag = "ATT_TAG";

			foreach (var block in blocks)
			{
				startFrom++;
				foreach (ObjectId attRefId in block.AttributeCollection)
				{
					AttributeReference attRef = attRefId.GetObject(OpenMode.ForWrite) as AttributeReference;
					Active.Editor.WriteMessage($"{attRef.Tag}\n");
					if (attRef == null) continue;

					foreach (var attr in attributeNames)
					{
						if (attr.Equals(attRef.Tag, StringComparison.InvariantCultureIgnoreCase)) 
						{
							attRef.TextString = startFrom.ToString();
							
						}

						if (!allPages) continue;
						if (attRef.Tag.Equals("Листов", StringComparison.InvariantCultureIgnoreCase))
							attRef.TextString = blocks.Count.ToString();
					}
				}
			}
		}

		public static void GetBlockAttributes()
		{

		}
	}
}
