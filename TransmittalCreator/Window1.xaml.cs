using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using TransmittalCreator.DBCad;
using TransmittalCreator.Models;
using TransmittalCreator.Utils;
using TransmittalCreator.ViewModel;

namespace TransmittalCreator;

public partial class Window1 : Window
{
	private BlockViewModel _data;
	public bool isClicked;

	public Window1(BlockViewModel data)
	{
		InitializeComponent();

		_data = data;
		var combolist = new List<BlockAttribute>();

		var attrList = new Dictionary<string, string>();
		var tr = Active.Database.TransactionManager.StartTransaction();

		var names = PrintUtils.GetAttrList(tr, attrList);
		attrList = names.Item2;

		foreach (var entry in attrList) combolist.Add(new BlockAttribute(entry.Key, entry.Value));

		comboAttributs.ItemsSource = combolist;

		DataContext = new FilenameModel();
	}

	public string BlockName { get; set; }

	private void NewCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
	{
		e.CanExecute = true;
	}

	private void NewCommand_Executed(object sender, ExecutedRoutedEventArgs e)
	{
		isClicked = true;
		Close();
	}

	private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
	{
		var regex = new Regex("[^0-9]+");
		e.Handled = regex.IsMatch(e.Text);
	}

	private void createButton_Click(object sender, RoutedEventArgs e)
	{
		isClicked = true;
		Close();
	}
}

//internal class ProxyDomain : MarshalByRefObject
//{
//	public Assembly GetAssembly(string assemblyPath)
//	{
//		try
//		{
//			return Assembly.LoadFrom(assemblyPath);
//		}
//		catch (Exception ex)
//		{
//			throw new InvalidOperationException(ex.Message);
//		}
//	}
//}