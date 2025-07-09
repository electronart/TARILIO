using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using eSearch.ViewModels;
using eSearch.Views;

#region Temp Testing
using System;
using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Themes.Fluent;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using eSearch.Models.Documents;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Linq;
using com.sun.corba.se.spi.activation;
//using ControlCatalog.Models;
//using ControlCatalog.Pages;
#endregion

namespace eSearch
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            TikaServer.EnsureRunning();
            // Styles.Insert(0, Fluent);
            Styles.Add(DataGridFluent);
            //Styles.Insert(0, DataGridFluent);
            AvaloniaXamlLoader.Load(this);


            DataContext = Program.ProgramConfig.AppDataContext;
            Program.ProgramConfig.AppDataContext.MainWindowViewModel.Initializing = false;

            BackgroundWorker cleanupWorker = new BackgroundWorker();
            cleanupWorker.DoWork += CleanupWorker_DoWork;
            cleanupWorker.RunWorkerAsync();
            //SetLowContrast();
            #region Init Themes to switch between
                // Pull the 'styled' theme out so we can switch back to it.
                foreach (var style in Styles)
                {
                    if (style is FluentTheme theme)
                    {
                        styledFluentTheme = theme;
                    }
                }
                highContrastTheme = new FluentTheme();
            #endregion
            if (Program.ProgramConfig.IsThemeHighContrast)
            {
                ReplaceFluentTheme(highContrastTheme);
            }
            #region Init MCP Servers
            Parallel.ForEach(Program.ProgramConfig.GetAllAvailableMCPServers(), availableServer =>
            {
                try
                {
                    var shouldBeRunning = Program.ProgramConfig.EnabledMCPServerNames.Contains(availableServer.DisplayName);
                    var isRunning = availableServer.IsServerRunning;
                    if (shouldBeRunning && !isRunning)
                    {
                        var started = availableServer.StartServer().Result;
                        if (!started)
                        {
                            Debug.WriteLine("Failed to start Server " + availableServer.DisplayName);
                        }
                    }
                    if (!shouldBeRunning && isRunning)
                    {
                        var stopped = availableServer.StopServer().Result;
                        if (!stopped)
                        {
                            Debug.WriteLine("Failed to stop Server " + availableServer.DisplayName);
                        }
                    }
                } catch (Exception ex)
                {
                    Debug.WriteLine($"MCP Server `{availableServer.DisplayName}` threw exception `{ex.ToString()}`");
                }
            });
            #endregion
        }

        FluentTheme? styledFluentTheme;
        FluentTheme? highContrastTheme;

        private void ReplaceFluentTheme(FluentTheme? newFluentTheme)
        {
            if (newFluentTheme == null) return;
            int i = Styles.Count;
            while (i --> 0)
            {
                var style = Styles[i];
                if (style is FluentTheme) Styles.RemoveAt(i);
            }
            Styles.Add(newFluentTheme);
        }

        public void getThemePrimaryColors(
            out Avalonia.Media.Color regionColor, 
            out Avalonia.Media.Color baseHighColor, 
            out Avalonia.Media.Color altHighColor,
            out Avalonia.Media.Color chromeLow)
        {
            foreach (var style in Styles)
            {
                if (style is FluentTheme theme)
                {
                    try
                    {
                        ThemeVariant themeVariant = (Program.ProgramConfig.IsThemeDark ?? false) ? ThemeVariant.Dark : ThemeVariant.Light;
                        regionColor = theme.Palettes[themeVariant].RegionColor;
                        baseHighColor = theme.Palettes[themeVariant].BaseHigh;
                        altHighColor = theme.Palettes[themeVariant].AltHigh;
                        chromeLow = theme.Palettes[themeVariant].ChromeLow;
                        return;
                    }
                    catch (KeyNotFoundException) {
                        // Not using a palette.
                    }
                }
            }
            // Fallback - High contrast.

            var black = new Avalonia.Media.Color(0, 0, 0, 0);
            var white = new Avalonia.Media.Color(0, 255, 255, 255);

            regionColor = (Program.ProgramConfig.IsThemeDark ?? false) ? black : white;
            baseHighColor = (Program.ProgramConfig.IsThemeDark ?? false) ? white : black;
            altHighColor = regionColor;
            chromeLow = regionColor;
            return;
        }

        public void SetHighContrast(bool highContrast)
        {
            if (highContrast)
            {
                ReplaceFluentTheme(highContrastTheme);
            } else
            {
                ReplaceFluentTheme(styledFluentTheme);
            }
        }

        private void CleanupWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            if (eSearch.Models.Utils.IsOnlyRunningCopyOfESearch())
            {
                // Cleanup Extractions folder.
                string path = System.IO.Path.Combine(Program.ESEARCH_TEMP_FILES_PATH, "Extractions");
                if (Directory.Exists(path))
                {
                    foreach (var file in Directory.GetFiles(path, "*", new EnumerationOptions { IgnoreInaccessible = true }))
                    {
                        try
                        {
                            System.IO.File.Delete(file);
                        } catch (Exception ex)
                        {
                            Debug.WriteLine($"Failed to clean up file {file}. {ex.Message}");
                        }
                    }
                }
            }
        }

        #region Theme handling

        public static readonly StyleInclude DataGridFluent = new StyleInclude(new Uri("avares://eSearch/Styles"))
        {
            Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml")
        };

        public static readonly StyleInclude DataGridDefault = new StyleInclude(new Uri("avares://eSearch/Styles"))
        {
            Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Default.xaml")
        };

        /*
        public FluentTheme Fluent
        {
            get
            {
                int i = 0;
                int len = Styles.Count;
                while (i < len)
                {
                    var style = Styles[i];
                    if (style is FluentTheme fluentTheme)
                    {
                        return fluentTheme;
                    }
                    ++i;
                }
                return null;
            }
        }
        */


        public static Styles DefaultLight = new Styles
        {
            new StyleInclude(new Uri("resm:Styles?assembly=eSearch"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/AccentColors.xaml")
            },
            new StyleInclude(new Uri("resm:Styles?assembly=eSearch"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/Base.xaml")
            },
            new StyleInclude(new Uri("resm:Styles?assembly=eSearch"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/BaseLight.xaml")
            },
            new StyleInclude(new Uri("resm:Styles?assembly=eSearch"))
            {
                Source = new Uri("avares://Avalonia.Themes.Default/Accents/BaseLight.xaml")
            },
            new StyleInclude(new Uri("resm:Styles?assembly=eSearch"))
            {
                Source = new Uri("avares://Avalonia.Themes.Default/DefaultTheme.xaml")
            }
        };

        public static Styles DefaultDark = new Styles
        {
            new StyleInclude(new Uri("resm:Styles?assembly=eSearch"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/AccentColors.xaml")
            },
            new StyleInclude(new Uri("resm:Styles?assembly=eSearch"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/Base.xaml")
            },
            new StyleInclude(new Uri("resm:Styles?assembly=eSearch"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/BaseDark.xaml")
            },
            new StyleInclude(new Uri("resm:Styles?assembly=eSearch"))
            {
                Source = new Uri("avares://Avalonia.Themes.Default/Accents/BaseDark.xaml")
            },
            new StyleInclude(new Uri("resm:Styles?assembly=eSearch"))
            {
                Source = new Uri("avares://Avalonia.Themes.Default/DefaultTheme.xaml")
            }
        };
        #endregion

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && Current.DataContext is AppDataContext appDataContext)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = appDataContext.MainWindowViewModel,
                };
                desktop.ShutdownRequested += Desktop_ShutdownRequested;
                desktop.Exit += Desktop_Exit;
            }
            base.OnFrameworkInitializationCompleted();
        }

        private void Desktop_Exit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            Debug.WriteLine("Application Exit. Saving Configuration / State");
            try
            {
                if (Program.GetMainWindow()?.DataContext is MainWindowViewModel mwvm)
                {
                    mwvm.IndexLibrary.SaveLibrary();
                }
                Program.SaveProgramConfig(); // Persist this for later.
            } catch (Exception ex)
            {
                Debug.WriteLine("Exception during exit: " + ex.ToString());
            }
            Debug.WriteLine("Shutting down Tika.");
            try
            {
                TikaServer.StopServer();
            } catch (Exception ex)
            {
                Debug.WriteLine("Exception shutting down Tika: " + ex.ToString());
            }
        }

        private void Desktop_ShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
        {
            Debug.WriteLine("Huh??");
            // When Avalonia closes, kill the tika server.
            //Debug.WriteLine("Application ShutDown. Closing Tika...");
            //TikaServer.StopServer();
            
        }
    }
}