using static Companion.Core.Localization.Text;
using System.IO.Pipes;
using Companion.Transport;
using Companion.Core.Localization;

SetLanguage(LanguagePreferences.Load(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GW2Companion", "language.txt")));

var allowed = new HashSet<string> { "--drop", "--interrupt", "--stall" };
if (args.Any(a => !allowed.Contains(a)))
{
    Console.Error.WriteLine(T("SimulatorUsage"));
    return 2;
}
using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
try
{
    Console.WriteLine(T("SimulatorIntro"));
    await using var pipe = new NamedPipeClientStream(".", BridgeReceiver.DefaultPipeName, PipeDirection.Out, PipeOptions.Asynchronous);
    await pipe.ConnectAsync(5000, stop.Token);
    var session = Guid.NewGuid();
    await BridgeProtocol.WriteAsync(pipe, new(1, session, 0, BridgeMessage.Hello), stop.Token);
    Console.WriteLine(T("SimulatorReady"));
    long sequence = 0, dropped = 0;
    for (var tick = 1; tick <= 40; tick++)
    {
        await Task.Delay(250, stop.Token);
        sequence++;
        if (args.Contains("--drop") && tick == 12) { dropped++; continue; }
        if (args.Contains("--interrupt") && tick == 20) { Console.WriteLine(T("SimulateInterrupt")); return 0; }
        if (args.Contains("--stall") && tick == 20)
        {
            Console.WriteLine(T("SimulateStall"));
            await Task.Delay(4000, stop.Token); return 0;
        }
        var player = tick % 2 == 0 ? T("FictionalPlayer", "Kael") : T("FictionalPlayer", "Luné");
        await BridgeProtocol.WriteAsync(pipe, new(1, session, sequence, BridgeMessage.Damage,
            tick * 250, player, tick % 2 + 1, tick * 100, dropped), stop.Token);
    }
    await BridgeProtocol.WriteAsync(pipe, new(1, session, ++sequence, BridgeMessage.End, 10_000, DroppedEvents: dropped), stop.Token);
    Console.WriteLine(T("SimulatorComplete"));
    return 0;
}
catch (OperationCanceledException) { Console.WriteLine(T("SimulatorStopped")); return 0; }
catch (Exception e) when (e is IOException or TimeoutException)
{ Console.Error.WriteLine(T("SimulatorFailed")); return 1; }
