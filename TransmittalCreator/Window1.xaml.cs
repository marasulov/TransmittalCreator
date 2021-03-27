using Autodesk.AutoCAD.DatabaseServices;
using DV2177.Common;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using TransmittalCreator.Models;
using TransmittalCreator.Services;
using TransmittalCreator.ViewModel;
using Path = System.IO.Path;

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

        public string BlockName
        {
            get => _blockName;
            set => _blockName = value;
        }


        public Window1(BlockViewModel data)
        {
            string executablePath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            ProxyDomain pd = new ProxyDomain();
            Assembly assembly = pd.GetAssembly(Path.Combine(executablePath, "MaterialDesignThemes.Wpf.dll"));

            ProxyDomain pd1 = new ProxyDomain();
            Assembly assembly1 = pd.GetAssembly(Path.Combine(executablePath, "MaterialDesignColors.dll"));

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

        private void createButton_Click(object sender, RoutedEventArgs e)
        {
            isClicked = true;
            this.Close();
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
