using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockExtractorView
{
    public class ItemProvider
    {
        public List<TreeModel> GetItems(string path)
        {
            var items = new List<TreeModel>();

            var dirInfo = new DirectoryInfo(path);

            foreach(var directory in dirInfo.GetDirectories())
            {
                var item = new DirectoryItem
                {
                    Name = directory.Name,
                    Path = directory.FullName,
                    Items = GetItems(directory.FullName)
                };

                items.Add(item);
            }

            foreach(var file in dirInfo.GetFiles())
            {
                var item = new FileModel()
                {
                    Name = file.Name, 
                    Path = file.FullName
                };

                items.Add(item);
            }

            return items;
        }
    }
}
