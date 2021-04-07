using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransmittalCreator.Services.CheckListCreator
{
    public class CheckListModel
    {
        public string CheckListFileName { get; set; }
        public List<Sheet> DictSheets { get; set; }

    }
}
