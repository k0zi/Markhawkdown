using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Markhawkdown.ViewModels;
using SukiUI.Controls;

namespace Markhawkdown.Views;

public partial class MainWindow : SukiWindow
{
    private bool _closeConfirmed;

    public MainWindow()
    {
        InitializeComponent();
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (_closeConfirmed || ViewModel is null || ViewModel.Documents.All(d => !d.IsDirty))
            return;

        e.Cancel = true;
        if (!await ViewModel.ConfirmCloseAllAsync())
            return;

        _closeConfirmed = true;
        Close();
    }

    private void OnExitClick(object? sender, RoutedEventArgs e) => Close();

    private static void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (ViewModel is null)
            return;

        foreach (var item in e.DataTransfer.TryGetFiles() ?? [])
        {
            if (item is IStorageFile && item.TryGetLocalPath() is { } path)
                await ViewModel.OpenPathAsync(path);
        }
    }
}
