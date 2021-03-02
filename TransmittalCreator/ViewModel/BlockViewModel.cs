using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using TransmittalCreator.Models;

namespace TransmittalCreator.ViewModel
{
    public class BlockViewModel
    {
        private BlockAttribute _selectedBlockAttribute;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<BlockAttribute> Blocks { get; set; }

        private RelayCommand addCommand;

        public RelayCommand AddCommand
        {
            get
            {
                return addCommand ??
                       (addCommand = new RelayCommand(obj =>
                       {
                           MessageBox.Show("Test");
                       }));
            }
        }

        public BlockAttribute SelectedBlock
        {
            get { return _selectedBlockAttribute; }
            set
            {
                _selectedBlockAttribute = value;
                OnPropertyChanged("SelectedBlock");
            }
        }

        public BlockViewModel(Dictionary<string, string> blocks)
        {
            foreach (var block in blocks)
            {
                new SpesSheetModel(block.Key, block.Value);
            }
        }
        //TODO list in model


        /// <summary>
        /// Raises OnPropertychangedEvent when property changes
        /// </summary>
        /// <param name="name">String representing the property name</param>
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
