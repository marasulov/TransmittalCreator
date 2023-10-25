using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using OfficeOpenXml;
using TransmittalCreator.DBCad;
using TransmittalCreator.Models;
using TransmittalCreator.Utils;
using OpenFileDialog = Autodesk.AutoCAD.Windows.OpenFileDialog;

namespace TransmittalCreator;

public class Hvac : EntityJig
{
	public Point3d endPnt;

	internal Hvac(Table table) : base(table)
	{
	}

	public static bool CreateHvacTable() // This method can have any name
	{
		try
		{
			var documents = Path.GetDirectoryName(Active.Document.Name);
			Environment.SetEnvironmentVariable("MYDOCUMENTS", documents);

			var ofd = new OpenFileDialog("Select a file using an OpenFileDialog", documents,
				"xlsx; *",
				"File Date Test T22",
				OpenFileDialog.OpenFileDialogFlags.DefaultIsFolder |
				OpenFileDialog.OpenFileDialogFlags.ForceDefaultFolder // .AllowMultiple
			);
			var sdResult = ofd.ShowDialog();

			if (sdResult != DialogResult.OK) return false;

			var filename = ofd.Filename;

			var hvacTables = CreateHvacTableListFromFile(filename);

			var type = typeof(HvacTable);

			CreateLayer();

			var filter = new MyMessageFilter();

			Application.AddMessageFilter(filter);

			foreach (var hvacTable in hvacTables)
			{
				// Check for user input events
				Application.DoEvents();
				if (filter.bCanceled)
				{
					Active.WriteMessage("\nLoop cancelled.");
					break;
				}

				PrintUtils.AddTable(hvacTable);
			}

			Application.RemoveMessageFilter(filter);
			return true;
		}
		catch (Exception e)
		{
			return false;
		}
	}

	public static List<HvacTable> CreateHvacTableListFromFile(string filename)
	{
		var fileInfo = new FileInfo(filename);
		var listData = new List<HvacTable>();

		using (var package = new ExcelPackage(fileInfo))
		{
			ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
			//create an instance of the the first sheet in the loaded file
			var worksheet = package.Workbook.Worksheets[0];
			var rowStart = 7;
			var rowCount = worksheet.Dimension.End.Row;

			for (var i = rowStart; i < rowCount - 1; i++)
			{
				var roomcell = worksheet.Cells[i, 1].Value;

				if (roomcell != null)
				{
					var totalStr = roomcell.ToString().ToUpper().Contains("TOTAL");

					if (!totalStr)
					{
						var roomNumber = worksheet.Cells[i, 1].Value?.ToString().Trim();
						var roomName = worksheet.Cells[i, 2].Value?.ToString().Trim();
						var roomTemp = worksheet.Cells[i, 5].Value?.ToString().Trim();
						var heating = worksheet.Cells[i, 26].Value?.ToString().Trim();
						var cooling = worksheet.Cells[i, 34].Value?.ToString().Trim();
						var supply = worksheet.Cells[i, 39].Value?.ToString().Trim();

						var supplyIn = "П";
						var supplyInd = worksheet.Cells[i, 38].Value?.ToString().Trim() ?? supplyIn;
						var exhaustInd = "В";
						if (worksheet.Cells[i, 40].Value != null)
							exhaustInd = worksheet.Cells[i, 40].Value.ToString().Trim();

						var exhaust = worksheet.Cells[i, 41].Value?.ToString().Trim();

						listData.Add(new HvacTable(roomNumber, roomName, roomTemp, heating, cooling, supply,
							supplyInd,
							exhaust, exhaustInd));
					}
				}
			}
		}

		return listData;
	}

	public static void CreateLayer()
	{
		using (var tr = Active.Database.TransactionManager.StartTransaction())
		{
			var ltb = (LayerTable)tr.GetObject(Active.Database.LayerTableId,
				OpenMode.ForRead);

			//create a new layout.

			if (!ltb.Has("Hvac_Calc"))

			{
				ltb.UpgradeOpen();

				var newLayer = new LayerTableRecord();

				newLayer.Name = "Hvac_Calc";
				newLayer.LineWeight = LineWeight.LineWeight005;
				newLayer.Description = "This is new layer";
				newLayer.IsPlottable = false;

				//red color
				//newLayer.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(255, 0, 0);
				ltb.Add(newLayer);
				tr.AddNewlyCreatedDBObject(newLayer, true);
			}

			tr.Commit();
			//make it as current
			Active.Database.Clayer = ltb["Hvac_Calc"];
		}
	}

	protected override SamplerStatus Sampler(JigPrompts prompts)
	{
		var doc = Active.Document;

		var prOptions1 = new JigPromptPointOptions("\nNext point:");
		prOptions1.BasePoint = (Entity as Line).StartPoint;
		prOptions1.UseBasePoint = true;
		prOptions1.UserInputControls = UserInputControls.Accept3dCoordinates
		                               | UserInputControls.AnyBlankTerminatesInput
		                               | UserInputControls.GovernedByOrthoMode
		                               | UserInputControls.GovernedByUCSDetect
		                               | UserInputControls.UseBasePointElevation
		                               | UserInputControls.InitialBlankTerminatesInput
		                               | UserInputControls.NullResponseAccepted;
		var prResult1 = prompts.AcquirePoint(prOptions1);
		if (prResult1.Status == PromptStatus.Cancel)
			return SamplerStatus.Cancel;

		if (prResult1.Value.Equals(endPnt)) return SamplerStatus.NoChange;

		endPnt = prResult1.Value;
		return SamplerStatus.OK;
	}

	protected override bool Update()
	{
		var doc = Active.Document;

		(Entity as Table).Position = endPnt;
		return true;
	}

	public class MyMessageFilter : IMessageFilter
	{
		public const int WM_KEYDOWN = 0x0100;
		public bool bCanceled;

		public bool PreFilterMessage(ref Message m)
		{
			if (m.Msg == WM_KEYDOWN)
			{
				// Check for the Escape keypress
				var kc = (Keys)(int)m.WParam & Keys.KeyCode;

				if (m.Msg == WM_KEYDOWN && kc == Keys.Escape) bCanceled = true;

				// Return true to filter all keypresses
				return true;
			}

			// Return false to let other messages through
			return false;
		}
	}
}