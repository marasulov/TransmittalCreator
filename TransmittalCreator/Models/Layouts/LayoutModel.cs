using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using TransmittalCreator.Services;

namespace TransmittalCreator.Models.Layouts
{
    public class LayoutModel
    {
        public ObjectId LayoutObjectOwnerId { get; set; }
        public ObjectId LayoutPlotId { get; set; }
        public string LayoutName { get; set; }
        public Layout Layout { get; set; }
        public ObjectId BlocksObjectId { get; set; }
        public PrintModel PrintModel { get; set; }
        public string CanonicalName { get; set; }
        

        public LayoutModel(ObjectId layoutObjectOwnerId, ObjectId layoutPlotId, Layout layout)
        {
            LayoutObjectOwnerId = layoutObjectOwnerId;
            LayoutPlotId = layoutPlotId;
            Layout = layout;
            LayoutName = layout.LayoutName;
        }

        public void CreatePrintModel(Transaction tr )
        {
            string selAttrName = "НОМЕР_ЛИСТА";
            PrintModel = Utils.GetPrintModelByBlockId(tr, selAttrName, BlocksObjectId);
        }
        
    }
}
