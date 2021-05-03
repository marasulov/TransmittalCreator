using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestWPF
{
    public class Node : INotifyPropertyChanged

    {

        ObservableCollection<Node> children = new ObservableCollection<Node>();

        string text;

        bool isChecked;



        public ObservableCollection<Node> Children

        {

            get { return this.children; }

        }

        public bool IsChecked

        {

            get { return this.isChecked; }

            set

            {

                this.isChecked = value;

                RaisePropertyChanged("IsChecked");

            }

        }

        public string Text

        {

            get { return this.text; }

            set

            {

                this.text = value;

                RaisePropertyChanged("Text");

            }

        }



        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)

        {

            if (this.PropertyChanged != null)

                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));



            if (propertyName == "IsChecked")

            {

                foreach (Node child in this.Children)

                    child.IsChecked = this.IsChecked;

            }

        }

    }

    
}
