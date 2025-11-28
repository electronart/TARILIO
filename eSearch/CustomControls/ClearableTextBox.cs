using Avalonia.Controls;
using Avalonia.Styling;
using eSearch.ViewModels;
using eSearch.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.CustomControls
{
    public class ClearableTextBox : TextBox, IStyleable
    {
        public event EventHandler? Cleared;

        Type IStyleable.StyleKey => typeof(TextBox);

        public ClearableTextBox()
        {

        }

        public new void Cleary()
        {
            base.Clear();
            Cleared?.Invoke(this, EventArgs.Empty);
        }

        public async void AddAttachment()
        {
            if (Program.GetMainWindow() is MainWindow window && window.DataContext is MainWindowViewModel mwvm)
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return; // Won't be. Just pleasing the compiler.
                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = S.Get("Select File(s) to attach"),
                    AllowMultiple = true,
                });
                if (files != null)
                {
                    foreach (var avFile in files)
                    {
                        System.IO.FileInfo fileNfo = new System.IO.FileInfo(avFile.Path.LocalPath);
                        mwvm.Session.Query.AttachedFiles.Add(fileNfo);
                    }
                }
                this.Focus();
            }
        }
    }
}
