using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using com.cybozu.labs.langdetect.util;
using DynamicData;
using eSearch.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace eSearch.Views.LLMConversations;

public partial class MessageControl : UserControl
{
    public static readonly RoutedEvent<RoutedEventArgs> RemoveRequestedEvent =
        RoutedEvent.Register<MessageControl, RoutedEventArgs>(
            nameof(RemoveRequested), RoutingStrategies.Bubble);

    public event EventHandler<RoutedEventArgs> RemoveRequested
    {
        add => AddHandler(RemoveRequestedEvent, value);
        remove => RemoveHandler(RemoveRequestedEvent, value);
    }

    public static readonly RoutedEvent<RoutedEventArgs> RegenerateRequestedEvent =
        RoutedEvent.Register<MessageControl, RoutedEventArgs>(
            nameof(RegenerateRequested), RoutingStrategies.Bubble);

    public event EventHandler<RoutedEventArgs> RegenerateRequested
    {
        add => AddHandler(RegenerateRequestedEvent, value);
        remove => RemoveHandler(RegenerateRequestedEvent, value);
    }


    public MessageControl()
    {
        InitializeComponent();
        this.DataContextChanged     += MessageControl_DataContextChanged;
        buttonBrowserDebug.Click    += ButtonBrowserDebug_Click;
        buttonCopyMessage.Click     += ButtonCopyMessage_Click;
        buttonDeleteMessage.Click   += ButtonDeleteMessage_Click;
        buttonStopGeneration.Click  += ButtonStopGeneration_Click;
        buttonRegenerateResponse.Click += ButtonRegenerateResponse_Click;
        NotesEditTextBox.LostFocus  += NotesEditTextBox_LostFocus;
        // queryTextBox.AddHandler(InputElement.KeyDownEvent, QueryTextBox_KeyDown, RoutingStrategies.Tunnel);
        NotesEditTextBox.AddHandler(InputElement.KeyDownEvent, NotesEditTextBox_KeyDown, RoutingStrategies.Tunnel);

        List<IBinding> regenerateResponseBindings =
        [
            this.GetObservable(IsRegenerateButtonVisibleProperty).ToBinding(),
            new Binding("IsFinishedStreaming"),
        ];
        buttonRegenerateResponse[!Button.IsVisibleProperty] = 
            new MultiBinding { 
                Converter = BoolConverters.And, 
                Bindings = regenerateResponseBindings
            };

    }

    

    public static readonly StyledProperty<bool> IsRegenerateButtonVisibleProperty = AvaloniaProperty.Register<MessageControl, bool>(nameof(IsRegenerateButtonVisible), defaultValue: true);

    public bool IsRegenerateButtonVisible
    {
        get => GetValue(IsRegenerateButtonVisibleProperty);
        set => SetValue(IsRegenerateButtonVisibleProperty, value);
    }

    /// <summary>
    /// Returns true if this control is the last child of its parent
    /// And the role is 'assistant'
    /// </summary>
    /// <returns></returns>
    private bool IsElegibleForRegenerate()
    {
        if (Parent is Panel panel)
        {
            var children = panel.Children;
            int index = children.IndexOf(this);
            if (index == children.Count - 1)
            {
                // This is the last child.
                if (DataContext is LLMMessageViewModel messageVM)
                {
                    if (messageVM.Role == "assistant")
                    {
                        // Last child and role is assistant.
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private void UpdateButtonVisibility()
    {
        IsRegenerateButtonVisible = IsElegibleForRegenerate();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Parent is Panel panel)
        {
            ((AvaloniaList<Control>)panel.Children).CollectionChanged += Children_CollectionChanged;
        }
        UpdateButtonVisibility();
    }

    private void Children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateButtonVisibility();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (Parent is Panel panel)
        {
            ((AvaloniaList<Control>)panel.Children).CollectionChanged -= Children_CollectionChanged;
        }
        base.OnDetachedFromVisualTree(e);
    }



    private void NotesEditTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is LLMMessageViewModel messageViewModel)
        {
            if (e.Key == Key.Enter)
            {
                if (e.KeyModifiers == KeyModifiers.Shift)
                {
                    int selectionStart = NotesEditTextBox.SelectionStart;
                    int selectionEnd = NotesEditTextBox.SelectionEnd;
                    string newStr = NotesEditTextBox.Text.Remove(selectionStart, selectionEnd - selectionStart).Insert(selectionStart, "\n");
                    NotesEditTextBox.Text = newStr;
                    NotesEditTextBox.CaretIndex += 1;
                    e.Handled = true;
                }
                else
                {
                    // Treat as submit.
                    messageViewModel.IsEditingNote = false;
                    e.Handled = true;
                    return;
                }
            }
        }
    }

    private void NotesEditTextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is LLMMessageViewModel messageViewModel)
        {
            messageViewModel.IsEditingNote = false;
        }
    }

    private void ButtonStopGeneration_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is LLMMessageViewModel messageViewModel)
        {
            if (messageViewModel.IsFinishedStreaming == false)
            {
                messageViewModel.CancellationSource?.Cancel();
                messageViewModel.IsFinishedStreaming = true;
            }
        }
    }

    private void ButtonDeleteMessage_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var args = new RoutedEventArgs(RemoveRequestedEvent);
        RaiseEvent(args);
    }

    private void ButtonRegenerateResponse_Click(object? sender, RoutedEventArgs e)
    {
        var args = new RoutedEventArgs(RegenerateRequestedEvent);
        RaiseEvent(args);
    }

    #region Copy Button Handling
    private IImage _iconCopy;
    private IImage _iconCopyDone;
    private IImage _currentCopyButtonIcon;

    public IImage CopyButtonImageSource
    {
        get => _currentCopyButtonIcon;
        set
        {
            _currentCopyButtonIcon = value;
        }
    }

    private async void ButtonCopyMessage_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is LLMMessageViewModel messageViewModel)
        {
            
            Models.AI.Message message = messageViewModel.GetFinalMessage() ?? new Models.AI.Message { 
                Content = "", 
                Role = "",
                Model = ""};
            await (Program.GetMainWindow()?.Clipboard?.SetTextAsync(message.Content) ?? Task.CompletedTask);
            // Display the checkmark to indicate it has copied briefly.
            messageViewModel.JustCopiedMessage = true; 
            await Task.Delay(1000 * 3);
            messageViewModel.JustCopiedMessage = false;
        }
    }


    #endregion

    private void ButtonBrowserDebug_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        MessageCEFViewer.ShowDevTools();
    }

    private void MessageControl_DataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is LLMMessageViewModel messageViewModel)
        {
            // Will cause the browser control to resize to the rendered message.
            MessageCEFViewer.SetAutomaticControlHeightEnabled(true); 
            MessageCEFViewer.RenderLLMMessage(messageViewModel);
            messageViewModel.PropertyChanged += MessageViewModel_PropertyChanged;
        }
    }

    private async void MessageViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (DataContext is LLMMessageViewModel viewModel)
        {
            if (e.PropertyName == nameof(LLMMessageViewModel.IsEditingNote))
            {
                if (viewModel.IsEditingNote)
                {
                    await Task.Factory.StartNew(() =>
                    {
                        Thread.Sleep(50); // TODO Horrible hack. Need a better solution
                    });
                    NotesEditTextBox.Focus();
                }
            }
        }
    }
}