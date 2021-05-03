using System.Collections.Generic;
using System.Windows;
using TransmittalCreator.Models;
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

        public LayoutTreeView(PrintPackageCreator packageCreator)
        {
            
            List<PrintPackageModel> printPackages = packageCreator.PrintPackageModels;

            foreach (var package in printPackages)
            {
                var layout = package.Layouts;
            }

            var nodes =new LayoutTreeViewModel().CreateTree(printPackages);

            InitializeComponent();

            //LayoutTreeViewModel layoutTreeViewModel = this.tree.Items[0] as LayoutTreeViewModel;
            this.tree.ItemsSource = nodes;
         

            this.tree.Focus();

            this.SizeToContent = SizeToContent.Height;

            //DataContext = ;
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            var newTree = this.tree;
            isClicked = true;
            this.Close();

        }
    }
}
