using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TransmittalCreator.Annotations;

namespace TransmittalCreator.Models
{
    public class FileModel : INotifyPropertyChanged
    {
        private string fileName;
        private string fileType;

        public string FileName
        {
            get { return fileName; }
            set { fileName = value; OnPropertyChanged("FileName");}
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
