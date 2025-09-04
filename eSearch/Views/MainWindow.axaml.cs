using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using eSearch.CustomControls;
using eSearch.Models;
using eSearch.Models.AI;
using eSearch.Models.Configuration;
using eSearch.Models.Documents.Parse;
using eSearch.Models.Indexing;
using eSearch.Models.Search;
using eSearch.Models.Voice;
using eSearch.Utils;
using eSearch.ViewModels;
using eSearch.ViewModels.StatusUI;
using ModelContextProtocol.Client;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.Views
{
    public partial class MainWindow : Window
    {
        HtmlDocumentControl htmlDocumentControl;
        ClearableTextBox queryTextBox;
        public MainWindow()
        {
            InitializeComponent();

            htmlDocumentControl = this.FindControl<HtmlDocumentControl>("DocumentViewer");
            queryTextBox = MWSearchControl.FindControl<ClearableTextBox>("QueryTextBox");
            queryTextBox.Cleared += QueryTextBox_Cleared;

            queryTextBox.AddHandler(InputElement.KeyDownEvent, QueryTextBox_KeyDown, RoutingStrategies.Tunnel);

            
            var wordWheel = WordWheelControl;
            wordWheel.WordSubmission += WordWheel_WordSubmission;

            this.DataContextChanged += MainWindow_DataContextChanged;

            menuItemSearchAISearch.Click += MenuItemSearchAISearch_Click;
            menuItemSearchMCPServers.Click += MenuItemSearchMCPServers_Click;
            menuItemAIExportConversation.Click += MenuItemAIExportConversation_Click;
            menuItemAIImportConversation.Click += MenuItemAIImportConversation_Click;

            menuItemPlugins.Click += MenuItemPlugins_Click;

            menuItemIndexNew.Click += MenuItemIndexNew_Click;
            menuItemIndexUpdate.Click += MenuItemIndexUpdate_Click;
            menuItemIndexManageIndexes.Click += MenuItemIndexManageIndexes_Click;
            ResultsSettingsButton.Click += ResultsSettingsButton_Click;
            ResultsCopyButton.Click += ResultsCopyButton_Click;
            DocumentCopyButton.Click += DocumentCopyButton_Click;
            ConversationCopyButton.Click += ConversationCopyButton_Click;

            menuAppearanceHighContrast.Click += MenuAppearanceHighContrast_Click;


            AddHandler(KeyDownEvent, MainWindow_KeyDown, RoutingStrategies.Tunnel);
            //this.KeyDown += MainWindow_KeyDown;

            menuItemLaunchAtStartup.Click += MenuItemLaunchAtStartup_Click;
            menuItemLaunchAtStartupCheckbox.IsChecked = Program.ProgramConfig.LaunchAtStartup;

            //ResultsGrid2.KeyUp += ResultsGrid2_KeyUp;
            ResultsGrid2.AddHandler(KeyDownEvent, ResultsGrid2_KeyDown, RoutingStrategies.Tunnel);

            MenuItemDebugMCPListTools.Click += MenuItemDebugMCPListTools_Click;
            menuItemShowSystemPrompt.Click += MenuItemShowSystemPrompt_Click;

            menuItemDebugListModels.Click += MenuItemDebugListModels_Click;

            Program.OnLanguageChanged += Program_OnLanguageChanged;

        }

        private void Program_OnLanguageChanged(object? sender, EventArgs e)
        {
            try
            {
                init_columns();
            } catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing results columns after language change: {ex.ToString()}");
            }
        }

        private async void MenuItemDebugListModels_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel mwvm)
            {
                if (mwvm.Session.Query.UseAISearch == false) return;
                var aiConfig = Program.ProgramConfig.GetSelectedConfiguration();
                if (aiConfig == null)
                {
                    return;
                }
                Debug.WriteLine("Fetching available models...");
                CancellationTokenSource cts = new CancellationTokenSource(new TimeSpan(0, 0, 30));
                var models = await Completions.TryGetAvailableModels(Completions.GetOpenAIEndpointURL(aiConfig), eSearch.Models.Utils.Base64Decode(aiConfig.APIKey), cts.Token);
                if (models == null) {
                    Debug.WriteLine("Error fetching models.");
                    return;
                }
                Debug.WriteLine($"{models.Count} models found:");
                foreach(var model in models)
                {
                    Debug.WriteLine($" - {model}");
                }
            }
        }

        private void MenuItemShowSystemPrompt_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel mwvm)
            {
                mwvm.Session.Query.ShowSystemPrompt = !mwvm.Session.Query.ShowSystemPrompt;
            }
        }

        private async void MenuItemAIImportConversation_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainWindowViewModel mwvm)
                {
                    var topLevel = TopLevel.GetTopLevel(this);
                    if (topLevel == null) throw new Exception("Unexpected - TopLevel is null");
                    var suggestExportToFolder = Program.ProgramConfig.PreferredConversationSaveLocation;
                    IStorageFolder? startFolder = await topLevel.StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);
                    if (suggestExportToFolder != null)
                    {
                        startFolder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(suggestExportToFolder);
                    }

                    var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        Title = S.Get("Import Conversation"),
                        AllowMultiple = false,
                        FileTypeFilter = AIImportExportFileTypes,
                        SuggestedStartLocation = startFolder
                    });

                    if (files.Count == 0) return; // Cancelled.
                    if (files.Count > 1) throw new Exception("May only import 1 conversation to view");
                    string? localPath = files[0]?.TryGetLocalPath();
                    if (localPath == null) throw new Exception("Could not understand path");
                    string extension = Path.GetExtension(localPath).ToLower();
                    Conversation? importedConversation = null;
                    if (extension == ".csv")
                    {
                        importedConversation = Conversation.ImportFromCSVFile(localPath);
                    }
                    if (extension == ".jsonl")
                    {
                        importedConversation = Conversation.ImportFromJsonLFile(localPath);
                    }
                    if (extension == ".json" || extension == ".econvo")
                    {
                        importedConversation = JsonConvert.DeserializeObject<Conversation>(File.ReadAllText(localPath));
                    }
                    if (importedConversation == null) throw new Exception("Imported Conversation null?");

                    if (mwvm.SelectedSearchSource?.Source is AISearchConfiguration aiSearchConfiguration)
                    {
                        mwvm.CurrentLLMConversation = new LLMConversationViewModel(importedConversation);
                        DocumentCopyButton.IsEnabled = true;
                        mwvm.Session.Query.Query = string.Empty; // Clear the query on submission.
                    } else
                    {
                        throw new Exception("Must have LLM Selected");
                    }
                }
            } catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                await TaskDialogWindow.OKDialog(S.Get("Error"), ex.Message, this);
            }
        }

        private static FilePickerFileType[] AIImportExportFileTypes
        {
            get
            {
                var csv = new FilePickerFileType("Comma Seperated Values (.csv)")
                {
                    Patterns = new[] { "*.csv" },
                    AppleUniformTypeIdentifiers = new[] { "public.csv" },
                    MimeTypes = new[] { "text/csv" }
                };

                var jsonl = new FilePickerFileType("JSON Lines (.jsonl)")
                {
                    Patterns = new[] { "*.jsonl" },
                    AppleUniformTypeIdentifiers = new[] { "public.jsonl" },
                    MimeTypes = new[] { "application/jsonl" }
                };

                var json = new FilePickerFileType("JSON (.json)")
                {
                    Patterns = new[] { "*.json" },
                    AppleUniformTypeIdentifiers = new[] { "public.json" },
                    MimeTypes = new[] { "application/json" }
                };

                var econvo = new FilePickerFileType("eSearch Conversation (.econvo)")
                {
                    Patterns = new[] { "*.econvo" },
                    AppleUniformTypeIdentifiers = new[] { "public.json" },
                    MimeTypes = new[] { "application/json" }
                };

                return [econvo, csv, json, jsonl];
            }
        }

        private async void MenuItemAIExportConversation_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainWindowViewModel mwvm)
                {
                    var conversation    = mwvm.CurrentLLMConversation?.ExtractConversation() ?? null;
                    if (conversation == null) throw new Exception("Conversation null");
                    if (!conversation.HasMessages()) throw new Exception("Conversation has no messages to export");
                    var exportConvoViewModel = Program.ProgramConfig.ExportConversationConfig.ToViewModel();
                    var exportWindow = new ExportConversationWindow { DataContext = exportConvoViewModel };
                    var res = await exportWindow.ShowDialog<object>(this);
                    if (res is TaskDialogResult tdr && tdr == TaskDialogResult.OK)
                    {
                        Program.ProgramConfig.ExportConversationConfig = ExportConversationConfig.FromViewModel(exportConvoViewModel);
                        Program.SaveProgramConfig();
                        string dateStr = exportConvoViewModel.AppendDate ? 
                            $" {DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")}" : string.Empty;
                        if (!System.IO.Directory.Exists(exportConvoViewModel.ExportDirectory))
                        {
                            System.IO.Directory.CreateDirectory(exportConvoViewModel.ExportDirectory);
                        }

                        string modelName = conversation.Messages.First().Model;
                        string modelNameFileNameFriendly = Models.Utils.SanitizeFileName(modelName);
                        string localPath = Path.Combine(exportConvoViewModel.ExportDirectory, $"{exportConvoViewModel.FileName} {modelNameFileNameFriendly.Trim()}{dateStr}.{exportConvoViewModel.SelectedExportFormat.Extension.ToLower()}");

                        if (Path.GetExtension(localPath).ToLower() == ".csv")
                        {
                            conversation.ExportAsCSVFile(localPath);
                        }
                        if (Path.GetExtension(localPath).ToLower() == ".jsonl")
                        {
                            conversation.ExportAsJsonLFile(localPath);
                        }
                        if (Path.GetExtension(localPath).ToLower() == ".json"
                         || Path.GetExtension(localPath).ToLower() == ".econvo")
                        {
                            string json = JsonConvert.SerializeObject(conversation, Formatting.Indented);
                            File.WriteAllText(localPath, json);
                        }

                        if (File.Exists(localPath))
                        {
                            eSearch.Models.Utils.RevealInFolderCrossPlatform(localPath); // TODO why do we have two utils?
                        }
                    }
                }
            } catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                await TaskDialogWindow.OKDialog(S.Get("Error"), ex.Message, this);
            }
        }

        private async void MenuItemDebugMCPListTools_Click(object? sender, RoutedEventArgs e)
        {
            Debug.WriteLine("# Listing Tools...");
            var mcpServerConfigs = Program.ProgramConfig.GetAllAvailableMCPServers();
            foreach(var mcpServerConfig in mcpServerConfigs)
            {
                if (mcpServerConfig.IsServerRunning)
                {
                    var clientTransport = mcpServerConfig.GetClientTransport();
                    var client = await McpClientFactory.CreateAsync(clientTransport);
                    Debug.WriteLine($"## Tools for Server `{mcpServerConfig.DisplayName}`");
                    foreach(var tool in await client.ListToolsAsync())
                    {
                        Debug.WriteLine($" - `{tool.Name}` ({tool.Description})");
                    }
                }
            }
            Debug.WriteLine("-- End of List--");
        }

        private async void MenuItemSearchMCPServers_Click(object? sender, RoutedEventArgs e)
        {
            await MCPConnectionConfigurationWindow.ShowDialog(this);
        }

        private void ResultsGrid2_KeyDown(object? sender, KeyEventArgs e)
        {
            var focusedElement = this.FocusManager?.GetFocusedElement();
            if (focusedElement is TreeDataGridCell cell
                && DataContext is MainWindowViewModel mwvm)
            {
                if (e.KeyModifiers == KeyModifiers.Control)
                {
                    if (e.Key == Key.A)
                    {
                        // CTRL + A - Select All
                        if (ResultsGrid2.Source?.Selection is CustomTreeDataGridRowSelectionModel<ResultViewModel> selectionModel)
                        {
                            selectionModel.SelectAll();
                        }
                    }
                    if (e.Key == Key.Down)
                    {
                        // CTRL + DOWN
                        int rowIndex = cell.RowIndex    + 1;
                        int colIndex = cell.ColumnIndex;
                        if (ResultsGrid2.TryGetCell(colIndex, rowIndex) is TreeDataGridCell cell2)
                        {
                            cell2.Focus();
                        }
                    }
                    if (e.Key == Key.Up)
                    {
                        // CTRL + UP
                        int rowIndex = cell.RowIndex - 1;
                        int colIndex = cell.ColumnIndex;
                        if (ResultsGrid2.TryGetCell(colIndex, rowIndex) is TreeDataGridCell cell2)
                        {
                            cell2.Focus();
                        }
                    }
                    e.Handled = true;
                    return;
                }
                if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                {
                    if (e.Key == Key.Down)
                    {
                        // Ctrl + Shift + Down
                        int rowIndex = cell.RowIndex + 1;
                        int colIndex = cell.ColumnIndex;
                        if (ResultsGrid2.TryGetCell(colIndex, rowIndex) is TreeDataGridCell cell2)
                        {
                            cell2.Focus();
                            var selectionModel = ((ITreeSelectionModel)ResultsGrid2.RowSelection);
                            selectionModel?.Select(new IndexPath(rowIndex));
                        }
                        e.Handled = true;
                        return;
                    }
                    if (e.Key == Key.Up)
                    {
                        // Ctrl + Shift + Up
                        int rowIndex = cell.RowIndex - 1;
                        int colIndex = cell.ColumnIndex;
                        if (ResultsGrid2.TryGetCell(colIndex, rowIndex) is TreeDataGridCell cell2)
                        {
                            cell2.Focus();
                            var selectionModel = ((ITreeSelectionModel)ResultsGrid2.RowSelection);
                            selectionModel?.Select(new IndexPath(rowIndex));
                        }
                        e.Handled = true;
                        return;
                    }
                    
                }
                if (e.Key == Key.Space)
                {
                    var selectionModel = ((ITreeSelectionModel)ResultsGrid2.RowSelection);

                    int rowIndex = cell.RowIndex;
                    if ( mwvm.Results[rowIndex] is ResultViewModel result)
                    {
                        if (mwvm.SelectedResults.Contains(result))
                        {
                            // Result is already checked. It can be unchecked if SelectedResults.Count > 1
                            if (mwvm.SelectedResults.Count > 1)
                            {
                                selectionModel.Deselect(new IndexPath(rowIndex));
                                result.IsResultChecked = false;
                            }
                        } else
                        {
                            // Result is not currently checked.
                            selectionModel.Select(new IndexPath(rowIndex));
                            result.IsResultChecked = true;
                            mwvm.SelectedResult = result;
                        }
                    }
                    e.Handled = true;
                    return;
                }

            }


            
        }

        private void ResultsGrid2_RowSelection_SelectionChanged(object? sender, Avalonia.Controls.Selection.TreeSelectionModelSelectionChangedEventArgs e)
        {
            if (DataContext is MainWindowViewModel mwvm)
            {
                foreach (ResultViewModel rvm in e.DeselectedItems.OfType<ResultViewModel>())
                {
                    mwvm.SelectedResults.Remove(rvm);
                    rvm.SetResultChecked(false);
                }
                foreach(ResultViewModel rvm in e.SelectedItems.OfType<ResultViewModel>())
                {
                    mwvm.SelectedResults.Add(rvm);
                    rvm.SetResultChecked(true);
                }


               mwvm.SelectedResult = ResultsGrid2.RowSelection?.SelectedItem as ResultViewModel ?? null;
            }
        }

        private void MenuItemLaunchAtStartup_Click(object? sender, RoutedEventArgs e)
        {
            Program.ProgramConfig.LaunchAtStartup = !Program.ProgramConfig.LaunchAtStartup;
            Program.SaveProgramConfig();
            menuItemLaunchAtStartupCheckbox.IsChecked = Program.ProgramConfig.LaunchAtStartup;
        }

        private async void MenuItemPlugins_Click(object? sender, RoutedEventArgs e)
        {
            await PluginsWindow.ShowPluginWindowDialog(this);
        }


        /// <summary>
        /// This is its own method and public as CEF may also call this.
        /// </summary>
        public async void FocusTextBox()
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (!queryTextBox.IsFocused)
                {
                    queryTextBox.Focus();
                    queryTextBox.CaretIndex = queryTextBox.Text?.Length ?? 0;
                    queryTextBox.SelectAll();
                }
            });
        }

        private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            if (DataContext is MainWindowViewModel mwvm)
            {
                if (e.Key == Key.Oem2) // Oem2 is the "/" key on most keyboards
                {
                    if (!queryTextBox.IsKeyboardFocusWithin)
                    {
                        FocusTextBox();
                        e.Handled = true;
                        return;
                    }
                }
                if (e.Key == Key.V && e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                {
                    mwvm.IsVoiceInputActive = !mwvm.IsVoiceInputActive;
                    e.Handled = true;
                    return;
                }
            }
        }

        private async void MenuItemSearchAISearch_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            await LLMConnectionConfigurationWindow.ShowDialog(this);
            init_searchSources();
            if (DataContext is MainWindowViewModel mwvm)
            {
                mwvm.Session.Query.UseAISearch = true;
            }
        }

        /// <summary>
        /// The currently set typeAhead.
        /// </summary>
        string _query_typeAhead = string.Empty;
        string _query_previous  = string.Empty;
        bool   _query_is_processing_change = false;

        private async void QueryTextBox_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            try
            {
                
                if (DataContext is MainWindowViewModel mwvm)
                {
                    bool isQuerySelectionTypeAhead = getIsQuerySelectionTypeAhead();
                    bool isCaretAtEnd = queryTextBox.CaretIndex == queryTextBox.Text?.Length;

                    // Behaviour of some keys are now modified.

                    switch(e.Key)
                    {
                        case Key.Enter:
                            if (e.KeyModifiers == KeyModifiers.Shift)
                            {
                                // SHIFT + Enter
                                if (mwvm.Session.Query.UseAISearch)
                                {
                                    int selectionStart = queryTextBox.SelectionStart;
                                    int selectionEnd   = queryTextBox.SelectionEnd;
                                    string newStr = queryTextBox.Text.Remove(selectionStart, selectionEnd - selectionStart).Insert(selectionStart, "\n");
                                    queryTextBox.Text = newStr;
                                    queryTextBox.CaretIndex += 1;
                                    e.Handled = true;
                                }
                            } else if (e.KeyModifiers == KeyModifiers.None)
                            {
                                // Just Enter Key
                                mwvm.ClickSearchSubmit();
                                e.Handled = true;
                                return;
                            }
                            break;
                        case Avalonia.Input.Key.Tab:
                            if (isQuerySelectionTypeAhead)
                            {
                                _query_is_processing_change = true;
                                await Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    e.Handled = true;
                                    queryTextBox.ClearSelection();
                                    queryTextBox.Text += " ";
                                    queryTextBox.CaretIndex = queryTextBox.Text.Length;
                                    queryTextBox.Focus();

                                });
                            } else
                            {
                                if (FocusManager?.GetFocusedElement() is TextBox tb)
                                {
                                }
                                e.Handled = false;
                                return;
                            }
                            break;

                        case Avalonia.Input.Key.Up:
                        case Avalonia.Input.Key.Down:
                            _query_is_processing_change = true;
                            if (mwvm.Wheel != null)
                            {
                                int selectedWheelItemIndex = mwvm.Wheel.SelectedItemIndex;
                                int numWords = mwvm.Wheel.WheelWords.Count;
                                if (selectedWheelItemIndex < numWords - 1 && !mwvm.Session.Query.UseAISearch)
                                {
                                    int offset = e.Key == Avalonia.Input.Key.Down ? 1 : -1;
                                    try
                                    {
                                        string nextWord = mwvm.Wheel?.WheelWords[selectedWheelItemIndex + offset].Word ?? string.Empty;
                                        string strQuery = mwvm.Session?.Query.Query ?? string.Empty;
                                        string lastWordStartSequence = strQuery.Split(' ').Last();
                                        if (isQuerySelectionTypeAhead)
                                        {
                                            lastWordStartSequence = lastWordStartSequence.Substring(0, lastWordStartSequence.Length - _query_typeAhead.Length);
                                        }

                                        if (nextWord.ToLower().StartsWith(lastWordStartSequence.ToLower()))
                                        {
                                            mwvm.Wheel.SelectedItemIndex = selectedWheelItemIndex + offset;
                                            if (isQuerySelectionTypeAhead)
                                            {
                                                mwvm.Session.Query.Query = strQuery.Substring(0, strQuery.Length - _query_typeAhead.Length);
                                            }
                                            queryTextBox.ClearSelection();
                                            await SetSearchTypeAhead(nextWord.Substring(lastWordStartSequence.Length));
                                        }

                                    }
                                    catch (ArgumentOutOfRangeException) {
                                        Debug.WriteLine("Range exception");
                                    }
                                    
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            } finally
            {
                _query_is_processing_change = false;
            }
        }

        private bool getIsQuerySelectionTypeAhead()
        {
            string? txt = queryTextBox.Text;
            int selectionStart = queryTextBox.SelectionStart;
            int selectionEnd = queryTextBox.SelectionEnd;
            if (selectionEnd < selectionStart)
            {
                return false; // This prevents an exception. 
            }
            string selectedText = txt?.Substring(selectionStart, selectionEnd - selectionStart) ?? string.Empty;
            if (selectedText == _query_typeAhead && selectionEnd == txt?.Length && selectedText != string.Empty)
            {
                return true;
            } else
            {
                return false;
            }
        }

        

        private async Task SetSearchTypeAhead(string charSeq)
        {
            if (DataContext is MainWindowViewModel mwvm)
            {
                _query_typeAhead = charSeq;
                string og_query = queryTextBox.Text;
                string newQuery = queryTextBox.Text + charSeq;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    queryTextBox.Text = newQuery; // Only if not bound
                    queryTextBox.SelectAll();
                    queryTextBox.SelectionStart = og_query.Length;
                    //queryTextBox.CaretIndex = newQuery.Length; // End of text
                });
            }
        }

        private void MenuAppearanceHighContrast_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (Application.Current is          App  app 
                && DataContext is MainWindowViewModel mwvm)
            {
                Program.ProgramConfig.IsThemeHighContrast = !Program.ProgramConfig.IsThemeHighContrast;
                Program.SaveProgramConfig();
                mwvm.RaisePropertyChanged(nameof(mwvm.IsThemeHighContrast));
                app.SetHighContrast(Program.ProgramConfig.IsThemeHighContrast);
            }
        }

        private async void ConversationCopyButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel mwvm && mwvm.SelectedSearchSource?.Source is AISearchConfiguration aiConfig)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var messageVM in
                    mwvm.CurrentLLMConversation?.Messages ?? new ObservableCollection<LLMMessageViewModel>())
                {
                    var msg = messageVM.GetFinalMessage(); // If a message is still streaming, will return null.
                    if (msg != null)
                    {
                        string role = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(msg.Role);
                        sb.Append(role.Trim()).Append(": ").AppendLine(msg.Content.Trim()).AppendLine();
                    }
                }
                string txtToCopy = sb.ToString();
                string aiQuery = mwvm.Session.Query.Query;
                var copyContext = Program.ProgramConfig.CopyConvoConfig.ToViewModel();
                copyContext.DocumentTextToCopy = txtToCopy;
                copyContext.AppendNoteText = aiConfig.Model;
                var copyConvoWindow = new CopyConversationWindow { DataContext = copyContext };
                await copyConvoWindow.ShowDialog(this);
                if (copyConvoWindow.DidPressOK())
                {
                    Program.ProgramConfig.CopyConvoConfig = CopyConversationConfig.FromViewModel(copyContext);
                    Program.SaveProgramConfig();
                    StringBuilder copyBuffer = new StringBuilder();
                    copyBuffer.AppendLine(copyContext.DocumentTextToCopy);
                    if (copyContext.AppendNoteChecked)
                    {
                        copyBuffer.AppendLine();
                        copyBuffer.Append(S.Get("Note:")).Append(" ");
                        copyBuffer.AppendLine(copyContext.AppendNoteText);
                    }
                retryPoint:
                    try
                    {

                        switch (copyContext.GetCopySetting())
                        {
                            case CopyConversationWindowViewModel.CopySetting.Clipboard:
                                await Program.GetMainWindow().Clipboard.SetTextAsync(copyBuffer.ToString());
                                break;
                            case CopyConversationWindowViewModel.CopySetting.File:
                                string filePath = copyContext.SavePath;


                                if (!System.IO.Directory.Exists(filePath))
                                {
                                    System.IO.Directory.CreateDirectory(filePath);
                                }

                                string fileName =
                                    copyContext.CopyToFileName
                                    + " " +
                                    (copyContext.AppendDateIsChecked ? DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") : string.Empty);
                                fileName = fileName.Trim();

                                if (string.IsNullOrWhiteSpace(fileName))
                                {
                                    throw new Exception(S.Get("Must provide a filename or check append date."));
                                }

                                filePath = Path.Combine(filePath, fileName);
                                filePath = filePath.Trim() + ".txt";
                                filePath.TrimStart(' ');
                                System.IO.File.WriteAllText(filePath, copyBuffer.ToString());
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        var res = await TaskDialogWindow.RetryCancel(ex, Program.GetMainWindow());
                        switch (res)
                        {
                            case TaskDialogResult.Retry:
                                goto retryPoint;
                            case TaskDialogResult.Cancel:
                                break;
                        }
                    }

                }
            }
        }

        private async void DocumentCopyButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel mwvm)
            {
                try
                {
                    if (mwvm.SelectedResult == null) return;
                    string txtToCopy = string.Empty;
                    string docFileName = string.Empty;
                    string aiQuery     = string.Empty;
                    string note = string.Empty;


                    txtToCopy = mwvm.SelectedResult.ExtractHtmlRender();
                    txtToCopy = eSearch.Models.Utils.HtmlToText.ConvertFromString(txtToCopy);
                    docFileName = Path.GetFileName(mwvm.SelectedResult.FilePath);
                    note = S.Get("Query: ") + mwvm.Session.Query.Query;

                    
                    if (mwvm.Session.Query.UseAISearch && mwvm.SelectedSearchSource != null)
                    {
                        note = mwvm.SelectedSearchSource.DisplayName;
                    }

                    CopyDocumentWindowViewModel copyContext = Program.ProgramConfig.CopyDocumentConfig.ToViewModel();

                    copyContext.DocumentTextToCopy      = txtToCopy;
                    copyContext.AppendFileNameText      = docFileName;
                    copyContext.AppendAIQueryText       = aiQuery;
                    copyContext.AppendNoteText          = note;
                    copyContext.IsAIExport = false; // We no longer use this dialog for copying AI output.

                    var documentCopyDialog = new CopyDocumentWindow();
                    documentCopyDialog.DataContext = copyContext;
                    await documentCopyDialog.ShowDialog(Program.GetMainWindow());
                    if (documentCopyDialog.DidPressOK())
                    {
                        Program.ProgramConfig.CopyDocumentConfig = CopyDocumentConfig.FromViewModel(copyContext);
                        Program.SaveProgramConfig();
                        StringBuilder copyBuffer = new StringBuilder();
                        copyBuffer.AppendLine(copyContext.DocumentTextToCopy);
                        if (copyContext.AppendFileNameChecked && !mwvm.Session.Query.UseAISearch)
                        {
                            copyBuffer.AppendLine();
                            copyBuffer.Append(S.Get("Filename:")).Append(" "); 
                            copyBuffer.AppendLine(copyContext.AppendFileNameText);
                        }
                        if (copyContext.AppendNoteChecked)
                        {
                            copyBuffer.AppendLine();
                            copyBuffer.Append(S.Get("Note:")).Append(" ");
                            copyBuffer.AppendLine(copyContext.AppendNoteText);
                        }
                    retryPoint:
                        try
                        {

                            switch (copyContext.GetCopySetting())
                            {
                                case CopyDocumentWindowViewModel.CopySetting.Clipboard:
                                    await Program.GetMainWindow().Clipboard.SetTextAsync(copyBuffer.ToString());
                                    break;
                                case CopyDocumentWindowViewModel.CopySetting.File:
                                    string filePath = copyContext.SavePath;


                                    if (!System.IO.Directory.Exists(filePath))
                                    {
                                        System.IO.Directory.CreateDirectory(filePath);
                                    }

                                    string fileName = 
                                        copyContext.CopyToFileName 
                                        + " " + 
                                        (copyContext.AppendDateIsChecked ? DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") : string.Empty);
                                    fileName = fileName.Trim();

                                    if (string.IsNullOrWhiteSpace(fileName))
                                    {
                                        throw new Exception(S.Get("Must provide a filename or check append date."));
                                    }

                                    filePath = Path.Combine(filePath, fileName);
                                    filePath = filePath.Trim() + ".txt";
                                    filePath.TrimStart(' ');
                                    System.IO.File.WriteAllText(filePath, copyBuffer.ToString());
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            var res = await TaskDialogWindow.RetryCancel(ex, Program.GetMainWindow());
                            switch (res)
                            {
                                case TaskDialogResult.Retry:
                                    goto retryPoint;
                                case TaskDialogResult.Cancel:
                                    break;
                            }
                        }

                    }
                    return;
                }
                catch (Exception ex)
                {
                    // TODO Error handling
                    throw;
                }
            }
        }


        

        bool handlingColumn0 = false;

        private async void ResultsCopyButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            #region Open the Folder Explorer Dialog to choose where the records will be exported.
            // Get the storage provider from the window
            var storageProvider = StorageProvider;

            // Configure folder picker options (optional)

            IStorageFolder? previousLocation = null;
            if (Program.ProgramConfig.CopyResultsFolderPath != null)
            {
                previousLocation = await storageProvider.TryGetFolderFromPathAsync(Program.ProgramConfig.CopyResultsFolderPath);
            }
            IStorageFolder? suggestedLocation;
            if (previousLocation != null)
            {
                suggestedLocation = previousLocation;
            } else
            {
                suggestedLocation = await storageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);
            }

            var options = new FolderPickerOpenOptions
            {
                Title = S.Get("Select output folder"),
                SuggestedStartLocation = suggestedLocation,
                AllowMultiple = false
            };

            // Open the folder picker dialog
            var folders = await storageProvider.OpenFolderPickerAsync(options);

            string folderPath = null;
            // Check if a folder was selected
            if (folders.Count > 0)
            {
                // Get the selected folder path
                folderPath = folders[0].Path.LocalPath;
                // Use the folderPath as needed
            }

            if (folderPath == null) return;
            Program.ProgramConfig.CopyResultsFolderPath = folderPath;
            Program.SaveProgramConfig();
            #endregion
            #region Get Selected Results
            List<IResult> resultsToCopy = new List<IResult>();
            foreach (ResultViewModel result in ResultsGrid2.RowSelection?.SelectedItems)
            {
                resultsToCopy.Add(result.GetResult());
            }
            #endregion
            #region Copy the results
            var copyTask = new CopyResultsTask(resultsToCopy, folderPath);
            copyTask.Execute();
            #endregion
            eSearch.Models.Utils.RevealInFolderCrossPlatform(folderPath);
        }
        private async void ResultsSettingsButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel mwvm)
            {
                var res = await ResultsSettingsWindow.ShowDialog(mwvm.Columns.ToArray(), mwvm.IndexLibrary.GetConfiguration(mwvm.SelectedIndex).ColumnSizingMode, mwvm.Session.Query);
                if (res.Item1 == TaskDialogResult.OK)
                {
                    var newSettings = res.Item2;

                    var indexConfig = mwvm.IndexLibrary.GetConfiguration(mwvm.SelectedIndex);


                    indexConfig.ColumnDisplaySettings.Clear();


                    foreach (var column in indexConfig.ColumnDisplaySettings)
                    {
                        var newSetting = newSettings.AvailableColumns.FirstOrDefault(newCol => newCol.Header.ToLower() == column.Header.ToLower());
                        if (newSetting != null)
                        {
                            column.Visible = newSetting.IsChecked;
                            column.DisplayIndex = newSettings.AvailableColumns.IndexOf(newSetting);
                        }
                    }
                    indexConfig.ColumnSizingMode = newSettings.SelectedColumnSizingMode;
                    mwvm.IndexLibrary.SaveLibrary(); // This will persist changes to column display settings.
                    mwvm.Columns = null; // HACK - This will cause columns to be reloaded when next retrieved.
                                         // Because mwvm.Columns changed is a subscribed event that initializes
                                         // Columns, this indirectly also calls init_columns

                    if (Program.ProgramConfig.IsProgramRegistered())
                    {
                        mwvm.Session.Query.LimitResults = newSettings.IsLimitResultsChecked;
                        mwvm.Session.Query.LimitResultsStartAt = newSettings.LimitResultsStartAt;
                        if (newSettings.LimitResultsEndAt != null)
                        {
                            mwvm.Session.Query.LimitResultsEndAt = (int)newSettings.LimitResultsEndAt;
                        }
                        Program.SaveProgramConfig();
                        init_columns();
                        UpdateSearchResults();
                    }
                }
            }
        }

        private async void MenuItemIndexManageIndexes_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel mwvm)
            {
                if (mwvm.SelectedIndex != null) mwvm.SelectedIndex.EnsureClosed();
                var updateIndexWindow = new UpdateIndexWindow();
                updateIndexWindow.Width = 800;
                updateIndexWindow.Height = 400;
                updateIndexWindow.CanResize = false;
                updateIndexWindow.WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner;

                var viewModel = new UpdateIndexWindowViewModel(updateIndexWindow, mwvm.IndexLibrary, mwvm.SelectedIndex);
                updateIndexWindow.DataContext = viewModel;

                var res = await updateIndexWindow.ShowDialog<object>(this);
                PauseSearchUpdates = true;
                init_searchSources();
                SelectAndDisplayIndex(mwvm.SelectedIndex);
                PauseSearchUpdates = false;
            }
        }

        private async void MenuItemIndexUpdate_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel mwvm)
            {
                if (mwvm.SelectedIndex == null) return; // Should not be possible to call this method when no index selected but handle this just in case.
                mwvm.SelectedIndex.EnsureClosed();
                var ixLib = mwvm.IndexLibrary;
                var res = await NewIndexWindow.ShowDialog(ixLib, mwvm.SelectedIndex, this);
                if (res.Item1 == TaskDialogResult.OK && res.Item2 is LuceneIndexConfiguration updatedConfig)
                {
                    PauseSearchUpdates = true;
                    ixLib.UpdateConfiguration(updatedConfig);
                    ixLib.SaveLibrary();
                    await UpdateIndexWithProgressDialog(updatedConfig);
                    init_searchSources();
                    SelectAndDisplayIndex(updatedConfig.LuceneIndex);
                    PauseSearchUpdates = false;
                }
            }
        }

        private async void MenuItemIndexNew_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel mwvm)
            {
                var ixLib = mwvm.IndexLibrary;
                var res = await NewIndexWindow.ShowDialog(ixLib, null, this);
                if (res.Item1 == TaskDialogResult.OK && res.Item2 is LuceneIndexConfiguration newIndexConfig)
                {
                    PauseSearchUpdates = true;
                    mwvm.Session.Query.UseAISearch = false;
                    ixLib.LuceneIndexes.Add(newIndexConfig);
                    ixLib.SaveLibrary();
                    await UpdateIndexWithProgressDialog(newIndexConfig);
                    init_searchSources();
                    SelectAndDisplayIndex(newIndexConfig.LuceneIndex);
                    mwvm.Session.Query.Query = string.Empty;
                    PauseSearchUpdates = false;
                }
            }
        }

        private async Task UpdateIndexWithProgressDialog(IIndexConfiguration indexConfig)
        {
            if (DataContext is MainWindowViewModel mwvm)
            {
                var ds = await ((LuceneIndexConfiguration)indexConfig).GetMultiDataSource(); // TODO Hack
                var idx = ((LuceneIndexConfiguration)indexConfig).LuceneIndex; // TODO Hack

                ProgressViewModel pvm = new ProgressViewModel();
                IndexTask ixTask = new IndexTask(ds, idx, pvm, false);
                mwvm.SelectedIndex = null;
                var taskRes = await IndexProgressWindow.ShowProgressDialogAndStartIndexTask(ixTask, Program.GetMainWindow());
                if (taskRes.Item1 == TaskDialogResult.OK)
                {
                    Program.SaveProgramConfig();
                    Program.IndexLibrary.SaveLibrary();
                }
            }
        }

        private IndexStatusControlViewModel? _selectedIndexStatusViewer = null;

        public async void SelectAndDisplayIndex(IIndex? index)
        {
            if (DataContext is MainWindowViewModel mwvm)
            {
                index?.OpenRead();
                
                mwvm.SelectedIndex = index;
                if (mwvm.SelectedIndex != null) mwvm.SelectedIndex.OpenRead();
                mwvm.Results = new EmptySearchResultsProvider();
                await init_wheel(index);
                if (Program.ProgramConfig.SearchAsYouType)
                {
                    UpdateSearchResults();
                }
            }
        }

        private void QueryTextBox_Cleared(object? sender, EventArgs e)
        {
            htmlDocumentControl.RenderBlankPageThemeColored();

            if (Program.ProgramConfig.SearchAsYouType == false)
            {
                // The clear button should update search results even when search as you type is disabled.
                UpdateSearchResults();
            }
        }

        public void UpdateTheme()
        {
            htmlDocumentControl.UpdateTheme();
            if (ResultsGrid2 != null 
                && ResultsGrid2.RowSelection?.SelectedItem is ResultViewModel rvm)
            {
                htmlDocumentControl.renderResultAccordingToSettings(rvm, this.DataContext as MainWindowViewModel);
            } else
            {
                htmlDocumentControl.RenderBlankPageThemeColored();
            }
        }

        public void UpdateLayout()
        {
            if (DataContext is MainWindowViewModel mwvm)
            {
                var useAISearch = mwvm.Session?.Query?.UseAISearch ?? false;
                SetResultsGridHidden(useAISearch); // This will also update column layout.
                updateRAABEnabledState();
            }
        }

        public void SetResultsGridHidden(bool hidden)
        {
            
            if (DataContext is MainWindowViewModel mwvm)
            {
                RowDefinitions definitions;

                mwvm.IsGridSplitterHidden = hidden;

                if (mwvm.SelectedLayout == MainWindowViewModel.LayoutPreference.Horizontal)
                {
                    ColumnDefinitions definitions2;
                    if (hidden)
                    {
                        definitions2 = ColumnDefinitions.Parse("0 0 *");
                    }
                    else
                    {
                        definitions2 = ColumnDefinitions.Parse("* 6 *");
                    }
                    ResultsAndDocumentView.ColumnDefinitions = definitions2;
                }
                else
                {
                    if (hidden)
                    {
                        definitions = RowDefinitions.Parse("0 0 *");
                    }
                    else
                    {
                        definitions = RowDefinitions.Parse("* 6 *");
                    }
                    ResultsAndDocumentView.RowDefinitions = definitions;
                }
            }
            

            
        }


        private void GetCurrentlySelectedItems(out List<ResultViewModel> selectedItems, out ResultViewModel? previousItem, out ResultViewModel? currentItem, out ResultViewModel? nextItem)
        {
            previousItem = null;
            nextItem = null;
            currentItem = null;
            selectedItems = new List<ResultViewModel>();
            if (ResultsGrid2.RowSelection?.SelectedItems is null) return;
            foreach (ResultViewModel selectedItem in ResultsGrid2.RowSelection?.SelectedItems?.OfType<ResultViewModel>() 
                     ?? Enumerable.Empty<ResultViewModel>())
            {
                selectedItems.Add(selectedItem);
            }

            if (DataContext is MainWindowViewModel mwvm)
            {
                
                if (ResultsGrid2.RowSelection?.SelectedItem is ResultViewModel rvm)
                {
                    currentItem = rvm;
                    int index = mwvm.Results.IndexOf(currentItem);
                    if ((index - 1) >= 0) previousItem             = (ResultViewModel)mwvm.Results[index - 1];
                    if ((index + 1) < mwvm.Results.Count) nextItem = (ResultViewModel)mwvm.Results[index + 1];
                }
            }
        }

        private bool IsContiguousSelection()
        {
            List<int> indexes = new List<int>();

            // Handle empty or single item cases
            if (indexes == null || indexes.Count == 0)
                return true;
            if (indexes.Count == 1)
                return true;

            // Find minimum and maximum values
            int minIndex = indexes.Min();
            int maxIndex = indexes.Max();

            // Calculate expected count of numbers in contiguous sequence
            int expectedCount = maxIndex - minIndex + 1;

            // If the count doesn't match expected, it's not contiguous
            if (expectedCount != indexes.Count)
                return false;

            // Create a HashSet for O(1) lookup
            HashSet<int> indexSet = new HashSet<int>(indexes);

            // Check if all numbers between min and max exist in the set
            for (int i = minIndex; i <= maxIndex; i++)
            {
                if (!indexSet.Contains(i))
                    return false;
            }

            return true;
        }

        //private void GetCurrentCell()
        //{
        //    // https://github.com/AvaloniaUI/Avalonia/discussions/6090
        //    var focused = FocusManager.Instan
        //    var currentRow = focused?.FindAncestorOfType<DataGridRow>() ?? myDataGrid.FindDescendantOfType<DataGridRowsPresenter>()
        //        .Children.OfType<DataGridRow>()
        //        .FirstOrDefault(r => r.FindDescendantOfType<DataGridCellsPresenter>()
        //            .Children.Any(p => p.Classes.Contains(":current")));
        //    var item = currentRow?.DataContext;

        //    var currentCell = currentRow?.FindDescendantOfType<DataGridCellsPresenter>().Children
        //        .OfType<DataGridCell>().FirstOrDefault(p => p.Classes.Contains(":current"));
        //}

        private void ResultsGrid_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Tab)
            {
                e.Handled = true;
            }
        }

        public void PrintViewedDocument()
        {
            if (htmlDocumentControl != null)
            {
                htmlDocumentControl.PrintDocument();
            }
        }

        private async void MainWindow_DataContextChanged(object? sender, System.EventArgs e)
        {
            if (this.DataContext is MainWindowViewModel viewModel)
            {
                viewModel.PropertyChanged += ViewModel_PropertyChanged;

                if (viewModel.SelectedLayout != MainWindowViewModel.LayoutPreference.Vertical)
                {
                    // TODO this is a bit of a horrible hack. Calling this function if layout preference is not vertical will break things.
                    viewModel.UpdateLayout();
                }

                viewModel.Session.Query.PropertyChanged += Query_PropertyChanged;
                init_searchSources();
                init_columns();
                UpdateLayout();
                if (viewModel.Session.Query.UseAISearch)
                {
                    InitAICompletionsWheel();
                }
                else
                {
                    await init_wheel(viewModel.SelectedIndex);
                }
                queryTextBox.SelectAll();
                queryTextBox.Focus();
                if (Program.ProgramConfig.SearchAsYouType && !viewModel.Session.Query.UseAISearch) UpdateSearchResults();
            }
        }

        private void updateRAABEnabledState()
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.AreRAABRadiosEnabled = viewModel.Session.Query.QueryTextBoxEnabled && !viewModel.Session.Query.UseAISearch;
            }
        }

        /// <summary>
        /// Populate the Search Sources drop down/current selection based on Program Config.
        /// </summary>
        public void init_searchSources()
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.AvailableSearchSources.Clear();
                vm.SelectedSearchSource = null;
                if (vm.Session.Query.UseAISearch)
                {
                    Program.ProgramConfig.AISearchConfigurations.Sort((x, y) => string.Compare(x.GetDisplayName(), y.GetDisplayName()));
                    foreach(var config in Program.ProgramConfig.AISearchConfigurations)
                    {
                        var src = new SearchSource(config.GetDisplayName(), config);
                        vm.AvailableSearchSources.Add(src);
                        if (config.Id.Equals(Program.ProgramConfig.SelectedAISearchConfigurationID))
                        {
                            vm.SelectedSearchSource = src;
                        }
                    }

                    if (vm.AvailableSearchSources.Count > 0 && Program.ProgramConfig.SelectedAISearchConfigurationID == null)
                    {
                        vm.SelectedSearchSource = vm.AvailableSearchSources[0];
                        Program.ProgramConfig.SelectedAISearchConfigurationID = Program.ProgramConfig.AISearchConfigurations[0].Id;
                    }
                }
                else
                {
                    var indexes = vm.IndexLibrary.GetAllIndexes(); // This already returns sorted.
                    foreach (var index in indexes)
                    {
                        var src = new SearchSource(index.Name, index);
                        vm.AvailableSearchSources.Add(src);
                        if (index.Id == vm.Session.SelectedIndexId)
                        {
                            vm.SelectedSearchSource = src;
                        }
                    }
                }
            }
        }

        

        private async void Query_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (DataContext is MainWindowViewModel mwvm && sender is QueryViewModel qvm)
            {
                bool updateSearchResults = false;

                

                if (e.PropertyName?.Equals(nameof(qvm.UseAISearch)) ?? false)
                {
                    MWSearchControl.QueryTextBox.Focus();
                    SetResultsGridHidden(qvm.UseAISearch);
                    if (DataContext is MainWindowViewModel viewModel)
                    {
                        viewModel.Session.Query.Query = string.Empty;
                        mwvm.Results = new EmptySearchResultsProvider();
                        htmlDocumentControl.RenderBlankPageThemeColored();
                        init_searchSources();
                        if (Program.ProgramConfig.SearchAsYouType) {
                            if (!mwvm.Initializing) updateSearchResults = true;
                        }
                        updateRAABEnabledState();
                        if (qvm.UseAISearch)
                        {
                            InitAICompletionsWheel();
                            mwvm.ShowMetadataPanel = false;
                        } else
                        {
                            await init_wheel(mwvm.SelectedIndex);
                            
                        }
                        if (Program.ProgramConfig.SearchAsYouType) UpdateSearchResults();
                    }
                    
                }
                if (e.PropertyName?.Equals(nameof(qvm.QueryTextBoxEnabled)) ?? false)
                {
                    updateRAABEnabledState();
                }
                if (e.PropertyName?.Equals(nameof(qvm.SearchWithinDocumentMetadata)) ?? false)
                {
                    mwvm.Wheel?.SetContentOnly(qvm.SearchWithinDocumentMetadata == false);
                    updateSearchResults = true;
                }
                

                if (e.PropertyName == nameof(mwvm.Session.Query))
                {
                    updateSearchResults = mwvm.Initializing || (Program.ProgramConfig.SearchAsYouType && !mwvm.Session.Query.UseAISearch);
                    var strQuery = mwvm.Session?.Query.Query;
                    

                    if (strQuery != null && strQuery.Trim() != "" && !_query_is_processing_change)
                    {
                        string lastWordStartSequence = strQuery.Split(' ').Last();
                        WordWheelControl.ScrollSelectionToTop = true;
                        if (lastWordStartSequence.Trim().Length > 0) mwvm.Wheel?.SetNewStartSequence(lastWordStartSequence);
                        WordWheelControl.ScrollSelectionToTop = false;
                        #region Typeahead logic
                        //_query_typeAhead = string.Empty;
                        if (queryTextBox.IsFocused && lastWordStartSequence.Length > 0 
                            && strQuery.Length > _query_previous.Length
                            && !(mwvm.Session?.Query.UseAISearch ?? false))
                        {
                            
                            int? selectedWheelItemIndex = mwvm.Wheel?.SelectedItemIndex;
                            if (selectedWheelItemIndex != null && selectedWheelItemIndex != -1)
                            {
                                // Wheel has something selected. Check if its suitable for typeahead.
                                var wheelWord = mwvm.Wheel?.WheelWords[selectedWheelItemIndex ?? 0].Word;
                                if (!string.IsNullOrWhiteSpace(wheelWord))
                                {
                                    if (wheelWord.ToLower().StartsWith(lastWordStartSequence.ToLower()))
                                    {
                                        if (wheelWord.Length > lastWordStartSequence.Length)
                                        {
                                            string seq = wheelWord.Substring(lastWordStartSequence.Length);
                                            await SetSearchTypeAhead(seq);
                                        }
                                    }
                                }
                            }
                        }
                        _query_previous = queryTextBox.Text ?? string.Empty;
                        if (getIsQuerySelectionTypeAhead())
                        {
                            try
                            {
                                _query_previous = _query_previous.Substring(0, _query_previous.Length - _query_typeAhead.Length);
                            }
                            catch (ArgumentOutOfRangeException) {

                                Debug.WriteLine("Out of range exception?");
                            }
                        }
                        #endregion
                    }
                }

                if (e.PropertyName == nameof(mwvm.Session.Query.SelectedSearchType))
                {
                    updateSearchResults = true;
                    // TODO Not a fan of the way this is done.
                    mwvm.RaisePropertyChanged(nameof(mwvm.IsRadioRAABAllWordsChecked));
                    mwvm.RaisePropertyChanged(nameof(mwvm.IsRadioRAABAnyWordsChecked));
                    mwvm.RaisePropertyChanged(nameof(mwvm.IsRadioRAABBoolChecked));
                }

                if (e.PropertyName == nameof(mwvm.Session.Query.UseSynonyms))
                {
                    try
                    {
                        if (mwvm.Session.Query.UseSynonyms && Program.ProgramConfig.SynonymsConfig?.ActiveSynonymFiles?.Count == 0)
                        {
                            // No Synonym Files Selected. Get the user to enter some.
                            await OpenSearchSettings();
                            if (Program.ProgramConfig.SynonymsConfig?.ActiveSynonymFiles?.Count == 0)
                            {
                                mwvm.Session.Query.UseSynonyms = false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // TODO Program.ProgramConfig can throw an exception at startup due to not yet loaded. Need a better way to handle this.
                    }
                }

                if (updateSearchResults
                    && !mwvm.Initializing // Prevent repeated searches during initialization as properties get set.
                )
                {
                    Debug.WriteLine("Query or query settings has changed. Performing search.");
                    // The search query has changed. Perform a new search on a background thread.
                    UpdateSearchResults();
                }
            }
        }

        private void InitAICompletionsWheel()
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                var wheel = FixedListWordWheel.GetAICompletionsWheel();
                viewModel.Wheel = new WheelViewModel(wheel);
                viewModel.Wheel.HideFrequency = true;
            }
        }


        /// <summary>
        /// Use this bool to prevent generating new search results when changing UI (ie, if doing bulk changes. 
        /// Setting this to false will update search results.
        /// </summary>
        public bool PauseSearchUpdates
        {
            get
            {
                return _searchUpdatesPaused;
            }
            set
            {
                _searchUpdatesPaused = value;
                if (!_searchUpdatesPaused)
                {
                    UpdateSearchResults();
                }
            }
        }

        private bool _searchUpdatesPaused = false;



        bool _searchResultsUpdating = false;
        bool _searchResultsSecondUpdateQueued = false;


        

        /// <summary>
        /// Cancels existing search. Starts a background worker to fetch new search results that eventually leads to UI update.
        /// 
        /// TODO Hack Currently public during transition to proper MWVM pattern
        /// </summary>
        public async Task UpdateSearchResults()
        {
            
            if (_searchResultsUpdating == true)
            {
                _searchResultsSecondUpdateQueued = true;
                return;
            }
            try
            {
                _searchResultsUpdating = true;
                if (DataContext is MainWindowViewModel mwvm)
                {
                    _searchWorker?.CancelAsync();
                    if (PauseSearchUpdates) return;
                    mwvm.Results = new EmptySearchResultsProvider();
                    List<ResultViewModel>? results = null;
                    if (!mwvm.Session.Query.UseAISearch)
                    {
                        #region Do Work
                        Exception ex = null;
                        try
                        {

                            if (mwvm.SelectedIndex != null)
                            {
                                var strQuery = mwvm.Session?.Query.Query;
                                if (strQuery != null)
                                {
                                    mwvm.SelectedIndex.OpenRead();


                                    if (mwvm.Session?.Query != null)
                                    {
                                        mwvm.Results = mwvm.SelectedIndex.PerformSearch(mwvm.Session.Query);
                                    }
                                }
                            }
                        }
                        catch (Exception searchEx)
                        {
                            ex = searchEx;
                        }
                        #endregion
                        #region After Work
                        if (ex != null)
                        {
                            // No results/some kind of error - Null session, Null query or Null index perhaps
                            Debug.WriteLine("Error during search");
                            Debug.WriteLine("Because of an exception...");
                            Debug.Write(ex.ToString());
                        }
                        #endregion
                    }
                    else
                    {
                        DocumentCopyButton.IsEnabled = false;
                        var selectedAIService = Program.ProgramConfig.GetSelectedConfiguration();
                        if (selectedAIService != null)
                        {
                            mwvm.ShowHitNavigation = false;
                            DoAiSearchCompletion();
                        }
                    }
                }

            } finally
            {
                _searchResultsUpdating = false;
                if (_searchResultsSecondUpdateQueued)
                {
                    _searchResultsSecondUpdateQueued = false;
                    UpdateSearchResults();
                }
            }

        }

        private BackgroundWorker? _searchWorker;

        private async Task OpenSearchSettings()
        {
            if (DataContext is MainWindowViewModel mwvm)
            {
                var taskRes = await SearchSettingsWindow.ShowDialog();
                var dialogResult = taskRes.Item1;
                if (dialogResult == Models.TaskDialogResult.OK)
                {
                    var newSettings = taskRes.Item2;
                    // Clicked OK. Save new preferences.

                    #region Save Synonyms Config
                    Program.ProgramConfig.SynonymsConfig.ActiveSynonymFiles = newSettings.GetSelectedSynonymFileNames().ToList();
                    Program.ProgramConfig.SynonymsConfig.UseSynonymFiles = newSettings.UseSynonymFiles;
                    Program.ProgramConfig.SynonymsConfig.UseEnglishWordNet = newSettings.UseWordNet;
                    Program.ProgramConfig.SynonymsConfig.LastViewedSynonymsFile = newSettings.SelectedSynonymFile?.FilePath;
                    Program.ProgramConfig.SearchAsYouType = newSettings.SearchAsYouType;
                    // Save Changes to Synonyms Files
                    foreach (var synonymsFile in newSettings.SynonymsFiles)
                    {
                        synonymsFile.SaveChanges();
                    }
                    #endregion
                    #region Save Stemming Config
                    string selectedStemmingFile = null;
                    if (newSettings.SelectedStemmingFile != null)
                    {
                        selectedStemmingFile = Path.Combine(Program.ESEARCH_STEMMING_DIR, newSettings.SelectedStemmingFile.FileName);
                    }

                    Program.ProgramConfig.StemmingConfig.UseEnglishPorter = newSettings.StemmingUseEnglishPorter;
                    Program.ProgramConfig.StemmingConfig.StemmingFile = selectedStemmingFile;

                    #endregion
                    #region Save Phonetics Config
                    Program.ProgramConfig.PhoneticConfig.SelectedEncoder = newSettings.GetSelectedEncoder();
                    #endregion
                    #region Save Search Term List Config
                    if (newSettings.UseList)
                    {
                        Program.ProgramConfig.SearchTermsListFile = newSettings.ListPath;
                        mwvm.Session.Query.QueryListFilePath = newSettings.ListPath;

                    }
                    else
                    {
                        Program.ProgramConfig.SearchTermsListFile = null;
                        mwvm.Session.Query.QueryListFilePath = null;
                    }
                    #endregion
                    Program.ProgramConfig.ListContentsOnEmptyQuery = newSettings.ListContentsOnEmptyQuery;
                    Program.SaveProgramConfig();
                    mwvm.Session.Query.InvalidateCachedThesauri();
                    mwvm.Session.Query.StemmingRules = Program.ProgramConfig.StemmingConfig.LoadActiveStemmingRules(); // Reload stemming rules.
                                                                                                                       // Finally, perform a new query by indicating the query has changed.
                    UpdateSearchResults();
                    // this.RaisePropertyChanged(nameof(Session.Query.Query));
                }
            }
            return;
        }


        /// <summary>
        /// Token that is used to cancel an AI Search Request.
        /// </summary>
        CancellationTokenSource? aiSearchCancellationTokenSource = null;

        /// <summary>
        /// Will cancel the ongoing AI Search HTTP Request if any.
        /// </summary>
        public void CancelCurrentAISearchReq()
        {
            aiSearchCancellationTokenSource?.Cancel();
        }

        private void RenderAiLandingPage()
        {
            if (DataContext is MainWindowViewModel mwvm)
            {
                if (mwvm.SelectedSearchSource?.Source is AISearchConfiguration aiSearchConfig)
                {
                    string modelName;
                    switch (aiSearchConfig.LLMService)
                    {
                        case LLMService.Perplexity:
                            modelName = aiSearchConfig.PerplexityModel.GetDescription();
                            break;
                        default:
                            modelName = aiSearchConfig.Model;
                            break;
                    }

                    var browser = GetHtmlDocumentControl() ?? null;
                    browser?.renderHtmlBody(
                        "<h3>" + HttpUtility.HtmlEncode(modelName) + "</h3>" +
                        "<p>" + HttpUtility.HtmlEncode(S.Get("Enter a query")) + "</p>", false);
                    mwvm.ShowHitNavigation = false;
                    return;
                }
            }
            
        }

        private async void DoAiSearchCompletion()
        {
            if (DataContext is MainWindowViewModel mwvm)
            {
                if (Program.ProgramConfig.AISearchConfigurations.Count < 1)
                {
                    PromptConfigureLLM();
                    return;
                }
                var browser = GetHtmlDocumentControl() ?? null;
                if (browser != null)
                {
                    var query = mwvm.Session.Query.Query;

                    if (string.IsNullOrWhiteSpace(query) && mwvm.SelectedSearchSource?.Source is AISearchConfiguration aiSearchConfig)
                    {
                        if (mwvm.CurrentLLMConversation == null)
                        {
                            RenderAiLandingPage();
                        }
                        return;
                    }

                    if (mwvm.CurrentLLMConversation == null)
                    {
                        // Starting New Conversation
                        try
                        {
                            aiSearchCancellationTokenSource = new CancellationTokenSource();
                            if (mwvm.SelectedSearchSource == null) throw new Exception("No Source Selected");
                            if (mwvm.SelectedSearchSource?.Source is AISearchConfiguration aiSearchConfiguration)
                            {
                                var conversationStarter = Completions.GetDefaultConversationStarter(aiSearchConfiguration);
                                mwvm.CurrentLLMConversation = new LLMConversationViewModel(conversationStarter);
                                browser.RenderLLMConversation(aiSearchConfiguration, conversationStarter);
                                browser.AddQueryToExistingLLMConversation(query);
                                
                                DocumentCopyButton.IsEnabled = true;
                                mwvm.Session.Query.Query = string.Empty; // Clear the query on submission.
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            browser.RenderBlankPageThemeColored();
                            mwvm.ShowHitNavigation = false;
                            return;
                        }
                        catch (Exception ex)
                        {
                            var html = "<p>" + HttpUtility.HtmlEncode(ex.Message) + "</p>";
                            browser.renderHtmlBody(html, false);
                            mwvm.ShowHitNavigation = false;
                        }
                    } else
                    {
                        // Continuing existing conversation...
                        if (mwvm.SelectedSearchSource?.Source is AISearchConfiguration aiSearchConfiguration)
                        {
                            var userMessage = new Message { 
                                Role = "user", 
                                Content = mwvm.Session.Query.Query, 
                                Model = Program.ProgramConfig.GetSelectedConfiguration()?.Model ?? string.Empty 
                            };
                            mwvm.CurrentLLMConversation.Messages.Add(new LLMMessageViewModel(userMessage));
                            var conversation = mwvm.CurrentLLMConversation.ExtractConversation();
                            CancellationTokenSource cancellationSource = new CancellationTokenSource();
                            CancellationToken cancellationToken = cancellationSource.Token;
                            var stream = Completions.GetCompletionStreamViaMCPAsync(aiSearchConfiguration, conversation, cancellationToken);
                            var responseMsg = new LLMMessageViewModel("assistant", stream, cancellationSource);
                            mwvm.CurrentLLMConversation.Messages.Add(responseMsg);
                            mwvm.Session.Query.Query = string.Empty; // Clear the query on submission.
                        }
                    }
                }
            }
        }

        public async void PromptConfigureLLM()
        {
            if (DataContext is MainWindowViewModel mwvm)
            {
                var res = await LLMConnectionConfigurationWindow.ShowDialog(this);
                if (Program.ProgramConfig.AISearchConfigurations.Count > 0)
                {
                    mwvm.Session.Query.UseAISearch = true;
                    init_searchSources();
                }
                else
                {
                    mwvm.Session.Query.UseAISearch = false;
                }
                UpdateLayout();
            }
        }

        private VoiceListener? voiceListener = null;

        private SearchSource? _previousSearchSource = null;

        private async void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is MainWindowViewModel mwvm)
            {
                if (e.PropertyName?.Equals(nameof(mwvm.Results)) ?? false)
                {
                    if (ResultsGrid2.Source is FlatTreeDataGridSourceCustomSortable<ResultViewModel> sortable)
                    {
                        sortable.SetItems(mwvm.Results);
                    }
                    //init_columns();
                }
                if (e.PropertyName?.Equals(nameof(mwvm.IsVoiceInputActive)) ?? false)
                {
                    try
                    {
                        if (OperatingSystem.IsWindows())
                        {
                            if (mwvm.IsVoiceInputActive)
                            {
                                if (voiceListener == null)
                                {
                                    voiceListener = new VoiceListener();
                                    voiceListener.BeginListening();
                                    voiceListener.OnVoiceInput += VoiceListener_OnVoiceInput;
                                }
                            } else
                            {
                                voiceListener?.StopListening();
                                voiceListener = null;
                            }
                        }
                    } catch (Exception ex)
                    {
                        mwvm.IsVoiceInputActive = false;
                        await TaskDialogWindow.OKDialog(S.Get("An error occurred"), ex.ToString(), this);
                    }
                }
                if (e.PropertyName?.Equals(nameof(mwvm.SelectedSearchSource)) ?? false)
                {
                    var newSearchSource = mwvm.SelectedSearchSource;

                    if (newSearchSource?.Source is IIndex index)
                    {
                        mwvm.SelectedIndex = index;
                    }
                    if (newSearchSource?.Source is AISearchConfiguration config)
                    {
                        if (newSearchSource != _previousSearchSource && _previousSearchSource?.Source is AISearchConfiguration prevConfig)
                        {
                            // Switched from one AI Search Source to another.
                            var firstUserMessage = mwvm.CurrentLLMConversation?.Messages.FirstOrDefault(m => m.Role == "user", null);
                            if (firstUserMessage != null)
                            {
                                var content = firstUserMessage.GetFinalMessage()?.Content;
                                Program.GetMainWindow()?.Clipboard?.SetTextAsync(content);
                            }
                        }
                        _previousSearchSource = newSearchSource;
                        Program.ProgramConfig.SelectedAISearchConfigurationID = config.Id;
                        Program.SaveProgramConfig();
                        var conversationStarter = Completions.GetDefaultConversationStarter(config);
                        mwvm.CurrentLLMConversation = new LLMConversationViewModel(conversationStarter);
                        var browser = GetHtmlDocumentControl();
                        browser?.RenderLLMConversation(config, conversationStarter);
                        return;
                    }
                }
                if (e.PropertyName?.Equals(nameof(mwvm.SelectedLayout)) ?? false)
                {

                    if (mwvm.SelectedLayout == MainWindowViewModel.LayoutPreference.Horizontal)
                    {
                        var columnDefinitions = ColumnDefinitions.Parse(ResultsAndDocumentView.RowDefinitions.ToString());
                        var rowDefinitions = RowDefinitions.Parse(ResultsAndDocumentView.ColumnDefinitions.ToString());
                        ResultsAndDocumentView.ColumnDefinitions = columnDefinitions;
                        ResultsAndDocumentView.RowDefinitions = rowDefinitions;
                        ResultsView.SetValue(Grid.ColumnProperty, ResultsView.GetValue(Grid.RowProperty));
                        DocumentView.SetValue(Grid.ColumnProperty, DocumentView.GetValue(Grid.RowProperty));
                        ResultsDocumentSplitter.SetValue(Grid.ColumnProperty, ResultsDocumentSplitter.GetValue(Grid.RowProperty));
                        ResultsView.SetValue(Grid.RowProperty, 0);
                        DocumentView.SetValue(Grid.RowProperty, 0);
                        ResultsDocumentSplitter.SetValue(Grid.RowProperty, 0);
                        ResultsDocumentSplitter.ResizeDirection = GridResizeDirection.Columns;

                    }
                    else
                    {
                        var columnDefinitions = ColumnDefinitions.Parse(ResultsAndDocumentView.RowDefinitions.ToString());
                        var rowDefinitions = RowDefinitions.Parse(ResultsAndDocumentView.ColumnDefinitions.ToString());
                        ResultsAndDocumentView.ColumnDefinitions = columnDefinitions;
                        ResultsAndDocumentView.RowDefinitions = rowDefinitions;
                        ResultsView.SetValue(Grid.RowProperty, ResultsView.GetValue(Grid.ColumnProperty));
                        DocumentView.SetValue(Grid.RowProperty, DocumentView.GetValue(Grid.ColumnProperty));
                        ResultsDocumentSplitter.SetValue(Grid.RowProperty, ResultsDocumentSplitter.GetValue(Grid.ColumnProperty));
                        ResultsView.SetValue(Grid.ColumnProperty, 0);
                        DocumentView.SetValue(Grid.ColumnProperty, 0);
                        ResultsDocumentSplitter.SetValue(Grid.ColumnProperty, 0);
                        ResultsDocumentSplitter.ResizeDirection = GridResizeDirection.Rows;
                    }
                    UpdateLayout();

                }
                if (e.PropertyName?.Equals(nameof(mwvm.SelectedResult)) ?? false)
                {
                    ResultsCopyButton.IsEnabled     = mwvm.SelectedResult != null;
                    DocumentCopyButton.IsEnabled    = mwvm.SelectedResult != null;
                    mwvm.ShowDocumentLocation  = false;
                    #region Update Geolocation UI
                    bool isGeolocationAvailable = false;

                    if (Program.ProgramConfig.IsProgramRegistered() && mwvm.SelectedResult != null)
                    {
                        var latitude = mwvm.SelectedResult.GetMetadataValue("Latitude");
                        var longitude = mwvm.SelectedResult.GetMetadataValue("Longitude");
                        if (latitude != null && longitude != null)
                        {
                            isGeolocationAvailable = true;
                        }
                    }
                    mwvm.IsDocumentLocationAvailable = isGeolocationAvailable;
                    mwvm.DocumentLocationButtonToolTip = isGeolocationAvailable ? S.Get("Show location") : S.Get("Location not available");
                    #endregion
                    #region Update the Html Viewer
                    if (mwvm.SelectedResult != null)
                    {
                        htmlDocumentControl.renderResultAccordingToSettings(mwvm.SelectedResult, mwvm);
                        if (mwvm.SelectedResult.DocumentMetaData != null)
                        {
                            MetadataDataGrid.ItemsSource = mwvm.SelectedResult.VisibleDocumentMetaData;
                        } else
                        {
                            var metaData = new List<Metadata>();
                            metaData.Add(new Metadata { Key = " ---- ", Value = S.Get("There is no metadata to display.") });
                            MetadataDataGrid.ItemsSource = new List<Metadata>();
                        }
                    } else
                    {
                        htmlDocumentControl.RenderBlankPageThemeColored();
                    }
                    #endregion
                }
                if (e.PropertyName?.Equals(nameof(mwvm.Columns)) ?? false)
                {
                    init_columns();
                }
                if (e.PropertyName?.Equals(nameof(mwvm.CurrentDocSelectedHit)) ?? false)
                {
                    htmlDocumentControl.GoToHit(mwvm.CurrentDocSelectedHit);
                }
                if (e.PropertyName?.Equals(nameof(mwvm.SelectedIndex)) ?? false)
                {

                    if (_selectedIndexStatusViewer != null)
                    {
                        mwvm.StatusMessages.Remove(_selectedIndexStatusViewer);
                        _selectedIndexStatusViewer.Dispose();
                        _selectedIndexStatusViewer = null;
                    }
                    


                    PauseSearchUpdates = true;
                    htmlDocumentControl.RenderBlankPageThemeColored();
                    mwvm.ShowDocumentLocation      = false;
                    mwvm.ShowMetadataPanel         = false;
                    mwvm.Session.Query.Query       = "";
                    mwvm.Session.SelectedIndexId   = mwvm.SelectedIndex?.Id ?? null;
                    mwvm.Results = new EmptySearchResultsProvider();
                    init_columns();
                    if (mwvm.SelectedIndex != null)
                    {
                        _selectedIndexStatusViewer = new IndexStatusControlViewModel(mwvm.SelectedIndex);
                        mwvm.StatusMessages.Add(_selectedIndexStatusViewer);
                    }
                    await init_wheel(mwvm.SelectedIndex);
                    PauseSearchUpdates = false;

                    
                }
                if (e.PropertyName?.Equals(nameof(mwvm.ShowDocumentLocation)) ?? false)
                {
                    if (mwvm.ShowDocumentLocation)
                    {
                        // Show the document location.
                        var latitude  = mwvm.SelectedResult.GetMetadataValue("Latitude");
                        var longitude = mwvm.SelectedResult.GetMetadataValue("Longitude");
                        // string url = "https://maps.google.com/?q=" + latitude + "," + longitude;
                        // string url = "https://www.openstreetmap.org/#map=11/" + latitude + "/" + longitude;
                        string url = "https://www.openstreetmap.org/?mlat=" + latitude + "&mlon=" + longitude;
                        htmlDocumentControl.NavigateToURL(url);
                    }
                    else
                    {
                        if (mwvm.SelectedResult != null)
                        {
                            htmlDocumentControl.renderResultAccordingToSettings(mwvm.SelectedResult, this.DataContext as MainWindowViewModel);
                        }
                        else
                        {
                            htmlDocumentControl.RenderBlankPageThemeColored();
                        }
                    }
                }

                if (e.PropertyName?.Equals(nameof(mwvm.SelectedIndex)) ?? false)
                {
                    // Ensure that the Search Source matches SelectedIndex, where possible.
                    foreach (var src in mwvm.AvailableSearchSources)
                    {
                        if (src.Source is IIndex index)
                        {
                            if (index.Id.Equals(mwvm.SelectedIndex?.Id))
                            {
                                mwvm.SelectedSearchSource = src;
                            }
                        }
                    }
                }
            }
        }

        [SupportedOSPlatform("windows")]
        private void VoiceListener_OnVoiceInput(object? sender, string e)
        {
            if (DataContext is MainWindowViewModel mwvm)
            {
                voiceListener?.StopListening();
                voiceListener = null;
                mwvm.IsVoiceInputActive = false;

                if (!string.IsNullOrEmpty(e))
                {
                    mwvm.Session.Query.Query = e.Trim();
                    ResultsGrid2.Focus();
                    UpdateSearchResults();
                }
            }
        }

        private bool _isSoundexLoaded = false;

        /// <summary>
        /// Should only be called from SelectedIndexChanged and the SelectIndex methods
        /// The SelectIndex method will initialize the wheel by itself, so call that instead.
        /// </summary>
        /// <param name="wheel"></param>
        private async Task init_wheel(IIndex? index)
        {
            _isSoundexLoaded = false;

            IWordWheel? wheel = index?.WordWheel ?? null;

            if (DataContext is MainWindowViewModel mwvm)
            {
                if (mwvm.Wheel == null)
                {
                    mwvm.Wheel = new WheelViewModel(wheel);
                }
                
                if (wheel != null)
                {
                    mwvm.Wheel.HideFrequency = false;
                    mwvm.Wheel.SetWordWheel(wheel);
                    await wheel.BeginLoad();
                    SoundexDictionary soundexDictionary = new SoundexDictionary(wheel);
                    await soundexDictionary.BuildDictionary();
                    index?.SetActiveSoundexDictionary(soundexDictionary);
                    _isSoundexLoaded = true;
                    //For Soundex, if UseSoundex is enabled should update search results now that soundex dictionary has loaded since previous results would not include soundex.
                }
                else
                {
                    mwvm.Wheel.HideFrequency = false;
                    mwvm.Wheel.SetWordWheel(null);
                }

                
            }
        }


        private bool columns_already_initialising = false;
        private bool columns_initialise_queued = false;

        private void init_columns()
        {
            if (columns_already_initialising)
            {
                columns_initialise_queued = true;
                return;
            }
            columns_already_initialising = true;
            try
            {
                try
                {
                    if (this.DataContext != null && this.DataContext is MainWindowViewModel vm)
                    {
                        ColumnList<ResultViewModel> list = new ColumnList<ResultViewModel>();

                        IColumn? sortColumn = null;


                        if (vm.SelectedIndex != null)
                        {
                            var config = vm.IndexLibrary.GetConfiguration(vm.SelectedIndex);


                            var columnSizeMode = config.ColumnSizingMode;
                            switch (columnSizeMode)
                            {
                                case ResultsSettingsWindowViewModel.ColumnWidthOption.WidthContent:
                                    // ResultsGrid.ColumnWidth = DataGridLength.SizeToCells;
                                    
                                    break;
                                case ResultsSettingsWindowViewModel.ColumnWidthOption.WidthWindow:
                                    // ResultsGrid.ColumnWidth = DataGridLength.Auto;
                                    break;
                            }

                            vm.Columns = config.ColumnDisplaySettings;


                            var source = new FlatTreeDataGridSourceCustomSortable<ResultViewModel>(vm.Results);
                            source.SortEvent += Source_SortEvent;

                            var checkBoxSelectColumn = new TemplateColumn<ResultViewModel>(string.Empty, new FuncDataTemplate<ResultViewModel>((_, _) => new CheckBox
                            {
                                [!CheckBox.IsCheckedProperty] = new Binding("IsResultChecked"),
                                [CheckBox.IsHitTestVisibleProperty] = false,
                                [CheckBox.MarginProperty] = new Thickness(5, 0, 0, 0)
                            })
                            );
                            source.Columns.Add(checkBoxSelectColumn); // Always comes first.
                            if (vm.Session.Query.SortBy == string.Empty) {
                                sortColumn = checkBoxSelectColumn;
                                //source.Columns[0].SortDirection = vm.Session.Query.SortAscending ? ListSortDirection.Ascending : ListSortDirection.Descending;
                            }

                            foreach (var colDef in vm.Columns.Where(c => c.Visible).OrderBy(c => c.DisplayIndex))
                            {
                                GridLength width;
                                switch(columnSizeMode)
                                {
                                    case ResultsSettingsWindowViewModel.ColumnWidthOption.WidthContent:
                                        width = GridLength.Auto;
                                        break;
                                    case ResultsSettingsWindowViewModel.ColumnWidthOption.WidthWindow:
                                        width = GridLength.Star;
                                        break;
                                    default:
                                        width = new GridLength(colDef.Width);
                                        break;
                                }


                                var textColumn = new TextColumn<ResultViewModel, string>
                                    (
                                    S.Get(colDef.Header), r => Models.Utils.GetValueByPathAsString(r, colDef.BindTo), width
                                    );
                                textColumn.Options.CanUserResizeColumn = true;
                                textColumn.Options.CanUserSortColumn = true;
                                textColumn.Options.MinWidth = new GridLength(50);
                                textColumn.PropertyChanged += TextColumn_PropertyChanged;
                                textColumn.Tag = colDef;
                                source.Columns.Add(textColumn);
                                if (colDef.Header == vm.Session.Query.SortBy)
                                {
                                    sortColumn = textColumn;
                                    //textColumn.SortDirection = vm.Session.Query.SortAscending ? ListSortDirection.Ascending : ListSortDirection.Descending;
                                }
                                
                            }
                            #region Experimenting with Cell Selection
                            //var selectionModel = new TreeDataGridCellSelectionModel<ResultViewModel>(source);
                            //selectionModel.SingleSelect = false;
                            //selectionModel.SelectionChanged += ResultsGrid2_SelectionModel_SelectionChanged;
                            #endregion
                            
                            
                            ResultsGrid2.Source = source;
                            ResultsGrid2.RowSelection.SelectionChanged += ResultsGrid2_RowSelection_SelectionChanged;
                            ResultsGrid2.RowSelection.SingleSelect = false;
                            if (sortColumn != null)
                            {
                                ((IColumn)sortColumn).SortDirection = vm.Session.Query.SortAscending ? ListSortDirection.Ascending : ListSortDirection.Descending;
                                //source.InvokeSortedEvent(); // HACK
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    TaskDialogWindow.OKDialog(S.Get("Something went wrong"), ex.ToString(), Program.GetMainWindow());
                }

            } finally
            {
                columns_already_initialising = false;
                if (columns_initialise_queued)
                {
                    columns_initialise_queued = false;
                    init_columns();
                }
            }
            
        }

        private void TextColumn_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (
                sender is TextColumn<ResultViewModel, string> textColumn && textColumn.Tag is DataColumn colDef)
            {
                if (e.PropertyName?.Equals(nameof(textColumn.ActualWidth)) ?? false) {
                    colDef.Width = (int)textColumn.ActualWidth;
                }
            }
        }

        private void Source_SortEvent(IColumn? column, ListSortDirection direction)
        {
            // TreeDataGrid has requested sorting...
            //PauseSearchUpdates = true;
            try
            {
                if (DataContext is MainWindowViewModel mwvm)
                {
                    
                    if (column != null)
                    {
                        if (mwvm.Results is IDataColumnSortable sortableCollection)
                        {
                            if (column.Tag is DataColumn dataColumn)
                            {
                                PauseSearchUpdates = true;
                                mwvm.Session.Query.SortAscending = direction == ListSortDirection.Ascending;
                                mwvm.Session.Query.SortBy = dataColumn.Header;
                                PauseSearchUpdates = false;
                            }
                            //if (column.Tag is DataColumn dataColumn)
                            //{
                            //    var header = dataColumn.Header;
                            //    if (header == mwvm.Session.Query.SortBy)
                            //    {
                            //        // Sorting on same header as before. Change the sort order.
                            //        mwvm.Session.Query.SortAscending = !mwvm.Session.Query.SortAscending;
                            //    } else
                            //    {
                            //        mwvm.Session.Query.SortAscending = true;
                            //        mwvm.Session.Query.SortBy = header;
                            //    }
                            //    UpdateSearchResults();
                            //} else
                            //{
                            //    // Checkbox Column
                            //    if (mwvm.Session.Query.SortBy == string.Empty)
                            //    {
                            //        // Already on default sort, just invert the order.
                            //        mwvm.Session.Query.SortAscending = !mwvm.Session.Query.SortAscending;
                                    
                            //    } else
                            //    {
                            //        mwvm.Session.Query.SortAscending = true;
                            //        mwvm.Session.Query.SortBy = string.Empty;
                            //    }
                            //    UpdateSearchResults();
                            //}
                        }
                    }
                }
            } catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            } finally
            {
                //PauseSearchUpdates = false;
            }
        }

        private void ResultsGrid2_SelectionModel_SelectionChanged(object? sender, TreeDataGridCellSelectionChangedEventArgs<ResultViewModel> e)
        {

        }

        private void DgColumn_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            
            if (e.Property.Name == "Width")
            {
                // TODO Detect Column Resize and Persist.
            }
        }

        private void WordWheel_WordSubmission(object? sender, Models.Search.LuceneWordWheel.WheelWord e)
        {
            if (this.DataContext is MainWindowViewModel mainWindowViewModel)
            {
                string word = e.Word;
                string query = mainWindowViewModel.Session.Query.Query;
                if (query.Length > 0 && !query.EndsWith(" "))
                {
                    // doesn't end in space, check if the user is completing a word.
                    string lastWordInQuery = query.Split(' ').Last().ToLower();
                    if (word.ToLower().StartsWith(lastWordInQuery.ToLower()))
                    {
                        // The selected word starts with the user entered word.
                        // Replace the last word.
                        Debug.WriteLine("Replacing last word");
                        string[] words = query.Split(' ');
                        words[words.Length - 1] = word;
                        query = string.Join(' ', words);
                    }
                    else
                    {
                        query += " ";
                        query += word.Trim();
                    }
                }
                else
                {
                    query += word;
                }
                query += " ";
                mainWindowViewModel.Session.Query.Query = query;
                queryTextBox.Focus();
                queryTextBox.CaretIndex = query.Length;
            }
        }

        // For debugging.
        public void ShowBrowserDebug()
        {
            htmlDocumentControl.ShowDevTools();
        }

        public HtmlDocumentControl GetHtmlDocumentControl()
        {
            return htmlDocumentControl;
        }

        private void MenuItem_Click_1(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
        }

        private void MenuItem_Click_2(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
        }
    }
}