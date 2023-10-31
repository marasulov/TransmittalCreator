using System.ComponentModel;
using System.Runtime.CompilerServices;
using TransmittalCreator.ViewModels.Annotations;


namespace TransmittalCreator.ViewModels.Base
{
	public abstract class ViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public virtual bool Set<T>(ref T field, T value, [CallerMemberName] string MemberName = null)
		{
			if (Equals(field, value))
			{
				return false;
			}

			field = value;
			OnPropertyChanged(MemberName);
			return true;
		}
	}
}
