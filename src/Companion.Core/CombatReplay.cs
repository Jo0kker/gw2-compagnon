namespace Companion.Core;

public sealed record DamageEvent(long Sequence, int TimeMs, string Player, int? Subgroup, long Damage);
public sealed record DamageRow(string Player, int? Subgroup, long Damage, double? Dps);
public sealed record CombatSnapshot(int DurationMs, bool Partial, IReadOnlyList<DamageRow> Players)
{
    public IReadOnlyList<DamageRow> Select(WidgetSettings settings, bool groups = false)
    {
        IEnumerable<DamageRow> rows = Players.Where(p => settings.Subgroup is null || p.Subgroup == settings.Subgroup);
        if (groups) rows = rows.GroupBy(p => p.Subgroup).Select(g => new DamageRow(
            g.Key is int n ? $"Sous-groupe {n}" : "Sous-groupe inconnu", g.Key, g.Sum(p => p.Damage),
            DurationMs > 0 ? g.Sum(p => p.Damage) / (DurationMs / 1000d) : null));
        return rows.OrderByDescending(p => p.Damage).ThenBy(p => p.Player, StringComparer.Ordinal).Take(settings.Rows).ToArray();
    }
}

/// <summary>Normalized synthetic replay only. This does not interpret the ArcDPS ABI or EVTC.</summary>
public static class CombatReplay
{
    public static CombatSnapshot Read(IEnumerable<DamageEvent> events, int durationMs, bool interrupted = false)
    {
        if (durationMs < 0) throw new ArgumentOutOfRangeException(nameof(durationMs));
        long previous = 0;
        var partial = interrupted;
        var accepted = new List<DamageEvent>();
        foreach (var e in events)
        {
            if (e.Sequence <= 0 || e.TimeMs < 0 || e.TimeMs > durationMs || e.Damage < 0 || string.IsNullOrWhiteSpace(e.Player))
                throw new InvalidDataException("Événement normalisé invalide.");
            if (e.Sequence <= previous) { if (e.Sequence < previous) partial = true; continue; }
            if (e.Sequence != previous + 1) partial = true;
            previous = e.Sequence;
            accepted.Add(e);
        }
        var players = accepted.GroupBy(e => (e.Player, e.Subgroup)).Select(g => new DamageRow(g.Key.Player, g.Key.Subgroup,
            g.Sum(e => e.Damage), durationMs > 0 ? g.Sum(e => e.Damage) / (durationMs / 1000d) : null)).ToArray();
        return new(durationMs, partial, players);
    }

    public static CombatSnapshot Demo() => Read(
    [new(1, 1000, "Kael — fictif", 1, 3_498_210), new(2, 2000, "Luné — fictif", 1, 2_984_441),
     new(3, 3000, "Morrigan — fictif", 2, 2_551_792), new(4, 4000, "Thalrik — fictif", 2, 2_337_118),
     new(5, 5000, "Elyndra — fictif", 3, 1_975_552), new(6, 6000, "Zarek — fictif", 3, 1_742_665),
     new(7, 7000, "Nyssia — fictif", 4, 1_560_284), new(8, 8000, "Vorhun — fictif", 4, 1_312_994)], 154_000);
}
