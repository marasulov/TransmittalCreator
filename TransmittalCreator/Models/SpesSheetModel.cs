using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace TransmittalCreator.Models
{
    public class SpesSheetModel : INotifyPropertyChanged
    {

        private string sheetNumber;
        private string docNumber;
        private string objectNameEng;
        /// <summary>
        /// Номер листа
        /// </summary>
        public string SheetNumber
        {
            get
            {
                return sheetNumber;
            }
            set
            {
                sheetNumber = value;
                OnPropertyChanged("SheetNumber");
            }
        }

        /// <summary>
        /// Document Number
        /// </summary>
        public string DocNumber
        {
            get
            {
                return docNumber;
            }
            set
            {
                docNumber = value;
                OnPropertyChanged("DocNumber");

                
            }
        }
        /// <summary>
        /// Document Name in English
        /// </summary>
        public string ObjectNameEng { get; set; }

        /// <summary>
        /// Document Title (English)
        /// </summary>
        public string DocTitleEng { get; set; }

        /// <summary>
        /// documnet name in rus
        /// </summary>
        public string ObjectNameRu { get; set; }
        /// <summary>
        /// Document Title (Rus)
        /// </summary>
        public string DocTitleRu { get; set; }

        public SpesSheetModel(string attr, string val)
        {
            this.SheetNumber = attr;
            this.DocNumber = val;
        }



        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }



    }
}
