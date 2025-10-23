using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using eSearch.Models.AI;
using eSearch.ViewModels;
using eSearch.Views;
using System;
using System.Threading;

namespace eSearch;

public partial class LocalLLMServerWindow : Window
{

    public LocalLLMServerWindow()
    {
        InitializeComponent();
        BtnStartServer.Click += BtnStartServer_Click;
        BtnStopServer.Click += BtnStopServer_Click;
        ButtonClose.Click += ButtonClose_Click;
        Closed += LocalLLMServerWindow_Closed;
    }

    private void ButtonClose_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private void LocalLLMServerWindow_Closed(object? sender, System.EventArgs e)
    {
        
    }

    private void UI_Update()
    {
        if (DataContext is LocalServerWindowViewModel vm)
        {
            vm.IsServerRunning = Program.RunningLocalLLMServer != null;
        }
    }

    private async void BtnStopServer_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            if (Program.RunningLocalLLMServer != null)
            {
                await Program.RunningLocalLLMServer.StopAsync();
                Program.RunningLocalLLMServer = null;
                UI_Update();
            }
        } catch (Exception ex)
        {
            await TaskDialogWindow.OKDialog("Error", ex.ToString(), this);
        } finally
        {
            UI_Update();
        }
    }

    private async void BtnStartServer_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            if (Program.RunningLocalLLMServer != null)
            {
                await Program.RunningLocalLLMServer.StopAsync();
                Program.RunningLocalLLMServer = null;
            }

            if (DataContext is LocalServerWindowViewModel vm)
            {
                int port = vm.Port;
                if (port > 0)
                {
                    Program.ProgramConfig.LocalLLMServerConfig.Port = port;
                    Program.SaveProgramConfig();
                }
                LocalLLMServer server = new LocalLLMServer(port);
                await server.StartAsync();
                Program.RunningLocalLLMServer = server;
            }
        } catch (Exception ex)
        {
            await TaskDialogWindow.OKDialog("Error", ex.ToString(), this);
        } finally
        {
            UI_Update();
        }
    }
}