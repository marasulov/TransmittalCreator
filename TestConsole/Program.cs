using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TransmittalCreator.Services;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {

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
            Console.ReadLine();
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

            //меж скобок 
            return rez;
        }
    }

    public class Params
    {
        public string Pc3 { get; set; }
        public string Pmp { get; set; }


    }
}
