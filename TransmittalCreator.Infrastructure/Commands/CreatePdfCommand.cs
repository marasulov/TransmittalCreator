using System;
using System.ComponentModel;
using System.Windows.Input;
using TransmittalCreator.Infrastructure.Commands.Base;
using TransmittalCreator.ViewModels.ViewModels;

namespace TransmittalCreator.Infrastructure.Commands
{
	public class CreatePdfCommand : BaseCommand
	{
		private readonly ICreatePdfVm _viewModel;

		public CreatePdfCommand(ICreatePdfVm viewModel)
		{
			_viewModel = viewModel;
			
			
		}

		public virtual bool CanExecute(object parameter)
		{
			return true; // _viewModel.IsValidPrinting;
		}

		public override void Execute(object? parameter)
		{
			
		}

		//public override void Execute(object? parameter) => 

		private ICommand _clickCommand;
		

		private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			OnCanExecuteChanged();
		}
	}
}
