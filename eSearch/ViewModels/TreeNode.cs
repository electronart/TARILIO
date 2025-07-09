using DynamicData;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{

    /// <summary>
    /// For use with Avalonia TreeView
    /// </summary>
    public class TreeNode : ViewModelBase
    {

        public TreeNode(string title, bool isEnabled, bool isChecked, object tag = null, IEnumerable<TreeNode> subNodes = null)
        {
            Title = title;
            IsEnabled = isEnabled;
            IsChecked = isChecked;
            Tag = tag;
            SubNodes = new ObservableCollection<TreeNode>();
            if (subNodes != null) SubNodes.AddRange(subNodes);
        }

        public void AddSubNode(TreeNode node)
        {
            node.SetParentNode(this);
            SubNodes.Add(node);
        }

        public ObservableCollection<TreeNode>? SubNodes { get; }

        public string Title { get; }

        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _isEnabled, value);
            }
        }

        private bool _isEnabled;

        public bool? IsChecked { 
            get
            {
                return _isChecked;
            }
            set
            {
                if (value != _isChecked)
                {
                    this.RaiseAndSetIfChanged(ref _isChecked, value);
                    if (this.ParentNode != null)
                    {
                        this.ParentNode.recalc_checked_state();
                    }
                    if (this.SubNodes != null)
                    {
                        if (value == true)
                        {
                            foreach(var subn in this.SubNodes)
                            {
                                subn.IsChecked = true;
                            }
                        }
                        if (value == false)
                        {
                            foreach (var subn in this.SubNodes)
                            {
                                subn.IsChecked = false;
                            }
                        }
                    }
                }
            }
        }

        private bool? _isChecked = false;
        public object Tag { get; }

        private void recalc_checked_state()
        {
            if (SubNodes != null)
            {
                bool all_checked   = true;
                bool all_unchecked = true;

                foreach (TreeNode node in SubNodes)
                {
                    if (node.IsChecked == true)
                    {
                        all_unchecked = false;
                    } else
                    {
                        all_checked = false;
                    }
                }

                if (all_checked)
                {
                    IsChecked = true;
                    return;
                }
                if (all_unchecked)
                {
                    IsChecked = false;
                    return;
                }
                IsChecked = null; // indeterminate.
            }
        }

        private void SetParentNode(TreeNode parentNode)
        {
            _parentNode = parentNode;
        }

        public TreeNode ParentNode { get {
                return _parentNode;    
            
            }
        }

        private TreeNode _parentNode = null;

    }
}
