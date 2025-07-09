using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using eSearch.CustomControls;
using eSearch.ViewModels;
using System.Diagnostics;

namespace eSearch.Views
{
    public partial class SearchControl : UserControl
    {
        public SearchControl()
        {
            InitializeComponent();

            BtnAND.Click += BtnAND_Click;
            BtnOR.Click += BtnOR_Click;
            BtnNOT.Click += BtnNOT_Click;
            
        }

        private void BtnNOT_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            AddConnectorToSearchBar("NOT");
        }

        private void BtnOR_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            AddConnectorToSearchBar("OR");
        }

        private void BtnAND_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            AddConnectorToSearchBar("AND");
        }
        
        private void AddConnectorToSearchBar(string connector)
        {
            int caretPos = QueryTextBox.SelectionStart;
            string? text = QueryTextBox.Text;
            bool prevCharIsSpace = caretPos > 0 && text?.Length > caretPos - 1 && text[caretPos - 1] == ' ';
            bool isNextCharSpace = caretPos > 0 && text?.Length > (caretPos) && text[caretPos] == ' ';
            string insert = "";
            if (!prevCharIsSpace)
            {
                insert += " ";
            }
            insert += connector;
            if (!isNextCharSpace)
            {
                insert += " ";
            }
            text = text?.Insert(caretPos, insert);
            if (DataContext is MainWindowViewModel vm)
            {
                vm.Session.Query.Query = text ?? string.Empty;
            }
            QueryTextBox.SelectionStart = caretPos + insert.Length;
            QueryTextBox.SelectionEnd = caretPos + insert.Length;
            QueryTextBox.Focus();
            
        }
    }
}
