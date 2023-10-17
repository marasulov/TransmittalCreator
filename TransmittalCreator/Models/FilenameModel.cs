using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using TransmittalCreator.Annotations;
using TransmittalCreator.ViewModel;

namespace TransmittalCreator.Models;

public class FilenameModel : INotifyPropertyChanged
{
	private const string PdfExt = ".pdf";
	private AttributeViewModel _attributeViewModel;

	private ICommand _clickCommand;

	private string _fileName = PdfExt;
	private bool _isCheckedAttribute = true;
	private bool _isCheckedNumbering = true;
	private int _numberingValue;
	private string _prefix;
	private string _suffix;

	private RelayCommand addCommand;

	public FilenameModel()
	{
		_attributeViewModel = new AttributeViewModel();
		_prefix = "";
		_suffix = "";
	}

	public FilenameModel(List<BlockAttribute> blockAttributes)
	{
		BlockAttributes = blockAttributes;

		_prefix = "";
		_suffix = "";
	}

	public ICommand ClickCommand
	{
		get { return _clickCommand ?? (_clickCommand = new CommandHandler(() => GetFileName(), () => CanExecute)); }
	}

	public bool CanExecute =>
		// check if executing is allowed, i.e., validate, check if a process is running, etc. 
		true;

	public AttributeViewModel AttributeViewModel
	{
		get => _attributeViewModel;
		set
		{
			_attributeViewModel = value;
			FileName += value.SelectedAttr.AttributeValue;
			OnPropertyChanged();
		}
	}

	public RelayCommand AddCommand
	{
		get
		{
			return addCommand ??
			       (addCommand = new RelayCommand(obj => { _numberingValue = NumberingValue; }));
		}
	}


	public event PropertyChangedEventHandler PropertyChanged;

	private void GetFileName()
	{
		//BlockAttribute.AttributeName
		var caption = "Word Processor";
		var button = MessageBoxButton.OK;
		var icon = MessageBoxImage.Warning;
		MessageBoxResult result;


		result = MessageBox.Show("fname.ToString()", caption, button, icon, MessageBoxResult.Yes);
	}

	private void SetFileName()
	{
	}

	private string SetNumberingValue()
	{
		if (_numberingValue != 0) return NumberingValue.ToString();
		return "";
	}

	[NotifyPropertyChangedInvocator]
	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		//SetFileName();
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}


	#region Props

	public BlockAttribute BlockAttribute
	{
		get => _attributeViewModel.SelectedAttr;
		set
		{
			_attributeViewModel.SelectedAttr = value;

			FileName = Prefix + value.AttributeValue + SetNumberingValue() + Suffix + PdfExt;
			OnPropertyChanged();
		}
	}

	public List<BlockAttribute> BlockAttributes { get; set; }

	public bool IsCheckedAttribute
	{
		get => _isCheckedAttribute;
		set
		{
			_isCheckedAttribute = value;
			if (value)
				FileName = Prefix + BlockAttribute?.AttributeValue + SetNumberingValue() + Suffix + PdfExt;
			else FileName = Prefix + SetNumberingValue() + Suffix + PdfExt;
			OnPropertyChanged();
		}
	}

	public bool IsCheckedNumbering
	{
		get => _isCheckedNumbering;
		set
		{
			_isCheckedNumbering = value;
			if (IsCheckedAttribute)
				FileName = Prefix + BlockAttribute?.AttributeValue + Suffix + PdfExt;
			else FileName = Prefix + Suffix + PdfExt;
			if (value == false) _numberingValue = 0;
			OnPropertyChanged(nameof(NumberingValue));
			OnPropertyChanged();
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
			if (_isCheckedNumbering)
			{
				_numberingValue = value;
				FileName = Prefix + BlockAttribute?.AttributeValue + value + Suffix + PdfExt;
			}
			else
			{
				_numberingValue = 0;
			}

			OnPropertyChanged();
		}
	}

	public string Suffix
	{
		get => _suffix;
		set
		{
			_suffix = value;

			FileName = Prefix + BlockAttribute?.AttributeValue + SetNumberingValue() + value + PdfExt;
			OnPropertyChanged();
		}
	}

	public string Prefix
	{
		get => _prefix;
		set
		{
			_prefix = value;
			FileName = value + BlockAttribute?.AttributeValue + SetNumberingValue() + Suffix + PdfExt;
			OnPropertyChanged();
		}
	}

	#endregion
}

public class CommandHandler : ICommand
{
	private readonly Action _action;
	private readonly Func<bool> _canExecute;

	/// <summary>
	///     Creates instance of the command handler
	/// </summary>
	/// <param name="action">Action to be executed by the command</param>
	/// <param name="canExecute">A bolean property to containing current permissions to execute the command</param>
	public CommandHandler(Action action, Func<bool> canExecute)
	{
		_action = action;
		_canExecute = canExecute;
	}

	/// <summary>
	///     Wires CanExecuteChanged event
	/// </summary>
	public event EventHandler CanExecuteChanged
	{
		add => CommandManager.RequerySuggested += value;
		remove => CommandManager.RequerySuggested -= value;
	}

	/// <summary>
	///     Forcess checking if execute is allowed
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