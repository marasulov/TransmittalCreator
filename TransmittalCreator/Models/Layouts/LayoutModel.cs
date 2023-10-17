using Autodesk.AutoCAD.DatabaseServices;
using TransmittalCreator.Utils;

namespace TransmittalCreator.Models.Layouts;

public class LayoutModel
{
	public LayoutModel(ObjectId layoutObjectOwnerId, ObjectId layoutPlotId, Layout layout)
	{
		LayoutObjectOwnerId = layoutObjectOwnerId;
		LayoutPlotId = layoutPlotId;
		Layout = layout;
		LayoutName = layout.LayoutName;
	}

	public ObjectId LayoutObjectOwnerId { get; set; }
	public ObjectId LayoutPlotId { get; set; }
	public string LayoutName { get; set; }
	public Layout Layout { get; set; }
	public ObjectId BlocksObjectId { get; set; }
	public PrintModel PrintModel { get; set; }
	public string CanonicalName { get; set; }

	public void CreatePrintModel(Transaction tr)
	{
		var selAttrName = "НОМЕР_ЛИСТА";
		PrintModel = PrintUtils.GetPrintModelByBlockId(tr, selAttrName, BlocksObjectId);
	}
}