using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using OfficeOpenXml;
using TransmittalCreator.Models;
using System.Reflection;



namespace TestConsole
{
    public class MyType
    {
        public MyType()
        {
            Console.WriteLine();
            Console.WriteLine("MyType instantiated!");
        }
    }

    class Program
    {
        private static void InstantiateMyTypeFail(AppDomain domain)
        {
            // Calling InstantiateMyType will always fail since the assembly info
            // given to CreateInstance is invalid.
            try
            {
                // You must supply a valid fully qualified assembly name here.
                domain.CreateInstance("Assembly text name, Version, Culture, PublicKeyToken", "MyType");
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.WriteLine(e.Message);
            }

            Console.ReadLine();
        }

        private static void InstantiateMyTypeSucceed(AppDomain domain)
        {
            try
            {
                string asmname = Assembly.GetCallingAssembly().FullName;
                domain.CreateInstance(asmname, "MyType");
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.WriteLine(e.Message);
            }
        }

        private static Assembly MyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            Console.WriteLine("Resolving...");
            return typeof(MyType).Assembly;
        }

        static void Main(string[] args)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;

            // This call will fail to create an instance of MyType since the
            // assembly resolver is not set
            InstantiateMyTypeFail(currentDomain);

            currentDomain.AssemblyResolve += new ResolveEventHandler(MyResolveEventHandler);

            // This call will succeed in creating an instance of MyType since the
            // assembly resolver is now set.
            InstantiateMyTypeFail(currentDomain);

            // This call will succeed in creating an instance of MyType since the
            // assembly name is valid.
            InstantiateMyTypeSucceed(currentDomain);

            //List<HvacTable>  hvacTables = new List<HvacTable>();

            //string filename = @"D:\docs\Desktop\Calculations_HVA.xlsx";

            //FileInfo fileInfo = new FileInfo(filename);
            //List<HvacTable> listData = new List<HvacTable>();

            //using (ExcelPackage package = new ExcelPackage(fileInfo))
            //{
            //    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            //    //create an instance of the the first sheet in the loaded file
            //    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
            //    int rowStart = 7;
            //    int rowCount = worksheet.Dimension.End.Row;
                
            //    for (int i = rowStart; i < rowCount-1; i++)
            //    {
            //        if (worksheet.Cells[i, 2].Value != null & !worksheet.Cells[i, 1].Value.ToString().Contains("Total") 
            //            &  worksheet.Cells[i, 26].Value.ToString() != "0")
            //        {
            //            string roomNumber = worksheet.Cells[i, 1].Value.ToString().Trim();
            //            string roomName = worksheet.Cells[i, 2].Value.ToString().Trim();
            //            string heating = worksheet.Cells[i, 26].Value.ToString().Trim();
            //            string cooling = worksheet.Cells[i, 34].Value.ToString().Trim();
            //            string supply = worksheet.Cells[i, 39].Value.ToString().Trim();
                        
            //            string supplyInd = "П";
            //            if(worksheet.Cells[i, 38].Value !=null) supplyInd = worksheet.Cells[i, 38].Value.ToString().Trim();
            //            string exhaustInd = "П";
            //            if(worksheet.Cells[i, 40].Value !=null) exhaustInd = worksheet.Cells[i, 40].Value.ToString().Trim();

            //            string exhaust = worksheet.Cells[i, 41].Value.ToString().Trim();

            //                listData.Add(new HvacTable(roomNumber, roomName, heating, cooling, supply, supplyInd, exhaust, exhaustInd));
            //        }
            //    }
            //}
            //Console.WriteLine(listData.Count);
            //HvacTable hvacTable = listData[0];
            //Type type = typeof(HvacTable);
            //int NumberOfRecords = type.GetProperties().Length;
            //Console.WriteLine(NumberOfRecords);
            
            /*File.Delete(@"C:\Users\yusufzhon.marasulov\Desktop\ВОР\new\UZLE-59-030-OPN-SCH-080103-RU-A1.docx");


            MobileStore store = new MobileStore(new ConsolePhoneReader(), new GeneralPhoneBinder(),
                new GeneralPhoneValidator(), new TextPhoneSaver());
            store.Process();

            IPrinter printer = new ConsolePrinter();
            Report report = new Report();
            report.Text = "Hello Wolrd";
            report.Print(printer);

            Console.Read();

            string filename = @"C:\Users\yusufzhon.marasulov\Desktop\1.txt";
            List<string> text = File.ReadAllLines(filename).ToList();
            
            List<MatchCollection> matchCollections= new List<MatchCollection>();


            string str3 = "ISO_expand_A1_(841.00_x_594.00_MM)";
            string str = "UserDefinedMetric (2378.00 x 841.00мм)";
            double width = 841.00;
            double height = 594.00;
            string pat = @"\d{1,}?\.\d{2}";

            foreach (var line in text)
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


                    if (strWidthD == width & strheightD == height)
                    {
                        Console.WriteLine("{0} ширина {1}-{2}  высота {3}-{4}", line, strWidthD, width, strheightD, height);
                        return;
                    }
                      
                    matchCollections.Add(str2);
                }
            }
            string list = GetWithIn(str);
            Console.ReadLine();*/
        }

        public static double ConvertToDouble(string Value)
        {
            if (Value == null)
            {
                return 0;
            }
            else
            {
                double OutVal;
                double.TryParse(Value, out OutVal);

                if (double.IsNaN(OutVal) || double.IsInfinity(OutVal))
                {
                    return 0;
                }
                return OutVal;
            }
        }

        public static string GetWithIn(string str)
        {
            string rez = "";

            Regex pattern =
                new Regex(@"\d{1,}?\.[00]{1,2}",
                    RegexOptions.Compiled |
                    RegexOptions.Singleline);

            foreach (Match m in pattern.Matches(str))
                if (m.Success)
                {
                    rez = m.Groups["val"].Value;

                }

            return rez;
        }
    }

    public class Params
    {
        public string Pc3 { get; set; }
        public string Pmp { get; set; }


    }
}
