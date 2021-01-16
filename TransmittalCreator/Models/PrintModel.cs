using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using DV2177.Common;
using TransmittalCreator.Services;

namespace TransmittalCreator.Models
{
    public class PrintModel
    {

        /// <summary>
        /// Номер листа
        /// </summary>
        public string DocNumber { get; }

        /// <summary>
        /// Pdf Document Name
        /// </summary>
        public string StampViewName { get; set; }

        /// <summary>
        /// Position of block
        /// </summary>
        public Point2d BlockPosition { get; set; }

        public Point2d BlockDimensions { get; set; }

        private double ScaleX { get; set; }

        private double width;
        public double Width
        {
            get { return width; }
            set { width = value; }
        }

        private double height;
        public double Height
        {
            get { return height; }
            set { height = value; }
        }

        

        public PrintModel(string _docNumber, ObjectId objectId)
        {

            var posPoints = GetBlockLengths(objectId);
            this.BlockPosition = posPoints.Item1;
            this.BlockDimensions = posPoints.Item2;
            this.StampViewName = posPoints.Item3;

            //if (this.StampViewName == )
            this.DocNumber = _docNumber;

        }

        public dynamic GetBlockPropsValue(string propName)
        {
            
            return "no result"; // возвращаем строку
        }

        public (Point2d, Point2d, string) GetBlockLengths(ObjectId objectId)
        {
            double blockWidth = 0, blockHeidht = 0;
            Point2d blockPosition;
            Point2d dimPoint2d;
            string blockStamp="";
            using (Transaction Tx = Active.Database.TransactionManager.StartTransaction())
            {
                BlockReference bref = Tx.GetObject(objectId, OpenMode.ForWrite) as BlockReference;
                this.ScaleX = bref.ScaleFactors.X;
                Point3d blockPos3d = Autodesk.AutoCAD.Internal.Utils.UcsToDisplay(new Point3d(bref.Position.X, bref.Position.Y,0), false);
                blockPosition = new Point2d(blockPos3d.X, blockPos3d.Y);
                DynamicBlockReferencePropertyCollection props = bref.DynamicBlockReferencePropertyCollection;

                foreach (DynamicBlockReferenceProperty prop in props)
                {
                    //object[] values = prop.GetAllowedValues();

                    if (prop.PropertyName == "Ширина")
                    {
                        blockWidth = double.Parse(prop.Value.ToString(),
                            System.Globalization.CultureInfo.InvariantCulture);
                    }
                    if (prop.PropertyName == "Высота")
                    {
                        blockHeidht = double.Parse(prop.Value.ToString(),
                            System.Globalization.CultureInfo.InvariantCulture);
                    }
                    if (prop.PropertyName == "Штамп")
                    {
                        blockStamp = prop.Value.ToString();
                    }
                }
                dimPoint2d = new Point2d(blockPosition.X + blockWidth, blockPosition.Y + blockHeidht);
                Tx.Commit();
            }

            return (blockPosition, dimPoint2d, blockStamp);
        }

        public bool IsFormatHorizontal()
        {
            double minPointX = this.BlockPosition.X;
            double minPointY = this.BlockPosition.Y;

            double maxPointX = this.BlockDimensions.X;
            double maxPointY = this.BlockDimensions.Y;

            this.Width = maxPointX - minPointX;
            this.Height = maxPointY - minPointY;

            if (Width > Height) return true;

            return false;
        }

        public string GetCanonNameByWidthAndHeight()
        {
            StandartCopier standartCopier = new StandartCopier();
            PlotConfig pConfig = PlotConfigManager.SetCurrentConfig(standartCopier.Pc3Location);

            //паттерн для размера листов
            string pat = @"\d{1,}?\.\d{2}";

            //double width = this.Width;
            //double height = this.Height;

            string canonName = "";

            foreach (var line in pConfig.CanonicalMediaNames)
            {
                Regex pattern = new Regex(pat, RegexOptions.Compiled |
                                               RegexOptions.Singleline);
                //string str2 = Regex.Split(str, pattern);
                if (pattern.IsMatch(line))
                {
                    MatchCollection str2 = pattern.Matches(line, 0);

                    string strWidth = str2[0].ToString();
                    string strheight = str2[1].ToString();
                    double strWidthD = Convert.ToDouble(strWidth, System.Globalization.CultureInfo.InvariantCulture);
                    double strheightD = Convert.ToDouble(strheight, System.Globalization.CultureInfo.InvariantCulture);

                    //double strheight = Convert.ToDouble(str2[1]);

                    double curWidth = this.width / this.ScaleX;
                    double curHeight = this.height / this.ScaleX;
                    if (strWidthD == curWidth & strheightD == curHeight)
                    {
                        //Console.WriteLine("{0} ширина {1}-{2}  высота {3}-{4}", line, strWidthD, this, strheightD,
                        //    this.height);
                        canonName = line;
                        break;
                    }
                }
            }
            return canonName;
        }
    }
}
