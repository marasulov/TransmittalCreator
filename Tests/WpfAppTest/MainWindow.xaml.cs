using System.Windows;

namespace WpfAppTest
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            var nodes = new Node().InitRoot();

            InitializeComponent();
            this.Tree.ItemsSource = nodes;
        }


    }

    

}
