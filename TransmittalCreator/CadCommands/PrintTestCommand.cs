using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;
using OfficeOpenXml.Packaging.Ionic.Zlib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;

namespace TransmittalCreator.CadCommands;

public class PrintTestCommand
{
    [DllImport("accore.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "acedTrans")]
    static extern int acedTrans(double[] point, IntPtr fromRb, IntPtr toRb, int disp, double[] result);
    [CommandMethod("DWGTOPDF")]
    public static void PlotCurrentLayout()
    {
        Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
        Database acCurDb = acDoc.Database;
        Editor ed = acDoc.Editor;
        using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
        {
            PromptSelectionOptions ptopts = new PromptSelectionOptions();
            PromptSelectionResult ptres = ed.GetSelection(ptopts);
            Point2d upLeftPoint = new Point2d();
            Point2d downRightPoint = new Point2d();
            if (ptres.Status == PromptStatus.OK)
            {
                List<double> xPoint = new List<double>();
                List<double> yPoint = new List<double>();
                Polyline frame = ptres.Value.GetObjectIds()[0].GetObject(OpenMode.ForRead) as Polyline;
                for (int i = 0; i < frame.NumberOfVertices; i++)
                {
                    xPoint.Add(frame.GetPoint2dAt(i).X);
                    yPoint.Add(frame.GetPoint2dAt(i).Y);
                }
                xPoint.Sort();
                yPoint.Sort();

                upLeftPoint = new Point2d(xPoint[xPoint.Count - 1], yPoint[yPoint.Count - 1]);
                downRightPoint = new Point2d(xPoint[0], yPoint[0]);
                var height = upLeftPoint.X - downRightPoint.X;
                var width = upLeftPoint.Y - downRightPoint.Y;
                var area = Math.Round(height * width, 0);
                //редактируем pc3 и pmp файлы
                ChangePc3(width + ".0", height + ".0", area + ".0");
                ChangePmp(width + ".0", height + ".0", area + ".0");
                // устанавливаем настройки печати
                LayoutManager layoutMgr = LayoutManager.Current;
                Layout acLayout;
                acLayout = acTrans.GetObject(layoutMgr.GetLayoutId(layoutMgr.CurrentLayout), OpenMode.ForRead) as Layout;
                PlotInfo acPlInfo = new PlotInfo();
                acPlInfo.Layout = acLayout.ObjectId;
                PlotSettings acPlSet = new PlotSettings(acLayout.ModelType);
                acPlSet.CopyFrom(acLayout);
                PlotSettingsValidator acPlSetVdr = PlotSettingsValidator.Current;
                //конвертируем координаты листа в координаты экрана
                ResultBuffer rbFrom = new ResultBuffer(new TypedValue(5003, 1)), rbTo = new ResultBuffer(new TypedValue(5003, 2));
                double[] firres = new double[] { 0, 0, 0 };
                double[] secres = new double[] { 0, 0, 0 };
                acedTrans(upLeftPoint.ToArray(), rbFrom.UnmanagedObject, rbTo.UnmanagedObject, 0, firres);
                acedTrans(downRightPoint.ToArray(), rbFrom.UnmanagedObject, rbTo.UnmanagedObject, 0, secres);
                Extents2d window = new Extents2d(firres[0], firres[1], secres[0], secres[1]);

                acPlSetVdr.SetPlotPaperUnits(acPlSet, PlotPaperUnit.Millimeters);
                acPlSetVdr.SetPlotWindowArea(acPlSet, window);
                acPlSetVdr.SetPlotType(acPlSet, Autodesk.AutoCAD.DatabaseServices.PlotType.Window);
                acPlSetVdr.SetUseStandardScale(acPlSet, true);
                acPlSetVdr.SetStdScaleType(acPlSet, StdScaleType.ScaleToFit);
                acPlSetVdr.SetPlotCentered(acPlSet, true);
                acPlSetVdr.SetPlotConfigurationName(acPlSet, "C:\\Users\\admin\\AppData\\Roaming\\Autodesk\\C3D 2014\\rus\\Plotters\\DWG To PDF.pc3", "UserDefinedMetric (100.00 x 500.00мм)");
                acPlInfo.OverrideSettings = acPlSet;
                PlotInfoValidator acPlInfoVdr = new PlotInfoValidator();
                acPlInfoVdr.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
                acPlInfoVdr.Validate(acPlInfo);
                if (PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting)
                {
                    using (PlotEngine acPlEng = PlotFactory.CreatePublishEngine())
                    {
                        PlotProgressDialog acPlProgDlg = new PlotProgressDialog(false, 1, true);
                        using (acPlProgDlg)
                        {
                            acPlProgDlg.set_PlotMsgString(PlotMessageIndex.DialogTitle, "Plot Progress");
                            acPlProgDlg.set_PlotMsgString(PlotMessageIndex.CancelJobButtonMessage, "Cancel Job");
                            acPlProgDlg.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, "Cancel Sheet");
                            acPlProgDlg.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption, "Sheet Set Progress");
                            acPlProgDlg.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "Sheet Progress");
                            acPlProgDlg.LowerPlotProgressRange = 0;
                            acPlProgDlg.UpperPlotProgressRange = 100;
                            acPlProgDlg.PlotProgressPos = 0;
                            acPlProgDlg.OnBeginPlot();
                            acPlProgDlg.IsVisible = true;
                            acPlEng.BeginPlot(acPlProgDlg, null);
                            acPlEng.BeginDocument(acPlInfo, acDoc.Name, null, 1, true, "C:\\Users\\admin\\Desktop\\Новая папка\\asdasd.pdf");
                            acPlProgDlg.set_PlotMsgString(PlotMessageIndex.Status, "Plotting: " + acDoc.Name + " - " + acLayout.LayoutName);
                            acPlProgDlg.OnBeginSheet();
                            acPlProgDlg.LowerSheetProgressRange = 0;
                            acPlProgDlg.UpperSheetProgressRange = 100;
                            acPlProgDlg.SheetProgressPos = 0;
                            PlotPageInfo acPlPageInfo = new PlotPageInfo();
                            acPlEng.BeginPage(acPlPageInfo, acPlInfo, true, null);
                            acPlEng.BeginGenerateGraphics(null);
                            acPlEng.EndGenerateGraphics(null);
                            acPlEng.EndPage(null);
                            acPlProgDlg.SheetProgressPos = 100;
                            acPlProgDlg.OnEndSheet();
                            acPlEng.EndDocument(null);
                            acPlProgDlg.PlotProgressPos = 100;
                            acPlProgDlg.OnEndPlot();
                            acPlEng.EndPlot(null);
                        }
                    }
                }
            }
        }
    }

    public static void ChangePc3(string width, string height, string area)
    {
        string fileName1 = "D:\\from comp\\Desktop\\Test\\DWG_To_PDF_Uzle.pc3";
        string content = "";
        using (var fs = File.Open(fileName1, FileMode.Open, FileAccess.Read))
        {
            fs.Seek(60L, SeekOrigin.Begin);
            using (var zs = new ZlibStream(fs, CompressionMode.Decompress))
            {
                using (StreamReader sr = new StreamReader(zs, Encoding.GetEncoding(1251)))
                {
                    content = sr.ReadToEnd();
                    content = content.Replace("actual_printable_bounds_urx=100.0", $"actual_printable_bounds_urx={width}");
                    content = content.Replace("actual_printable_bounds_ury=500.0", $"actual_printable_bounds_ury={height}");
                    content = content.Replace("printable_bounds_urx=100.0", $"printable_bounds_urx={width}");
                    content = content.Replace("printable_bounds_ury=500.0", $"printable_bounds_ury={height}");
                    content = content.Replace("printable_area=50000.0", $"printable_area={area}");
                    content = content.Replace("media_bounds{    urx = 100.0    ury = 500.0   }", "media_bounds{    urx = " + height + "    ury = " + width + "   }");
                    using (FileStream fs_out = File.Open(fileName1 + ".txt", FileMode.Create, FileAccess.ReadWrite))
                    {
                        fs_out.Write(Encoding.Default.GetBytes(content), 0, Encoding.Default.GetBytes(content).Length);
                    }
                }
            }
        }
        string fileName = fileName1 + ".txt";
        using (FileStream fs = File.Open(fileName, FileMode.Open, FileAccess.Read))
        {
            using (StreamReader sr = new StreamReader(fs, Encoding.GetEncoding(1251)))
            {
                String pref_s = "PIAFILEVERSION_2.0,PC3VER1,compress\r\npmzlibcodec";
                long decompresse_stream_size = fs.Length;
                long compressed_stream_size = fs.Length;
                string s = sr.ReadToEnd();
                using (FileStream fs_out = File.Open("C:\\Users\\admin\\AppData\\Roaming\\Autodesk\\C3D 2014\\rus\\Plotters\\DWG To PDF.pc3", FileMode.Create, FileAccess.ReadWrite))
                {
                    using (ZlibStream zs = new ZlibStream(fs_out, CompressionMode.Compress,
                                                             CompressionLevel.BestCompression, false))
                    {
                        fs_out.Write(Encoding.Default.GetBytes(pref_s), 0, Encoding.Default.GetBytes(pref_s).Length);
                        fs_out.Write(BitConverter.GetBytes(new ZlibCodec(CompressionMode.Compress).Adler32), 0, 4);
                        fs_out.Write(BitConverter.GetBytes(decompresse_stream_size), 0, 4);
                        fs_out.Write(BitConverter.GetBytes(compressed_stream_size), 0, 4);

                        zs.Write(Encoding.Default.GetBytes(s), 0, Encoding.Default.GetBytes(s).Length);
                    }
                }
            }
        }
    }
    public static void ChangePmp(string width, string height, string area)
    {
        string fileName1 = "C:\\Users\\admin\\Desktop\\pdf\\Исходные\\DWG To PDF.pmp";
        string content = "";
        using (FileStream fs = File.Open(fileName1, FileMode.Open, FileAccess.Read))
        {
            fs.Seek(60L, SeekOrigin.Begin);
            using (ZlibStream zs = new ZlibStream(fs, CompressionMode.Decompress))
            {
                using (StreamReader sr = new StreamReader(zs, Encoding.GetEncoding(1251)))
                {
                    content = sr.ReadToEnd();
                    content = content.Replace("media_bounds_urx=100.0", $"media_bounds_urx={width}");
                    content = content.Replace("media_bounds_ury=500.0", $"media_bounds_ury={height}");
                    content = content.Replace("printable_bounds_urx=100.0", $"printable_bounds_urx={width}");
                    content = content.Replace("printable_bounds_ury=500.0", $"printable_bounds_ury={height}");
                    content = content.Replace("printable_area=50000.0", $"printable_area={area}");
                    using (FileStream fs_out = File.Open(fileName1 + ".txt", FileMode.Create, FileAccess.ReadWrite))
                    {
                        fs_out.Write(Encoding.Default.GetBytes(content), 0, Encoding.Default.GetBytes(content).Length);
                    }
                }
            }
        }
        string fileName = fileName1 + ".txt";
        using (FileStream fs = File.Open(fileName, FileMode.Open, FileAccess.Read))
        {
            using (StreamReader sr = new StreamReader(fs, Encoding.GetEncoding(1251)))
            {
                String pref_s = "PIAFILEVERSION_2.0,PC3VER1,compress\r\npmzlibcodec";
                long decompresse_stream_size = fs.Length;
                long compressed_stream_size = fs.Length;
                string s = sr.ReadToEnd();
                using (FileStream fs_out = File.Open("C:\\Users\\admin\\AppData\\Roaming\\Autodesk\\C3D 2014\\rus\\Plotters\\PMP Files\\DWG To PDF.pmp", FileMode.Create, FileAccess.ReadWrite))
                {
                    using (ZlibStream zs = new ZlibStream(fs_out, CompressionMode.Compress,
                                                             CompressionLevel.BestCompression, false))
                    {
                        fs_out.Write(Encoding.Default.GetBytes(pref_s), 0, Encoding.Default.GetBytes(pref_s).Length);
                        fs_out.Write(BitConverter.GetBytes(new ZlibCodec(CompressionMode.Compress).Adler32), 0, 4);
                        fs_out.Write(BitConverter.GetBytes(decompresse_stream_size), 0, 4);
                        fs_out.Write(BitConverter.GetBytes(compressed_stream_size), 0, 4);
                        zs.Write(Encoding.Default.GetBytes(s), 0, Encoding.Default.GetBytes(s).Length);
                    }
                }
            }
        }
    }
}