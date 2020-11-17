using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace TransmittalCreator.Models
{
    public class PrintModel
    {
        private string _docNumber;

        /// <summary>
        /// Номер листа
        /// </summary>
        public string DocNumber { get; }

        /// <summary>
        /// Pdf Document Name
        /// </summary>
        public string FormatValue { get; }

        /// <summary>
        /// Position of block
        /// </summary>
        public Point3d BlockPosition { get;}


        public PrintModel(string _docNumber, string _formatValue, Point3d _point3d)
        {
            this.DocNumber = _docNumber;
            this.FormatValue = _formatValue;
            this.BlockPosition = _point3d;
        }
    }
}
