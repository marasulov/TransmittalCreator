using System;
using System.Windows.Input;

namespace TransmittalCreator.ViewModels
{
	public class RenumberPagesViewModel : ViewModel
	{
		private int _enumerationStartValue = 1;
		private bool _enumerateLayouts;
		private bool _insertTotalPages;
		private bool _canExecute = true;
		private ICommand _renumberPagesCommand;
		private bool _buttonClicked;
		private bool _deleteEmptyLayouts;
		private string _pageName;
		private string _suffix;
		private string _prefix;

		/// <summary>
		/// Нумерация листов с
		/// </summary>
		public int RenumberStartValue
		{
			get => _enumerationStartValue;
			set
			{
				Set(ref _enumerationStartValue, value);
				PageName = Prefix + value + Suffix;
			}
		}

		/// <summary>
		/// Нумеровать Layouts
		/// </summary>
		public bool RenumberLayouts
		{
			get => _enumerateLayouts;
			set => Set(ref _enumerateLayouts, value);
		}

		/// <summary>
		/// Вставить количество листов в штамп
		/// </summary>
		public bool InsertTotalPages
		{
			get => _insertTotalPages;
			set => Set(ref _insertTotalPages, value);
		}

		/// <summary>
		/// Удалить листы без блока штампа
		/// </summary>
		public bool DeleteEmptyLayouts
		{
			get => _deleteEmptyLayouts;
			set => Set(ref _deleteEmptyLayouts, value);
		}

		/// <summary>
		/// Команда для перенумерования
		/// </summary>
		public ICommand RenumberPagesCommand
		{
			get => _renumberPagesCommand;
			set => Set(ref _renumberPagesCommand, value);
		}

		public string PageName
		{
			get => _pageName;
			set => Set(ref _pageName, value);
		}

		public string Prefix
		{
			get => _prefix;
			set
			{
				Set(ref _prefix, value);
				PageName = value + RenumberStartValue + Suffix;
			}
		}

		public string Suffix
		{
			get => _suffix;
			set
			{
				Set(ref _suffix, value);
				PageName = Prefix + RenumberStartValue + value;
			}
		}

		public bool CanExecute
		{
			get => this._canExecute;

			set
			{
				if (_canExecute == value)
				{
					return;
				}

				this._canExecute = value;
			}
		}

		public bool ButtonClicked
		{
			get => _buttonClicked;
			set => Set(ref _buttonClicked, value);
		}

		public RenumberPagesViewModel()
		{
			RenumberPagesCommand = new RelayCommand(CloseWindow, param => this._canExecute);
		}

		public void CloseWindow(object parameter)
		{
			string clicked = parameter as string;
			if (clicked == "Hai")
			{
				_buttonClicked = true;
				OnRequestClose(this, new EventArgs());
			}
		}

		public event EventHandler OnRequestClose;

	}
}
