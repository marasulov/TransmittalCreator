using System.ComponentModel;
using TransmittalCreator.Infrastructure.Commands.Base;
using TransmittalCreator.ViewModels;

namespace TransmittalCreator.Infrastructure.Commands
{
	public class RenumberPagesCommand : BaseCommand
	{
		private readonly RenumberPagesViewModel _renumberPages;

		public RenumberPagesCommand(RenumberPagesViewModel renumberPages)
		{
			_renumberPages = renumberPages;
		}

		public override bool CanExecute(object? parameter)
		{
			return true;
		}

		public override void Execute(object? parameter)
		{
			
		}

		private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			OnCanExecuteChanged();
		}
	}
}
