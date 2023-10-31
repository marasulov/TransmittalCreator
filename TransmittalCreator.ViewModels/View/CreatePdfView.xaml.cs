using System.Windows;


namespace TransmittalCreator.ViewModels.View;

/// <summary>
///     Логика взаимодействия для PdfCreatorView.xaml
/// </summary>
public partial class CreatePdfView : Window
{
	public CreatePdfView(CreatePdfVm pdfCreatorViewModel)
	{
		InitializeComponent();

		DataContext = pdfCreatorViewModel;
		Loaded += On_Loaded;
	}

	private void On_Loaded(object sender, RoutedEventArgs e)
	{
		Style = (Style)FindResource("WindowElementStyle");
	}
}