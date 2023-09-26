using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;

namespace WpfAppTest
{
	public class MainWindowViewModel : ICloseWindow, INotifyPropertyChanged
	{
		private Phone selectedPhone;
 
		public ObservableCollection<Phone> Phones { get; set; }
		public Phone SelectedPhone
		{
			get { return selectedPhone; }
			set
			{
				selectedPhone = value;
				OnPropertyChanged("SelectedPhone");
			}
		}
 
		public MainWindowViewModel()
		{
			Phones = new ObservableCollection<Phone>
			{
				new Phone { Title="iPhone 7", Company="Apple", Price=56000 },
				new Phone {Title="Galaxy S7 Edge", Company="Samsung", Price =60000 },
				new Phone {Title="Elite x3", Company="HP", Price=56000 },
				new Phone {Title="Mi5S", Company="Xiaomi", Price=35000 }
			};
		}
 
		public event PropertyChangedEventHandler PropertyChanged;
		public void OnPropertyChanged([CallerMemberName]string prop = "")
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(prop));
		}



		private DelegateCommand _closeWindow;

		public DelegateCommand CloseWindow =>
			_closeWindow ?? (_closeWindow = new DelegateCommand(ExecuteShowDialog));

		private void ExecuteShowDialog()
		{
			Close ?.Invoke();
		}

		public Action Close { get; set; }

		public bool CanClose()
		{
			return false;
		}
	}

	public interface ICloseWindow
	{
		Action Close { get; set; }
		bool CanClose();
	}

	public class WindowCloser
	{
		public static bool GetEnableWindowClosing(DependencyObject obj)
		{
			return (bool)obj.GetValue(EnableWindowClosingProperty);
		}

		public static void SetEnableWindowClosing(DependencyObject obj, bool value)
		{
			obj.SetValue(EnableWindowClosingProperty, value);
		}

		public static readonly DependencyProperty EnableWindowClosingProperty = 
			DependencyProperty.RegisterAttached("EnableWindowClosing", typeof(bool), typeof(WindowCloser),
				new PropertyMetadata(false, OnEnableWindowClosingChanged));

		private static void OnEnableWindowClosingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is Window window)
			{
				window.Loaded += (s, e) =>
				{
					if (window.DataContext is ICloseWindow vm)
					{
						vm.Close += () =>
						{
							window.Close();
						};

						window.Closing += (s, e) =>
						{
							e.Cancel = !vm.CanClose();
						};
					}
				};
			}
		}

		private static void  OnEnableWindowClosingChanged()
		{
			throw new NotImplementedException();
		}
	}
}
