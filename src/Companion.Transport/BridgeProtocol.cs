using static Companion.Core.Localization.Text;
using System.Buffers.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Companion.Transport;

public enum BridgeMessage { Hello, Damage, Heartbeat, End }

// Experimental normalized simulation protocol, NOT ArcDPS raw event layout.
public sealed record BridgeFrame(int Version, Guid SessionId, long Sequence, BridgeMessage Kind,
    int TimeMs = 0, string? Player = null, int? Subgroup = null, long Damage = 0,
    long DroppedEvents = 0, bool Synthetic = true);

public static class BridgeProtocol
{
    public const int Version = 1;
    public const int MaxFrameBytes = 65_536;
    public const int MaxDurationMs = 86_400_000;
    private static readonly JsonSerializerOptions Options = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        MaxDepth = 8
    };

    public static void Validate(BridgeFrame frame)
    {
        if (frame.Version != Version) throw new InvalidDataException(T("ProtocolVersion"));
        if (!frame.Synthetic) throw new InvalidDataException(T("SyntheticOnly"));
        if (frame.SessionId == Guid.Empty || !Enum.IsDefined(frame.Kind) || frame.Sequence < 0 ||
            frame.TimeMs is < 0 or > MaxDurationMs || frame.DroppedEvents < 0 || frame.Damage < 0 ||
            (frame.Subgroup is not null && frame.Subgroup is < 1 or > 15))
            throw new InvalidDataException(T("InvalidHeader"));
        if (frame.Kind == BridgeMessage.Hello && (frame.Sequence != 0 || frame.TimeMs != 0))
            throw new InvalidDataException(T("FirstMessage"));
        if (frame.Kind != BridgeMessage.Hello && frame.Sequence == 0)
            throw new InvalidDataException(T("InvalidSequence"));
        if (frame.Kind == BridgeMessage.Damage)
        {
            if (string.IsNullOrWhiteSpace(frame.Player) || frame.Player.Length > 128 || frame.Player.Any(char.IsControl))
                throw new InvalidDataException(T("InvalidPlayer"));
        }
        else if (frame.Player is not null || frame.Subgroup is not null || frame.Damage != 0)
            throw new InvalidDataException(T("UnexpectedData"));
    }

    public static async ValueTask WriteAsync(Stream stream, BridgeFrame frame, CancellationToken token = default)
    {
        Validate(frame);
        var payload = JsonSerializer.SerializeToUtf8Bytes(frame, Options);
        if (payload.Length > MaxFrameBytes) throw new InvalidDataException(T("MessageLarge"));
        var header = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);
        await stream.WriteAsync(header, token).ConfigureAwait(false);
        await stream.WriteAsync(payload, token).ConfigureAwait(false);
        await stream.FlushAsync(token).ConfigureAwait(false);
    }

    public static async ValueTask<BridgeFrame?> ReadAsync(Stream stream, CancellationToken token = default)
    {
        var header = new byte[4];
        if (await stream.ReadAsync(header.AsMemory(0, 1), token).ConfigureAwait(false) == 0) return null;
        await stream.ReadExactlyAsync(header.AsMemory(1), token).ConfigureAwait(false);
        var length = BinaryPrimitives.ReadInt32LittleEndian(header);
        if (length is < 2 or > MaxFrameBytes) throw new InvalidDataException(T("MessageLength"));
        var payload = new byte[length];
        await stream.ReadExactlyAsync(payload, token).ConfigureAwait(false);
        BridgeFrame frame;
        try { frame = JsonSerializer.Deserialize<BridgeFrame>(payload, Options) ?? throw new InvalidDataException(T("MessageEmpty")); }
        catch (JsonException e) { throw new InvalidDataException(T("InvalidJson"), e); }
        Validate(frame);
        return frame;
    }
}
