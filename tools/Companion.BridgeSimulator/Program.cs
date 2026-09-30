using System.IO.Pipes;
using Companion.Transport;

var allowed = new HashSet<string> { "--drop", "--interrupt", "--stall" };
if (args.Any(a => !allowed.Contains(a)))
{
    Console.Error.WriteLine("Usage : dotnet run --project tools/Companion.BridgeSimulator -- [--drop] [--interrupt] [--stall]");
    return 2;
}
using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
try
{
    Console.WriteLine("SIMULATION uniquement. Aucune connexion à Guild Wars 2. Ouvrez d’abord l’application.");
    await using var pipe = new NamedPipeClientStream(".", BridgeReceiver.DefaultPipeName, PipeDirection.Out, PipeOptions.Asynchronous);
    await pipe.ConnectAsync(5000, stop.Token);
    var session = Guid.NewGuid();
    await BridgeProtocol.WriteAsync(pipe, new(1, session, 0, BridgeMessage.Hello), stop.Token);
    Console.WriteLine("Connexion locale ouverte. Sélectionnez la source « Simulateur local » dans l’application.");
    long sequence = 0, dropped = 0;
    for (var tick = 1; tick <= 40; tick++)
    {
        await Task.Delay(250, stop.Token);
        sequence++;
        if (args.Contains("--drop") && tick == 12) { dropped++; continue; }
        if (args.Contains("--interrupt") && tick == 20) { Console.WriteLine("Coupure volontaire en cours de combat."); return 0; }
        if (args.Contains("--stall") && tick == 20)
        {
            Console.WriteLine("Silence volontaire de 4 secondes pour tester le timeout.");
            await Task.Delay(4000, stop.Token); return 0;
        }
        var player = tick % 2 == 0 ? "Kael — fictif" : "Luné — fictif";
        await BridgeProtocol.WriteAsync(pipe, new(1, session, sequence, BridgeMessage.Damage,
            tick * 250, player, tick % 2 + 1, tick * 100, dropped), stop.Token);
    }
    await BridgeProtocol.WriteAsync(pipe, new(1, session, ++sequence, BridgeMessage.End, 10_000, DroppedEvents: dropped), stop.Token);
    Console.WriteLine("Combat fictif terminé. Le dashboard conserve les dernières valeurs.");
    return 0;
}
catch (OperationCanceledException) { Console.WriteLine("Simulation arrêtée."); return 0; }
catch (Exception e) when (e is IOException or TimeoutException)
{ Console.Error.WriteLine("Connexion impossible ou interrompue. Vérifiez que l’application est ouverte sous le même utilisateur."); return 1; }
