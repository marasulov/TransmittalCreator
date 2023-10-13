using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using TransmittalCreator.ViewModels.Enums;
using TransmittalCreator.ViewModels.ViewModels;


namespace TransmittalCreator.ViewModels
{
	public class CreatePdfVm : ViewModel, ICreatePdfVm
	{
		private BlockAttribute _selectedAttr;
		private bool _canExecute;
		private ICommand _printPagesCommand;
		private bool _buttonClicked;
		private string _fileName;
		private string _suffix;
		private string _prefix;
		private int _renumberStartValue;
		private bool _isCheckedNumbering;
		private Ordering _ordering = Ordering.IsSelectedOrder;

		public CreatePdfVm()
		{
			PrintPagesCommand = new RelayCommand(CloseWindow, param => _canExecute);
		}

		public bool CanClose()
		{
			if (SelectedAttr != null | IsCheckedNumbering)
			{
				_canExecute = true;
				return true;
			}

			_canExecute = false;
			return false;
		}


		public Ordering Ordering
		{
			get => _ordering;
			set
			{
				if (_ordering == value)
					return;
				_ordering = value;
				OnPropertyChanged();
				OnPropertyChanged("IsX");
				OnPropertyChanged("IsY");
				OnPropertyChanged("IsSelectedOrder");
			}
		}

		public bool IsX
		{
			get => Ordering == Ordering.IsX;
			set => Ordering = value ? Ordering.IsX : Ordering;
		}

		public bool IsY
		{
			get => Ordering == Ordering.IsY;
			set => Ordering = value ? Ordering.IsY : Ordering;
		}

		public bool IsSelectedOrder
		{
			get => Ordering == Ordering.IsSelectedOrder;
			set => Ordering = value ? Ordering.IsSelectedOrder : Ordering;
		}

		public ObservableCollection<BlockAttribute> Attributes { get; set; }

		public BlockAttribute SelectedAttr
		{
			get => _selectedAttr;
			set => Set(ref _selectedAttr, value);
		}

		/// <summary>
		/// Нумерация листов с
		/// </summary>
		public int RenumberStartValue
		{
			get => _renumberStartValue;
			set
			{
				Set(ref _renumberStartValue, value);
				FileName = Prefix + value + Suffix;
			}
		}

		public bool RenumberLayouts { get; set; }

		public bool InsertTotalPages
		{
			get => _insertTotalPages;
			set => Set(ref _insertTotalPages, value);
		}

		/// <summary>
		/// Команда для печати
		/// </summary>
		public ICommand PrintPagesCommand
		{
			get
			{
				if (_printPagesCommand == null)
				{
					_printPagesCommand = new RelayCommand(
						p => this.CanClose());
				}

				return _printPagesCommand;
			}
			set => Set(ref _printPagesCommand, value);
		}

		public string FileName
		{
			get => _fileName;
			set => Set(ref _fileName, value);
		}

		public string Prefix
		{
			get => _prefix;
			set
			{
				Set(ref _prefix, value);
				FileName = value + RenumberStartValue + Suffix;
			}
		}

		public string Suffix
		{
			get => _suffix;
			set
			{
				Set(ref _suffix, value);
				FileName = Prefix + RenumberStartValue + value;
			}
		}

		public bool IsAttributeChecked { get; set; }

		public bool IsCheckedNumbering
		{
			get => _isCheckedNumbering;
			set => Set(ref _isCheckedNumbering, value);
		}

		public bool ButtonClicked
		{
			get => _buttonClicked;
			set => Set(ref _buttonClicked, value);
		}

		public event PropertyChangedEventHandler PropertyChanged;


		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private bool _insertTotalPages;
		

		public void CloseWindow(object parameter)
		{
			string clicked = parameter as string;
			if (clicked != "Clicked") return;
			_buttonClicked = true;
			OnRequestClose(this, new EventArgs());
		}

		public event EventHandler OnRequestClose;
	}
}
