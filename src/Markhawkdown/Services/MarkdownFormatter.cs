using System.Text.RegularExpressions;
using AvaloniaEdit;
using AvaloniaEdit.Document;

namespace Markhawkdown.Services;

public enum MarkdownFormat
{
    Bold,
    Italic,
    Strikethrough,
    InlineCode,
    CodeBlock,
    Heading1,
    Heading2,
    Heading3,
    Quote,
    BulletList,
    NumberedList,
    TaskList,
    Link,
    Image,
    HorizontalRule,
    Table,
}

/// <summary>
/// Applies Markdown formatting to the selection (or caret position) of a <see cref="TextEditor"/>.
/// Every operation is a single undo step.
/// </summary>
public static partial class MarkdownFormatter
{
    [GeneratedRegex(@"^#{1,6} ")]
    private static partial Regex HeadingPrefix();

    [GeneratedRegex(@"^\d+\. ")]
    private static partial Regex NumberedPrefix();

    [GeneratedRegex(@"^[-*+] \[[ xX]\] ")]
    private static partial Regex TaskPrefix();

    [GeneratedRegex(@"^[-*+] (?!\[[ xX]\] )")]
    private static partial Regex BulletPrefix();

    [GeneratedRegex(@"^> ?")]
    private static partial Regex QuotePrefix();

    public static void Apply(TextEditor editor, MarkdownFormat format)
    {
        var document = editor.Document;
        document.BeginUpdate();
        try
        {
            switch (format)
            {
                case MarkdownFormat.Bold: ToggleWrap(editor, "**"); break;
                case MarkdownFormat.Italic: ToggleWrap(editor, "*"); break;
                case MarkdownFormat.Strikethrough: ToggleWrap(editor, "~~"); break;
                case MarkdownFormat.InlineCode: ToggleWrap(editor, "`"); break;
                case MarkdownFormat.CodeBlock: InsertCodeBlock(editor); break;
                case MarkdownFormat.Heading1: ToggleHeading(editor, 1); break;
                case MarkdownFormat.Heading2: ToggleHeading(editor, 2); break;
                case MarkdownFormat.Heading3: ToggleHeading(editor, 3); break;
                case MarkdownFormat.Quote: ToggleLinePrefix(editor, QuotePrefix(), _ => "> "); break;
                case MarkdownFormat.BulletList: ToggleLinePrefix(editor, BulletPrefix(), _ => "- ", TaskPrefix(), NumberedPrefix()); break;
                case MarkdownFormat.NumberedList: ToggleLinePrefix(editor, NumberedPrefix(), i => $"{i + 1}. ", BulletPrefix(), TaskPrefix()); break;
                case MarkdownFormat.TaskList: ToggleLinePrefix(editor, TaskPrefix(), _ => "- [ ] ", BulletPrefix(), NumberedPrefix()); break;
                case MarkdownFormat.Link: InsertLink(editor, "[", "szöveg"); break;
                case MarkdownFormat.Image: InsertLink(editor, "![", "leírás"); break;
                case MarkdownFormat.HorizontalRule: InsertBlock(editor, "---"); break;
                case MarkdownFormat.Table:
                    InsertBlock(editor, "| Oszlop 1 | Oszlop 2 | Oszlop 3 |\n| --- | --- | --- |\n| | | |");
                    break;
            }
        }
        finally
        {
            document.EndUpdate();
        }

        editor.Focus();
    }

