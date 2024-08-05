using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WpfAppForTest.App
{
	public class DialogService : IDialogService
	{
		public void ShowDialog()
		{
			var dialog = new DialogWindow();
			dialog.ShowDialog();
		}
	}

	public interface IDialogService
	{
		void ShowDialog();
	}
}
