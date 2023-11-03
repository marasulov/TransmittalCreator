using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using TransmittalCreator.DBCad;
using Exception = System.Exception;

namespace TransmittalCreator.Services.Blocks;

public class BlockSelector
{
	public PromptSelectionResult SelectionResult { get; set; }

	public void GetFilterForSelectBlockId(string blockName = "")
	{
		TypedValue[] filList;
		if (string.IsNullOrWhiteSpace(blockName))
		{
			filList = new TypedValue[1];
			filList.SetValue(new TypedValue((int)DxfCode.Start, "INSERT"),
				0); // new TypedValue((int)DxfCode.Start, "INSERT"), new TypedValue((int)DxfCode.Operator, "<and"), new TypedValue((int)DxfCode.BlockName, blockName), new TypedValue((int)DxfCode.Operator, "and>")};
			var filter = new SelectionFilter(filList);
			var opts = new PromptSelectionOptions
			{
				MessageForAdding = "Select block references: "
			};
			SelectionResult = Active.Editor.GetSelection(opts, filter);
		}
		else
		{
			SelectionResult = Active.Editor.SelectDynBlock("Select block references: ", blockName);
		}
	}

	private void pso_KeywordInput(object sender, SelectionTextInputEventArgs e)
	{
		// Пользователь выбрал ключевое слово - сгенерируем исключение
		throw new Autodesk.AutoCAD.Runtime.Exception(
			ErrorStatus.OK, e.Input);
	}
}

public static class Extension
{
	private static string names;

	public static PromptSelectionResult SelectDynBlock(this Editor ed, string message, params string[] blockNames)
	{
		names = string.Join(",", blockNames);
		var filter = new SelectionFilter(
			new[]
			{
				new TypedValue(0, "INSERT"),
				new TypedValue(2, "`*U*," + names)
			});
		var options = new PromptSelectionOptions();
		if (!string.IsNullOrEmpty(message))
			options.MessageForAdding = message;
		ed.SelectionAdded += OnSelectionAdded;
		var result = ed.GetSelection(options, filter);
		ed.SelectionAdded -= OnSelectionAdded;
		return result;
	}

	private static void OnSelectionAdded(object sender, SelectionAddedEventArgs e)
	{
		var ids = e.AddedObjects.GetObjectIds();
		using var tr = ids[0].Database.TransactionManager.StartOpenCloseTransaction();
		for (var i = 0; i < ids.Length; i++)
		{
			var br = (BlockReference)tr.GetObject(ids[i], OpenMode.ForRead);
			var btr = (BlockTableRecord)tr.GetObject(br.DynamicBlockTableRecord, OpenMode.ForRead);
			if (!Autodesk.AutoCAD.Internal.Utils.WcMatchEx(btr.Name, names, true))
				e.Remove(i);
		}

		tr.Commit();
	}
}