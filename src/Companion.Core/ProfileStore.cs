using System.Text.Json;

namespace Companion.Core;

/// <summary>First local slice: one writer, bounded JSON and atomic replacement, no credentials.</summary>
public sealed class ProfileStore(string path)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public const int MaxBytes = 2_000_000;

    public Workspace Load()
    {
        if (!File.Exists(path)) return Workspace.CreateDefault();
        using var file = File.OpenRead(path);
        if (file.Length > MaxBytes) throw new InvalidDataException("Le fichier de profils est trop volumineux.");
        var state = JsonSerializer.Deserialize<Workspace>(file, JsonOptions)
            ?? throw new InvalidDataException("Le fichier de profils est vide.");
        state.Validate();
        return state;
    }

    public void Save(Workspace state)
    {
        state.Validate();
        var bytes = JsonSerializer.SerializeToUtf8Bytes(state, JsonOptions);
        if (bytes.Length > MaxBytes) throw new InvalidDataException("Les profils dépassent la limite de stockage.");
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temporary = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                file.Write(bytes);
                file.Flush(flushToDisk: true);
            }
            if (File.Exists(fullPath)) File.Replace(temporary, fullPath, fullPath + ".bak");
            else File.Move(temporary, fullPath);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
