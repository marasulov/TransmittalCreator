using System.Collections.Generic;
using TransmittalCreator.ViewModels.Models;


namespace TestWPF.ViewModel
{
	
	public partial class MainViewModel 
	{
		private bool _isChecked;
		private BlockAttribute  _selectedBlockAttribute;
		public MainViewModel()
		{
			Attributes = new List<BlockAttribute>
			{
				new BlockAttribute("Attr1", "val1"),
				new BlockAttribute("Attr12", "val2"),
				new BlockAttribute("Attr13", "val3"),
			};
		}

		public List<BlockAttribute> Attributes { get; set; }

		public bool IsAttributeChecked
		{
			get => _isChecked;
			set
			{
				if(_isChecked==value) return;
				_isChecked = value;
				

				if(!value) SelectedAttr = null;
			}
		}


		public BlockAttribute SelectedAttr
		{
			get => _selectedBlockAttribute;
			set
			{
				if (_selectedBlockAttribute == value)
				{
					return;
				}
				_selectedBlockAttribute = value;

			}
		}


        private string firstname;

		
		private string lastname;

	}
}
