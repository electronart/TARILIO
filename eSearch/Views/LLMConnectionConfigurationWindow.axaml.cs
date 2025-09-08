using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using eSearch.Models;
using eSearch.ViewModels;
using System.Threading.Tasks;
using System;
using eSearch.Views;
using S = eSearch.ViewModels.TranslationsViewModel;
using eSearch.Models.AI;
using System.Linq;
using eSearch.Models.Configuration;
using sun.misc;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using org.apache.xmlbeans.impl.xb.xsdschema;
using System.IO;

namespace eSearch;

public partial class LLMConnectionConfigurationWindow : Window
{
    public TaskDialogResult DialogResult = TaskDialogResult.Cancel;

    public LLMConnectionConfigurationWindow()
    {
        InitializeComponent();

        BtnAddConnection.Click += BtnAddConnection_Click;
        BtnRemoveConnection.Click += BtnRemoveConnection_Click;
        ButtonSetupTest.Click += ButtonSetupTest_Click;
        ButtonSetupCancel.Click += ButtonSetupCancel_Click;
        ButtonClose.Click += ButtonClose_Click;
        BtnRenameConnection.Click += BtnRenameConnection_Click;
        DataContextChanged += LLMConnectionConfigurationWindow_DataContextChanged;
        BtnEditConnection.Click += BtnEditConnection_Click;
        BtnPromptImport.Click += BtnPromptImport_Click;
        BtnPromptExport.Click += BtnPromptExport_Click;

        //AutoCompleteBoxModelName.GotFocus += AutoCompleteBoxModelName_GotFocus;

    }

