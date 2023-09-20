using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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

			comboAttributs.ItemsSource = combolist;

			
		}
	}
}
