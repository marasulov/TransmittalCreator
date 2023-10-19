using System.Collections.Generic;
using System.Linq;
using System.Windows;
using TransmittalCreator.Models;
using TransmittalCreator.ViewModel;

namespace TransmittalCreator.View;

/// <summary>
///     Логика взаимодействия для LayoutTreeView.xaml
/// </summary>
public partial class LayoutTreeView : Window
{
	//BlockViewModel _data;
	public string _blockName;
	public List<PrintPackageModel> _printPackages;
	public bool isClicked;

	public LayoutTreeView(List<PrintPackageModel> printPackages)
	{
		_printPackages = printPackages;

		foreach (var package in _printPackages)
		{
			var layout = package.Layouts;
		}

		var nodes = new LayoutTreeViewModel().CreateTree(_printPackages);

		InitializeComponent();

		//LayoutTreeViewModel layoutTreeViewModel = this.tree.Items[0] as LayoutTreeViewModel;
		tree.ItemsSource = nodes;

		tree.Focus();

		SizeToContent = SizeToContent.Height;

		//DataContext = ;
	}

	private void CreateButton_Click(object sender, RoutedEventArgs e)
	{
		var v = tree.ItemsSource.Cast<LayoutTreeViewModel>().ToList();
		foreach (var layoutTree in v[0].Children)
		{
			switch (layoutTree.IsChecked)
			{
				case true:
					continue;
				case false:
					RemoveUnchekedLayout(layoutTree);
					break;
			}

			var tempLayouts = layoutTree.Children;
			;

			if (layoutTree.IsChecked != null) continue;
			var selectedPackage = _printPackages.FirstOrDefault(x => x.PdfFileName == layoutTree.Name);
			var selLayouts = selectedPackage.Layouts;

			foreach (var layoutTreeChild in tempLayouts)
			{
				if (layoutTreeChild.IsChecked != false) continue;
				var layoutName = layoutTreeChild.Name;

				var allLayouts = layoutTreeChild.Children;

				var layoutModel = selLayouts.FirstOrDefault(l => l.LayoutName == layoutName);
				selLayouts.Remove(layoutModel);
			}
		}

		isClicked = true;
		Close();
	}

	private void RemoveUnchekedLayout(LayoutTreeViewModel layoutTree)
	{
		var layoutName = layoutTree.Name;

		var unchekedLayout = _printPackages.FirstOrDefault(x => x.PdfFileName == layoutName);
		if (unchekedLayout != null)
			_printPackages.Remove(unchekedLayout);
	}
}