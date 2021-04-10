using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Autodesk.AutoCAD.Runtime;
using TransmittalCreator.Models;
using MessageBox = System.Windows.Forms.MessageBox;

namespace TransmittalCreator.ViewModel
{
    public class SheetViewModel 
    {
        private SpesSheetModel _selectedSheetModel;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<SpesSheetModel> Sheets { get; set; }

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

        public SpesSheetModel SelectedSheet
        {
            get { return _selectedSheetModel; }
            set
            {
                _selectedSheetModel = value;
                //OnPropertyChanged("SelectedSheet");
            }
        }

        public SheetViewModel(Dictionary<string,string> sheets)
        {
            foreach (var sheet in sheets)
            {
                new SpesSheetModel (sheet.Key, sheet.Value);
            }
        }


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
