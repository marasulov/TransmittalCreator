using System.ComponentModel;
using System.Runtime.CompilerServices;
using TransmittalCreator.Annotations;

namespace TransmittalCreator.Models;

public class FileModel : INotifyPropertyChanged
{
	private string fileName;

	public string FileName
	{
		get => fileName;
		set
		{
			fileName = value;
			OnPropertyChanged();
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	[NotifyPropertyChangedInvocator]
	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}