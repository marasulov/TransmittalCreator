using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TestWPF
{
    class ApplicationViewModel : INotifyPropertyChanged
    {
        private bool? _isChecked = true;
        private ApplicationViewModel _parent;

        public List<ApplicationViewModel> Children { get; private set; }

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

        public Dictionary<string, List<string>> dict { get; private set; } = new Dictionary<string, List<string>>
        {
            {"Weapons1", new List<string>() {"blades", "knifes", "asaasa"}},
            {
                "level1", new List<string>() {"level21", "level22", "level23"}
            }
        };

        public ApplicationViewModel()
        {
        }

        public List<ApplicationViewModel> CreateFoos()
        {
            ApplicationViewModel root = new ApplicationViewModel("filename"){IsInitiallySelected = true};

            foreach (KeyValuePair<string, List<string>> kvp  in dict)
            {
                var header = kvp.Key;

                var level1= new ApplicationViewModel(header);
                level1.Name = header;
                level1.Children = new List<ApplicationViewModel>();
                foreach (var val in kvp.Value)
                {
  
                    level1.Children.Add(new ApplicationViewModel(val));
                }

                root.Children.Add( level1);
            }

            root.Initialize();
            return new List<ApplicationViewModel> { root };
        }

        public ApplicationViewModel(string name)
        {
            this.Name = name;
            this.Children = new List<ApplicationViewModel>();
        }

        void Initialize()
        {
            foreach (ApplicationViewModel child in this.Children)
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
