using System;
using System.Windows;
using System.Windows.Input;
using TransmittalCreator.ViewModels;

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

		/// <summary>
		/// Нумерация листов с
		/// </summary>
		public int RenumberStartValue
		{
			get => _enumerationStartValue;
			set => Set(ref _enumerationStartValue, value);
		}

		/// <summary>
		/// Нумеровать Layouts
		/// </summary>
		public bool RenumberLayouts
		{
			get => _enumerateLayouts;
			set=> Set(ref _enumerateLayouts, value);
		}

		/// <summary>
		/// Вставить количество листов в штамп
		/// </summary>
		public bool InsertTotalPages
		{
			get => _insertTotalPages;
			set => Set(ref _insertTotalPages, value);
		}
		
		

		//public ICommand RenumberPagesCommand { get; }
		

		public ICommand RenumberPagesCommand
		{
			get => _renumberPagesCommand;
			set => Set(ref _renumberPagesCommand, value);
		}

		public bool CanExecute
		{
			get
			{
				return this._canExecute;
			}

			set
			{
				if (this._canExecute == value)
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
				//MessageBox.Show($"передать параметр {clicked}");
				_buttonClicked = true;
				OnRequestClose(this, new EventArgs());
			}

			//MessageBox.Show($"{_enumerationStartValue}");
		}

		public event EventHandler OnRequestClose;

	}
}
