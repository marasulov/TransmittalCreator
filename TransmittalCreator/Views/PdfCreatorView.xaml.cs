using System.Collections.Generic;
using System.Windows;
using TransmittalCreator.Models;

namespace TransmittalCreator.Views
{
	/// <summary>
	/// Логика взаимодействия для PdfCreatorView.xaml
	/// </summary>
	public partial class PdfCreatorView : Window
	{
		public Dictionary<string,string> BlockAttributes { get; set; }
		public PdfCreatorView(FilenameModel filenameModel)
		{
			InitializeComponent();

			//AttributeViewModel attributeViewModel = new AttributeViewModel();

			//comboAttributs.ItemsSource = blockAttributes;

			DataContext = filenameModel;
		}
	}
}
