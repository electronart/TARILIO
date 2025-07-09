 using Avalonia.Controls;
using Avalonia.Media;
using com.sun.corba.se.pept.transport;
using eSearch.ViewModels;
using sun.tools.tree;
using System;
using System.Diagnostics;
using static eSearch.Models.Search.LuceneWordWheel;

namespace eSearch.Views
{
    public partial class WordWheelControl : UserControl
    {

        public event EventHandler<WheelWord> WordSubmission;


        /// <summary>
        /// Only to be set to true under certain circumstances otherwise causes UI usability issues. We only want it to scroll to top for the typeahead, but user interaction
        /// should not scroll it to the top.
        /// </summary>
        public bool ScrollSelectionToTop = false;

        public WordWheelControl()
        {
            InitializeComponent();
            this.DataContextChanged += WordWheelControl_DataContextChanged;
            attachEvents();
        }

        private void WordWheelControl_DataContextChanged(object? sender, System.EventArgs e)
        {
            Debug.WriteLine("WordWheelControl_DataContextChanged");
            attachEvents();
        }

        private void attachEvents()
        {
            Debug.WriteLine("attachEvents");
            if (this.DataContext != null)
            {
                Debug.WriteLine("DataContext is of type " + this.DataContext.GetType().ToString());
            } else
            {
                Debug.WriteLine("DataContext is null");
            }
            var viewModel = this.DataContext as MainWindowViewModel;
            if (viewModel != null && viewModel.Wheel != null)
            {
                viewModel.PropertyChanged += mwvm_propertyChanged;
                viewModel.Wheel.PropertyChanged += mwvm_propertyChanged;
                wordWheelListBox.SelectionChanged   += WheelListBox_SelectionChanged;
                wordWheelListBox.DoubleTapped       += WordWheelListBox_DoubleTapped;
                wordWheelListBox.KeyUp += WordWheelListBox_KeyUp;
                Debug.WriteLine("Events attached. ViewModel is not null. There are " + viewModel.Wheel.WheelWords.Count + " wheelwords");
            } else
            {
               Debug.WriteLine("ViewModel is null!");
            }
        }
        private void WordWheelListBox_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                if (this.DataContext is MainWindowViewModel mwvm)
                {
                    int index = mwvm.Wheel.SelectedItemIndex;
                    if (index != -1)
                    {
                        var word = mwvm.Wheel.WheelWords[index];
                        WordSubmission?.Invoke(this, word);
                    }
                }
            }
        }

        private void WordWheelListBox_DoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Debug.WriteLine("WordWheel Double Tapped");
            try
            {
                var index = (this.DataContext as MainWindowViewModel).Wheel.SelectedItemIndex;
                Debug.WriteLine("Selected Item Index is currently " + index);
                var wheelWord = (this.DataContext as MainWindowViewModel).Wheel.WheelWords[index];
                WordSubmission?.Invoke(this, wheelWord);
            } catch(Exception ex)
            {
                Debug.WriteLine("Non-fatal Exception during WordWheel DoubleTap");
                Debug.WriteLine(ex.ToString());
            }
            e.Handled = true;
        }

        /*
        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Debug.WriteLine("WordWheelControl.axaml.cs: View Model Property Changed: " + e.PropertyName);
        }
        */

        private void mwvm_propertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //Debug.WriteLine("WordWheelControl.axaml.cs Wheel_PropertyChanged. e.PropertyName = \"" +  e.PropertyName + "\"");
            if (e.PropertyName == nameof(MainWindowViewModel.Wheel))
            {
                (this.DataContext as MainWindowViewModel).Wheel.PropertyChanged += mwvm_propertyChanged;
            }
            if (e.PropertyName == nameof(WheelViewModel.SelectedItemIndex) || e.PropertyName == nameof(MainWindowViewModel.Wheel))
            {
                var index = (this.DataContext as MainWindowViewModel).Wheel.SelectedItemIndex;
                wordWheelListBox.SelectedIndex = index;
                wordWheelListBox.ScrollIntoView(index);
            }
        }

        private void WheelListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (this.DataContext is MainWindowViewModel mwvm)
            {
                if (wordWheelListBox.SelectedItem != null)
                {
                    
                    if (mwvm.Wheel != null)
                    {
                        mwvm.Wheel.SelectedItemIndex = wordWheelListBox.SelectedIndex;
                        int numItems = mwvm.Wheel.WheelWords.Count;
                        if (ScrollSelectionToTop)
                        {
                            wordWheelListBox.ScrollIntoView(numItems - 1); // Scroll to the end before scrolling into view, this way it will scroll up and show items underneath the item.
                        }
                    }
                    wordWheelListBox.ScrollIntoView(wordWheelListBox.SelectedItem);
                }
            }

        }
    }
}
