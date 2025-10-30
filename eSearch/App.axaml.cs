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
using DocumentFormat.OpenXml.Drawing;
using Avalonia.Threading;
using eSearch.Models.Logging;
using eSearch.ViewModels.StatusUI;
//using ControlCatalog.Models;
//using ControlCatalog.Pages;
#endregion

namespace eSearch
{
    public partial class App : Application
    {
        private SplashWindow? _splashWindow;

        public override void Initialize()
        {
            
            // Styles.Insert(0, Fluent);
            Styles.Add(DataGridFluent);
            //Styles.Insert(0, DataGridFluent);
            AvaloniaXamlLoader.Load(this);


            
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

        private FluentTheme CreateHighContrastTheme()
        {
            var theme = new FluentTheme();  // Keep this.

            var lightPalette = new ColorPaletteResources
            {
                RegionColor = Colors.White,
                BaseHigh = Colors.Black,
                AltHigh = Colors.White,
                ChromeLow = Colors.White
            };

            var darkPalette = new ColorPaletteResources
            {
                RegionColor = Colors.Black,
                BaseHigh = Colors.White,
                AltHigh = Colors.Black,
                ChromeLow = Colors.Black
            };

            theme.Palettes[ThemeVariant.Light] = lightPalette;
            theme.Palettes[ThemeVariant.Dark] = darkPalette;
            return theme;
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
                        Debug.WriteLine("No palette");
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
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _splashWindow = new SplashWindow();
                desktop.MainWindow = _splashWindow;
                _splashWindow.Show();
                Task.Run(async () =>
                {
                    try
                    {
                        #region Init Tasks
                        Task initProgramConfig = Task.Run(() =>
                        {
                            var programConfig = Program.ProgramConfig; // This will call the getter and cause it to perform IO.
                        });

                        Task initTikaTask = Task.Run(() =>
                        {
                            TikaServer.EnsureRunning();
                        });

                        string? llama_error_msg = null;
                        bool llama_initialized = false;
                        Exception? llama_exception = null;

                        Task initLLamaSharp = Task.Run(() =>
                        {


                            try
                            {
                                MSLogger wrappedDebugLogger = new MSLogger(new DebugLogger());
                                llama_initialized = LLamaBackendConfigurator.ConfigureBackend2(null, false, async delegate (string msg)
                                {
                                    llama_error_msg = msg;
                                }, wrappedDebugLogger).GetAwaiter().GetResult();
                            }
                            catch (Exception ex)
                            {
                                llama_exception = ex;
                            }
                        });
                        Task.WaitAll(initLLamaSharp, initTikaTask, initProgramConfig);
                        #endregion
                        // Here's where the initialization logic goes that needs to appear before the main window...
                        var upTime = Program.GetSystemUptime();
                        if (upTime.TotalMinutes < 10 && Program.LLAMA_BACKEND == "NONE")
                        {
                            await Task.Delay(TimeSpan.FromSeconds(30));
                            // Restart the app with same args
                            var psi = new ProcessStartInfo
                            {
                                FileName = Environment.ProcessPath,
                                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                                UseShellExecute = true // Helps with desktop apps
                            };
                            Process.Start(psi);

                            // Exit current process immediately (prevents app from starting)
                            Environment.Exit(0);
                            return;
                        }

                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            DataContext = Program.ProgramConfig.AppDataContext;
                            Program.ProgramConfig.AppDataContext.MainWindowViewModel.Initializing = false;

                            #region Init Themes to switch between
                            // Pull the 'styled' theme out so we can switch back to it.
                            foreach (var style in Styles)
                            {
                                if (style is FluentTheme theme)
                                {
                                    styledFluentTheme = theme;
                                }
                            }
                            if (highContrastTheme == null) highContrastTheme = CreateHighContrastTheme();

                            #endregion
                            if (Program.ProgramConfig.IsThemeHighContrast)
                            {
                                ReplaceFluentTheme(highContrastTheme);
                            }
                        });
                        

                        BackgroundWorker cleanupWorker = new BackgroundWorker();
                        cleanupWorker.DoWork += CleanupWorker_DoWork;
                        cleanupWorker.RunWorkerAsync();
                        
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
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"MCP Server `{availableServer.DisplayName}` threw exception `{ex.ToString()}`");
                            }
                        });
                        #endregion


                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            var _mainWindow = new MainWindow
                            {
                                DataContext = Program.ProgramConfig.AppDataContext.MainWindowViewModel
                            };
                            desktop.MainWindow = _mainWindow;
                            _mainWindow.Show();
                            _splashWindow.Close();
                            desktop.ShutdownRequested += Desktop_ShutdownRequested;
                            desktop.Exit += Desktop_Exit;
                            _splashWindow = null;

                            #region Display any llama sharp errors now
                            if (llama_error_msg != null && !llama_initialized)
                            {

                                if (_mainWindow.DataContext is MainWindowViewModel mwvm)
                                {
                                    var errorStatus =
                                    new StatusControlViewModel
                                    {
                                        StatusTitle = "LlamaSharp not Initialized",
                                        StatusMessage = "Click for details",
                                        ClickAction = async () =>
                                        {
                                            await TaskDialogWindow.OKDialog("LlamaSharp Error", llama_error_msg, _mainWindow);
                                        }
                                    };
                                    errorStatus.DismissAction = () =>
                                    {
                                        mwvm.StatusMessages.Remove(errorStatus);
                                    };
                                    mwvm.StatusMessages.Add(errorStatus);
                                }
                                Debug.WriteLine(llama_error_msg);
                            }
                            if (llama_exception != null)
                            {
                                Window? mainWindow = null;
                                while (mainWindow == null)
                                {
                                    await Task.Delay(TimeSpan.FromSeconds(2));
                                    mainWindow = Program.GetMainWindow();

                                }
                                if (mainWindow.DataContext is MainWindowViewModel mwvm)
                                {
                                    var errorStatus =
                                    new StatusControlViewModel
                                    {
                                        StatusTitle = "LlamaSharp not Initialized",
                                        StatusMessage = "Click for details",
                                        ClickAction = async () =>
                                        {
                                            await TaskDialogWindow.ExceptionDialog("LlamaSharp Exception", llama_exception, mainWindow);
                                        }
                                    };
                                    errorStatus.DismissAction = () =>
                                    {
                                        mwvm.StatusMessages.Remove(errorStatus);
                                    };
                                    mwvm.StatusMessages.Add(errorStatus);
                                }
                                Debug.WriteLine(llama_exception.ToString());
                            }
                            #endregion
                        });
                    } catch (Exception ex)
                    {
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            if (_splashWindow != null)
                            {
                                await TaskDialogWindow.ExceptionDialog("Error Starting Application", ex, _splashWindow);
                                _splashWindow?.Close();
                            }
                        });
                    }
                });

                
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