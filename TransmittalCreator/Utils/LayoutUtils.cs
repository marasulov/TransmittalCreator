using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace TransmittalCreator.Utils;

public class LayoutUtils
{
	public static void DeleteEmptyLayouts(Database db, Editor ed, List<string> notEmptyList)
	{
		var layoutManager = LayoutManager.Current;
		var layoutNames = GetTabOrderedLayoutNames(db);
		if (layoutNames.Count == notEmptyList.Count) return;
		foreach (var layoutName in layoutNames)
			if ((layoutName != "Model") & !notEmptyList.Contains(layoutName))
				layoutManager.DeleteLayout(layoutName);
		ed.Regen();
	}

	public static List<string> GetTabOrderedLayoutNames(Database db)
	{
		if (db == null)
			return null;

		var tabOrderedLayouts = new Dictionary<int, string>();
		using (var tr = db.TransactionManager.StartTransaction())
		{
			var layoutDict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
			foreach (var layoutEntry in layoutDict)
			{
				var layout = (Layout)tr.GetObject(layoutEntry.Value, OpenMode.ForRead);
				if (!layout.ModelType)
					tabOrderedLayouts[layout.TabOrder] = layout.LayoutName;
			}

			tr.Commit();
		}

		return tabOrderedLayouts.OrderBy(n => n.Key).Select(n => n.Value).ToList();
	}
}