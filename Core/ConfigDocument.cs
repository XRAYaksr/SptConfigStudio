using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace SptConfigStudio.Core;

/// <summary>A config file plus the editable JSON DOM and the exact text it was loaded from.</summary>
public sealed class ConfigDocument
{
    public string FileName { get; }
    public string FilePath { get; }
    public JsonNode Root { get; private set; }

    private string _baseline;
    private string _baselineLayoutFree;
    private string _eol = "\n";
    private int _indent = 2;
    private bool _trailingEol;

    public event Action? Changed;

    public ConfigDocument(string path)
    {
        FilePath = path;
        FileName = Path.GetFileName(path);
        var text = File.ReadAllText(path);
        Root = Parse(text) ?? throw new InvalidDataException($"{FileName}: 解析失败，文件可能不是合法 JSON。");
        _baseline = Normalize(text);
        _baselineLayoutFree = StripLayout(_baseline);
        DetectLayout(text);
    }

    /// <summary>
    /// Records how the file on disk is laid out so a save reproduces it. Re-run after every read:
    /// a restore can hand back a file with a different EOL or indent than the one we first saw.
    /// </summary>
    private void DetectLayout(string text)
    {
        _eol = text.Contains("\r\n") ? "\r\n" : "\n";
        _indent = DetectIndent(text);
        _trailingEol = text.EndsWith('\n') || text.EndsWith('\r');
        PreservesOriginalFormatting = Normalize(Serialize()) == _baseline;
    }

    /// <summary>False when merely re-writing the untouched DOM changes formatting (eol, escaping, indent, key order).</summary>
    public bool PreservesOriginalFormatting { get; private set; }

    public bool IsDirty => StripLayout(Serialize()) != _baselineLayoutFree;

    /// <summary>The exact text the file had when loaded (normalised line endings), used as the diff baseline.</summary>
    public string BaselineText => _baseline;

    public static JsonNode? Parse(string text) =>
        JsonNode.Parse(text,
            new JsonNodeOptions { PropertyNameCaseInsensitive = false },
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

    /// <summary>Renders the DOM the way SPT's own writer does: indented, raw UTF-8, unescaped CJK.</summary>
    public string Serialize()
    {
        using var ms = new MemoryStream();
        var writerOptions = new JsonWriterOptions
        {
            Indented = true,
            IndentCharacter = ' ',
            IndentSize = Math.Clamp(_indent, 1, 8),
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        using (var writer = new Utf8JsonWriter(ms, writerOptions))
        {
            Root.WriteTo(writer);
        }
        // Utf8JsonWriter always emits CRLF; keep the DOM text LF so BuildFileContent owns the file's EOL.
        return Encoding.UTF8.GetString(ms.ToArray()).Replace("\r\n", "\n");
    }

    public string BuildFileContent()
    {
        var body = Serialize();
        var text = _eol == "\r\n" ? body.Replace("\n", "\r\n") : body;
        return _trailingEol ? text + _eol : text;
    }

    public void NotifyChanged() => Changed?.Invoke();

    /// <summary>Replaces the whole DOM from hand-edited text. Returns false and a message when it will not parse.</summary>
    public bool TryApplyRawText(string text, out string? error)
    {
        try
        {
            var node = Parse(text);
            if (node is null)
            {
                error = "内容为空，未应用。";
                return false;
            }
            Root = node;
            error = null;
            Changed?.Invoke();
            return true;
        }
        catch (JsonException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>Writes through a temp file and replaces atomically. The caller owns the backup gate.</summary>
    public void WriteToDisk()
    {
        var tmp = FilePath + ".tmp-write";
        File.WriteAllText(tmp, BuildFileContent(), new UTF8Encoding(false));
        File.Replace(tmp, FilePath, null, ignoreMetadataErrors: true);
        _baseline = Normalize(Serialize());
        _baselineLayoutFree = StripLayout(_baseline);
        Changed?.Invoke();
    }

    /// <summary>Reloads from disk, discarding unsaved edits.</summary>
    public void Reload()
    {
        var text = File.ReadAllText(FilePath);
        Root = Parse(text) ?? throw new InvalidDataException($"{FileName}: 重新解析失败。");
        _baseline = Normalize(text);
        _baselineLayoutFree = StripLayout(_baseline);
        DetectLayout(text);
        Changed?.Invoke();
    }

    public static string Normalize(string s) => s.Replace("\r\n", "\n").TrimEnd('\n', ' ', '\t');

    /// <summary>
    /// Drops line breaks and the indentation that follows them. Raw newlines never appear inside JSON string
    /// values, so this only removes layout and lets a file with hand-placed blank lines compare equal to itself.
    /// </summary>
    public static string StripLayout(string json)
    {
        var sb = new StringBuilder(json.Length);
        for (var i = 0; i < json.Length; i++)
        {
            var c = json[i];
            if (c is '\r' or '\n')
            {
                while (i + 1 < json.Length && json[i + 1] is ' ' or '\t' or '\r' or '\n') i++;
                continue;
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    private static int DetectIndent(string text)
    {
        foreach (var line in text.Split('\n'))
        {
            var spaces = line.Length - line.TrimStart(' ').Length;
            if (spaces > 0) return spaces;
        }
        return 2;
    }
}
