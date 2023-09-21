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
		public PdfCreatorView(Dictionary<string, string> attributeList)
		{
			InitializeComponent();

			List<BlockAttribute> combolist = new List<BlockAttribute>();

			foreach (KeyValuePair<string, string> entry in attributeList)
			{
				combolist.Add(new BlockAttribute(entry.Key, entry.Value));
			}

			//AttributeViewModel attributeViewModel = new AttributeViewModel();

			comboAttributs.ItemsSource = combolist;

			DataContext = new FilenameModel();
		}
	}
}
