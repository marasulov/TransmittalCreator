using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.EditorInput;

namespace TransmittalCreator.Utils
{
	public class LayoutUtils
	{
		public static void DeleteEmptyLayouts(Database db, Editor ed, List<string> notEmptyList)
		{
			var layoutManager = LayoutManager.Current;
			var layoutNames = GetTabOrderedLayoutNames(db);
			if (layoutNames.Count == notEmptyList.Count) return;
			foreach (string layoutName in layoutNames)
			{
				if (layoutName != "Model" & !notEmptyList.Contains(layoutName))
					layoutManager.DeleteLayout(layoutName);
			}
			ed.Regen();
		}

		public static List<string> GetTabOrderedLayoutNames(Database db)
		{
			if (db == null)
				return null;

			var tabOrderedLayouts = new Dictionary<int, string>();
			using (Transaction tr = db.TransactionManager.StartTransaction())
			{
				DBDictionary layoutDict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
				foreach (DBDictionaryEntry layoutEntry in layoutDict)
				{
					Layout layout = (Layout)tr.GetObject(layoutEntry.Value, OpenMode.ForRead);
					if (!layout.ModelType)
						tabOrderedLayouts[layout.TabOrder] = layout.LayoutName;
				}
				tr.Commit();
			}
			return tabOrderedLayouts.OrderBy(n => n.Key).Select(n => n.Value).ToList();
		}
	}
}
