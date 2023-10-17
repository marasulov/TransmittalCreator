using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using TransmittalCreator.ViewModels.Annotations;
using TransmittalCreator.ViewModels.Enums;
using TransmittalCreator.ViewModels.ViewModels;


namespace TransmittalCreator.ViewModels
{
	public class CreatePdfVm : ViewModel, ICreatePdfVm
	{
		private BlockAttribute _selectedAttr;

		private ICommand _printPagesCommand;
		private bool _buttonClicked;
		private string _fileName;
		private string _suffix;
		private string _prefix;
		private string _renumberStartValue;
		private bool _isCheckedNumbering;
		private bool _insertTotalPages;
		private Ordering _ordering = Ordering.IsSelectedOrder;
		private bool _isAttributeChecked;
		private string _selectedItem;
		private bool _isChecked;
		private BlockAttribute _selectedBlockAttribute;

		public CreatePdfVm()
		{
			PrintPagesCommand = new RelayCommand(CloseWindow, canExecute => CanClose());
		}

		/// <summary>
		/// Все атрибуты
		/// </summary>
		public List<BlockAttribute> Attributes { get; set; }

		public bool IsAttributeChecked
		{
			get => _isAttributeChecked;
			set
			{
				if (Set(ref _isAttributeChecked, value))
				{
					if (!_isAttributeChecked)
					{
						SelectedAttr = null;
					}
				}
			}
		}

		/// <summary>
		/// Выбранный атрибут
		/// </summary>
		public BlockAttribute SelectedAttr
		{
			get => _selectedAttr;
			//set => Set(ref _selectedAttr, value);
			set
			{
				Set(ref _selectedAttr, value);
				FileName = Prefix + value?.AttributeValue + RenumberStartValue + Suffix;
				OnPropertyChanged(nameof(FileName));
				//if(_selectedAttr == value) return;
				//_selectedAttr = value; OnPropertyChanged();
			}
		}

		public bool IsCheckedNumbering
		{
			get => _isCheckedNumbering;
			set
			{

				if (Set(ref _isCheckedNumbering, value))
				{
					if (!_isCheckedNumbering)
					{
						RenumberStartValue = null;
					}
				}


			}
		}

		/// <summary>
		/// Нумерация листов с
		/// </summary>
		public string RenumberStartValue
		{
			get => _renumberStartValue;
			set
			{
				Set(ref _renumberStartValue, value);
				FileName = Prefix + SelectedAttr?.AttributeValue + value + Suffix;
				OnPropertyChanged(nameof(FileName));
			}
		}

		public bool CanClose()
		{
			return (IsAttributeChecked & SelectedAttr != null) | IsCheckedNumbering;
		}

		/// <summary>
		/// Очередь печати 
		/// </summary>
		public Ordering Ordering
		{
			get => _ordering;
			set
			{
				if (_ordering == value)
					return;
				_ordering = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(IsX));
				OnPropertyChanged(nameof(IsY));
				OnPropertyChanged(nameof(IsSelectedOrder));
			}
		}

		/// <summary>
		/// Очередь печати по расположение X
		/// </summary>
		public bool IsX
		{
			get => Ordering == Ordering.IsX;
			set => Ordering = value ? Ordering.IsX : Ordering;
		}

		/// <summary>
		/// Очередь печати по расположение Y
		/// </summary>
		public bool IsY
		{
			get => Ordering == Ordering.IsY;
			set => Ordering = value ? Ordering.IsY : Ordering;
		}

		/// <summary>
		/// Очередь печати по выбору
		/// </summary>
		public bool IsSelectedOrder
		{
			get => Ordering == Ordering.IsSelectedOrder;
			set => Ordering = value ? Ordering.IsSelectedOrder : Ordering;
		}
		public bool RenumberLayouts { get; set; }

		public bool InsertTotalPages
		{
			get => _insertTotalPages;
			set => Set(ref _insertTotalPages, value);
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
				FileName = value + SelectedAttr?.AttributeValue + RenumberStartValue + Suffix;
				OnPropertyChanged(nameof(FileName));
			}
		}

		public string Suffix
		{
			get => _suffix;
			set
			{
				Set(ref _suffix, value);
				FileName = Prefix + SelectedAttr?.AttributeValue + RenumberStartValue + value;
				OnPropertyChanged(nameof(FileName));
			}
		}

		public bool ButtonClicked
		{
			get => _buttonClicked;
			set => Set(ref _buttonClicked, value);
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
