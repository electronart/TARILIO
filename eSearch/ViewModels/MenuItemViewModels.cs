using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace eSearch.ViewModels
{
    public interface IMenuElement { } // Marker Interface for typing the collection

    public class MenuItemModel : ViewModelBase, IMenuElement // Basic Menu Item
    {
        public string Header
        {
            get => _header;
            set => this.RaiseAndSetIfChanged(ref _header, value);
        }

        private string _header = "Untitled";

        public object? IsEnabled
        {
            get => _isEnabled;
            set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
        }

        private object? _isEnabled = true;

        public object? IsVisible
        {
            get => _isVisible;
            set => this.RaiseAndSetIfChanged(ref _isVisible, value);
        }

        private object? _isVisible = true;

        public object? Command
        {
            get => _command;
            set => this.RaiseAndSetIfChanged(ref _command, value);
        }

        private object? _command;

        public IEnumerable<IMenuElement> Items 
        {   get => _items;
            set => this.RaiseAndSetIfChanged(ref _items, value); 
        } // Supports submenus with mixed types

        private IEnumerable<IMenuElement> _items = new List<IMenuElement>();
    }

    public class CheckBoxMenuItemModel : MenuItemModel // For creating Menu Items with Checkboxes.
    {
        public object? IsChecked
        {
            get => _isChecked;
            set => this.RaiseAndSetIfChanged(ref _isChecked, value);
        }

        private object? _isChecked = false;
    }

    public class SeperatorModel : IMenuElement { } // Just a type marker for a seperator.
}
