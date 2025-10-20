using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using eSearch.ViewModels;
using System;

namespace eSearch.Views;

public partial class LLMGenerationParametersControl : UserControl
{

    public event EventHandler? ParametersChanged;

    public LLMGenerationParametersControl()
    {
        InitializeComponent();
        DataContextChanged += LLMGenerationParametersControl_DataContextChanged;
        if (DataContext != null && DataContext is LLMGenerationParametersViewModel vm)
        {
            vm.PropertyChanged += Vm_PropertyChanged;
        }
    }

    private void LLMGenerationParametersControl_DataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is LLMGenerationParametersViewModel vm)
        {
            vm.PropertyChanged += Vm_PropertyChanged;
            vm.AnyParameterChanged += (sender, e) =>
            {
                ParametersChanged?.Invoke(sender, e);
            };
        }
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        ParametersChanged?.Invoke(this, e);
    }
}