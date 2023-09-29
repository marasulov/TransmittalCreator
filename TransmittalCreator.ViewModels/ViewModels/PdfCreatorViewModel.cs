using System.Collections.Generic;
using System.Windows.Input;

namespace TransmittalCreator.ViewModels
{
	public class PdfCreatorViewModel : ViewModel
	{
		private BlockAttribute _selectedAttr;
		public List<BlockAttribute> Attributes { get; set; }

		public PdfCreatorViewModel()
		{
			Attributes = new List<BlockAttribute>();
		}
		public PdfCreatorViewModel(List<BlockAttribute> attributes)
		{
			Attributes = attributes;
		}

		public BlockAttribute SelectedAttr
		{
			get => _selectedAttr;
			set
			{
				_selectedAttr = value;
				OnPropertyChanged("SelectedAttr");
			}
		}

		private ICommand _clickCommand;
		public ICommand ClickCommand
		{
			get
			{
				return _clickCommand ?? (_clickCommand = new CommandHandler(() => GetFileName(), () => CanExecute));
			}
		}

		public bool CanExecute =>
			// check if executing is allowed, i.e., validate, check if a process is running, etc. 
			true;

		private string GetFileName()
		{
			return SelectedAttr.AttributeName;
		}

	}
}
