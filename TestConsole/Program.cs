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


namespace TestConsole
{
    [Serializable]
    public class MyType: ISerializable
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
            myProperty_value = (string) info.GetValue("props", typeof(string));
        }
    }

    class Program
    {
   
        static void Main(string[] args)
        {

            string fileName = "dataStuff.myData";

            // Use a BinaryFormatter or SoapFormatter.
            IFormatter formatter = new BinaryFormatter();
            //IFormatter formatter = new SoapFormatter();

            SerializeItem(fileName, formatter); // Serialize an instance of the class.
            DeserializeItem(fileName, formatter); // Deserialize the instance.
            Console.WriteLine("Done");
            Console.ReadLine();
        }

        public static void SerializeItem(string fileName, IFormatter formatter)
        {
            // Create an instance of the type and serialize it.
            MyType t = new MyType();
            t.MyProperty = "Hello World";

            FileStream s = new FileStream(fileName , FileMode.Create);
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
