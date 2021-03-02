using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace TransmittalCreator.Models
{
    public class BlockAttribute : INotifyPropertyChanged
    {

        private string _attributeName;
        private string _attributeValue;
        
        /// <summary>
        /// Название атрибута
        /// </summary>
        public string AttributeName
        {
            get => _attributeName;
            set
            {
                _attributeName = value;
                OnPropertyChanged("AttributeName");
            }
        }

        /// <summary>
        /// Document Number
        /// </summary>
        public string AttributeValue
        {
            get => _attributeValue;
            set
            {
                _attributeValue = value;
                OnPropertyChanged("AttributeValue");
            }
        }

        public BlockAttribute(string attrName, string attrValue)
        {
            this.AttributeName = attrName;
            this.AttributeValue = attrValue;
        }

        public string GetAttrName(BlockAttribute blockAttribute)
        {
            return blockAttribute.AttributeName;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

    }
}
