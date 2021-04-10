using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;

namespace TransmittalCreator.Models
{
    public class LayoutModel
    {
        public ObjectId LayoutId { get; set; }
        public string LayoutName { get; set; }
        public Layout Layout { get; set; }

        public LayoutModel(ObjectId layoutId, Layout layout)
        {
            LayoutId = layoutId;
            Layout = layout;
            LayoutName = layout.LayoutName;
        }
    }
}
