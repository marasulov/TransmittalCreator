using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using TransmittalCreator.ViewModel;

namespace TransmittalCreator
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BlockViewModel _data;
        string _blockName;
        public MainWindow(BlockViewModel data)
        {
            InitializeComponent();

            _data = data;
            this.DataContext = data;
            List<BlockModel> combolist = new List<BlockModel>();

            Dictionary<string, string> attrList = new Dictionary<string, string>();
            Transaction tr = Active.Database.TransactionManager.StartTransaction();

            var names = Utils.GetAttrList(tr, attrList);
            attrList = names.Item2;
            _blockName = names.Item1;

            foreach (KeyValuePair<string, string> entry in attrList)
            {
                combolist.Add(new BlockModel(entry.Key, entry.Value));
            }


            ComboBoxPosition.ItemsSource = combolist;
            ComboBoxNomination.ItemsSource = combolist;
            ComboBoxPosition.ItemsSource = combolist;

        }

        private void NewCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void NewCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {

            BlockModel comboBox1SelectedItem = (BlockModel)ComboBoxPosition.SelectedItem;
            BlockModel comboBox2SelectedItem = (BlockModel)ComboBoxNomination.SelectedItem;
            MessageBox.Show(comboBox1SelectedItem.AttributName + comboBox1SelectedItem.AttributName + _blockName);
            this.Close();

            ObjectIdCollection blocksIds = MyCommands.GetAllBlockReferenceByName(_blockName, Active.Database);
            List<Sheet> dict = new List<Sheet>();
            Transaction tr = Active.Database.TransactionManager.StartTransaction();
            MyCommands.GetSheetsFromBlocks(Active.Editor, dict,tr,blocksIds);


        }
    }
}
