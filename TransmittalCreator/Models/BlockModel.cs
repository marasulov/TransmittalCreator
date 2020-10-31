using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace TransmittalCreator.Models
{
    public class BlockModel : INotifyPropertyChanged
    {

        private string attributName;
        private string attributValue;
        
        /// <summary>
        /// Название атрибута
        /// </summary>
        public string AttributName
        {
            get
            {
                return attributName;
            }
            set
            {
                attributName = value;
                OnPropertyChanged("AttributName");
            }
        }

        /// <summary>
        /// Document Number
        /// </summary>
        public string AttributValue
        {
            get
            {
                return attributValue;
            }
            set
            {
                attributValue = value;
                OnPropertyChanged("AttributValue");
            }
        }

        public BlockModel(string attrName, string attrValue)
        {
            this.AttributName = attrName;
            this.AttributValue = attrValue;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }



    }
}
