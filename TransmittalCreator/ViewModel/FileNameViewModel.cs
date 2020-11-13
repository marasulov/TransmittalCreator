﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using TransmittalCreator.Annotations;
using TransmittalCreator.Models;

namespace TransmittalCreator.ViewModel
{
    public class FileNameViewModel : INotifyPropertyChanged
    {
        private FileModel selectedFile;

        public ObservableCollection<FileModel> FileModels { get; set; }
        

        public FileModel SelectedFile
        {
            get { return selectedFile; }
            set { selectedFile = value; }
        }

        public FileNameViewModel()
        {
            FileModels = new ObservableCollection<FileModel>
            {
                new FileModel { FileName="iPhone 7" },
                new FileModel {FileName="Galaxy S7 Edge" },
                new FileModel {FileName= "Elite x3"},
                new FileModel {FileName= "Mi5S"}
            };
        }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
