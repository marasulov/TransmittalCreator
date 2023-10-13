using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Prism.Commands;


namespace WpfAppForTest.App
{

	public class AttributeViewModel : ICloseWindow, INotifyPropertyChanged
	{
		private BlockAttribute _selectedAttr;
		public ObservableCollection<BlockAttribute> Attributes { get; set; }
		public BlockAttribute SelectedAttr
		{
			get => _selectedAttr;
			set
			{
				_selectedAttr = value;
				OnPropertyChanged();
			}
		}


		private RelayCommand addCommand;

		public RelayCommand AddCommand => new RelayCommand(execute => AddItem(), canExecute => IsEmptyPrefix());

		private void AddItem()
		{
			MessageBox.Show("Add item");
		}

		public string Prefix
		{
			get => _prefix;
			set
			{
				_prefix = value;
				OnPropertyChanged();
				//Set(ref _prefix, value);

			}
		}

		public string Suffix
		{
			get => _suffix;
			set
			{
				_suffix = value;
				OnPropertyChanged();
				//Set(ref _prefix, value);

			}
		}

		private bool IsEmptyPrefix()
		{
			if (string.IsNullOrEmpty(Prefix) | string.IsNullOrEmpty(Suffix) | SelectedAttr == null)
				return false;
			return true;
		}



		//public RelayCommand AddCommand
		//{
		//	get
		//	{
		//		return addCommand ??
		//			   (addCommand = new RelayCommand(obj =>
		//			   {
		//				   _selectedAttr = SelectedAttr;
		//			   }));
		//	}
		//}

		public AttributeViewModel()
		{
			Attributes = new ObservableCollection<BlockAttribute>
			{
				new BlockAttribute{AttributeName = "name1",AttributeValue = "value1"},
				new BlockAttribute{AttributeName = "name2",AttributeValue = "value2"},
				new BlockAttribute{AttributeName = "name3",AttributeValue = "value3"},
			};
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public BlockAttribute GetSelectedAttribute()
		{
			return _selectedAttr;
		}

		[ObservableProperty]
		private int numberingValue = 2;

		[ObservableProperty]
		private string name = "asdsad";

		//public string Name
		//{
		//	get => name;
		//	set => SetProperty(ref name, value);
		//}

		//IDialogService _closeCommand = new DialogService();

		private DelegateCommand _closeWindow;
		private string _prefix;
		private string _suffix;

		public DelegateCommand CloseWindow =>
			_closeWindow ?? (_closeWindow = new DelegateCommand(ExecuteShowDialog));

		private void ExecuteShowDialog()
		{
			Close?.Invoke();
		}

		public Action Close { get; set; }
		public void Load()
		{

		}
	}

	interface ICloseWindow
	{
		Action Close { get; set; }
	}
}
