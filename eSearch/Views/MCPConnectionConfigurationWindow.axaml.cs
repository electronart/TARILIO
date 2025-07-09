using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using DynamicData;
using eSearch.Interop.AI;
using eSearch.Models;
using eSearch.Models.AI.MCP;
using eSearch.Utils;
using eSearch.ViewModels;
using eSearch.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch;

public partial class MCPConnectionConfigurationWindow : Window
{
    public TaskDialogResult DialogResult = TaskDialogResult.Cancel;


    private Timer? timer;
    

    public MCPConnectionConfigurationWindow()
    {
        InitializeComponent();

        BtnPasteConfiguration.Click += BtnPasteConfiguration_Click;
        BtnAddConnection.Click += BtnAddConnection_Click;
        BtnRemoveConnection.Click += BtnRemoveConnection_Click;
        BtnSaveConfiguration.Click += BtnSaveConfiguration_Click;
        BtnClose.Click += BtnClose_Click;
        BtnCancelEditConfiguration.Click += BtnCancelEditConfiguration_Click;

        this.Opened += MCPConnectionConfigurationWindow_Opened;
        this.Closed += MCPConnectionConfigurationWindow_Closed;
        DataContextChanged += MCPConnectionConfigurationWindow_DataContextChanged;

        BtnStartServer.Click += BtnStartServer_Click;
        BtnStopServer.Click += BtnStopServer_Click;
        BtnEditConnection.Click += BtnEditConnection_Click;
    }

