using Avalonia.Controls;
using Avalonia.Input;
using ChocolateyPackageBuilder.Gui.ViewModels;

namespace ChocolateyPackageBuilder.Gui.Views;

public partial class CustomInstallerProjectView : UserControl
{
    private ActionItemViewModel? _draggedAction;

    public CustomInstallerProjectView()
    {
        InitializeComponent();
    }

    private async void ActionDragHandle_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { Tag: ActionItemViewModel action }) return;
        _draggedAction = action;
        var data = new DataTransfer();
        data.Add(DataTransferItem.CreateText("cpb-action"));
        await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move);
    }

    private void ActionCard_OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = _draggedAction is null ? DragDropEffects.None : DragDropEffects.Move;
        e.Handled = true;
    }

    private void ActionCard_OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is CustomInstallerProjectViewModel viewModel &&
            sender is Control { Tag: ActionItemViewModel target })
            viewModel.MoveActionBefore(_draggedAction, target);
        _draggedAction = null;
        e.Handled = true;
    }

    private void ActionStack_OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is CustomInstallerProjectViewModel viewModel)
            viewModel.MoveActionToEnd(_draggedAction);
        _draggedAction = null;
        e.Handled = true;
    }
}