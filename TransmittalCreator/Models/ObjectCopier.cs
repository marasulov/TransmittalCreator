using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;

namespace TransmittalCreator.Models
{
    public class ObjectCopier
    {
        private Point3d _minPoint3d;
        public Point3d MinPoint3d
        {
            get => _minPoint3d;
            set => _minPoint3d = value;
        }
        private Point3d _maxPoint3d;
        public Point3d MaxPoint3d
        {
            get => _maxPoint3d;
            set => _maxPoint3d = value;
        }

        public ObjectCopier()
        {


        }
    }
}
