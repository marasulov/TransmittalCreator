using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TransmittalCreator.Models;

namespace TransmittalCreator.ViewModel;

public class AttributeViewModel : INotifyPropertyChanged
{
	private BlockAttribute _selectedAttr;

	private RelayCommand addCommand;

	public AttributeViewModel( /*Dictionary<string, string> attributes*/)
	{
		Attributes = new ObservableCollection<BlockAttribute>();
	}

	public ObservableCollection<BlockAttribute> Attributes { get; set; }

	public BlockAttribute SelectedAttr
	{
		get => _selectedAttr;
		set
		{
			_selectedAttr = value;
			OnPropertyChanged();
		}
	}

	public RelayCommand AddCommand
	{
		get
		{
			return addCommand ??
			       (addCommand = new RelayCommand(obj => { _selectedAttr = SelectedAttr; }));
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public BlockAttribute GetSelectedAttribute()
	{
		return _selectedAttr;
	}
}