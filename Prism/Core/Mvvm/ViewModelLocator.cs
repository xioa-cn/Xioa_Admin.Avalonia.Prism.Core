using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace Prism.Mvvm;

public static class ViewModelLocator
{
    public static readonly AttachedProperty<bool> AutoWireViewModelProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("AutoWireViewModel", typeof(ViewModelLocator));

    public static readonly AttachedProperty<bool> AutoWireViewModelOnAttachedProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("AutoWireViewModelOnAttached", typeof(ViewModelLocator), true);

    public static readonly AttachedProperty<bool> AutoWireViewModelOverwriteProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("AutoWireViewModelOverwrite", typeof(ViewModelLocator));

    public static readonly AttachedProperty<bool> AutoWireViewModelRecursiveProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("AutoWireViewModelRecursive", typeof(ViewModelLocator));

    static ViewModelLocator()
    {
        AutoWireViewModelProperty.Changed.AddClassHandler<Control>((control, args) =>
        {
            if (args.NewValue is bool shouldWire && shouldWire)
            {
                RewireViewModel(control, GetAutoWireViewModelOverwrite(control));
                if (GetAutoWireViewModelOnAttached(control))
                {
                    control.AttachedToLogicalTree -= OnAttachedToLogicalTree;
                    control.AttachedToLogicalTree += OnAttachedToLogicalTree;
                }
            }
        });
    }

    public static bool GetAutoWireViewModel(Control control) => control.GetValue(AutoWireViewModelProperty);

    public static void SetAutoWireViewModel(Control control, bool value) => control.SetValue(AutoWireViewModelProperty, value);

    public static bool GetAutoWireViewModelOnAttached(Control control) => control.GetValue(AutoWireViewModelOnAttachedProperty);

    public static void SetAutoWireViewModelOnAttached(Control control, bool value) => control.SetValue(AutoWireViewModelOnAttachedProperty, value);

    public static bool GetAutoWireViewModelOverwrite(Control control) => control.GetValue(AutoWireViewModelOverwriteProperty);

    public static void SetAutoWireViewModelOverwrite(Control control, bool value) => control.SetValue(AutoWireViewModelOverwriteProperty, value);

    public static bool GetAutoWireViewModelRecursive(Control control) => control.GetValue(AutoWireViewModelRecursiveProperty);

    public static void SetAutoWireViewModelRecursive(Control control, bool value) => control.SetValue(AutoWireViewModelRecursiveProperty, value);

    public static void RewireViewModel(Control control, bool overwrite = false)
    {
        ArgumentNullException.ThrowIfNull(control);
        if (!overwrite && control.DataContext is not null)
        {
            return;
        }

        control.DataContext = ViewModelLocationProvider.AutoWireViewModelChanged(control);
    }

    private static void OnAttachedToLogicalTree(object? sender, global::Avalonia.LogicalTree.LogicalTreeAttachmentEventArgs e)
    {
        if (sender is Control control && GetAutoWireViewModel(control))
        {
            RewireViewModel(control, GetAutoWireViewModelOverwrite(control));
            WireTemplateChildren(control);
        }
    }

    private static void WireTemplateChildren(Control root)
    {
        foreach (var child in root.GetLogicalDescendants().OfType<Control>())
        {
            if (GetAutoWireViewModel(child) || GetAutoWireViewModelRecursive(root))
            {
                RewireViewModel(child, GetAutoWireViewModelOverwrite(child));
            }
        }
    }
}