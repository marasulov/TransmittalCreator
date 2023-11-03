using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using TransmittalCreator.Models;

namespace TransmittalCreator.ViewModel;

internal class LayoutTreeViewModel : INotifyPropertyChanged
{
	private bool? _isChecked = true;

	private LayoutTreeViewModel _parent;

	//TODO подумать как передать данные с автокада сюда
	private IObservable<PrintPackageModel> _printPackages;

	private RelayCommand addCommand;

	public LayoutTreeViewModel()
	{
	}

	public LayoutTreeViewModel(string name)
	{
		Children = new List<LayoutTreeViewModel>();
		Name = name;
	}

	public List<LayoutTreeViewModel> Children { get; set; }

	public bool IsInitiallySelected { get; private set; }

	public string Name { get; private set; }

	public bool? IsChecked
	{
		get => _isChecked;
		set => SetIsChecked(value, true, true);
	}

	public event PropertyChangedEventHandler PropertyChanged;

	private bool OnCreateCommand(object arg)
	{
		return true;
	}

	private void CreateCommand(object obj)
	{
		var v = obj.ToString();
	}


	public List<LayoutTreeViewModel> CreateTree(IList<PrintPackageModel> printPackages)
	{
		var fileName = printPackages.Select(x => x.DwgFileName).FirstOrDefault();

		_printPackages = printPackages as IObservable<PrintPackageModel>;

		var root = new LayoutTreeViewModel(fileName)
		{
			IsInitiallySelected = true
		};

		foreach (var printPackageModel in printPackages)
		{
			var header = printPackageModel.PdfFileName;

			var level1 = new LayoutTreeViewModel(header)
			{
				Name = header,
				Children = new List<LayoutTreeViewModel>()
			};
			foreach (var val in printPackageModel.Layouts) level1.Children.Add(new LayoutTreeViewModel(val.LayoutName));

			root.Children.Add(level1);
		}

		root.Initialize();
		return new List<LayoutTreeViewModel> { root };
	}

	private void Initialize()
	{
		foreach (var child in Children)
		{
			child._parent = this;
			child.Initialize();
		}
	}

	private void SetIsChecked(bool? value, bool updateChildren, bool updateParent)
	{
		if (value == _isChecked)
			return;

		_isChecked = value;

		if (updateChildren && _isChecked.HasValue)
			Children.ForEach(c => c.SetIsChecked(_isChecked, true, false));

		if (updateParent && _parent != null)
			_parent.VerifyCheckState();

		OnPropertyChanged("IsChecked");
	}

	private void VerifyCheckState()
	{
		bool? state = null;
		for (var i = 0; i < Children.Count; ++i)
		{
			var current = Children[i]._isChecked;
			if (i == 0)
			{
				state = current;
			}
			else if (state != current)
			{
				state = null;
				break;
			}
		}

		SetIsChecked(state, false, true);
	}

	public void OnPropertyChanged([CallerMemberName] string prop = "")
	{
		if (PropertyChanged != null)
			PropertyChanged(this, new PropertyChangedEventArgs(prop));
	}
}