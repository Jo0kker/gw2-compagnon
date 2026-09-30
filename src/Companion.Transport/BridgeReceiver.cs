using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using Companion.Core;

namespace Companion.Transport;

public enum BridgeConnection { Waiting, Connected, Disconnected, Stale, Rejected, Stopped, Faulted }
public sealed record BridgeStatus(BridgeConnection Connection, Guid? SessionId, CombatSnapshot? Combat,
    DateTimeOffset? LastReceivedAt, long DroppedEvents, bool Finished, string? Error);

public sealed class BridgeReceiver
{
    public static string DefaultPipeName => "gw2-companion-simulator-" +
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Environment.UserDomainName + "\\" + Environment.UserName)))[..16];
    private readonly string pipeName;
    private readonly TimeSpan timeout;
    private readonly object gate = new();
    private readonly BridgeSession session = new();
    private BridgeConnection connection = BridgeConnection.Waiting;
    private DateTimeOffset? lastReceivedAt;
    private string? error;
    private int running;

    public BridgeReceiver(string? pipeName = null, TimeSpan? timeout = null)
    {
        this.pipeName = pipeName ?? DefaultPipeName;
        this.timeout = timeout ?? TimeSpan.FromSeconds(3);
        if (this.pipeName.Length is < 1 or > 80 || this.pipeName.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-'))
            throw new ArgumentException("Nom de pipe invalide.", nameof(pipeName));
        if (this.timeout < TimeSpan.FromMilliseconds(50) || this.timeout > TimeSpan.FromMinutes(1))
            throw new ArgumentOutOfRangeException(nameof(timeout));
    }

    public BridgeStatus Snapshot()
    {
        lock (gate) return new(connection, session.Id, session.Snapshot(), lastReceivedAt, session.DroppedEvents, session.Finished, error);
    }

    private void SetState(BridgeConnection state, string? message = null)
    {
        lock (gate) { connection = state; error = message; session.Interrupt(); }
    }

    public async Task RunAsync(CancellationToken stop)
    {
        if (Interlocked.Exchange(ref running, 1) != 0) throw new InvalidOperationException("Le récepteur est déjà en cours.");
        try
        {
            while (!stop.IsCancellationRequested)
            {
                await using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.In, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly, 4096, 4096);
                await pipe.WaitForConnectionAsync(stop).ConfigureAwait(false);
                try
                {
                    var first = true;
                    while (true)
                    {
                        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stop);
                        deadline.CancelAfter(timeout);
                        var frame = await BridgeProtocol.ReadAsync(pipe, deadline.Token).ConfigureAwait(false);
                        if (frame is null) { SetState(BridgeConnection.Disconnected); break; }
                        lock (gate)
                        {
                            if (first) session.Begin(frame); else session.Apply(frame);
                            first = false; connection = BridgeConnection.Connected;
                            lastReceivedAt = DateTimeOffset.UtcNow; error = null;
                        }
                    }
                }
                catch (OperationCanceledException) when (!stop.IsCancellationRequested)
                { SetState(BridgeConnection.Stale, "Aucun message complet reçu dans le délai prévu."); }
                catch (InvalidDataException e) { SetState(BridgeConnection.Rejected, e.Message); }
                catch (IOException) { SetState(BridgeConnection.Disconnected, "La connexion locale a été interrompue."); }
            }
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { SetState(BridgeConnection.Faulted, "Le canal local ne peut pas être ouvert."); return; }
        finally { Interlocked.Exchange(ref running, 0); }
        SetState(BridgeConnection.Stopped);
    }
}
