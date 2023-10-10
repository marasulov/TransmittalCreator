using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TestWPF.ViewModel
{
	[ObservableObject]
	public partial class MainViewModel
	{
		public MainViewModel()
		{

		}

		public void Load()
		{
			
			
		}

		[ObservableProperty]
		private string firstname;

		[ObservableProperty]
		private string lastname;

	}
}
