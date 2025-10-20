using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using eSearch.ViewModels.StatusUI;
using System;

namespace eSearch.Views.StatusUI;

public partial class StatusControl : UserControl
{

    public StatusControl()
    {
        InitializeComponent();
        this.BtnDismiss.Click += BtnDismiss_Click;
        this.BtnCancel.Click += BtnCancel_Click;
        this.PointerPressed += OnPointerPressed;
        Background = Brushes.Transparent; // Make control hit-testable for cursor.
    }

    private void BtnCancel_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is StatusControlViewModel vm)
        {
            vm.CancelAction?.Invoke();
        }
    }

    private void BtnDismiss_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is StatusControlViewModel vm)
        {
            vm.DismissAction?.Invoke();
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is StatusControlViewModel vm)
        {
            vm.ClickAction?.Invoke();
        }
    }
}