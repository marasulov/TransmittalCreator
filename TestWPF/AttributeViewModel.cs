using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TestWPF
{
    public class AttributeViewModel : INotifyPropertyChanged
    {
        private Attribute selectedAttr;
        public Attribute SelectedAttr
        {
            get => selectedAttr;
            set
            {
                selectedAttr = value;
                OnPropertyChanged("SelectedAttr");
            }
        }

        public AttributeViewModel()
        {
            Attributes = new ObservableCollection<Attribute>
            {
                new Attribute{AttributeName = "name1", AttributeValue = "value1"},
                new Attribute{AttributeName = "name2", AttributeValue = "value2"},
                new Attribute{AttributeName = "name3", AttributeValue = "value3"},
                new Attribute{AttributeName = "name4", AttributeValue = "value4"},
            };
        }

        public ObservableCollection<Attribute> Attributes { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
