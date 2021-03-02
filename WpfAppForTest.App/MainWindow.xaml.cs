using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace WpfAppForTest.App
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool isClicked = false;
        public MainWindow()
        {
            InitializeComponent();

            DataContext = new FilenameModel();

            //fileNameTextblock.DataContext = FullName();

            //string fileName = "";

            //if (createButton != null)
            //{

            //    fileName = comboAttributs.Text;

            //    if (!string.IsNullOrEmpty(suffixTextBox.Text))
            //    {
            //        fileName = string.Concat(fileName, suffixTextBox.Text);
            //    }

            //    if (!string.IsNullOrEmpty(prefixTextBox.Text))
            //    {
            //        fileName = string.Concat(prefixTextBox, fileName);
            //    }
            //}
        }

        private string FullName()
        {
            return this.comboAttributs.SelectedValue?.ToString() + "" + this.suffixTextBox?.Text + "" +
                   this.numberingTextbox.Text;
        }

        private void NewCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void NewCommand_Executed(object sender, RoutedEventArgs e)
        {

            isClicked = true;
            this.Close();

        }

        private void ComboAttributs_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            var selectedItem = comboBox.SelectedItem as BlockAttribute;

            TextBlock1.Text = selectedItem.AttributeValue;
        }
    }

    [ValueConversion(typeof(string), typeof(Visibility))]
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (string.IsNullOrEmpty((string)value))
            {
                return Visibility.Collapsed;
            }
            else
            {
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
