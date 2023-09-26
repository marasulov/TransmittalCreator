using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TransmittalCreator.Infrastructure.Commands.Base
{
	public abstract class Command : ICommand
	{
		public abstract bool CanExecute(object? parameter);

		public abstract void Execute(object? parameter);

		public event EventHandler? CanExecuteChanged
		{
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}
	}
}
