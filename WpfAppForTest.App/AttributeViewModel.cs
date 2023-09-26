using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Prism.Commands;

namespace WpfAppForTest.App
{
    public class AttributeViewModel : INotifyPropertyChanged, ICloseWindow
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
                new BlockAttribute{AttributeName = "name1",AttributeValue = "value1"},
                new BlockAttribute{AttributeName = "name2",AttributeValue = "value2"},
                new BlockAttribute{AttributeName = "name3",AttributeValue = "value3"},
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

        //IDialogService _closeCommand = new DialogService();

        private DelegateCommand _closeWindow;

        public DelegateCommand CloseWindow =>
	        _closeWindow ?? (_closeWindow = new DelegateCommand(ExecuteShowDialog));

        private void ExecuteShowDialog()
        {
	        Close ?.Invoke();
        }

        public Action Close { get; set; }
    }

    interface ICloseWindow
    {
	    Action Close { get; set; }
    }
}
