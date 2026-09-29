using System.Collections.ObjectModel;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Markhawkdown.Services;
using SukiUI;
using SukiUI.Enums;

namespace Markhawkdown.ViewModels;

public sealed partial class MainWindowViewModel(FileService fileService, DialogService dialogService) : ObservableObject
{
    public ObservableCollection<DocumentViewModel> Documents { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDocument))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand), nameof(SaveAsCommand), nameof(CloseCommand), nameof(FormatCommand), nameof(SetViewModeCommand))]
    private DocumentViewModel? _selectedDocument;

    [ObservableProperty]
    private bool _wordWrap = true;

    public bool HasDocument => SelectedDocument is not null;

    public bool IsDarkTheme => SukiTheme.GetInstance().ActiveBaseTheme == ThemeVariant.Dark;

    [RelayCommand]
    public void NewDocument()
    {
        var document = new DocumentViewModel();
        Documents.Add(document);
        SelectedDocument = document;
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        foreach (var path in await fileService.PickFilesToOpenAsync())
            await OpenPathAsync(path);
    }

    public async Task OpenPathAsync(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var existing = Documents.FirstOrDefault(d =>
            d.FilePath is not null && string.Equals(Path.GetFullPath(d.FilePath), fullPath, StringComparison.Ordinal));
        if (existing is not null)
        {
            SelectedDocument = existing;
            return;
        }

        string text;
        try
        {
            text = await FileService.ReadAsync(fullPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await dialogService.ShowErrorAsync($"Nem sikerült megnyitni a fájlt:\n{fullPath}\n\n{ex.Message}");
            return;
        }

        var document = new DocumentViewModel(fullPath, text);

        // Replace the empty placeholder document instead of stacking tabs next to it.
        if (SelectedDocument is { IsUntouchedUntitled: true } placeholder)
            Documents[Documents.IndexOf(placeholder)] = document;
        else
            Documents.Add(document);

        SelectedDocument = document;
    }

    [RelayCommand(CanExecute = nameof(HasDocument))]
    private Task SaveAsync() => SaveDocumentAsync(SelectedDocument!, saveAs: false);

    [RelayCommand(CanExecute = nameof(HasDocument))]
    private Task SaveAsAsync() => SaveDocumentAsync(SelectedDocument!, saveAs: true);

    [RelayCommand]
    private async Task SaveAllAsync()
    {
        foreach (var document in Documents.Where(d => d.IsDirty).ToList())
        {
            if (!await SaveDocumentAsync(document, saveAs: false))
                return;
        }
    }

    [RelayCommand(CanExecute = nameof(CanClose))]
    private async Task CloseAsync(DocumentViewModel? document)
    {
        document ??= SelectedDocument;
        if (document is null || !await ConfirmCloseAsync(document))
            return;

        var index = Documents.IndexOf(document);
        Documents.Remove(document);
        if (Documents.Count == 0)
            NewDocument();
        else if (SelectedDocument is null || SelectedDocument == document)
            SelectedDocument = Documents[Math.Min(index, Documents.Count - 1)];
    }

    private bool CanClose(DocumentViewModel? document) => document is not null || HasDocument;

    [RelayCommand]
    private void NextDocument() => CycleDocument(+1);

    [RelayCommand]
    private void PreviousDocument() => CycleDocument(-1);

    [RelayCommand(CanExecute = nameof(HasDocument))]
    private void Format(MarkdownFormat format) => SelectedDocument?.RequestFormat(format);

    [RelayCommand(CanExecute = nameof(HasDocument))]
    private void SetViewMode(ViewMode mode)
    {
        if (SelectedDocument is not null)
            SelectedDocument.ViewMode = mode;
    }

    [RelayCommand]
    private void ToggleWordWrap() => WordWrap = !WordWrap;

    [RelayCommand]
    private void ToggleBaseTheme()
    {
        SukiTheme.GetInstance().SwitchBaseTheme();
        OnPropertyChanged(nameof(IsDarkTheme));
    }

    [RelayCommand]
    private void SetColorTheme(SukiColor color) => SukiTheme.GetInstance().ChangeColorTheme(color);

    /// <summary>Asks about every unsaved document. Returns false when the user cancels.</summary>
    public async Task<bool> ConfirmCloseAllAsync()
    {
        foreach (var document in Documents.Where(d => d.IsDirty).ToList())
        {
            SelectedDocument = document;
            if (!await ConfirmCloseAsync(document))
                return false;
        }

        return true;
    }

    private async Task<bool> ConfirmCloseAsync(DocumentViewModel document)
    {
        if (!document.IsDirty)
            return true;

        return await dialogService.AskUnsavedChangesAsync(document.Title) switch
        {
            UnsavedChangesChoice.Save => await SaveDocumentAsync(document, saveAs: false),
            UnsavedChangesChoice.Discard => true,
            _ => false,
        };
    }

    private async Task<bool> SaveDocumentAsync(DocumentViewModel document, bool saveAs)
    {
        var path = document.FilePath;
        if (saveAs || path is null)
        {
            path = await fileService.PickFileToSaveAsync(document.FilePath is null ? document.Title + ".md" : document.Title);
            if (path is null)
                return false;
        }

        try
        {
            await FileService.WriteAsync(path, document.Document.Text);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await dialogService.ShowErrorAsync($"Nem sikerült menteni a fájlt:\n{path}\n\n{ex.Message}");
            return false;
        }

        document.MarkSaved(path);
        return true;
    }

    private void CycleDocument(int step)
    {
        if (Documents.Count == 0)
            return;

        var index = SelectedDocument is null ? 0 : Documents.IndexOf(SelectedDocument);
        SelectedDocument = Documents[(index + step + Documents.Count) % Documents.Count];
    }
}
