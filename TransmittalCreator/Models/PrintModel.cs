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
        public Point2d BlockPosition { get; set; }

        public Point2d BlockDimensions { get; set; }

        private double ScaleX { get; set; }

        public double width;
        public double height;



        public PrintModel(string _docNumber, ObjectId objectId)
        {
            this.DocNumber = _docNumber;
            var posPoints = GetBlockLengths(objectId);
            this.BlockPosition = posPoints.Item1;
            this.BlockDimensions = posPoints.Item2;
        }

        private (Point2d, Point2d) GetBlockLengths(ObjectId objectId)
        {
            double blockWidth = 0, blockHeidht = 0;
            Point2d blockPosition;
            Point2d dimPoint2d;
            using (Transaction Tx = Active.Database.TransactionManager.StartTransaction())
            {
                BlockReference bref = Tx.GetObject(objectId, OpenMode.ForWrite) as BlockReference;
                this.ScaleX = bref.ScaleFactors.X;
                Point3d blockPos3d = Autodesk.AutoCAD.Internal.Utils.UcsToDisplay(new Point3d(bref.Position.X, bref.Position.Y,0), false);
                blockPosition = new Point2d(blockPos3d.X, blockPos3d.Y);
                DynamicBlockReferencePropertyCollection props = bref.DynamicBlockReferencePropertyCollection;

                foreach (DynamicBlockReferenceProperty prop in props)
                {
                    object[] values = prop.GetAllowedValues();

                    if (prop.PropertyName == "Ширина")
                    {
                        blockWidth = double.Parse(prop.Value.ToString(),
                            System.Globalization.CultureInfo.InvariantCulture);
                        Active.Editor.WriteMessage(blockWidth.ToString());
                    }
                    if (prop.PropertyName == "Высота")
                    {
                        blockHeidht = double.Parse(prop.Value.ToString(),
                            System.Globalization.CultureInfo.InvariantCulture);
                        Active.Editor.WriteMessage("\n{0}", blockHeidht.ToString());
                    }
                }
                dimPoint2d = new Point2d(blockPosition.X + blockWidth, blockPosition.Y + blockHeidht);
                Tx.Commit();
            }

            return (blockPosition, dimPoint2d);
        }

        public bool IsFormatHorizontal()
        {
            double minPointX = this.BlockPosition.X;
            double minPointY = this.BlockPosition.Y;

            double maxPointX = this.BlockDimensions.X;
            double maxPointY = this.BlockDimensions.Y;

            this.width = maxPointX - minPointX;
            this.height = maxPointY - minPointY;

            if (width > height) return true;

            return false;
        }

        public string GetCanonNameByExtents()
        {
            StandartCopier standartCopier = new StandartCopier();
            PlotConfig pConfig = PlotConfigManager.SetCurrentConfig(standartCopier.Pc3Location);
                
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

                    Console.WriteLine(strWidthD);

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
