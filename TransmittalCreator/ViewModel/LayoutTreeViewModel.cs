using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TransmittalCreator.Models;
using TransmittalCreator.Services;

namespace TransmittalCreator.ViewModel
{
    class LayoutTreeViewModel: INotifyPropertyChanged
    {
        private bool? _isChecked = true;
        private LayoutTreeViewModel _parent;
        public PrintPackageCreator PrintPackage { get; set; }

        public List<LayoutTreeViewModel> Children { get; set; }

        public bool IsInitiallySelected { get; private set; }

        public string Name { get; private set; }

        public bool? IsChecked
        {
            get { return _isChecked; }
            set { this.SetIsChecked(value, true, true); }
        }

        private RelayCommand addCommand;
        public RelayCommand AddCommand
        {
            get
            {
                return addCommand ??
                       (addCommand = new RelayCommand(CreateCommand, OnCreateCommand));
            }
        }

        private bool OnCreateCommand(object arg)
        {
            return true;
        }

        private void CreateCommand(object obj)
        {
            var v = obj.ToString();
        }

        public LayoutTreeViewModel()
        {
        }

        public LayoutTreeViewModel(string name)
        {
            Children = new List<LayoutTreeViewModel>();
            Name = name;
        }
        //TODO подумать как передать данные с автокада сюда
        private IObservable<PrintPackageModel> _printPackages;
        public IObservable<PrintPackageModel> PrintPackages { get { return _printPackages; }
            set { _printPackages = value; OnPropertyChanged("printPackages"); } }
        

        public List<LayoutTreeViewModel> CreateTree(IList<PrintPackageModel> printPackages)
        {
            var fileName = printPackages.Select(x => x.DwgFileName).FirstOrDefault();
            
            _printPackages = printPackages as IObservable<PrintPackageModel>;

            LayoutTreeViewModel root = new LayoutTreeViewModel(fileName)
            {
                IsInitiallySelected = true,
            };

            foreach (var printPackageModel  in printPackages)
            {

                var header = printPackageModel.PdfFileName;

                var level1 = new LayoutTreeViewModel(header)
                {
                    Name = header,
                    Children = new List<LayoutTreeViewModel>()
                };
                foreach (var val in printPackageModel.Layouts)
                {
                    level1.Children.Add(new LayoutTreeViewModel(val.LayoutName));
                }

                root.Children.Add( level1);
            }

            root.Initialize();
            return new List<LayoutTreeViewModel> { root };
            
        }

        void Initialize()
        {
            foreach (LayoutTreeViewModel child in this.Children)
            {
                child._parent = this;
                child.Initialize();
            }
        }

        void SetIsChecked(bool? value, bool updateChildren, bool updateParent)
        {
            if (value == _isChecked)
                return;

            _isChecked = value;

            if (updateChildren && _isChecked.HasValue)
                this.Children.ForEach(c => c.SetIsChecked(_isChecked, true, false));

            if (updateParent && _parent != null)
                _parent.VerifyCheckState();

            this.OnPropertyChanged("IsChecked");
        }

        void VerifyCheckState()
        {
            bool? state = null;
            for (int i = 0; i < this.Children.Count; ++i)
            {
                bool? current = this.Children[i]._isChecked;
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
            this.SetIsChecked(state, false, true);

        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }


}
