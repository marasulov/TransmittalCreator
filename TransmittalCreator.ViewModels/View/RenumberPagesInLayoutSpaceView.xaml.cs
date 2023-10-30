using System.Windows;
using TransmittalCreator.ViewModels;

namespace TransmittalCreator.View;

/// <summary>
///     Логика взаимодействия для EnumeratePagesView.xaml
/// </summary>
public partial class RenumberPagesInLayoutSpaceView : Window
{
	public RenumberPagesInLayoutSpaceView(RenumberPagesViewModel renumberPages)
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