    private async void BtnPromptExport_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            string prompts_dir = Program.ESEARCH_LLM_SYSTEM_PROMPTS_DIR;
            Directory.CreateDirectory(prompts_dir);
            if (DataContext is LLMConnectionWindowViewModel vm)
            {
                string systemPrompt = vm.CustomSystemPrompt;
                var dialog = new SaveFileDialog // TODO
                {
                    // Set the default directory
                    Directory = prompts_dir,
                    // Set the default file extension
                    DefaultExtension = "txt",
                    // Configure file filters to allow only .txt files
                    Filters = new()
            {
                new FileDialogFilter
                {
                    Name = "Text Files",
                    Extensions = { "txt" }
                }
            },
                    // Set the dialog title
                    Title = S.Get("Export Prompt")
                };

                var res = await dialog.ShowAsync(this);
                if (res != null)
                {
                    File.WriteAllText(res, systemPrompt);
                }
            }
        } catch (Exception ex)
        {
            Debug.WriteLine($"Error exporting prompt {ex.ToString()}");
            await TaskDialogWindow.OKDialog(S.Get("An error occurred"), ex.ToString(), this);
        }

    }

    private async void BtnPromptImport_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        string prompts_dir = Program.ESEARCH_LLM_SYSTEM_PROMPTS_DIR;
        Directory.CreateDirectory(prompts_dir);

        try
        {
            var dialog = new OpenFileDialog // TODO
            {
                Title = S.Get("Import Prompt"),
                Directory = prompts_dir,
                AllowMultiple = false // Set to true if you want multi-select
            };

            // Add .txt filter (name is user-friendly, extensions are the actual filter)
            dialog.Filters = new List<FileDialogFilter>
        {
            new FileDialogFilter
            {
                Name = "Text Files",
                Extensions = { "txt" }
            },
            new FileDialogFilter
            {
                Name = "All Files",
                Extensions = { "*" }
            }
        };

            // Show the dialog (pass 'this' as the parent Window)
            var result = await dialog.ShowAsync(this);
            if (result.Length == 1)
            {
                string txt = File.ReadAllText(result[0]);
                if (DataContext is LLMConnectionWindowViewModel vm)
                {
                    vm.CustomSystemPrompt = txt;
                }
            }
        } catch (Exception ex)
        {
            Debug.WriteLine($"Error importing prompt {ex.ToString()}");
            await TaskDialogWindow.OKDialog(S.Get("An error occurred"), ex.ToString(), this);
        }
    }

    CancellationTokenSource? modelLookupCancellationTokenSource = null;

    private string _autoCompleteFetchModelsURL      = string.Empty;
    private string _autoCompleteFetchModelsAPIKey   = string.Empty;


    //private async void AutoCompleteBoxModelName_GotFocus(object? sender, Avalonia.Input.GotFocusEventArgs e)
    //{
        
    //    if (DataContext is LLMConnectionWindowViewModel viewModel)
    //    {
    //        if (viewModel.EnteredModelName != string.Empty) return; // Only populate when the box starts empty.
            
    //        string endpoint = viewModel.SelectedService == LLMService.Custom ? viewModel.ServerURL : Completions.GetOpenAIEndpointURL(viewModel.SelectedService);
    //        string api_key  = viewModel.APIKey;
    //        try
    //        {
    //            if (_autoCompleteFetchModelsURL == endpoint && _autoCompleteFetchModelsAPIKey == api_key)
    //            {
    //                // We already looked this endpoint up.
    //                InvokeDropDown(AutoCompleteBoxModelName);
    //            }
    //            else
    //            {
    //                // Look this endpoint up try to get a list of models.
    //                _autoCompleteFetchModelsAPIKey = api_key;
    //                _autoCompleteFetchModelsURL = endpoint;
    //                AutoCompleteBoxModelName.ItemsSource = new List<string>(); // Clear any previous suggestions.
    //                if (!string.IsNullOrWhiteSpace(endpoint)
    //                && !string.IsNullOrWhiteSpace(api_key))
    //                {
    //                    modelLookupCancellationTokenSource?.Dispose();
    //                    modelLookupCancellationTokenSource = new CancellationTokenSource(new TimeSpan(0, 0, 30));
    //                    var models = await Completions.TryGetAvailableModels(endpoint, api_key, modelLookupCancellationTokenSource.Token);
    //                    AutoCompleteBoxModelName.ItemsSource = models;
    //                    AutoCompleteBoxModelName.PopulateComplete();
    //                    // Seriously why can't they just expose basic functionality..
    //                    InvokeDropDown(AutoCompleteBoxModelName); // This may throw an exception...

    //                }
    //            }
    //        } catch (Exception ex)
    //        {
    //            Debug.WriteLine($"An error occurred listing models for autocomplete box: {ex.ToString()}");
    //        }
    //    }
    //}

    /// <summary>
    /// Uses reflection, unsafe HACK
    /// </summary>
    /// <param name="autoCompleteBox"></param>
    /// <exception cref="ArgumentNullException"></exception>
    private void InvokeDropDown(AutoCompleteBox autoCompleteBox)
    {
        if (autoCompleteBox == null)
        {
            throw new ArgumentNullException(nameof(autoCompleteBox));
        }

        // Get the type (use autoCompleteBox.GetType() if it's a subclass)
        Type type = typeof(AutoCompleteBox);

        // Find the private method (adjust BindingFlags if it's static: add BindingFlags.Static and remove Instance)
        MethodInfo method = type.GetMethod("OpeningDropDown",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (method != null)
        {
            // Invoke it (pass parameters as object[] if needed, e.g., new object[] { arg1, arg2 })
            method.Invoke(autoCompleteBox, [false]);
        }
        else
        {
            // Handle case where method is not found (e.g., misspelled or doesn't exist)
            Console.WriteLine("Method 'OpeningDropDown' not found.");
        }
    }

    private void BtnEditConnection_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is LLMConnectionWindowViewModel viewModel)
        {
            viewModel.IsEditing = true;
        }
    }

    private async void BtnRenameConnection_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is LLMConnectionWindowViewModel viewModel && viewModel.SelectedConnection != null)
        {
            var original_name = viewModel.SelectedConnection.GetDisplayName();
            var res = await TextInputDialog.ShowDialog(this, S.Get("Rename"), string.Empty, original_name, original_name, null, 45);
            if (res.Item1 == TaskDialogResult.OK)
            {
                string new_name = res.Item2.Text;
                viewModel.SelectedConnection.CustomDisplayName = new_name;
                Program.SaveProgramConfig();
                DataContext = LLMConnectionWindowViewModel.FromProgramConfiguration();
            }
        }
    }

    private void ButtonClose_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private void BtnRemoveConnection_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is LLMConnectionWindowViewModel viewModel)
        {
            if (viewModel.SelectedConnection != null)
            {
                int i = Program.ProgramConfig.AISearchConfigurations.FindIndex(connection => connection.Id == viewModel.PreviousID);
                if (i != -1) Program.ProgramConfig.AISearchConfigurations.RemoveAt(i);
                Program.SaveProgramConfig();
                DataContext = LLMConnectionWindowViewModel.FromProgramConfiguration();
            }
        }
    }

    private void BtnAddConnection_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is LLMConnectionWindowViewModel viewModel)
        {
            viewModel.SelectedConnection = null;
            viewModel.PreviousID = null;
            viewModel.ShowConnectionForm = true;
            viewModel.SelectedService = LLMService.Perplexity;
            viewModel.SelectedService = LLMService.Custom; // Switching back and forth will clear the form and ensure the right controls are visible.
            viewModel.IsEditing = true;
        }
    }

    private void LLMConnectionConfigurationWindow_DataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is LLMConnectionWindowViewModel viewModel)
        {
            viewModel.ShowConnectionForm = false;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (DataContext is LLMConnectionWindowViewModel viewModel)
        {
            if ( (e.PropertyName?.Equals(nameof(LLMConnectionWindowViewModel.IsEditing)) ?? false) || 
                 (e.PropertyName?.Equals(nameof(LLMConnectionWindowViewModel.IsTesting)) ?? false) )
            {
                viewModel.EnableConnectionForm = (viewModel.IsEditing && !viewModel.IsTesting);
            }

            if (e.PropertyName?.Equals(nameof(LLMConnectionWindowViewModel.SelectedConnection)) ?? false)
            {
                if (viewModel.SelectedConnection == null)
                {
                    viewModel.PreviousID = null;
                    viewModel.ShowConnectionForm = false;
                    BtnEditConnection.IsVisible = false;
                }
                else
                {
                    viewModel.ShowConnectionForm = true;
                    viewModel.IsEditing = false;
                    BtnEditConnection.IsVisible = true;
                    viewModel.PreviousID = null; // This will get set in the populate values from config method below.
                    viewModel.PopulateValuesFromConfiguration(viewModel.SelectedConnection);
                }

                BtnRemoveConnection.IsEnabled = viewModel.SelectedConnection != null;
                BtnRenameConnection.IsEnabled = viewModel.SelectedConnection != null;
            }
            if (e.PropertyName?.Equals(nameof(LLMConnectionWindowViewModel.SelectedService)) ?? false)
            {
                #region Hide/Show Server URL Textbox/Model Selection Drop down as appropriate.
                switch (viewModel.SelectedService)
                {
                    case LLMService.Custom:
                        viewModel.HideServerURL = false;
                        break;
                    default:
                        viewModel.HideServerURL = true;
                        break;
                }
                viewModel.HidePerplexityModelSelectionDropDown = viewModel.SelectedService != LLMService.Perplexity;
                #endregion
                #region Clear Inputs
                viewModel.ServerURL        = string.Empty;
                viewModel.APIKey           = string.Empty;
                viewModel.EnteredModelName = string.Empty;
                viewModel.CustomSystemPrompt = string.Empty;
                #endregion
            }
        }
    }

    private void ButtonSetupCancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is LLMConnectionWindowViewModel viewModel)
        {
            viewModel.ValidationError = string.Empty;
            viewModel.SelectedConnection = null;
            viewModel.ShowConnectionForm = false;
            viewModel.IsEditing = false;
        }
    }

    private async void ButtonSetupTest_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is LLMConnectionWindowViewModel viewModel)
        {
            if (!IsInputValid(out string reason)) // Validate the form.
            {
                viewModel.ValidationError = reason;
                return;
            }
            else
            {
                viewModel.ValidationError = string.Empty;
            }
            viewModel.IsTesting = true;

            var newAIConfig = viewModel.ToAiSearchConfiguration();
            var testsOK = await newAIConfig.Test();
            if (testsOK.Item1 == true)
            {
                // Valid Configuration.
                #region Update or add configuration to program config, depending on if we're editing or adding a configuration.
                if (viewModel.PreviousID != null)
                {
                    // Editing. First remove the existing config from the Program.
                    int i = Program.ProgramConfig.AISearchConfigurations.FindIndex(config => config.Id == viewModel.PreviousID);
                    if (i != -1) Program.ProgramConfig.AISearchConfigurations.RemoveAt(i);
                }
                Program.ProgramConfig.AISearchConfigurations.Add(newAIConfig);
                Program.SaveProgramConfig();
                DataContext = LLMConnectionWindowViewModel.FromProgramConfiguration();
                #endregion
                await TaskDialogWindow.OKDialog(viewModel.ApplicationName, S.Get("Connected Successfully."), this);
            }
            else
            {
                // Issue with configuration.
                viewModel.IsTesting = false;
                await TaskDialogWindow.OKDialog(S.Get("Error"), testsOK.Item2, this);
            }
        }
    }

    private bool IsInputValid(out string reason)
    {
        if (DataContext is LLMConnectionWindowViewModel vm)
        {
            switch (vm.SelectedService)
            {
                case Models.AI.LLMService.Perplexity:
                case LLMService.OpenRouter:
                    // Require an API Key.
                    if (string.IsNullOrWhiteSpace(vm.APIKey))
                    {
                        reason = S.Get("API Key Required");
                        return false;
                    }
                    break;
                case Models.AI.LLMService.ChatGPT:
                case LLMService.Ollama:
                case LLMService.LMStudio:
                    if (string.IsNullOrWhiteSpace(vm.EnteredModelName))
                    {
                        reason = S.Get("Model Name required");
                        return false;
                    }
                    break;
                case Models.AI.LLMService.Custom:
                    // DONT require an API Key - They may be using localhost, which may or may not require one.
                    // DO Require a Server URL.
                    if (string.IsNullOrEmpty(vm.ServerURL))
                    {
                        reason = S.Get("Server URL Required");
                        return false;
                    }
                    // DO NOT Require a model name, depending on the API, this is not required.
                    break;
            }
            reason = "Valid";
            return true;
        } else
        {
            reason = "Application Error: No DataContext Set";
            return false;
        }
    }

    public static async Task<TaskDialogResult> ShowDialog(Window owner)
    {
        var vm = LLMConnectionWindowViewModel.FromProgramConfiguration();
        var dialog = new LLMConnectionConfigurationWindow();
        dialog.DataContext = vm;
        await ((Window)dialog).ShowDialog(owner);
        return dialog.DialogResult;
    }

}