    private void BtnEditConnection_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MCPConnectionConfigurationWindowViewModel vm)
        {
            if (vm.SelectedMCPServer is UserConfiguredMCPServer userConfiguredServer)
            {
                vm.IsFormEditMode = true;
                vm.CurrentConfigurationJson = userConfiguredServer.Json;
            }
        }
    }

    private async void BtnStopServer_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MCPConnectionConfigurationWindowViewModel vm)
        {
            if (vm.SelectedMCPServer != null)
            {
                await vm.SelectedMCPServer.StopServer();
                BtnStopServer.IsEnabled = false;
                Program.ProgramConfig.EnabledMCPServerNames.RemoveAll(x => x == vm.SelectedMCPServer.DisplayName);
                Program.SaveProgramConfig();
            }
        }
    }

    private async void BtnStartServer_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MCPConnectionConfigurationWindowViewModel vm)
        {
            if (vm.SelectedMCPServer != null)
            {
                await vm.SelectedMCPServer.StartServer();
                BtnStartServer.IsEnabled = false;
                if (!Program.ProgramConfig.EnabledMCPServerNames.Contains(vm.SelectedMCPServer.DisplayName))
                {
                    Program.ProgramConfig.EnabledMCPServerNames.Add(vm.SelectedMCPServer.DisplayName);
                }
                Program.SaveProgramConfig();
            }
        }
    }

    private void MCPConnectionConfigurationWindow_DataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MCPConnectionConfigurationWindowViewModel vm)
        {
            vm.PropertyChanged += ViewModel_PropertyChanged;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (DataContext is MCPConnectionConfigurationWindowViewModel vm)
        {
            if (e.PropertyName == nameof(vm.SelectedMCPServer))
            {
                vm.IsFormEditMode = false;
                if (vm.SelectedMCPServer != null)
                {
                    vm.ShowServerConfigurationPanel = true;
                }
            }
            if (e.PropertyName == nameof(vm.IsFormEditMode))
            {
                if (vm.IsFormEditMode)
                {
                    TextBoxConfigJson.Classes.Remove("ConsoleDisplay");
                } else
                {
                    TextBoxConfigJson.Classes.Add("ConsoleDisplay");
                }
            }
        }
    }

    private void MCPConnectionConfigurationWindow_Closed(object? sender, System.EventArgs e)
    {
        timer?.Stop();
        var temp = timer;
        timer = null;
        temp?.Dispose();
    }

    private void MCPConnectionConfigurationWindow_Opened(object? sender, System.EventArgs e)
    {
        timer?.Stop();
        timer?.Dispose();

        timer = new Timer(500);
        timer.Elapsed += Timer_Elapsed;
        timer.Start();
    }

    private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
    {

        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            #region Update ListBox
            foreach (var listBoxItem in ListBoxConfiguredServers.GetVisualDescendants().OfType<ListBoxItem>())
            {
                // TODO A bit of an ugly hack here.
                try
                {
                    var textBlock = listBoxItem.FindChildByClass<TextBlock>("ServerStatus");
                    if (textBlock != null)
                    {
                        textBlock.FontSize = 12;
                        if (listBoxItem.DataContext is IESearchMCPServer mcpServer)
                        {
                            if (mcpServer.IsServerRunning && !mcpServer.IsErrorState)
                            {
                                textBlock.Text = "• " + S.Get("Running");
                                textBlock.Foreground = new SolidColorBrush(new Color(255, 50, 255, 50));
                            }
                            else
                            {
                                string state = mcpServer.IsErrorState ? S.Get("Error") : S.Get("Not Running");
                                textBlock.Text = "• " + state;
                                textBlock.Foreground = new SolidColorBrush(new Color(255, 255, 50, 50));
                            }
                        }
                    }
                } catch (Exception ex)
                {
                    // Exceptions here are caused by removing a server whilst it is iterating the servers...
                    // Just swallow these..
                    Debug.WriteLine(ex.ToString());
                }
            }
            #endregion
            #region If currently viewing a server, update the console output...
            if (DataContext is MCPConnectionConfigurationWindowViewModel vm)
            {
                if (!vm.IsFormEditMode && vm.SelectedMCPServer != null)
                {
                    // Currently viewing a server...
                    bool running = vm.SelectedMCPServer.IsServerRunning;
                    if (running)
                    {
                        // Display the last 10 lines of console output
                        vm.CurrentConfigurationJson =
                        string.Join(Environment.NewLine, vm.SelectedMCPServer.ConsoleOutputDisplayLines);
                        //            //vm.SelectedMCPServer.ConsoleOutputDisplayLines.Skip(
                        //            //    Math.Max(0, vm.SelectedMCPServer.ConsoleOutputDisplayLines.Count() - 10)
                        //            )
                        //);
                        if (string.IsNullOrWhiteSpace(vm.CurrentConfigurationJson))
                        {
                            vm.CurrentConfigurationJson = "No Output Yet.";
                        }
                    } else
                    {
                        vm.CurrentConfigurationJson = S.Get("Server Stopped");
                    }
                    BtnStartServer.IsEnabled = !running;
                    BtnStopServer.IsEnabled = running;
                }
            }
            #endregion
        });
    }

    private void BtnCancelEditConfiguration_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MCPConnectionConfigurationWindowViewModel vm)
        {
            vm.IsFormEditMode = false;
        }
    }

    private void BtnClose_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private void BtnAddConnection_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MCPConnectionConfigurationWindowViewModel vm)
        {
            vm.SelectedMCPServer = null;
            vm.CurrentConfigurationJson = string.Empty;
            vm.ShowServerConfigurationPanel = true;
            vm.IsFormEditMode = true;
        }
    }

    private async void BtnSaveConfiguration_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MCPConnectionConfigurationWindowViewModel vm)
        {
            if (vm.SelectedMCPServer == null)
            {
                // We're creating a new MCP Server.
                UserConfiguredMCPServer.TryGetValidUserConfiguredMCPServer(vm.CurrentConfigurationJson, out var userConfiguredServer);
                if (userConfiguredServer == null)
                {
                    await TaskDialogWindow.OKDialog(S.Get("Invalid Configuration"), S.Get("Enter a valid MCP Server Configuration"), this);
                    return;
                }

                #region Ensure no other server configuration has the same name
                var existingServer = vm.AvailableMCPServers.FirstOrDefault(x => x.DisplayName ==  userConfiguredServer.DisplayName, null);
                if (existingServer != null)
                {
                    await TaskDialogWindow.OKDialog(string.Format(S.Get("\"{0}\" already exists."), userConfiguredServer.DisplayName), S.Get("Server must be given a unique name."), this);
                    return;
                }
                #endregion
                #region TODO Prompt about safety/trust

                #endregion
                vm.AvailableMCPServers.Add(userConfiguredServer);
                vm.AvailableMCPServers.OrderBy( x => x.DisplayName);
                vm.SelectedMCPServer = userConfiguredServer;
                SaveConfigurations();
            }
            else
            {
                // We're updating an existing MCP Server.
                if (vm.SelectedMCPServer is UserConfiguredMCPServer userConfiguredServer)
                {
                    await userConfiguredServer.StopServer();
                    UserConfiguredMCPServer.TryGetValidUserConfiguredMCPServer(vm.CurrentConfigurationJson, out var updatedUserConfiguredServer);
                    if (updatedUserConfiguredServer == null)
                    {
                        await TaskDialogWindow.OKDialog(S.Get("Invalid Configuration"), S.Get("Enter a valid MCP Server Configuration"), this);
                        return;
                    }

                    var existingServer = vm.AvailableMCPServers.FirstOrDefault(x => x.DisplayName == updatedUserConfiguredServer.DisplayName && x != userConfiguredServer, null);
                    if (existingServer != null)
                    {
                        await TaskDialogWindow.OKDialog(string.Format(S.Get("\"{0}\" already exists."), userConfiguredServer.DisplayName), S.Get("Server must be given a unique name."), this);
                        return;
                    }
                    vm.AvailableMCPServers.Remove(userConfiguredServer);
                    vm.AvailableMCPServers.Add(updatedUserConfiguredServer);
                    vm.AvailableMCPServers.OrderBy(x => x.DisplayName);
                    vm.SelectedMCPServer = userConfiguredServer;
                    SaveConfigurations();
                    if (Program.ProgramConfig.EnabledMCPServerNames.Contains(userConfiguredServer.DisplayName)) await userConfiguredServer.StartServer();

                }
            }
        }
    }

    private async void BtnRemoveConnection_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MCPConnectionConfigurationWindowViewModel vm)
        {
            var selectedServer = vm.SelectedMCPServer;
            if (selectedServer is UserConfiguredMCPServer)
            {
                if (selectedServer == null) return;
                var dialogResult = await TaskDialogWindow.DeleteCancel(
                    string.Format(S.Get("Delete {0}?"), selectedServer.DisplayName),
                    string.Empty,
                    this
                );

                if (dialogResult == TaskDialogResult.Cancel) return;
                await selectedServer.StopServer();
                vm.AvailableMCPServers.Remove(selectedServer);
                vm.SelectedMCPServer = null;
                vm.ShowServerConfigurationPanel = false;
                SaveConfigurations();
            }
        }
    }



    private async void BtnPasteConfiguration_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MCPConnectionConfigurationWindowViewModel vm)
        {
            vm.CurrentConfigurationJson = await Clipboard?.GetTextAsync()  ?? string.Empty;
        }
    }

    private void SaveConfigurations()
    {
        if (DataContext is MCPConnectionConfigurationWindowViewModel vm)
        {
            Program.ProgramConfig.UserConfiguredMCPServers.Clear();
            Program.ProgramConfig.UserConfiguredMCPServers.AddRange(vm.AvailableMCPServers.OfType<UserConfiguredMCPServer>());
            Program.SaveProgramConfig();
        }
    }

    public static async Task<TaskDialogResult> ShowDialog(Window owner)
    {
        
        var dialog = new MCPConnectionConfigurationWindow();
        var context = new MCPConnectionConfigurationWindowViewModel();

        List<IESearchMCPServer> mcpServers = new List<IESearchMCPServer>();
        context.AvailableMCPServers.AddRange(Program.ProgramConfig.GetAllAvailableMCPServers());

        dialog.DataContext = context;
        await ((Window)dialog).ShowDialog(owner);
        return dialog.DialogResult;
    }

}