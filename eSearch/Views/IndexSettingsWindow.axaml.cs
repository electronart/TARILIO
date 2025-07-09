using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using eSearch.Models;
using eSearch.ViewModels;
using System;
using System.Threading.Tasks;

namespace eSearch;

public partial class IndexSettingsWindow : Window
{

    TaskDialogResult DialogResult = TaskDialogResult.Cancel;

    public IndexSettingsWindow()
    {
        InitializeComponent();
        BtnOK.Click += BtnOK_Click;
        BtnCancel.Click += BtnCancel_Click;
    }

    private void BtnCancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DialogResult = TaskDialogResult.Cancel;
        Close();
    }

    private void BtnOK_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DialogResult = TaskDialogResult.OK;
        Close();
    }

    public static async Task<Tuple<TaskDialogResult,IndexSettingsWindowViewModel>> ShowDialog(IndexSettingsWindowViewModel vm, Window owner)
    {
        IndexSettingsWindow window = new IndexSettingsWindow();
        window.DataContext = vm;
        await window.ShowDialog(owner);
        return new Tuple<TaskDialogResult, IndexSettingsWindowViewModel>(window.DialogResult, vm);
    }
}