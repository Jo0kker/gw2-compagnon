using static Companion.Core.Localization.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Companion.Core;

public sealed record ProfileImport(Profile Profile, IReadOnlyList<string> MissingWidgetKinds);

/// <summary>Portable allow-list, deliberately separate from local Workspace and future credentials.</summary>
public static class ProfileTransfer
{
    public const int MaxBytes = 1_000_000;
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true, MaxDepth = 16,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true
    };
    private sealed record Document(int Version, string Name, List<PageDocument> Pages);
    private sealed record PageDocument(string Name, List<WidgetDocument> Widgets);
    private sealed record WidgetDocument(string Kind, WidgetLayout Layout, bool Hidden, WidgetSettings? Settings = null);
    private static bool Known(string kind) => kind is "damage-players" or "damage-groups";

    public static byte[] Export(Profile profile)
    {
        new Workspace(1, profile.Id, [profile]).Validate();
        var document = new Document(1, profile.Name, profile.Pages.Select(p => new PageDocument(p.Name,
            p.Widgets.Select(w => new WidgetDocument(w.Kind, w.Layout with { }, w.Hidden,
                Known(w.Kind) ? new WidgetSettings(w.Settings.Subgroup, w.Settings.Rows) : null)).ToList())).ToList());
        var bytes = JsonSerializer.SerializeToUtf8Bytes(document, Options);
        if (bytes.Length > MaxBytes) throw new InvalidDataException(T("ExportTooLarge"));
        return bytes;
    }

    public static ProfileImport Import(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length > MaxBytes) throw new InvalidDataException(T("ImportTooLarge"));
        Document document;
        try { document = JsonSerializer.Deserialize<Document>(bytes, Options) ?? throw new InvalidDataException(T("EmptyDocument")); }
        catch (JsonException e) { throw new InvalidDataException(T("InvalidPortable"), e); }
        if (document.Version != 1) throw new InvalidDataException(T("PortableVersion"));
        if (document.Pages is null || document.Pages.Count is < 1 or > 50)
            throw new InvalidDataException(T("PageCount"));
        var pages = new List<ProfilePage>();
        var missing = new HashSet<string>(StringComparer.Ordinal);
        foreach (var p in document.Pages)
        {
            if (p is null || p.Widgets is null || p.Widgets.Count > 200)
                throw new InvalidDataException(T("InvalidPortablePage"));
            var widgets = new List<Widget>();
            foreach (var w in p.Widgets)
            {
                if (w is null || string.IsNullOrWhiteSpace(w.Kind) || w.Layout is null)
                    throw new InvalidDataException(T("InvalidPortableWidget"));
                if (!Known(w.Kind)) missing.Add(w.Kind);
                widgets.Add(new(Guid.NewGuid(), w.Kind, Known(w.Kind) ? w.Settings ?? new() : new(), w.Layout with { }, w.Hidden));
            }
            pages.Add(new(Guid.NewGuid(), p.Name, widgets));
        }
        var profile = new Profile(Guid.NewGuid(), document.Name, pages);
        new Workspace(1, profile.Id, [profile]).Validate();
        return new(profile, missing.Order(StringComparer.Ordinal).ToArray());
    }

    public static ProfileImport ReadFile(string path)
    {
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (file.Length > MaxBytes) throw new InvalidDataException(T("ImportTooLarge"));
        // Bound the actual read as well as the initial length check.
        var buffer = new byte[MaxBytes + 1];
        var count = file.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
        return Import(buffer.AsSpan(0, count));
    }
}
