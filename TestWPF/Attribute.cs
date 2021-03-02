using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TestWPF
{
    public class Attribute:INotifyPropertyChanged
    {
        private string _attributeName;
        private string _attributeValue;

        public string AttributeName
        {
            get => _attributeName;
            set
            {
                _attributeName = value;
                OnPropertyChanged("AttributeName");
            }
        }

        public string AttributeValue
        {
            get => _attributeValue;
            set => _attributeValue = value;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
