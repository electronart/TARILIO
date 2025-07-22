using Avalonia.Controls;
using eSearch.ViewModels;
using System;
using System.IO;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.Views
{
    public partial class CopyConversationWindow : Window
    {
        bool pressedOK = false;

        public CopyConversationWindow()
        {
            InitializeComponent();
            ButtonOK.Click += ButtonOK_Click;
            ButtonCancel.Click += ButtonCancel_Click;
            ButtonBrowseForSavePath.Click += ButtonBrowseForSavePath_Click;
            KeyUp += CopyDocumentWindow_KeyUp;
            DataContextChanged += CopyConversationWindow_DataContextChanged;
        }

        private void CopyConversationWindow_DataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is CopyConversationWindowViewModel vm)
            {
                vm.PropertyChanged += Vm_PropertyChanged;
                if (vm.GetCopySetting() == CopyConversationWindowViewModel.CopySetting.Clipboard)
                {
                    vm.DialogOKButtonText = S.Get("Copy");
                }
                else
                {
                    vm.DialogOKButtonText = S.Get("Save");
                }
            }
        }

        private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (DataContext is CopyDocumentWindowViewModel vm)
            {
                if (    e.PropertyName == nameof(vm.IsRadioClipBoardChecked) 
                    ||  e.PropertyName == nameof(vm.IsRadioFileChecked) )
                {
                    if (vm.GetCopySetting() == CopyDocumentWindowViewModel.CopySetting.Clipboard)
                    {
                        vm.DialogOKButtonText = S.Get("Copy");
                    } else
                    {
                        vm.DialogOKButtonText = S.Get("Save");
                    }
                }
            }
        }

        private void CopyDocumentWindow_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Escape)
            {
                Close();
            }
        }

        private async void ButtonBrowseForSavePath_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var openFolderDialog = new OpenFolderDialog();
            var initialDiretory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (DataContext != null && DataContext is CopyConversationWindowViewModel copyContext)
            {
                if (Directory.Exists(copyContext.SavePath)) initialDiretory = copyContext.SavePath;

                openFolderDialog.Directory = initialDiretory;

                var res = await openFolderDialog.ShowAsync(this);
                if (res != null)
                {
                    // res is a directory.
                    copyContext.SavePath = res;
                }
            }
        }

        public bool DidPressOK()
        {
            return pressedOK;
        }

        private void ButtonCancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            pressedOK = false;
            this.Close();
        }

        private void ButtonOK_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            pressedOK = true;
            this.Close();
        }
    }
}
