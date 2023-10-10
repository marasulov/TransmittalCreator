using System.Windows;
using TransmittalCreator.ViewModels.ViewModels;

namespace TransmittalCreator.View
{
	/// <summary>
	/// Логика взаимодействия для EnumeratePagesView.xaml
	/// </summary>
	public partial class RenumberPagesInModelSpaceView : Window
	{
		public RenumberPagesInModelSpaceView(RenumberPagesInModelSpaceViewModel renumberPages)
		{
			InitializeComponent();

			DataContext = renumberPages;
			Loaded += On_Loaded;
		}

		private void On_Loaded(object sender, RoutedEventArgs e)
		{
			Style = (Style)FindResource("WindowElementStyle");
		}
	}
}
