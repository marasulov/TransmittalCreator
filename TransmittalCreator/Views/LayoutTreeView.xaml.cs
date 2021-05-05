using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TransmittalCreator.Models;
using TransmittalCreator.Models.Layouts;
using TransmittalCreator.Services;
using TransmittalCreator.ViewModel;

namespace TransmittalCreator.Views
{
    /// <summary>
    /// Логика взаимодействия для LayoutTreeView.xaml
    /// </summary>
    public partial class LayoutTreeView : Window
    {
        public bool isClicked = false;
        BlockViewModel _data;
        public string _blockName;
        public List<PrintPackageModel> _printPackages;
        private LayoutTreeViewModel _unchekedTree;

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
            this.tree.ItemsSource = nodes;

            this.tree.Focus();

            this.SizeToContent = SizeToContent.Height;

            //DataContext = ;
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            var v = this.tree.ItemsSource.Cast<LayoutTreeViewModel>().ToList();
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

                var tempLayouts = layoutTree.Children;;
                
                //var newSequence = (from el in tempLayouts
                //    where el.IsChecked == true
                //    select el).ToList();
                if (layoutTree.IsChecked != null) continue;
                var selectedPackage = _printPackages.FirstOrDefault(x => x.PdfFileName == layoutTree.Name);
                var selLayouts = selectedPackage.Layouts;

                foreach (var layoutTreeChild in tempLayouts)
                {
                    if (layoutTreeChild.IsChecked != false) continue;
                    string layoutName = layoutTreeChild.Name;

                    var allLayouts = layoutTreeChild.Children;

                    var layoutModel = selLayouts.FirstOrDefault(l => l.LayoutName == layoutName);
                    selLayouts.Remove(layoutModel);
                }
                //layoutTree.Children = newSequence;
            }

            isClicked = true;
            this.Close();
        }

        private void RemoveUnchekedLayout(LayoutTreeViewModel layoutTree)
        {
            string layoutName = layoutTree.Name;

            var unchekedLayout = _printPackages.FirstOrDefault(x => x.PdfFileName == layoutName);
            if (unchekedLayout != null)
                _printPackages.Remove(unchekedLayout);
        }

    }
}
