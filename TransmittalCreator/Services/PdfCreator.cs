using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using DV2177.Common;

namespace TransmittalCreator.Services
{
    class PdfCreator
    {
        //private Point3d _blockPoint3d;
        public Extents3d BlockPoint3d { get; }
        public string PdfFileName { get; }
        public string FormatValue { get; }

        public PdfCreator(Extents3d blockPoint3d, string pdfFileName, string formatValue)
        {
            this.BlockPoint3d = blockPoint3d;
            this.PdfFileName = pdfFileName;
            this.FormatValue = formatValue;
        }

        public PdfCreator(Extents3d blockPoint3d)
        {
            this.BlockPoint3d = blockPoint3d;
        }

        public Extents2d Extents3dToExtents2d()
        {
            Extents3d point3d = this.BlockPoint3d;

            Point3d minPoint3dWcs = new Point3d(this.BlockPoint3d.MinPoint[0], point3d.MinPoint[1], point3d.MinPoint[2]);
            Point3d minPoint3d = Autodesk.AutoCAD.Internal.Utils.UcsToDisplay(minPoint3dWcs, false);
            Point3d maxPoint3dWcs = new Point3d(point3d.MaxPoint[0], point3d.MaxPoint[1], point3d.MaxPoint[2]);
            Point3d maxPoint3d = Autodesk.AutoCAD.Internal.Utils.UcsToDisplay(maxPoint3dWcs, false);
            Extents2d points = new Extents2d(new Point2d(minPoint3d[0], minPoint3d[1]), new Point2d(maxPoint3d[0], maxPoint3d[1]));

            return points;
        }

        public bool IsFormatHorizontal()
        {
            double minPointX = BlockPoint3d.MinPoint[0];
            double minPointY = BlockPoint3d.MinPoint[1];

            double maxPointX = BlockPoint3d.MaxPoint[0];
            double maxPointY = BlockPoint3d.MaxPoint[1];

            double lengthX = maxPointX - minPointX;
            double lengthY = maxPointY - minPointY;

            if (lengthY > lengthX) return false;

            return true;
        }

        public static string GetLocalNameByAtrrValue(string attrvalue = "А3")
        {
            PlotConfig pConfig = PlotConfigManager.SetCurrentConfig("C:\\Users\\yusufzhon.marasulov\\AppData\\Roaming\\Autodesk\\AutoCAD 2019\\R23.0\\enu\\Plotters\\DWG To PDF.pc3");
            string canonName = "";
            foreach (var canonicalMediaName in pConfig.CanonicalMediaNames)
            {
                string localName = pConfig.GetLocalMediaName(canonicalMediaName);
                if (localName == attrvalue)
                {
                    canonName = canonicalMediaName;
                    Active.Editor.WriteMessage("\n  " + canonicalMediaName + " | " + localName);
                }

            }

            //int cnt = 0;
            //string canonName = "";
            //foreach (string mediaName in acPlSetVdr.GetCanonicalMediaNameList(acPlSet))
            //{
            //    string localName = acPlSetVdr.GetLocaleMediaName(acPlSet, cnt);
            //    if (localName == attrvalue)
            //    {
            //        canonName = mediaName;
            //        Active.Editor.WriteMessage("\n распечатано на " + mediaName + " | " + localName);
            //    }
            //    // Выводим имена форматов (Locale и Canonical) принтера текущей настройки принтера
            //    Active.Editor.WriteMessage("\n  " + mediaName + " | " + localName);
            //    cnt = cnt + 1;
            //}

            return canonName;
        }
    }
}
