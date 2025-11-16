using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using eSearch.Models.AI;
using eSearch.Utils;
using eSearch.ViewModels;
using eSearch.Views;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace eSearch;

public partial class LocalLLMServerWindow : ReactiveWindow<LocalServerWindowViewModel>
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
        }
        catch (Exception ex)
        {
            await TaskDialogWindow.OKDialog("Error", ex.ToString(), this);
        }
        finally
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
        }
        catch (Exception ex)
        {
            await TaskDialogWindow.OKDialog("Error", ex.ToString(), this);
        }
        finally
        {
            UI_Update();
        }
    }

    private async void ButtonCopyAddress_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is LocalServerWindowViewModel vm && Clipboard != null)
        {
            await Clipboard.SetTextAsync(vm.DetectedIPAddress);
            vm.JustCopiedAddress = true;
            await Task.Delay(TimeSpan.FromSeconds(3));
            vm.JustCopiedAddress = false;

        }
    }

    private async void BtnAddFirewallException_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var helper = new WindowsDefenderHelper();
            helper.AddFirewallException();
        }
        catch (UnauthorizedAccessException ex)
        {
            if (ex.Message == "REQUIRE_ELEVATION")
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    ProcessStartInfo proc = new ProcessStartInfo();
                    proc.UseShellExecute = true;
                    proc.WorkingDirectory = Environment.CurrentDirectory;
                    proc.FileName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                    proc.Arguments = "--firewall-add-exception";
                    proc.Verb = "runas";

                    var process = Process.Start(proc);
                    if (process != null)
                    {
                        process.WaitForExit();
                        if (process.ExitCode != 0)
                        {
                            // Something went wrong.
                            throw new Exception("Error creating Firewall rule. Check logs");
                        }
                        else
                        {
                            if (DataContext is LocalServerWindowViewModel vm)
                            {
                                vm.IsFirewallAllowed = true;
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("Failed to launch elevator");
                    }
                } catch (Exception ex2)
                {
                    await TaskDialogWindow.ExceptionDialog("Error adding Firewall rule", ex2, this);
                }
            }
        }
        catch (Exception ex)
        {
            await TaskDialogWindow.ExceptionDialog("Error adding Firewall rule", ex, this);
        }
    }
}