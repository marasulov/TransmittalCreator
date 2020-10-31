using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TransmittalCreator.Models;

namespace TransmittalCreator.ViewModel
{
    public class BlockViewModel
    {
        private BlockModel _selectedBlockModel;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<BlockModel> Blocks { get; set; }

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

        public BlockModel SelectedBlock
        {
            get { return _selectedBlockModel; }
            set
            {
                _selectedBlockModel = value;
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
