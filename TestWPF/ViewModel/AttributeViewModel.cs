using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestWPF.ViewModel
{
	public class AttributeViewModel
	{
		public ObservableCollection<string> CmbContent { get; private set; }

		public AttributeViewModel()
		{
			CmbContent = new ObservableCollection<string>
			{
				"test 1", 
				"test 2"
			};
		}
	}
}
