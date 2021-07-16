using Autodesk.AutoCAD.DatabaseServices;
using TransmittalCreator.DBCad;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using TransmittalCreator.Models;
using TransmittalCreator.Services;
using Application = System.Windows.Forms.Application;
using OpenFileDialog = Autodesk.AutoCAD.Windows.OpenFileDialog;

namespace TransmittalCreator
{
    public class Hvac : EntityJig
    {
        public Point3d endPnt = new Point3d();
        public static bool CreateHvacTable() // This method can have any name
        {
            try
            {
                string documents = Path.GetDirectoryName(Active.Document.Name);
                Environment.SetEnvironmentVariable("MYDOCUMENTS", documents);

                var ofd = new OpenFileDialog("Select a file using an OpenFileDialog", documents,
                    "xlsx; *",
                    "File Date Test T22",
                    OpenFileDialog.OpenFileDialogFlags.DefaultIsFolder |
                    OpenFileDialog.OpenFileDialogFlags.ForceDefaultFolder // .AllowMultiple
                );
                DialogResult sdResult = ofd.ShowDialog();

                if (sdResult != DialogResult.OK) return false;

                string filename = ofd.Filename;

                List<HvacTable> hvacTables = CreateHvacTableListFromFile(filename);

                Type type = typeof(HvacTable);

                int tableCols = type.GetProperties().Length;
                CreateLayer();

                MyMessageFilter filter = new MyMessageFilter();

                Application.AddMessageFilter(filter);

                foreach (var hvacTable in hvacTables)
                {
                    // Check for user input events
                    Application.DoEvents();
                    if (filter.bCanceled == true)
                    {
                        Active.Editor.WriteMessage("\nLoop cancelled.");
                        break;
                    }

                    Utils.AddTable(hvacTable);
                }

                Application.RemoveMessageFilter(filter);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public class MyMessageFilter : IMessageFilter
        {
            public const int WM_KEYDOWN = 0x0100;
            public bool bCanceled = false;

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg == WM_KEYDOWN)
                {
                    // Check for the Escape keypress
                    Keys kc = (Keys)(int)m.WParam & Keys.KeyCode;

                    if (m.Msg == WM_KEYDOWN && kc == Keys.Escape)
                    {
                        bCanceled = true;
                    }

                    // Return true to filter all keypresses
                    return true;
                }

                // Return false to let other messages through
                return false;
            }
        }

        public static List<HvacTable> CreateHvacTableListFromFile(string filename)
        {
            FileInfo fileInfo = new FileInfo(filename);
            List<HvacTable> listData = new List<HvacTable>();

            using (ExcelPackage package = new ExcelPackage(fileInfo))
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                //create an instance of the the first sheet in the loaded file
                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                int rowStart = 7;
                int rowCount = worksheet.Dimension.End.Row;

                for (int i = rowStart; i < rowCount - 1; i++)
                {
                    var roomcell = worksheet.Cells[i, 1].Value;

                    if (roomcell != null)
                    {
                        bool totalStr = roomcell.ToString().ToUpper().Contains("TOTAL");

                        if (!totalStr)
                        {
                            string roomNumber = worksheet.Cells[i, 1].Value?.ToString().Trim();
                            string roomName = worksheet.Cells[i, 2].Value?.ToString().Trim();
                            string roomTemp = worksheet.Cells[i, 5].Value?.ToString().Trim();
                            string heating = worksheet.Cells[i, 26].Value?.ToString().Trim();
                            string cooling = worksheet.Cells[i, 34].Value?.ToString().Trim();
                            string supply = worksheet.Cells[i, 39].Value?.ToString().Trim();

                            string supplyIn = "П";
                            var supplyInd = worksheet.Cells[i, 38].Value?.ToString().Trim() ?? supplyIn;
                            string exhaustInd = "В";
                            if (worksheet.Cells[i, 40].Value != null)
                                exhaustInd = worksheet.Cells[i, 40].Value.ToString().Trim();

                            string exhaust = worksheet.Cells[i, 41].Value?.ToString().Trim();

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
            using (Transaction tr = Active.Database.TransactionManager.StartTransaction())
            {
                LayerTable ltb = (LayerTable)tr.GetObject(Active.Database.LayerTableId,
                    OpenMode.ForRead);

                //create a new layout.

                if (!ltb.Has("Hvac_Calc"))

                {
                    ltb.UpgradeOpen();

                    LayerTableRecord newLayer = new LayerTableRecord();

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

        internal Hvac(Table table) : base(table)
        {
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            Document doc = Active.Document;

            JigPromptPointOptions prOptions1 = new JigPromptPointOptions("\nNext point:");
            prOptions1.BasePoint = (Entity as Line).StartPoint;
            prOptions1.UseBasePoint = true;
            prOptions1.UserInputControls = UserInputControls.Accept3dCoordinates
                                           | UserInputControls.AnyBlankTerminatesInput
                                           | UserInputControls.GovernedByOrthoMode
                                           | UserInputControls.GovernedByUCSDetect
                                           | UserInputControls.UseBasePointElevation
                                           | UserInputControls.InitialBlankTerminatesInput
                                           | UserInputControls.NullResponseAccepted;
            PromptPointResult prResult1 = prompts.AcquirePoint(prOptions1);
            if (prResult1.Status == PromptStatus.Cancel)
                return SamplerStatus.Cancel;

            if (prResult1.Value.Equals(endPnt))
            {
                return SamplerStatus.NoChange;
            }

            endPnt = prResult1.Value;
            return SamplerStatus.OK;
        }

        protected override bool Update()
        {
            Document doc = Active.Document;

            (Entity as Table).Position = endPnt;
            return true;
        }
    }
}
