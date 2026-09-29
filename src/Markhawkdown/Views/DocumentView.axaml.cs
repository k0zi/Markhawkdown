using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using AvaloniaEdit.TextMate;
using Markhawkdown.Services;
using Markhawkdown.ViewModels;
using TextMateSharp.Grammars;

namespace Markhawkdown.Views;

public partial class DocumentView : UserControl
{
    private readonly RegistryOptions _registryOptions;
    private readonly TextMate.Installation _textMate;
    private DocumentViewModel? _viewModel;

    public DocumentView()
    {
        InitializeComponent();

        _registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        _textMate = Editor.InstallTextMate(_registryOptions);
        _textMate.SetGrammar(_registryOptions.GetScopeByLanguageId(_registryOptions.GetLanguageByExtension(".md").Id));

        Editor.Options.EnableHyperlinks = false;
        Editor.Options.EnableEmailHyperlinks = false;
        Editor.Options.HighlightCurrentLine = true;
        Editor.Options.ConvertTabsToSpaces = true;
        Editor.Options.IndentationSize = 2;
        Editor.TextArea.Caret.PositionChanged += (_, _) => UpdateCaretPosition();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Application.Current is { } app)
        {
            app.ActualThemeVariantChanged += OnThemeVariantChanged;
            ApplyEditorTheme();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current is { } app)
            app.ActualThemeVariantChanged -= OnThemeVariantChanged;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_viewModel is not null)
        {
            _viewModel.FormatRequested -= OnFormatRequested;
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = DataContext as DocumentViewModel;
        if (_viewModel is null)
            return;

        _viewModel.FormatRequested += OnFormatRequested;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Each document keeps its own TextDocument, so undo history survives tab switches.
        Editor.Document = _viewModel.Document;
        ApplyViewMode();
        UpdateCaretPosition();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DocumentViewModel.ViewMode))
            ApplyViewMode();
    }

    private void OnFormatRequested(MarkdownFormat format)
    {
        if (_viewModel?.ViewMode == ViewMode.Preview)
            _viewModel.ViewMode = ViewMode.Split;

        MarkdownFormatter.Apply(Editor, format);
    }

    private void ApplyViewMode()
    {
        var mode = _viewModel?.ViewMode ?? ViewMode.Split;
        var columns = Root.ColumnDefinitions;

        EditorHost.IsVisible = mode != ViewMode.Preview;
        PreviewHost.IsVisible = mode != ViewMode.Edit;
        Splitter.IsVisible = mode == ViewMode.Split;

        columns[0].Width = mode == ViewMode.Preview ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        columns[2].Width = mode == ViewMode.Edit ? new GridLength(0) : new GridLength(1, GridUnitType.Star);

        if (mode != ViewMode.Preview)
            Editor.Focus();
    }

    private void UpdateCaretPosition()
    {
        if (_viewModel is null)
            return;

        var caret = Editor.TextArea.Caret;
        _viewModel.CaretLine = caret.Line;
        _viewModel.CaretColumn = caret.Column;
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e) => ApplyEditorTheme();

    private void ApplyEditorTheme()
    {
        var isDark = Application.Current?.ActualThemeVariant != ThemeVariant.Light;
        _textMate.SetTheme(_registryOptions.LoadTheme(isDark ? ThemeName.DarkPlus : ThemeName.LightPlus));
        // Keep the SukiUI surface visible behind the editor instead of the TextMate theme background.
        Editor.Background = Avalonia.Media.Brushes.Transparent;
    }
}
