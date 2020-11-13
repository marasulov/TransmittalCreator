using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TransmittalCreator.Services;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Diagnostics.Stopwatch sw = new Stopwatch();
            sw.Start();
            
            string jsonFile =File.ReadAllText(@"C:\Users\yusufzhon.marasulov\source\repos\TransmittalCreator\TestConsole\conf.json");

            Params account = JsonConvert.DeserializeObject<Params>(jsonFile);

            Console.WriteLine(account.Pc3);

            StandartCopier stdCopier = new StandartCopier();
            Console.WriteLine(stdCopier.CopyParamsFiles());

            sw.Stop();
            Console.WriteLine((sw.ElapsedMilliseconds / 100.0).ToString());

            Console.ReadLine();
        }
    }

    public class Params
    {
        public string Pc3 { get; set; }
        public string Pmp { get; set; }


    }
}
