using static Companion.Core.Localization.Text;
using Companion.Core;

namespace Companion.Transport;

/// <summary>Bounded incremental aggregation. Serialized access is owned by the receiver.</summary>
public sealed class BridgeSession
{
    public const int MaxPlayers = 512;
    private readonly Dictionary<(string Player, int? Subgroup), long> damage = [];
    private long lastSequence;
    private long totalDamage;
    private int timeMs;
    private bool partial;
    public Guid? Id { get; private set; }
    public long DroppedEvents { get; private set; }
    public bool Finished { get; private set; }

    public void Begin(BridgeFrame hello)
    {
        BridgeProtocol.Validate(hello);
        if (hello.Kind != BridgeMessage.Hello) throw new InvalidDataException(T("HandshakeRequired"));
        if (Id == hello.SessionId)
        {
            if (!Finished) partial = true;
            if (hello.DroppedEvents < DroppedEvents) throw new InvalidDataException(T("LossCounterReversed"));
        }
        else
        {
            Id = hello.SessionId; damage.Clear(); lastSequence = 0; totalDamage = 0;
            timeMs = 0; partial = false; DroppedEvents = 0; Finished = false;
        }
        if (hello.DroppedEvents > DroppedEvents) partial = true;
        DroppedEvents = hello.DroppedEvents;
    }

    public void Apply(BridgeFrame frame)
    {
        BridgeProtocol.Validate(frame);
        if (Id is null || frame.SessionId != Id || frame.Kind == BridgeMessage.Hello)
            throw new InvalidDataException(T("SessionMismatch"));
        if (frame.Sequence <= lastSequence)
        {
            if (frame.Sequence < lastSequence) partial = true;
            return; // Never count a replayed sequence twice.
        }
        if (Finished) throw new InvalidDataException(T("EncounterEnded"));
        if (frame.TimeMs < timeMs || frame.DroppedEvents < DroppedEvents)
            throw new InvalidDataException(T("ClockReversed"));
        if (frame.Sequence != lastSequence + 1 || frame.DroppedEvents > DroppedEvents) partial = true;
        if (frame.Kind == BridgeMessage.Damage)
        {
            var key = (frame.Player!, frame.Subgroup);
            if (!damage.ContainsKey(key) && damage.Count >= MaxPlayers)
                throw new InvalidDataException(T("PlayerLimit"));
            if (frame.Damage > long.MaxValue - totalDamage)
                throw new InvalidDataException(T("DamageOverflow"));
            damage[key] = damage.GetValueOrDefault(key) + frame.Damage;
            totalDamage += frame.Damage;
        }
        lastSequence = frame.Sequence; timeMs = frame.TimeMs; DroppedEvents = frame.DroppedEvents;
        Finished = frame.Kind == BridgeMessage.End;
    }

    public void Interrupt() { if (Id is not null && !Finished) partial = true; }

    public CombatSnapshot? Snapshot() => Id is null ? null : new(timeMs, partial,
        damage.Select(p => new DamageRow(p.Key.Player, p.Key.Subgroup, p.Value,
            timeMs > 0 ? p.Value / (timeMs / 1000d) : null)).ToArray());
}
