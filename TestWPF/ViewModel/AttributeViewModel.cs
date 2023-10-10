using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TestWPF.ViewModel
{
	[ObservableObject]
	public partial class AttributeViewModel
	{

		public AttributeViewModel()
		{
		}

		public void Load()
		{
			
		}

		[ObservableProperty] 
		private string firstname;
	}
}
