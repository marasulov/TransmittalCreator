using Autodesk.AutoCAD.DatabaseServices;
using TransmittalCreator.DBCad;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using TransmittalCreator.Models;
using TransmittalCreator.Services;
using TransmittalCreator.ViewModel;
using MessageBox = System.Windows.Forms.MessageBox;

namespace TransmittalCreator
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class PdfCreator : Window
    {
        public bool isClicked = false;
        BlockViewModel _data;
        public string _blockName;
        public PdfCreator(BlockViewModel data)
        {
            InitializeComponent();

            _data = data;
            this.DataContext = data;
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

            ComboObjectNameEn.ItemsSource = combolist;
            NameBlock.Text = _blockName;
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
    }
}
