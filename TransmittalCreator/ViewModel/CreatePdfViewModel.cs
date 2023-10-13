using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using TransmittalCreator.Annotations;
using TransmittalCreator.Models;

namespace TransmittalCreator.ViewModel
{
	//public class CreatePdfViewModel :INotifyPropertyChanged
	//{
	//	private BlockAttribute _selectedAttr;
	//	public ObservableCollection<BlockAttribute> Attributes { get; set; }

	//	public CreatePdfViewModel()
	//	{
	//		Attributes = new ObservableCollection<BlockAttribute>();
	//	}
	//	public CreatePdfViewModel(ObservableCollection<BlockAttribute> attributes)
	//	{
	//		Attributes = attributes;
	//	}

	//	public BlockAttribute SelectedAttr
	//	{
	//		get => _selectedAttr;
	//		set
	//		{
	//			_selectedAttr = value;
	//			OnPropertyChanged("SelectedAttr");
	//		}
	//	}

	//	public event PropertyChangedEventHandler PropertyChanged;

	//	[NotifyPropertyChangedInvocator]
	//	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	//	{
	//		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	//	}

	//	private RelayCommand createPdfCommand;
	//	public RelayCommand CreatePdfCommand
	//	{
	//		get
	//		{
	//			return createPdfCommand ??
	//			       (createPdfCommand = new RelayCommand(obj =>
	//			       {
	//				       _selectedAttr = SelectedAttr;
	//			       }));
	//		}
	//	}

	//	private ICommand _clickCommand;
	//	public ICommand ClickCommand
	//	{
	//		get
	//		{
	//			return _clickCommand ?? (_clickCommand = new CommandHandler(() => GetFileName(), () => CanExecute));
	//		}
	//	}

	//	public bool CanExecute =>
	//		// check if executing is allowed, i.e., validate, check if a process is running, etc. 
	//		true;

	//	private void GetFileName()
	//	{
	//		string caption = "Word Processor";
	//		MessageBoxButton button = MessageBoxButton.OK;
	//		MessageBoxImage icon = MessageBoxImage.Warning;
	//		MessageBoxResult result;

	//		result = MessageBox.Show(SelectedAttr.AttributeName, caption, button, icon, MessageBoxResult.Yes);
	//	}


	//}
}
