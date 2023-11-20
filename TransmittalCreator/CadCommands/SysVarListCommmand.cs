using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using DocumentFormat.OpenXml.Drawing.Charts;
using TransmittalCreator.DBCad;
namespace TransmittalCreator.CadCommands
{
    public class SysVarListCommmand
    {
        [CommandMethod("SysVarListTest")]
        public void SysVarListTest() // This method can have any name
        {
            var strList = new List<string>() { "Name,Value,PrimaryType,SecondaryType,TypeFlags,Storage" };
            
            using (SystemVariableEnumerator sysVar
                   = new SystemVariableEnumerator())
            {
                while (sysVar.MoveNext())
                {
                    Variable var = sysVar.Current;
                    var primaryType = var.PrimaryType;
                    var secondaryType = var.SecondaryType;
                    var typeFlag = var.TypeFlags;
                    var storageType = var.Storage;
                    var value = Application.GetSystemVariable(var.Name);


                    var curVar = var.Name + "," + value + "," + primaryType + "," + secondaryType + "," + typeFlag + "," + storageType + ",";
                    Active.WriteMessage(curVar);
                    strList.Add(curVar);
                }
            }
            var folderdwg = Path.Combine(Path.GetDirectoryName(Active.Database.OriginalFileName), "vars.csv");
            File.WriteAllLines(folderdwg, strList);

        }

    }
}
