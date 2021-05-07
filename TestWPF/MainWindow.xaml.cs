using System.Windows;
using System.Windows.Input;

namespace TestWPF
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            
            //ApplicationViewModel root = this.tree.Items[0] as ApplicationViewModel;

            var nodes =new ApplicationViewModel().CreateFoos();

            InitializeComponent();
           
            this.tree.ItemsSource = nodes;
         
            this.tree.Focus();
            
            base.CommandBindings.Add(
                new CommandBinding(
                    ApplicationCommands.Undo,
                    (sender, e) => // Execute
                    {                        
                        e.Handled = true;
                        //nodes.IsChecked = false;
                        this.tree.Focus();
                    },
                    (sender, e) => // CanExecute
                    {
                        e.Handled = true;
                        //e.CanExecute = (root.IsChecked != false);
                    }));



            this.SizeToContent = SizeToContent.Height;
        }

        //private void NewCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        //{
        //    e.CanExecute = true;
        //}

        //private void NewCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        //{
        //    var avm = e.Parameter as ApplicationViewModel;
            
        //    foreach (var param in avm.Children)
        //    {
        //        foreach (var pparam in param.Children)
        //        {
        //            var vv = pparam.IsChecked.Value;
        //        }
        //    }
        //    MessageBox.Show("The New command was invoked");
        //}
    }
}
