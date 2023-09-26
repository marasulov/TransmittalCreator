using System.Collections.Generic;
using System.Windows;
using TransmittalCreator.ViewModel;

namespace TransmittalCreator.View
{
	/// <summary>
	/// Логика взаимодействия для PdfCreatorView.xaml
	/// </summary>
	public partial class PdfCreatorView : Window
	{
		public Dictionary<string,string> BlockAttributes { get; set; }
		public PdfCreatorView(PdfCreatorViewModel pdfCreatorViewModel)
		{
			InitializeComponent();

			//AttributeViewModel attributeViewModel = new AttributeViewModel();

			//comboAttributs.ItemsSource = blockAttributes;
			comboAttributs.ItemsSource = pdfCreatorViewModel.Attributes;
			DataContext = pdfCreatorViewModel;
		}
	}
}
