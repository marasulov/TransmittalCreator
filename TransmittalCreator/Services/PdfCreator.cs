using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using TransmittalCreator.DBCad;

namespace TransmittalCreator.Services
{
    class PdfCreator
    {
        //private Point3d _blockPoint3d;
        public Point2d BlockPoint3d { get; }
        public string PdfFileName { get; }
        public string FormatValue { get; }
        public double Width { get; set; }
        public double Height { get; set; }

        public PdfCreator(Point2d blockPoint3d, string pdfFileName, string formatValue)
        {
            this.BlockPoint3d = blockPoint3d;
            this.PdfFileName = pdfFileName;
            this.FormatValue = formatValue;
        }

        public PdfCreator(Point2d blockPoint3d)
        {
            this.BlockPoint3d = blockPoint3d;
        }

        private static string GetLocalNameByAtrrValue(string attrvalue = "А3")
        {
            StandartCopier standartCopier = new StandartCopier();
            PlotConfig pConfig = PlotConfigManager.SetCurrentConfig(standartCopier.Pc3Location);
            string canonName = "";
            foreach (var canonicalMediaName in pConfig.CanonicalMediaNames)
            {
                string localName = pConfig.GetLocalMediaName(canonicalMediaName);
                if (localName == attrvalue)
                {
                    canonName = canonicalMediaName;

                }

                Active.Editor.WriteMessage("\n" + canonicalMediaName);
            }

            return canonName;
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

                    if (strWidthD == this.Width & strheightD == this.Height)
                    {
                        Console.WriteLine("{0} ширина {1}-{2}  высота {3}-{4}", line, strWidthD, this, strheightD,
                            this.Height);
                        canonName = line;
                        break;
                    }
                }
            }
            return canonName;
        }

        public void GetBlockDimensions(ObjectId objectId)
        {
            double blockWidth = 0, blockHeidht = 0;
            using (Transaction Tx = Active.Database.TransactionManager.StartTransaction())
            {
                BlockReference bref = Tx.GetObject(objectId, OpenMode.ForWrite) as BlockReference;

                
                    DynamicBlockReferencePropertyCollection props =
                        bref.DynamicBlockReferencePropertyCollection;

                    foreach (DynamicBlockReferenceProperty prop in props)
                    {
                        object[] values = prop.GetAllowedValues();

                        if (prop.PropertyName == "Ширина")
                        {
                            this.Width = double.Parse(prop.Value.ToString(), System.Globalization.CultureInfo.InvariantCulture);
                            Active.Editor.WriteMessage(blockWidth.ToString());
                        }
                        if (prop.PropertyName == "Высота")
                        {
                            this.Height = double.Parse(prop.Value.ToString(), System.Globalization.CultureInfo.InvariantCulture);
                            Active.Editor.WriteMessage("\n{0}", blockHeidht.ToString());
                        }
                    }
                
                Tx.Commit();
            }
        }
    }

}
