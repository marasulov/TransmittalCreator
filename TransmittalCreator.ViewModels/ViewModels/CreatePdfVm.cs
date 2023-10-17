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
		private int _renumberStartValue;
		private bool _isCheckedNumbering;
		private bool _insertTotalPages;
		private Ordering _ordering = Ordering.IsSelectedOrder;
		private bool _isPrintingAll;
		private int _comboboxSelectedIndex = -1;
		private bool _isAttributeChecked;
		private string _selectedItem;
		private bool _isChecked;
		private BlockAttribute _selectedBlockAttribute;

		public CreatePdfVm()
		{
			//PrintPagesCommand = new RelayCommand(CloseWindow, canExecute => CanClose());
			Attributes = new List<BlockAttribute>
			{
				new BlockAttribute("Attrname1", "Attrvalue1"),
				new BlockAttribute("Attrname2", "Attrvalue2"),
				new BlockAttribute("Attrname3", "Attrvalue3"),
				new BlockAttribute("Attrname4", "Attrvalue4"),
				new BlockAttribute("Attrname5", "Attrvalue5")
			};
		}

		//public bool IsChecked
		//{
		//	get => _isChecked;
		//	set
		//	{
		//		if (_isChecked == value) return;
		//		_isChecked = value;
		//		OnPropertyChanged();

		//		if (!value) SelectedBlockAttribute = null;
		//	}
		//}


		//public BlockAttribute SelectedBlockAttribute
		//{
		//	get => _selectedBlockAttribute;
		//	set
		//	{
		//		if (_selectedBlockAttribute == value)
		//		{
		//			return;
		//		}
		//		_selectedBlockAttribute = value;
		//		OnPropertyChanged();
		//	}
		//}

		/// <summary>
		/// Все атрибуты
		/// </summary>
		public List<BlockAttribute> Attributes { get; set; }

		public bool IsAttributeChecked
		{
			get => _isAttributeChecked;
			set
			{
				if(_isAttributeChecked == value) 
					return;
				_isAttributeChecked = value;
				OnPropertyChanged();

				if (!value) SelectedAttr = null;
				//if (Set(ref _isAttributeChecked, value))
				//{
				//	if (!_isAttributeChecked)
				//	{
				//		SelectedAttr = null;
				//	}
				//}

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
				if(_selectedAttr == value) return;
				_selectedAttr = value; OnPropertyChanged();
				//var oldItem = _selectedAttr;
				//_selectedAttr = value;
				//OnPropertyChanged();

				//if (!IsAttributeChecked) //If does not satisfy condition, set item back to old item
				//	Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => SelectedAttr = oldItem),
				//		DispatcherPriority.ApplicationIdle);
			}
		}


		public bool IsCheckedNumbering
		{
			get => _isCheckedNumbering;
			set
			{
				Set(ref _isCheckedNumbering, value);
				//{
				//	if (!_isCheckedNumbering)
				//	{
				//		RenumberStartValue = 0;
				//	}
				//}


			}
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
				//FileName = Prefix + value + Suffix;
				//OnPropertyChanged("FileName");

				if (IsCheckedNumbering)
				{
					if (IsAttributeChecked & SelectedAttr != null)
					{
						FileName = Prefix + SelectedAttr.AttributeValue + value + Suffix;
					}
					else
					{
						FileName = Prefix + value + Suffix;
					}

				}
				else
				{
					if (IsAttributeChecked & SelectedAttr != null)
					{
						FileName = Prefix + SelectedAttr.AttributeValue + Suffix;
					}
					else
					{
						FileName = Prefix + Suffix;
					}

				}
				OnPropertyChanged("FileName");

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
				OnPropertyChanged("IsX");
				OnPropertyChanged("IsY");
				OnPropertyChanged("IsSelectedOrder");
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
				if (IsCheckedNumbering)
				{
					FileName = value + RenumberStartValue + Suffix;
				}
				else
				{
					FileName = value + Suffix;
				}
				OnPropertyChanged("FileName");
			}
		}

		public string Suffix
		{
			get => _suffix;
			set
			{
				Set(ref _suffix, value);
				if (IsCheckedNumbering)
				{
					FileName = Prefix + SelectedAttr.AttributeValue + RenumberStartValue + value;
				}

				else
				{
					FileName = Prefix + value;
				}
				OnPropertyChanged("FileName");
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
