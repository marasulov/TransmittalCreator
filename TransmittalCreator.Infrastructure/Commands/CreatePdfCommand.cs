using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TransmittalCreator.Infrastructure.Commands.Base;

namespace TransmittalCreator.Infrastructure.Commands
{
	public class CreatePdfCommand : BaseCommand
	{
		//private readonly 
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
