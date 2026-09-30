using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using Companion.Core;
using Companion.Desktop;
using Companion.Transport;

internal static class BridgeTests
{
    public static async Task<(int Total, int Failed)> RunAsync()
    {
        var tests = new (string Name, Func<Task> Run)[]
        {
            ("Frames fragmentées et concaténées décodées exactement", Framing),
            ("Protocole : taille, version, JSON et troncature refusés", InvalidFrames),
            ("Agrégation bornée, pertes, doublons et reconnexion", Session),
            ("Pipe réel : réception, coupure et nouvelle session", PipeRoundTrip),
            ("Pipe réel : timeout, protocole rejeté et arrêt annulable", PipeErrors),
            ("Dashboard : source locale explicite et valeurs conservées après coupure", Presentation)
        };
        var failed = 0;
        foreach (var (name, run) in tests)
        {
            try { await run(); Console.WriteLine($"PASS {name}"); }
            catch (Exception error) { failed++; Console.Error.WriteLine($"FAIL {name}: {error}"); }
        }
        return (tests.Length, failed);
    }

    private static async Task Framing()
    {
        var session = Guid.NewGuid();
        var hello = new BridgeFrame(1, session, 0, BridgeMessage.Hello);
        var data = new BridgeFrame(1, session, 1, BridgeMessage.Damage, 1000, "A", 1, 1200);
        using var stream = new MemoryStream();
        await BridgeProtocol.WriteAsync(stream, hello); await BridgeProtocol.WriteAsync(stream, data);
        using var fragmented = new FragmentedStream(stream.ToArray());
        Check(await BridgeProtocol.ReadAsync(fragmented) == hello);
        Check(await BridgeProtocol.ReadAsync(fragmented) == data);
        Check(await BridgeProtocol.ReadAsync(fragmented) is null);
    }

    private static async Task InvalidFrames()
    {
        var header = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(header, BridgeProtocol.MaxFrameBytes + 1);
        await Refuses<InvalidDataException>(() => BridgeProtocol.ReadAsync(new MemoryStream(header)).AsTask());
        await Refuses<EndOfStreamException>(() => BridgeProtocol.ReadAsync(new MemoryStream([20, 0])).AsTask());
        BinaryPrimitives.WriteInt32LittleEndian(header, 20);
        await Refuses<EndOfStreamException>(() => BridgeProtocol.ReadAsync(new MemoryStream(header)).AsTask());
        await Refuses<InvalidDataException>(() => BridgeProtocol.ReadAsync(JsonFrame("{}")).AsTask());
        await Refuses<InvalidDataException>(() => BridgeProtocol.ReadAsync(JsonFrame("{invalid")).AsTask());
        var future = new BridgeFrame(2, Guid.NewGuid(), 0, BridgeMessage.Hello);
        await Refuses<InvalidDataException>(() => BridgeProtocol.WriteAsync(new MemoryStream(), future).AsTask());
        await Refuses<InvalidDataException>(() => BridgeProtocol.WriteAsync(new MemoryStream(), future with { Version = 1, Synthetic = false }).AsTask());
    }

    private static Task Session()
    {
        var id = Guid.NewGuid(); var s = new BridgeSession();
        s.Begin(new(1, id, 0, BridgeMessage.Hello));
        var a = new BridgeFrame(1, id, 1, BridgeMessage.Damage, 1000, "A", 1, 1200);
        s.Apply(a); s.Apply(a);
        Check(s.Snapshot()!.Players[0].Damage == 1200 && !s.Snapshot()!.Partial);
        s.Apply(new(1, id, 3, BridgeMessage.Heartbeat, 2000, DroppedEvents: 1));
        Check(s.Snapshot()!.Partial && s.Snapshot()!.Players[0].Dps == 600);
        s.Interrupt(); s.Begin(new(1, id, 0, BridgeMessage.Hello, DroppedEvents: 1));
        s.Apply(a); Check(s.Snapshot()!.Players[0].Damage == 1200);
        s.Apply(new(1, id, 4, BridgeMessage.End, 3000, DroppedEvents: 1));
        Check(s.Finished && s.Snapshot()!.Partial);
        var next = Guid.NewGuid(); s.Begin(new(1, next, 0, BridgeMessage.Hello));
        Check(s.Snapshot()!.Players.Count == 0 && !s.Snapshot()!.Partial);
        s.Apply(new(1, next, 1, BridgeMessage.Damage, 1000, "A", 1, long.MaxValue));
        Rejects(() => s.Apply(new(1, next, 2, BridgeMessage.Damage, 2000, "A", 1, 1)));
        Rejects(() => s.Apply(new(1, id, 2, BridgeMessage.Heartbeat, 1000)));
        var bounded = new BridgeSession(); bounded.Begin(new(1, id, 0, BridgeMessage.Hello));
        for (var i = 1; i <= BridgeSession.MaxPlayers; i++) bounded.Apply(new(1, id, i, BridgeMessage.Damage, i, $"P{i}", 1, 1));
        Rejects(() => bounded.Apply(new(1, id, 513, BridgeMessage.Damage, 513, "Too many", 1, 1)));
        return Task.CompletedTask;
    }

