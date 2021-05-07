using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockExtractorView
{
    public class DirectoryItem:TreeModel
    {
        public List<TreeModel> Items { get; set; }

        public DirectoryItem()
        {
            Items = new List<TreeModel>();
        }
    }
}
