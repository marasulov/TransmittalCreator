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
            

            InitializeComponent();
			Loaded += MainWindow_Loaded;
        }

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is ICloseWindow vm)
			{
				vm.Close += () =>
				{
					this.Close();
				};
			}
		}
	}

    

}
