using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using TemplateEngine.Docx;
using TransmittalCreator.Services.Serializers;

namespace TransmittalCreator.Services.CheckListCreator
{
    public class CheckListCreator
    {
        
        //public CheckListModel CheckListModel { get; set; }

        //public CheckListCreator(CheckListModel checkListModel)
        //{
            
        //    CheckListModel = checkListModel;
        //}

        //public void CreateCheckList()
        //{


        //    var tableContent = new TableContent("Team Members Table");
        //    foreach (var pipe in CheckListModel.DictSheets)
        //    {
              
        //    }
        //    var valuesToFill = new Content(tableContent);
        //    string sourceFile = @"D:\docs\Desktop\ВОР\new\UZLE-59-030-OPN-SCH-080103-RU-A1.docx";
        //    string destFile = @"D:\docs\Desktop\ВОР\new\UZLE-A1.docx";
        //    File.Delete(destFile);
        //    File.Copy(sourceFile, destFile);

        //    using (var outputDocument = new TemplateProcessor(destFile).SetRemoveContentControls(true))
        //    {
        //        outputDocument.FillContent(valuesToFill);
        //        outputDocument.SaveChanges();
        //    }
        //}
    }
}
