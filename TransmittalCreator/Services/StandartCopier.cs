using System;
using System.IO;
using System.Reflection;
using Autodesk.AutoCAD.DatabaseServices;
using Newtonsoft.Json;
using TransmittalCreator.Annotations;

namespace TransmittalCreator.Services
{

    public class StandartCopier
    {
        //private string confFile = @"\\uz-fs\Install\CAD\Blocks\conf.json";
        private string confFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,"conf.json");
        private string pathEnvPc3Dir = HostApplicationServices.Current.GetEnvironmentVariable("PrinterConfigDir");
        private string pathEnvPmpDir = HostApplicationServices.Current.GetEnvironmentVariable("PrinterDescDir");

        public string Pc3Dest { get; set; }
        public string PmpDest { get; set; }
        public string Pc3Location { get; set; }
        public string PmpLocation { get; set; }

        public StandartCopier()
        {
            string jsonFile = File.ReadAllText(confFile);
            Params pParams = JsonConvert.DeserializeObject<Params>(jsonFile);

            //destination folders to copy
            this.Pc3Dest = Path.Combine(pathEnvPc3Dir, pParams.Pc3);
            this.PmpDest = Path.Combine(pathEnvPmpDir, pParams.Pmp);

            // location folder and files path
            string locationFolder = Path.GetDirectoryName(confFile);
            this.Pc3Location = Path.Combine(locationFolder, pParams.Pc3);
            this.PmpLocation = Path.Combine(locationFolder, pParams.Pmp);
        }

        public bool CopyParamsFiles()
        {
            if (!File.Exists(this.Pc3Dest)) IsFileCopied(this.Pc3Location, this.Pc3Dest);
            if (!File.Exists(this.PmpDest)) IsFileCopied(this.PmpLocation, this.PmpDest);

            return true;
        }

        private bool IsFileCopied(string location, string destination )
        {
            try
            {
                File.Copy(location, destination);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}
