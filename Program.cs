using controller_ps4;
using System.Diagnostics;

class Program
{
    private static DS4VirtualInputBridge? bridge;
    private static CancellationTokenSource? cts;

    static async Task Main()
    {
        ViGEmInstaller.Run();

        cts = new CancellationTokenSource();
        Console.WriteLine("Démarrage de l'émulation manette...");
        Console.WriteLine("Appuyez sur Ctrl+P pour arrêter l'application");

        bridge = new DS4VirtualInputBridge();
        var bridgeTask = bridge.RunAsync(cts.Token);

        _ = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.P && (key.Modifiers & ConsoleModifiers.Control) != 0)
                    {
                        Console.WriteLine("\nArrêt demandé...");
                        cts.Cancel();
                    }
                }
                await Task.Delay(100);
            }
        });

        try
        {
            await bridgeTask;
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("Arrêt de l'émulation manette...");
        }
        finally
        {
            bridge?.Dispose();
        }
    }
}