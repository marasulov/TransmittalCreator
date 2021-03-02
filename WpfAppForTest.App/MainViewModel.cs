using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfAppForTest.App
{
    public class MainViewModel
    {
        public AttributeViewModel AttributeViewModel { get; set; }
        public FilenameModel FilenameModel { get; set; }

        public string FileName { get; set; }

        public void SetFileName()
        {
            FileName = AttributeViewModel.SelectedAttr.AttributeValue + FilenameModel.NumberingValue;
        }
    }
}
