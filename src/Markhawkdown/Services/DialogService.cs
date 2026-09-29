using Avalonia.Controls;
using SukiUI.MessageBox;

namespace Markhawkdown.Services;

public enum UnsavedChangesChoice
{
    Save,
    Discard,
    Cancel,
}

public sealed class DialogService(Window owner)
{
    public async Task<UnsavedChangesChoice> AskUnsavedChangesAsync(string documentTitle)
    {
        var result = await SukiMessageBox.ShowDialogResult(
            owner,
            $"A(z) „{documentTitle}” dokumentum nem mentett módosításokat tartalmaz. Menti a változásokat?",
            SukiMessageBoxButtons.YesNoCancel,
            "Markhawkdown",
            "Nem mentett módosítások",
            SukiMessageBoxIcons.Warning);

        return result switch
        {
            SukiMessageBoxResult.Yes => UnsavedChangesChoice.Save,
            SukiMessageBoxResult.No => UnsavedChangesChoice.Discard,
            _ => UnsavedChangesChoice.Cancel,
        };
    }

    public Task ShowErrorAsync(string message) =>
        SukiMessageBox.ShowDialogResult(
            owner,
            message,
            SukiMessageBoxButtons.OK,
            "Markhawkdown",
            "Hiba",
            SukiMessageBoxIcons.Error);
}
