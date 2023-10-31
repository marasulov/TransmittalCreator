using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TestWPF.Models;
using TransmittalCreator.ViewModels;
using TransmittalCreator.ViewModels.Models;


namespace TestWPF.ViewModel
{
	
	public partial class MainViewModel : ObservableObject
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
				OnPropertyChanged();

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
				OnPropertyChanged();
			}
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
