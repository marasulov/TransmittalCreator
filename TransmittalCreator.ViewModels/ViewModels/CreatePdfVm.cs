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

		public CreatePdfVm()
		{
			PrintPagesCommand = new RelayCommand(CloseWindow, canExecute=>CanClose());
		}

		public bool IsPrintAll
		{
			get => _isPrintingAll;
			set => Set(ref _isPrintingAll, value);
		}


		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
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

		/// <summary>
		/// Все атрибуты
		/// </summary>
		public ObservableCollection<BlockAttribute> Attributes { get; set; }

		/// <summary>
		/// Выбранный атрибут
		/// </summary>
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
				//FileName = Prefix + value + Suffix;
				//OnPropertyChanged("FileName");

				if (IsCheckedNumbering)
				{
					if (IsAttributeChecked & SelectedAttr != null)
					{
						FileName = Prefix + SelectedAttr.AttributeValue +  value + Suffix;
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
					FileName = Prefix + RenumberStartValue + value;
				}
				
				else
				{
					FileName = Prefix + value;
				}
				OnPropertyChanged("FileName");
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

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
