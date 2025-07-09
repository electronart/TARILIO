using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using eSearch.ViewModels;

namespace eSearch.Views.LLMConversations;

public partial class LLMConversationControl : UserControl
{
    public LLMConversationControl()
    {
        InitializeComponent();
    }

    private void MessageControl_RemoveRequested(object sender, RoutedEventArgs e)
    {
        if (sender is MessageControl messageControl
            && messageControl.DataContext is LLMMessageViewModel messageViewModel
            && DataContext is LLMConversationViewModel conversationViewModel)
        {
            conversationViewModel.Messages.Remove(messageViewModel);
        }
    }
}