using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Autodesk.AutoCAD.DatabaseServices;
using DV2177.Common;
using TransmittalCreator.Models;
using TransmittalCreator.Services;
using TransmittalCreator.ViewModel;

namespace TransmittalCreator
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public bool isClicked = false;
        private BlockViewModel _data;
        private string _blockName;

        public Window1(BlockViewModel data)
        { 
            ProxyDomain pd = new ProxyDomain();
            Assembly assembly = pd.GetAssembly(@"C:\Program Files\Autodesk\ApplicationPlugins\WPF\Contents\MaterialDesignThemes.Wpf.dll");

            ProxyDomain pd1 = new ProxyDomain();
            Assembly assembly1 = pd.GetAssembly(@"C:\Program Files\Autodesk\ApplicationPlugins\WPF\Contents\MaterialDesignColors.dll");

            InitializeComponent();

            _data = data;
            List<BlockAttribute> combolist = new List<BlockAttribute>();

            Dictionary<string, string> attrList = new Dictionary<string, string>();
            Transaction tr = Active.Database.TransactionManager.StartTransaction();

            var names = Utils.GetAttrList(tr, attrList);
            attrList = names.Item2;
            _blockName = names.Item1;

            foreach (KeyValuePair<string, string> entry in attrList)
            {
                combolist.Add(new BlockAttribute(entry.Key, entry.Value));
            }

            comboAttributs.ItemsSource = combolist;

            DataContext = new FilenameModel();
        }

        private void NewCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void NewCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            isClicked = true;
            this.Close();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }

    class ProxyDomain : MarshalByRefObject
    {
        public Assembly GetAssembly(string assemblyPath)
        {
            try
            {
                return Assembly.LoadFrom(assemblyPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }
    }

   
}
