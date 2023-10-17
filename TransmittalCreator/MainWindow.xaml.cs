using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using TransmittalCreator.DBCad;
using TransmittalCreator.Models;
using TransmittalCreator.Utils;

namespace TransmittalCreator;

/// <summary>
///     Логика взаимодействия для MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
	//BlockViewModel _data;
	public string _blockName;
	public bool isClicked;

	public MainWindow( /*BlockViewModel data*/)
	{
		InitializeComponent();

		//_data = data;
		//this.DataContext = data;
		var combolist = new List<BlockAttribute>();

		var attrList = new Dictionary<string, string>();
		var tr = Active.Database.TransactionManager.StartTransaction();

		var names = PrintUtils.GetAttrList(tr, attrList);
		tr?.Dispose();
		attrList = names.Item2;
		_blockName = names.Item1;

		foreach (var entry in attrList) combolist.Add(new BlockAttribute(entry.Key, entry.Value));

		ComboObjectNameEn.ItemsSource = combolist;
		ComboObjectNameRu.ItemsSource = combolist;

		ComboBoxPosition.ItemsSource = combolist;
		ComboBoxNomination.ItemsSource = combolist;
		ComboBoxComment.ItemsSource = combolist;

		ComboBoxTrItem.ItemsSource = combolist;
		ComboBoxTrDocNumber.ItemsSource = combolist;
		ComboBoxTrDocTitleEn.ItemsSource = combolist;
		ComboBoxTrDocTitleRu.ItemsSource = combolist;

		NameBlock.Text = _blockName;
	}

	private void NewCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
	{
		e.CanExecute = true;
	}

	private void NewCommand_Executed(object sender, ExecutedRoutedEventArgs e)
	{
		isClicked = true;
		Close();
	}
}