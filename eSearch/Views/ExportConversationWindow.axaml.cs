using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using eSearch.Models;
using eSearch.ViewModels;
using System;
using System.IO;

namespace eSearch;

public partial class ExportConversationWindow : Window
{
    public ExportConversationWindow()
    {
        InitializeComponent();
        ButtonOK.Click += ButtonOK_Click;
        ButtonCancel.Click += ButtonCancel_Click;
        BtnBrowseForExportFolder.Click += BtnBrowseForExportFolder_Click;
    }

    private async void BtnBrowseForExportFolder_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var openFolderDialog = new OpenFolderDialog();
        var initialDiretory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (DataContext != null && DataContext is ExportConversationWindowViewModel exportContext)
        {
            if (Directory.Exists(exportContext.ExportDirectory)) initialDiretory = exportContext.ExportDirectory;

            openFolderDialog.Directory = initialDiretory;

            var res = await openFolderDialog.ShowAsync(this);
            if (res != null)
            {
                // res is a directory.
                exportContext.ExportDirectory = res;
            }
        }
    }

    private void ButtonCancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private void ButtonOK_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(TaskDialogResult.OK);
    }
}