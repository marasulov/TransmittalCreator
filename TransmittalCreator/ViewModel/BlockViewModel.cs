using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using TransmittalCreator.Models;

namespace TransmittalCreator.ViewModel;

public class BlockViewModel
{
	private BlockAttribute _selectedBlockAttribute;

	private RelayCommand addCommand;

	public BlockViewModel(Dictionary<string, string> blocks)
	{
		foreach (var block in blocks) new SpesSheetModel(block.Key, block.Value);
	}

	public ObservableCollection<BlockAttribute> Blocks { get; set; }

	public RelayCommand AddCommand
	{
		get
		{
			return addCommand ??
			       (addCommand = new RelayCommand(obj => { MessageBox.Show("Test"); }));
		}
	}

	public BlockAttribute SelectedBlock
	{
		get => _selectedBlockAttribute;
		set
		{
			_selectedBlockAttribute = value;
			OnPropertyChanged();
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	/// <summary>
	///     Raises OnPropertychangedEvent when property changes
	/// </summary>
	/// <param name="name">String representing the property name</param>
	protected void OnPropertyChanged([CallerMemberName] string name = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}