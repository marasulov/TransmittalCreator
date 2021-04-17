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
        public string DocNumber { get; set; }

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
        public double Width { get; set; }
        public double Height { get; set; }
        public double MinPointX { get; private set; }
        public double MinPointY { get; private set; }

        public PrintModel(string docNumber, ObjectId objectId)
        {
            GetBlockLengths(objectId);
            this.DocNumber = docNumber;
        }

        public PrintModel(ObjectId objectId)
        {

        }

        private void GetBlockLengths(ObjectId objectId)
        {
            double blockWidth = 0, blockHeight = 0;
            
            using (Transaction Tx = Active.Database.TransactionManager.StartTransaction())
            {
                BlockReference bref = Tx.GetObject(objectId, OpenMode.ForWrite) as BlockReference;
                this.ScaleX = bref.ScaleFactors.X;
                Point3d blockPos3d = Autodesk.AutoCAD.Internal.Utils.UcsToDisplay(new Point3d(bref.Position.X, bref.Position.Y,0), false);
                MinPointX = blockPos3d.X;
                MinPointY = blockPos3d.Y;
                BlockPosition = new Point2d(MinPointX, MinPointY);
                DynamicBlockReferencePropertyCollection props = bref.DynamicBlockReferencePropertyCollection;

                foreach (DynamicBlockReferenceProperty prop in props)
                {
                    if (prop.PropertyName == "Ширина")
                    {
                        blockWidth = double.Parse(prop.Value.ToString(),
                            System.Globalization.CultureInfo.InvariantCulture);
                    }
                    if (prop.PropertyName == "Высота")
                    {
                        blockHeight = double.Parse(prop.Value.ToString(),
                            System.Globalization.CultureInfo.InvariantCulture);
                    }
                    if (prop.PropertyName == "Штамп")
                    {
                        StampViewName = prop.Value.ToString();
                    }
                }
                BlockDimensions = new Point2d(BlockPosition.X + blockWidth, BlockPosition.Y + blockHeight);
                Tx.Commit();
            }
        }

        public bool IsFormatHorizontal()
        {
            Width = BlockDimensions.X - BlockPosition.X;
            Height = BlockDimensions.Y - BlockPosition.Y;

            if (Width > Height) return true;

            return false;
        }

        public string GetCanonNameByWidthAndHeight()
        {
            StandartCopier standartCopier = new StandartCopier();
            PlotConfig pConfig = PlotConfigManager.SetCurrentConfig(standartCopier.Pc3Location);

            //паттерн для размера листов
            string pat = @"\d{1,}?\.\d{2}";

            string canonName = "";

            foreach (var line in pConfig.CanonicalMediaNames)
            {
                Regex pattern = new Regex(pat, RegexOptions.Compiled |
                                               RegexOptions.Singleline);
                if (pattern.IsMatch(line))
                {
                    var items = DivideStringToWidthAndHeight(pattern, line);
                    double curWidth;
                    double curHeight;

                    if (IsFormatHorizontal())
                    {
                        curWidth = Math.Round(Width / ScaleX);
                        curHeight = Math.Round(Height / ScaleX);
                    }
                    else
                    {
                        curWidth = Math.Round(Height / ScaleX);
                        curHeight = Math.Round(Width / ScaleX);
                    }
                    
                    if (items.Item1 == curWidth & items.Item2 == curHeight)
                    {
                        canonName = line;
                        break;
                    }
                }
            }
            return canonName;
        }

        private (double, double) DivideStringToWidthAndHeight(Regex pattern, string line)
        {
            MatchCollection str2 = pattern.Matches(line, 0);

            string strWidth = str2[0].ToString();
            string strHeight = str2[1].ToString();
            double strWidthD = Convert.ToDouble(strWidth, System.Globalization.CultureInfo.InvariantCulture);
            double strheightD = Convert.ToDouble(strHeight, System.Globalization.CultureInfo.InvariantCulture);

            return (strWidthD, strheightD);
        }
    }
}
