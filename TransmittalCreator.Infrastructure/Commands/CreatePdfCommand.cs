using System;
using System.Windows.Input;
using TransmittalCreator.Infrastructure.Commands.Base;
using TransmittalCreator.ViewModels;

namespace TransmittalCreator.Infrastructure.Commands
{
	public class CreatePdfCommand : BaseCommand
	{
		private readonly CreatePdfVm _viewModel;

		public override void Execute(object? parameter)
		{
			throw new NotImplementedException();
		}

		//public override void Execute(object? parameter) => 

		private ICommand _clickCommand;
		

		private void GetFileName()
		{
			throw new NotImplementedException();
		}
	}
}
