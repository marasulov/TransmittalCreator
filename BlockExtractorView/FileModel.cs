using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BlockExtractorView
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

        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
