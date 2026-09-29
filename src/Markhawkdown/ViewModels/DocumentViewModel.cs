using System.Text.RegularExpressions;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using Markhawkdown.Services;

namespace Markhawkdown.ViewModels;

public sealed partial class DocumentViewModel : ObservableObject
{
    private static int _untitledCounter;

    private readonly DispatcherTimer _previewTimer;
    private readonly string _untitledName;

    public DocumentViewModel(string? filePath = null, string text = "")
    {
        _untitledName = $"Névtelen {++_untitledCounter}";
        _filePath = filePath;

        Document = new TextDocument(text);
        Document.UndoStack.ClearAll();
        Document.UndoStack.MarkAsOriginalFile();
        Document.TextChanged += OnTextChanged;
        Document.UndoStack.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(UndoStack.IsOriginalFile))
                OnDirtyChanged();
        };

        _previewTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _previewTimer.Tick += (_, _) =>
        {
            _previewTimer.Stop();
            UpdatePreview();
        };

        UpdatePreview();
    }

    /// <summary>Raised when the view should apply a formatting action to its editor.</summary>
    public event Action<MarkdownFormat>? FormatRequested;

    public TextDocument Document { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title), nameof(DisplayTitle), nameof(AssetPathRoot), nameof(ToolTip))]
    private string? _filePath;

    [ObservableProperty]
    private ViewMode _viewMode = ViewMode.Split;

    [ObservableProperty]
    private string _previewMarkdown = "";

    [ObservableProperty]
    private int _caretLine = 1;

    [ObservableProperty]
    private int _caretColumn = 1;

    [ObservableProperty]
    private int _wordCount;

    [ObservableProperty]
    private int _characterCount;

    public string Title => FilePath is null ? _untitledName : Path.GetFileName(FilePath);

    public string DisplayTitle => IsDirty ? Title + " •" : Title;

    public string ToolTip => FilePath ?? _untitledName;

    public bool IsDirty => !Document.UndoStack.IsOriginalFile;

    public bool IsUntouchedUntitled => FilePath is null && Document.TextLength == 0 && !IsDirty;

    public string AssetPathRoot => FilePath is null
        ? Environment.CurrentDirectory
        : Path.GetDirectoryName(Path.GetFullPath(FilePath)) ?? Environment.CurrentDirectory;

    public void RequestFormat(MarkdownFormat format) => FormatRequested?.Invoke(format);

    public void MarkSaved(string path)
    {
        FilePath = path;
        Document.UndoStack.MarkAsOriginalFile();
        OnDirtyChanged();
    }

    private void OnTextChanged(object? sender, EventArgs e)
    {
        _previewTimer.Stop();
        _previewTimer.Start();
    }

    private void OnDirtyChanged()
    {
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(DisplayTitle));
    }

    /// <summary>Renders GitHub-style task list items as check boxes, which the preview engine does not support.</summary>
    private static string PrepareForPreview(string text)
    {
        if (!text.Contains("] ", StringComparison.Ordinal))
            return text;

        var lines = text.Split('\n');
        var inFence = false;
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].TrimStart().StartsWith("```", StringComparison.Ordinal))
                inFence = !inFence;
            else if (!inFence)
                lines[i] = TaskItem().Replace(lines[i], m => $"{m.Groups[1].Value}- {(m.Groups[2].Value == " " ? '☐' : '☑')} ");
        }

        return string.Join('\n', lines);
    }

    [GeneratedRegex(@"^(\s*)[-*+] \[([ xX])\] ")]
    private static partial Regex TaskItem();

    private void UpdatePreview()
    {
        var text = Document.Text;
        PreviewMarkdown = PrepareForPreview(text);
        CharacterCount = text.Length;
        WordCount = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
