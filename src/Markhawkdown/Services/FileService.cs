using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Markhawkdown.Services;

public sealed class FileService(TopLevel owner)
{
    private static readonly FilePickerFileType MarkdownType = new("Markdown")
    {
        Patterns = ["*.md", "*.markdown", "*.mdown", "*.mkd"],
        MimeTypes = ["text/markdown"],
    };

    private static readonly FilePickerFileType TextType = new("Szövegfájl")
    {
        Patterns = ["*.txt"],
        MimeTypes = ["text/plain"],
    };

    public async Task<IReadOnlyList<string>> PickFilesToOpenAsync()
    {
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Markdown fájl megnyitása",
            AllowMultiple = true,
            FileTypeFilter = [MarkdownType, TextType, FilePickerFileTypes.All],
        });

        return files.Select(f => f.TryGetLocalPath()).OfType<string>().ToList();
    }

    public async Task<string?> PickFileToSaveAsync(string suggestedName)
    {
        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Mentés másként",
            SuggestedFileName = suggestedName,
            DefaultExtension = "md",
            ShowOverwritePrompt = true,
            FileTypeChoices = [MarkdownType, TextType],
        });

        return file?.TryGetLocalPath();
    }

    public static Task<string> ReadAsync(string path) => File.ReadAllTextAsync(path);

    public static Task WriteAsync(string path, string text) =>
        File.WriteAllTextAsync(path, text, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
}
