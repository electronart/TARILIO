using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using com.googlecode.mp4parser.boxes.apple;
using DynamicData;
using eSearch.Models;
using eSearch.ViewModels;
using eSearch.Views;
using System;
using System.Linq;
using System.Threading.Tasks;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch;

public partial class PluginsWindow : Window
{
    public PluginsWindow()
    {
        InitializeComponent();
        BtnInstallPlugin.Click += BtnInstallPlugin_Click;
        BtnUninstallPlugin.Click += BtnUninstallPlugin_Click;
        BtnClose.Click += BtnClose_Click;
        ListBoxPlugins.SelectionChanged += ListBoxPlugins_SelectionChanged;
    }

    private void BtnClose_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private async void BtnUninstallPlugin_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is PluginsWindowViewModel vm)
        {
            if (vm.SelectedPlugin == null) return;

            var res = await TaskDialogWindow.OKCancel(
                String.Format(S.Get("Remove Plugin {0}?"), vm.SelectedPlugin.ToString()),
                "",
                this,
                S.Get("Remove"));

            if (res == TaskDialogResult.OK)
            {
                vm.IsPluginInstalling = true;
                try
                {
                    await Program.PluginLoader.UninstallPlugin(vm.SelectedPlugin);
                    vm.AvailablePlugins.Remove(vm.SelectedPlugin);
                }
                catch (Exception ex)
                {
                    await TaskDialogWindow.OKDialog("Error Removing Plugin", ex.ToString(), this);
                }
                finally
                {
                    vm.IsPluginInstalling = false;
                }
            }
        }
    }

    private void ListBoxPlugins_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is PluginsWindowViewModel vm)
        {
            var manifest = vm.SelectedPlugin?.GetPluginManifest();
            vm.PluginName           = manifest?.GetPluginName() ?? "";
            vm.PluginAuthor         = manifest?.GetPluginAuthor() ?? "";
            vm.PluginDescription    = manifest?.GetPluginDescription() ?? "";
            vm.PluginVersion        = vm.SelectedPlugin?.GetPluginVersion() ?? "";
        }
    }

    private async void BtnInstallPlugin_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is PluginsWindowViewModel viewModel)
        {
            string pluginFile = null;
            #region Prompt user to browse for a file.
            var dialog = new OpenFileDialog
            {
                Title = S.Get("Open Plugin File"),
                AllowMultiple = false,
                Filters = new()
                {
                    new FileDialogFilter { Name = "eSearch Plugin", Extensions = { "esPlugin" }}
                }
            };
            var result = await dialog.ShowAsync(this);
            if (result?.Length > 0)
            {
                pluginFile = result[0];
            }
            #endregion
            #region If a file is selected, show progress and install the plugin.
            if (pluginFile != null && System.IO.File.Exists(pluginFile))
            {
                try
                {
                    viewModel.IsPluginInstalling = true;
                    await Program.PluginLoader.InstallPlugin(pluginFile);
                    viewModel.AvailablePlugins.Clear();
                    viewModel.AvailablePlugins.AddRange(await Program.PluginLoader.GetInstalledPlugins());
                    viewModel.SelectedPlugin = viewModel.AvailablePlugins.Last();
                } catch (Exception ex)
                {
                    await TaskDialogWindow.OKDialog(S.Get("Could not install plugin"), ex.Message, this);
                } finally
                {
                    viewModel.IsPluginInstalling = false;
                }
            }
            #endregion
            
        }
    }

    public static async Task ShowPluginWindowDialog(Window owner)
    {
        var viewModel = new PluginsWindowViewModel();
        var plugins   = await Program.PluginLoader.GetInstalledPlugins();
        
        viewModel.AvailablePlugins.AddRange(plugins);
        viewModel.PluginName        = string.Empty;
        viewModel.PluginDescription = string.Empty;
        viewModel.PluginAuthor      = string.Empty;

        var window = new PluginsWindow();
        window.DataContext = viewModel;

        await window.ShowDialog(owner);
    }
}