    /// <param name="marker">A marker made of one repeated character, e.g. <c>**</c>, <c>*</c>, <c>~~</c>, <c>`</c>.</param>
    private static void ToggleWrap(TextEditor editor, string marker)
    {
        var document = editor.Document;
        var start = editor.SelectionStart;
        var length = editor.SelectionLength;
        var end = start + length;
        var m = marker.Length;
        var c = marker[0];

        // "*" and "**" share a character, so decide by the length of the run of marker characters:
        // "*x*" and "***x***" are italic, "**x**" and "***x***" are bold.
        bool IsWrapped(int run) => marker == "*" ? run % 2 == 1 : run >= m;

        // Markers right outside the selection (or around the caret): **[text]** -> text, **|** -> |.
        var outsideLeft = CountRun(document, start - 1, -1, c, 0);
        var outsideRight = CountRun(document, end, +1, c, document.TextLength);
        if (IsWrapped(outsideLeft) && IsWrapped(outsideRight))
        {
            document.Remove(end, m);
            document.Remove(start - m, m);
            if (length == 0)
                editor.CaretOffset = start - m;
            else
                editor.Select(start - m, length);
            return;
        }

        if (length == 0)
        {
            document.Insert(start, marker + marker);
            editor.CaretOffset = start + m;
            return;
        }

        // Markers are part of the selection: [**text**] -> text.
        var insideLeft = CountRun(document, start, +1, c, end);
        var insideRight = CountRun(document, end - 1, -1, c, start);
        if (length > 2 * m && insideLeft < length && IsWrapped(insideLeft) && IsWrapped(insideRight))
        {
            var inner = document.GetText(start + m, length - 2 * m);
            document.Replace(start, length, inner);
            editor.Select(start, inner.Length);
            return;
        }

        // Keep leading/trailing whitespace outside of the markers, otherwise Markdown ignores them.
        var selected = document.GetText(start, length);
        var trimmedStart = selected.Length - selected.TrimStart().Length;
        var trimmedEnd = selected.Length - selected.TrimEnd().Length;
        var core = selected.Trim();
        if (core.Length == 0)
            return;

        document.Replace(start + trimmedStart, length - trimmedStart - trimmedEnd, marker + core + marker);
        editor.Select(start + trimmedStart + m, core.Length);
    }

    /// <summary>
    /// Counts the consecutive <paramref name="c"/> characters starting at <paramref name="offset"/> and moving by
    /// <paramref name="step"/>, without crossing <paramref name="limit"/> (exclusive going forward, inclusive going back).
    /// </summary>
    private static int CountRun(TextDocument document, int offset, int step, char c, int limit)
    {
        var count = 0;
        while ((step > 0 ? offset < limit : offset >= limit) && document.GetCharAt(offset) == c)
        {
            count++;
            offset += step;
        }

        return count;
    }

    private static void ToggleHeading(TextEditor editor, int level)
    {
        var prefix = new string('#', level) + " ";
        var lines = GetSelectedLines(editor);
        var document = editor.Document;

        var allHaveLevel = lines.All(l => document.GetText(l).StartsWith(prefix, StringComparison.Ordinal));

        foreach (var line in lines.AsEnumerable().Reverse())
        {
            var text = document.GetText(line);
            var existing = HeadingPrefix().Match(text);
            if (existing.Success)
                document.Remove(line.Offset, existing.Length);
            if (!allHaveLevel)
                document.Insert(line.Offset, prefix);
        }

        SelectLines(editor, lines);
    }

    /// <param name="prefix">Pattern that detects the prefix being toggled.</param>
    /// <param name="createPrefix">Creates the prefix for the n-th line (0-based).</param>
    /// <param name="replaced">Other list prefixes that get replaced when this one is applied.</param>
    private static void ToggleLinePrefix(TextEditor editor, Regex prefix, Func<int, string> createPrefix, params Regex[] replaced)
    {
        var lines = GetSelectedLines(editor);
        var document = editor.Document;

        var nonEmpty = lines.Where(l => l.Length > 0).ToList();
        var targets = nonEmpty.Count > 0 ? nonEmpty : lines;
        var allHavePrefix = targets.All(l => prefix.IsMatch(document.GetText(l)));

        for (var i = targets.Count - 1; i >= 0; i--)
        {
            var line = targets[i];
            var text = document.GetText(line);

            var current = prefix.Match(text);
            if (allHavePrefix)
            {
                document.Remove(line.Offset, current.Length);
                continue;
            }

            if (current.Success)
                document.Remove(line.Offset, current.Length);
            else
            {
                foreach (var other in replaced)
                {
                    var match = other.Match(text);
                    if (!match.Success)
                        continue;
                    document.Remove(line.Offset, match.Length);
                    break;
                }
            }

            document.Insert(line.Offset, createPrefix(i));
        }

        SelectLines(editor, lines);
    }

