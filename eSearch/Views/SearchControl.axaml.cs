using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using eSearch.CustomControls;
using eSearch.ViewModels;
using org.apache.http.auth;
using System;
using System.Diagnostics;
using System.IO;

namespace eSearch.Views
{
    public partial class SearchControl : UserControl
    {

        private const double NarrowThreshold = 900;
        private const double UltraNarrowThreshold = 520;
        private bool _isWide = true; // Initial assumption; will update on first size change.
        private bool _isUltraNarrow = false;

        public SearchControl()
        {
            InitializeComponent();

            BtnAND.Click += BtnAND_Click;
            BtnOR.Click += BtnOR_Click;
            BtnNOT.Click += BtnNOT_Click;

            this.SizeChanged += SearchControl_SizeChanged;
            //ResponsiveLayoutUpdate();
        }

        private void SearchControl_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
            {
                ResponsiveLayoutUpdate(e.NewSize.Width);
            }
        }

        public void RemoveAttachment(object fileInfo)
        {
            if (fileInfo is FileInfo nfo)
            {
                if (DataContext is MainWindowViewModel mwvm)
                {
                    mwvm.Session.Query.AttachedFiles.Remove(nfo);
                }
            }
        }

        private void ResponsiveLayoutUpdate(double width)
        {
            Debug.WriteLine($"Width: {width}");
            var newIsWide = width >= NarrowThreshold;
            var newIsUltraNarrow = width <= UltraNarrowThreshold;
            if (newIsWide != _isWide)
            {
                _isWide = newIsWide;
                
                if (_isWide)
                {
                    SearchBoxRow.RowDefinitions = RowDefinitions.Parse("Auto,Auto");
                    SearchBoxRow.ColumnDefinitions = ColumnDefinitions.Parse("Auto, *, Auto, Auto");

                    Grid.SetRow(ComboBoxSearchSource, 1);
                    Grid.SetColumn(ComboBoxSearchSource, 0);

                    Grid.SetRow(StackPanelCenterTextBoxAndOrNot, 1);
                    Grid.SetColumn(StackPanelCenterTextBoxAndOrNot, 1);
                    Grid.SetColumnSpan(StackPanelCenterTextBoxAndOrNot, 1);

                } else
                {
                    SearchBoxRow.RowDefinitions = RowDefinitions.Parse("Auto,Auto,Auto");
                    SearchBoxRow.ColumnDefinitions = ColumnDefinitions.Parse("Auto, *, Auto, Auto");

                    Grid.SetRow(ComboBoxSearchSource, 0);
                    Grid.SetColumn(ComboBoxSearchSource, 0);

                    Grid.SetRow(StackPanelCenterTextBoxAndOrNot, 1);
                    Grid.SetColumn(StackPanelCenterTextBoxAndOrNot, 0);
                    Grid.SetColumnSpan(StackPanelCenterTextBoxAndOrNot, 2);


                }
            }

            if (newIsUltraNarrow != _isUltraNarrow)
            {
                _isUltraNarrow = newIsUltraNarrow;
                if (!_isUltraNarrow)
                {
                    ComboBoxSearchSource.Width = 280;
                    DockPanel.SetDock(StackPanelStemmingSynonymsSoundex, Dock.Left);
                } else
                {
                    DockPanel.SetDock(StackPanelStemmingSynonymsSoundex, Dock.Top);
                }
            }
            if (_isUltraNarrow)
            {
                ComboBoxSearchSource.Width = Math.Max( (280 - (UltraNarrowThreshold - width)), 100);
            }
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
