using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TransmittalCreator.Models;

namespace TransmittalCreator.ViewModel
{
    public class AttributeViewModel : INotifyPropertyChanged
    {
        private BlockAttribute _selectedAttr;
        public ObservableCollection<BlockAttribute> Attributes { get; set; }
        public BlockAttribute SelectedAttr
        {
            get => _selectedAttr;
            set
            {
                _selectedAttr = value;
                OnPropertyChanged("SelectedAttr");
            }
        }

        
        private RelayCommand addCommand;
        public RelayCommand AddCommand
        {
            get
            {
                return addCommand ??
                       (addCommand = new RelayCommand(obj =>
                       {
                           _selectedAttr = SelectedAttr;
                       }));
            }
        }

        public AttributeViewModel()
        {
            Attributes = new ObservableCollection<BlockAttribute>
            {
                new BlockAttribute("name1","value1"),
                new BlockAttribute("name2","value2"),
                new BlockAttribute("name3","value3"),
            };
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public BlockAttribute GetSelectedAttribute()
        {
            return _selectedAttr;
        }
    }
}