    private static void InsertCodeBlock(TextEditor editor)
    {
        var document = editor.Document;
        var start = editor.SelectionStart;
        var length = editor.SelectionLength;
        var selected = document.GetText(start, length);

        var before = NeedsLeadingNewLine(document, start) ? "\n" : "";
        var after = NeedsTrailingNewLine(document, start + length) ? "\n" : "";

        if (length == 0)
        {
            document.Insert(start, before + "```\n\n```" + after);
            editor.CaretOffset = start + before.Length + 4;
            return;
        }

        var body = selected.TrimEnd('\n', '\r');
        document.Replace(start, length, before + "```\n" + body + "\n```" + after);
        editor.Select(start + before.Length + 4, body.Length);
    }

    private static void InsertLink(TextEditor editor, string opening, string placeholder)
    {
        var document = editor.Document;
        var start = editor.SelectionStart;
        var length = editor.SelectionLength;
        var selected = document.GetText(start, length);

        if (length > 0 && Uri.TryCreate(selected.Trim(), UriKind.Absolute, out _))
        {
            // Selected text is an URL: use it as the target and select the label.
            document.Replace(start, length, $"{opening}{placeholder}]({selected.Trim()})");
            editor.Select(start + opening.Length, placeholder.Length);
            return;
        }

        var label = length > 0 ? selected : placeholder;
        const string url = "https://";
        document.Replace(start, length, $"{opening}{label}]({url})");

        if (length > 0)
            editor.Select(start + opening.Length + label.Length + 2, url.Length);
        else
            editor.Select(start + opening.Length, label.Length);
    }

    private static void InsertBlock(TextEditor editor, string block)
    {
        var document = editor.Document;
        var offset = editor.SelectionStart + editor.SelectionLength;
        var line = document.GetLineByOffset(offset);
        var insertAt = line.EndOffset;

        var before = line.Length > 0 ? "\n\n" : "\n";
        if (line.Length == 0)
        {
            insertAt = line.Offset;
            before = line.LineNumber > 1 && document.GetLineByNumber(line.LineNumber - 1).Length > 0 ? "\n" : "";
        }

        var text = before + block + "\n";
        document.Insert(insertAt, text);
        editor.CaretOffset = insertAt + text.Length;
    }

    private static List<DocumentLine> GetSelectedLines(TextEditor editor)
    {
        var document = editor.Document;
        var start = editor.SelectionStart;
        var end = start + editor.SelectionLength;

        var first = document.GetLineByOffset(start);
        var last = document.GetLineByOffset(end);

        // A selection ending at the very start of a line does not include that line.
        if (editor.SelectionLength > 0 && last.Offset == end && last.LineNumber > first.LineNumber)
            last = last.PreviousLine;

        var lines = new List<DocumentLine>();
        for (var line = first; line is not null; line = line.NextLine)
        {
            lines.Add(line);
            if (line == last)
                break;
        }

        return lines;
    }

    private static void SelectLines(TextEditor editor, List<DocumentLine> lines)
    {
        var start = lines[0].Offset;
        var end = lines[^1].EndOffset;
        if (lines.Count == 1 && editor.SelectionLength == 0)
            editor.CaretOffset = end;
        else
            editor.Select(start, end - start);
    }

    private static bool NeedsLeadingNewLine(TextDocument document, int offset) =>
        offset > 0 && document.GetCharAt(offset - 1) != '\n';

    private static bool NeedsTrailingNewLine(TextDocument document, int offset) =>
        offset < document.TextLength && document.GetCharAt(offset) != '\n' && document.GetCharAt(offset) != '\r';
}
