using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using TransmittalCreator.DBCad;
using TransmittalCreator.Services;

namespace TransmittalCreator.Models;

public class PrintModel
{
	public PrintModel(string docNumber, ObjectId objectId)
	{
		GetBlockLengths(objectId);
		DocNumber = docNumber;
	}

	public PrintModel(ObjectId objectId)
	{
	}

	private double _scaleX { get; set; }

	/// <summary>
	///     Номер листа
	/// </summary>
	public string DocNumber { get; set; }

	/// <summary>
	///     Pdf Document Name
	/// </summary>
	public string StampViewName { get; set; }

	/// <summary>
	///     Position of block
	/// </summary>
	public Point2d BlockPosition { get; set; }

	/// <summary>
	///     размеры блока
	/// </summary>
	public Point2d BlockDimensions { get; set; }

	public double Width { get; set; }
	public double Height { get; set; }
	public double MinPointX { get; private set; }
	public double MinPointY { get; private set; }

	public double GetBlockScale()
	{
		return _scaleX;
	}

	private void GetBlockLengths(ObjectId objectId)
	{
		double blockWidth = 0, blockHeight = 0;

		using (var tr = Active.Database.TransactionManager.StartTransaction())
		{
			var bref = tr.GetObject(objectId, OpenMode.ForWrite) as BlockReference;
			_scaleX = bref.ScaleFactors.X;
			var blockPos3d =
				Autodesk.AutoCAD.Internal.Utils.UcsToDisplay(new Point3d(bref.Position.X, bref.Position.Y, 0), false);
			MinPointX = blockPos3d.X;
			MinPointY = blockPos3d.Y;
			BlockPosition = new Point2d(MinPointX, MinPointY);
			var props = bref.DynamicBlockReferencePropertyCollection;

			foreach (DynamicBlockReferenceProperty prop in props)
				switch (prop.PropertyName)
				{
					case "Ширина":
						blockWidth = double.Parse(prop.Value.ToString(),
							CultureInfo.InvariantCulture);
						break;
					case "Высота":
						blockHeight = double.Parse(prop.Value.ToString(),
							CultureInfo.InvariantCulture);
						break;
					case "Штамп":
						StampViewName = prop.Value.ToString();
						break;
				}

			BlockDimensions = new Point2d(BlockPosition.X + blockWidth, BlockPosition.Y + blockHeight);
			tr.Commit();
		}
	}

	public void GetBlockMinPositions(BlockReference bref)
	{
		var blockPos3d =
			Autodesk.AutoCAD.Internal.Utils.UcsToDisplay(new Point3d(bref.Position.X, bref.Position.Y, 0), false);
		MinPointX = blockPos3d.X;
		MinPointY = blockPos3d.Y;
	}

	public bool IsFormatHorizontal()
	{
		Width = BlockDimensions.X - BlockPosition.X;
		Height = BlockDimensions.Y - BlockPosition.Y;

		return Width > Height;
	}

	public string GetCanonNameByWidthAndHeight()
	{
		var standartCopier = new StandartCopier();
		var pConfig = PlotConfigManager.SetCurrentConfig(standartCopier.Pc3Source);

		//паттерн для размера листов
		var pat = @"\d{1,}?\.\d{2}";

		var canonName = "";

		foreach (var line in pConfig.CanonicalMediaNames)
		{
			var pattern = new Regex(pat, RegexOptions.Compiled |
			                             RegexOptions.Singleline);
			if (!pattern.IsMatch(line)) continue;
			var items = DivideStringToWidthAndHeight(pattern, line);
			double curWidth;
			double curHeight;

			if (IsFormatHorizontal())
			{
				curWidth = Math.Round(Width / _scaleX);
				curHeight = Math.Round(Height / _scaleX);
			}
			else
			{
				curWidth = Math.Round(Height / _scaleX);
				curHeight = Math.Round(Width / _scaleX);
			}

			if ((items.Item1 == curWidth) & (items.Item2 == curHeight))
			{
				canonName = line;
				break;
			}
		}

		return canonName;
	}

	private static (double, double) DivideStringToWidthAndHeight(Regex pattern, string line)
	{
		var str2 = pattern.Matches(line, 0);

		var strWidth = str2[0].ToString();
		var strHeight = str2[1].ToString();
		var strWidthD = Convert.ToDouble(strWidth, CultureInfo.InvariantCulture);
		var strheightD = Convert.ToDouble(strHeight, CultureInfo.InvariantCulture);

		return (strWidthD, strheightD);
	}
}