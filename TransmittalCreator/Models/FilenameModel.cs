using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TransmittalCreator.Annotations;
using TransmittalCreator.ViewModel;

namespace TransmittalCreator.Models
{
    public class FilenameModel:INotifyPropertyChanged
    {
        private const string PdfExt = ".pdf";
        private int _numberingValue = 0;
        private string _suffix;
        private string _prefix;
        private AttributeViewModel _attributeViewModel;
        private string _fileName = PdfExt;
        private bool _isCheckedNumbering = true;
        private bool _isCheckedAttribute = true;

        #region Props
        public BlockAttribute BlockAttribute
        {
            get => _attributeViewModel.SelectedAttr;
            set
            {
                _attributeViewModel.SelectedAttr = value; 

                FileName = Prefix + value.AttributeValue + SetNumberingValue() + Suffix+PdfExt;
                OnPropertyChanged("BlockAttribute");
            }
        }

        public bool IsCheckedAttribute
        {
            get => _isCheckedAttribute;
            set
            {
                _isCheckedAttribute = value;
                if(value==true)
                    FileName = Prefix +BlockAttribute?.AttributeValue +SetNumberingValue()+ Suffix+PdfExt;
                else FileName = Prefix +SetNumberingValue()+ Suffix+PdfExt;
                OnPropertyChanged("IsCheckedAttribute");
            }
        }

        public bool IsCheckedNumbering
        {
            get => _isCheckedNumbering;
            set
            {
                _isCheckedNumbering = value;
                if(IsCheckedAttribute == true)
                    FileName = Prefix + BlockAttribute?.AttributeValue+Suffix+PdfExt;
                else FileName = Prefix + Suffix+PdfExt;
                if(value == false) _numberingValue = 0;
                OnPropertyChanged(nameof(NumberingValue));
                OnPropertyChanged("IsCheckedNumbering");
                
            }
        }
        public string FileName
        {
            get => _fileName;
            set
            {
                if (_fileName != value) _fileName = value;
                OnPropertyChanged("Filename");
            }
        }

        public int NumberingValue
        {
            get => _numberingValue;
            set
            {
                if(_isCheckedNumbering){ 
                    _numberingValue = value; 
                    FileName = Prefix + BlockAttribute?.AttributeValue+value + Suffix + PdfExt;
                }
                else _numberingValue = 0;
                OnPropertyChanged("NumberingValue");
            }
        }

        public string Suffix
        {
            get => _suffix;
            set
            {
                _suffix = value;
              
                FileName = Prefix + BlockAttribute?.AttributeValue + SetNumberingValue() + value + PdfExt;
                OnPropertyChanged("Suffix");
            }
        }

        public string Prefix
        {
            get => _prefix;
            set
            {
                _prefix = value;
                FileName = value + BlockAttribute?.AttributeValue + SetNumberingValue() + Suffix + PdfExt;
                OnPropertyChanged("Prefix");
            }
        }
        #endregion
        public FilenameModel()
        {
            _attributeViewModel = new AttributeViewModel();
            _prefix = "";
            _suffix = "";
        }

        public AttributeViewModel AttributeViewModel
        {
            get => _attributeViewModel;
            set
            {
                _attributeViewModel = value;
                FileName += value.SelectedAttr.AttributeValue;
                OnPropertyChanged("AttributeViewModel");
            }
        }

        private void SetFileName()
        {
           
        }

        private string SetNumberingValue()
        {
            if (_numberingValue != 0) return NumberingValue.ToString();
            return "";
        }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            //SetFileName();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
