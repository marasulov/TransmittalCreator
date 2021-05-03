using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace WpfAppTest
{
    public class Node : INotifyPropertyChanged
    {
        public Node()
        {
            this.isChecked = false;
        }
        public Node(Node parent,string nodeName):this()
        {
            Parent = parent;
            NodeName = nodeName;
        }
        public Node(Node parent, string nodeName, Boolean bChecked)
            : this(parent, nodeName)
        {
            this.isChecked = bChecked;
        }
        public Node Parent;
 
        public event PropertyChangedEventHandler PropertyChanged;       
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
        private Boolean? isChecked;

        public Boolean? IsChecked
        {
            get { return this.isChecked; }
            set
            {
                if (this.isChecked != value)
                {
                    this.isChecked = value;
                    NotifyPropertyChanged("IsChecked");
                    if (this.isChecked == true) // If the node is selected
                    {
                        if (this.childNode != null)
                            foreach (Node dt in this.childNode)
                                dt.IsChecked = true;
                        if (this.Parent != null)
                        {
                            Boolean bExistUncheckedChildren = false;
                            foreach (Node dt in this.Parent.ChildNode)
                                if (dt.IsChecked != true)
                                {
                                    bExistUncheckedChildren = true;
                                    break;
                                }

                            if (bExistUncheckedChildren)
                                this.Parent.IsChecked = null;
                            else
                                this.Parent.IsChecked = true;
                        }
                    }
                    else if (this.isChecked == false) // If the node is not selected
                    {
                        if (this.childNode != null)
                            foreach (Node dt in this.childNode)
                                dt.IsChecked = false;
                        if (this.Parent != null)
                        {
                            Boolean bExistCheckedChildren = false;
                            foreach (Node dt in this.Parent.ChildNode)
                                if (dt.IsChecked != false)
                                {
                                    bExistCheckedChildren = true;
                                    break;
                                }

                            if (bExistCheckedChildren)
                                this.Parent.IsChecked = null;
                            else
                                this.Parent.IsChecked = false;
                        }
                    }
                    else
                    {
                        if (this.Parent != null)
                            this.Parent.IsChecked = null;
                    }
                }
            }

        }

        private ObservableCollection<Node> childNode;
 
        public string NodeName { get; set; }
 
        public ObservableCollection<Node> InitRoot()
        {
            ObservableCollection<Node> dts = new ObservableCollection<Node>();
            
            for(int i=0;i<2;i++)
            {                
                Node dt = new Node(null, i.ToString()+"a");
                dts.Add(dt);                
            }
            return dts;
        }
 
        public ObservableCollection<Node> ChildNode
        {
            get
            {
                if (this.childNode == null)
                {
                    this.childNode = new ObservableCollection<Node>();
                    if(NodeName.Equals("0a"))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            this.childNode.Add(new Node(this, "b" + i.ToString(), this.isChecked == true));
                        }
                    }
                    else if(NodeName.Equals("1a"))
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            this.childNode.Add(new Node(this, "c" + i.ToString(), this.isChecked == true));
                        }
                    }                    
                }
                return childNode;
            }
        }
    }
}
