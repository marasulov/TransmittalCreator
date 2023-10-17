using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using TransmittalCreator.DBCad;

namespace TransmittalCreator.Services;

internal class PdfCreator
{
	public PdfCreator(Point2d blockPoint3d, string pdfFileName, string formatValue)
	{
		BlockPoint3d = blockPoint3d;
		PdfFileName = pdfFileName;
		FormatValue = formatValue;
	}

	public PdfCreator(Point2d blockPoint3d)
	{
		BlockPoint3d = blockPoint3d;
	}

	//private Point3d _blockPoint3d;
	public Point2d BlockPoint3d { get; }
	public string PdfFileName { get; }
	public string FormatValue { get; }
	public double Width { get; set; }
	public double Height { get; set; }

	private static string GetLocalNameByAtrrValue(string attrvalue = "А3")
	{
		var standartCopier = new StandartCopier();
		var pConfig = PlotConfigManager.SetCurrentConfig(standartCopier.Pc3Source);
		var canonName = "";
		foreach (var canonicalMediaName in pConfig.CanonicalMediaNames)
		{
			var localName = pConfig.GetLocalMediaName(canonicalMediaName);
			if (localName == attrvalue) canonName = canonicalMediaName;

			Active.Editor.WriteMessage("\n" + canonicalMediaName);
		}

		return canonName;
	}

	public string GetCanonNameByExtents()
	{
		var standartCopier = new StandartCopier();
		var pConfig = PlotConfigManager.SetCurrentConfig(standartCopier.Pc3Source);

		var pat = @"\d{1,}?\.\d{2}";

		//double width = this.Width;
		//double height = this.Height;

		var canonName = "";

		foreach (var line in pConfig.CanonicalMediaNames)
		{
			var pattern = new Regex(pat, RegexOptions.Compiled |
			                             RegexOptions.Singleline);
			//string str2 = Regex.Split(str, pattern);
			if (pattern.IsMatch(line))
			{
				var str2 = pattern.Matches(line, 0);

				var strWidth = str2[0].ToString();
				var strheight = str2[1].ToString();
				var strWidthD = Convert.ToDouble(strWidth, CultureInfo.InvariantCulture);
				var strheightD = Convert.ToDouble(strheight, CultureInfo.InvariantCulture);

				Console.WriteLine(strWidthD);

				//double strheight = Convert.ToDouble(str2[1]);

				if ((strWidthD == Width) & (strheightD == Height))
				{
					Console.WriteLine("{0} ширина {1}-{2}  высота {3}-{4}", line, strWidthD, this, strheightD,
						Height);
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
		using (var Tx = Active.Database.TransactionManager.StartTransaction())
		{
			var bref = Tx.GetObject(objectId, OpenMode.ForWrite) as BlockReference;


			var props =
				bref.DynamicBlockReferencePropertyCollection;

			foreach (DynamicBlockReferenceProperty prop in props)
			{
				var values = prop.GetAllowedValues();

				if (prop.PropertyName == "Ширина")
				{
					Width = double.Parse(prop.Value.ToString(), CultureInfo.InvariantCulture);
					Active.Editor.WriteMessage(blockWidth.ToString());
				}

				if (prop.PropertyName == "Высота")
				{
					Height = double.Parse(prop.Value.ToString(), CultureInfo.InvariantCulture);
					Active.Editor.WriteMessage("\n{0}", blockHeidht.ToString());
				}
			}

			Tx.Commit();
		}
	}
}