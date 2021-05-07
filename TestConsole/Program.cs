using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using OfficeOpenXml;
using TransmittalCreator.Models;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;


namespace TestConsole
{
    [Serializable]
    public class MyType : ISerializable
    {
        public MyType()
        {
            //Console.WriteLine();
            //Console.WriteLine("MyType instantiated!");
        }

        private string myProperty_value;

        public string MyProperty
        {
            get { return myProperty_value; }
            set { myProperty_value = value; }
        }

        // Implement this method to serialize data. The method is called
        // on serialization.
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Use the AddValue method to specify serialized values.
            info.AddValue("props", myProperty_value, typeof(string));
        }

        // The special constructor is used to deserialize values.
        public MyType(SerializationInfo info, StreamingContext context)
        {
            // Reset the property value using the GetValue method.
            myProperty_value = (string)info.GetValue("props", typeof(string));
        }
    }

    class Program
    {

        static void Main(string[] args)
        {




            //string dir = @"C:\Users\yusufzhon.marasulov\Desktop";
            //DirectoryInfo directoryInfo = new DirectoryInfo(dir);
            //string[] allfiles = Directory.GetFiles(dir, "*.pdf", SearchOption.AllDirectories);
            //string[] allDirs = Directory.GetDirectories(@"C:\Users\yusufzhon.marasulov\Desktop");

            //string tree = ScanFolder(directoryInfo);

            //Console.WriteLine("Done");
            //Console.ReadLine();
        }

        static string ScanFolder(DirectoryInfo directory, string indentation = "\t", int maxLevel = -1, int deep = 0)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine(string.Concat(Enumerable.Repeat(indentation, deep)) + directory.Name);

            if (maxLevel == -1 || maxLevel < deep)
            {
                foreach (var subdirectory in directory.GetDirectories())
                    builder.Append(ScanFolder(subdirectory, indentation, maxLevel, deep + 1));
            }

            foreach (var file in directory.GetFiles())
                builder.AppendLine(string.Concat(Enumerable.Repeat(indentation, deep + 1)) + file.Name);

            return builder.ToString();
        }


        public static void DirectorySearch(string dir)
        {
            try
            {
                foreach (string f in Directory.GetFiles(dir))
                {
                    Console.WriteLine(Path.GetFileName(f));
                }
                foreach (string d in Directory.GetDirectories(dir))
                {
                    Console.WriteLine(Path.GetFileName(d));
                    DirectorySearch(d);
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public static void SerializeItem(string fileName, IFormatter formatter)
        {
            // Create an instance of the type and serialize it.
            MyType t = new MyType();
            t.MyProperty = "Hello World";

            FileStream s = new FileStream(fileName, FileMode.Create);
            formatter.Serialize(s, t);
            s.Close();
        }

        public static void DeserializeItem(string fileName, IFormatter formatter)
        {
            FileStream s = new FileStream(fileName, FileMode.Open);
            MyType t = (MyType)formatter.Deserialize(s);
            Console.WriteLine(t.MyProperty);
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
