using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TransmittalCreator.Annotations;
using TransmittalCreator.ViewModel;

namespace TransmittalCreator.Models
{
    public class FilenameModel:INotifyPropertyChanged
    {
	    private ICommand _clickCommand;
	    public ICommand ClickCommand{
		    get
		    {
			    return _clickCommand ?? (_clickCommand = new CommandHandler(() => MyAction(), ()=> CanExecute));
		    }
	    }

	    private void MyAction()
	    {
		    var fname = FileName;
		    string messageBoxText = "Do you want to save changes?";
		    string caption = "Word Processor";
		    MessageBoxButton button = MessageBoxButton.OK;
		    MessageBoxImage icon = MessageBoxImage.Warning;
		    MessageBoxResult result;

		    result = MessageBox.Show(fname, caption, button, icon, MessageBoxResult.Yes);
	    }

	    public bool CanExecute
	    {
		    get
		    {
			    // check if executing is allowed, i.e., validate, check if a process is running, etc. 
			    return true;
		    }
	    }

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

        private RelayCommand addCommand;
        public RelayCommand AddCommand
        {
	        get
	        {
		        return addCommand ??
		               (addCommand = new RelayCommand(obj =>
		               {
			               _numberingValue = NumberingValue;
		               }));
	        }
        }
    }

    public class CommandHandler : ICommand
    {
	    private Action _action;
	    private Func<bool> _canExecute;

	    /// <summary>
	    /// Creates instance of the command handler
	    /// </summary>
	    /// <param name="action">Action to be executed by the command</param>
	    /// <param name="canExecute">A bolean property to containing current permissions to execute the command</param>
	    public CommandHandler(Action action, Func<bool> canExecute)
	    {
		    _action = action;
		    _canExecute = canExecute;
	    }

	    /// <summary>
	    /// Wires CanExecuteChanged event 
	    /// </summary>
	    public event EventHandler CanExecuteChanged
	    {
		    add { CommandManager.RequerySuggested += value; }
		    remove { CommandManager.RequerySuggested -= value; }
	    }

	    /// <summary>
	    /// Forcess checking if execute is allowed
	    /// </summary>
	    /// <param name="parameter"></param>
	    /// <returns></returns>
	    public bool CanExecute(object parameter)
	    {
		    return _canExecute.Invoke();
	    }

	    public void Execute(object parameter)
	    {
		    _action();
	    }
    }
}