    private static async Task PipeRoundTrip()
    {
        var name = "gw2-test-" + Guid.NewGuid().ToString("N");
        var receiver = new BridgeReceiver(name); using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var task = receiver.RunAsync(stop.Token);
        try
        {
            var id = Guid.NewGuid();
            await using (var client = await Connect(name, stop.Token))
            {
                await BridgeProtocol.WriteAsync(client, new(1, id, 0, BridgeMessage.Hello), stop.Token);
                await BridgeProtocol.WriteAsync(client, new(1, id, 1, BridgeMessage.Damage, 1000, "A", 1, 1500), stop.Token);
                await Until(() => receiver.Snapshot().Combat?.Players.Count == 1);
                Check(receiver.Snapshot().Connection == BridgeConnection.Connected);
                Check(receiver.Snapshot().Combat!.Players[0].Dps == 1500);
            }
            await Until(() => receiver.Snapshot().Connection == BridgeConnection.Disconnected);
            Check(receiver.Snapshot().Combat!.Partial && receiver.Snapshot().LastReceivedAt is not null);
            await using (var client = await Connect(name, stop.Token))
            {
                var next = Guid.NewGuid();
                await BridgeProtocol.WriteAsync(client, new(1, next, 0, BridgeMessage.Hello), stop.Token);
                await BridgeProtocol.WriteAsync(client, new(1, next, 1, BridgeMessage.End, 1000), stop.Token);
                await Until(() => receiver.Snapshot().Finished);
                Check(receiver.Snapshot().SessionId == next && !receiver.Snapshot().Combat!.Partial);
            }
            await Until(() => receiver.Snapshot().Connection == BridgeConnection.Disconnected);
            Check(!receiver.Snapshot().Combat!.Partial);
        }
        finally { await stop.CancelAsync(); await task.WaitAsync(TimeSpan.FromSeconds(2)); }
        Check(receiver.Snapshot().Connection == BridgeConnection.Stopped);
    }

    private static async Task PipeErrors()
    {
        var name = "gw2-test-" + Guid.NewGuid().ToString("N");
        var receiver = new BridgeReceiver(name, TimeSpan.FromMilliseconds(750));
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(10)); var task = receiver.RunAsync(stop.Token);
        try
        {
            await using (var client = await Connect(name, stop.Token))
            {
                await BridgeProtocol.WriteAsync(client, new(1, Guid.NewGuid(), 0, BridgeMessage.Hello), stop.Token);
                await Until(() => receiver.Snapshot().Connection == BridgeConnection.Stale);
                Check(receiver.Snapshot().Combat!.Partial);
            }
            await using (var client = await Connect(name, stop.Token))
            {
                await BridgeProtocol.WriteAsync(client, new(1, Guid.NewGuid(), 1, BridgeMessage.Damage, 1, "A", 1, 1), stop.Token);
                await Until(() => receiver.Snapshot().Connection == BridgeConnection.Rejected);
            }
            await using (var client = await Connect(name, stop.Token))
            {
                await BridgeProtocol.WriteAsync(client, new(1, Guid.NewGuid(), 0, BridgeMessage.Hello), stop.Token);
                await Until(() => receiver.Snapshot().Connection == BridgeConnection.Connected);
                await stop.CancelAsync();
                await task.WaitAsync(TimeSpan.FromSeconds(2));
                Check(receiver.Snapshot().Connection == BridgeConnection.Stopped);
            }
        }
        finally { await stop.CancelAsync(); await task.WaitAsync(TimeSpan.FromSeconds(2)); }
    }

    private static Task Presentation()
    {
        var dir = Path.Combine(Path.GetTempPath(), "gw2-vm-" + Guid.NewGuid());
        try
        {
            var path = Path.Combine(dir, "profiles.json");
            var vm = new DashboardViewModel(new ProfileStore(path), Workspace.CreateDefault(), path) { UseLocalBridge = true };
            Check(vm.Widgets[0].Data.Count == 0 && vm.ConnectionLabel.Contains("Simulation"));
            var partial = CombatReplay.Read([new(1, 1, "A", 1, 1500)], 1000, true);
            vm.UpdateBridge(new(BridgeConnection.Disconnected, Guid.NewGuid(), partial, DateTimeOffset.UtcNow, 2, false, null));
            Check(vm.Widgets[0].Data[0].Damage == 1500);
            Check(vm.ConnectionLabel.Contains("partielles") && vm.Widgets[0].Source.Contains("incomplet"));
            vm.UseLocalBridge = false;
            Check(vm.Widgets[0].Data.Count == 8 && vm.ConnectionLabel.Contains("démonstration"));
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        return Task.CompletedTask;
    }

    private static MemoryStream JsonFrame(string json)
    {
        var body = Encoding.UTF8.GetBytes(json); var data = new byte[body.Length + 4];
        BinaryPrimitives.WriteInt32LittleEndian(data, body.Length); body.CopyTo(data, 4); return new(data);
    }
    private static async Task<NamedPipeClientStream> Connect(string name, CancellationToken token)
    {
        var client = new NamedPipeClientStream(".", name, PipeDirection.Out, PipeOptions.Asynchronous);
        try { await client.ConnectAsync(2000, token); return client; }
        catch { await client.DisposeAsync(); throw; }
    }
    private static async Task Until(Func<bool> condition)
    {
        var timer = Stopwatch.StartNew();
        while (!condition())
        {
            if (timer.Elapsed > TimeSpan.FromSeconds(3)) throw new TimeoutException("État attendu non reçu.");
            await Task.Delay(10);
        }
    }
    private static void Check(bool condition) { if (!condition) throw new Exception("Assertion échouée"); }
    private static void Rejects(Action action) { try { action(); } catch (InvalidDataException) { return; } throw new Exception("Rejet attendu"); }
    private static async Task Refuses<T>(Func<Task> action) where T : Exception
    { try { await action(); } catch (T) { return; } throw new Exception($"Exception {typeof(T).Name} attendue"); }
    private sealed class FragmentedStream(byte[] data) : MemoryStream(data)
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default) => base.ReadAsync(buffer[..Math.Min(1, buffer.Length)], token);
    }
}
