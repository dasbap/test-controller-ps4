using controller_ps4;
using System.Diagnostics;

class Program
{
    private static DS4VirtualInputBridge? bridge;
    private static CancellationTokenSource? cts;

    static async Task Main()
    {
        Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "BloqueSourisChiaki.exe"));
        cts = new CancellationTokenSource(); 

        Console.WriteLine("Démarrage de l'émulation manette...");
        bridge = new DS4VirtualInputBridge();
        var bridgeTask = bridge.RunAsync(cts.Token);

        Console.WriteLine("Appuyez sur Ctrl+C pour quitter.");

        try
        {
            await Task.Delay(Timeout.Infinite, cts.Token);
        }
        catch (TaskCanceledException) { }

        Console.WriteLine("Arrêt de l'émulation manette...");
        bridge?.Dispose();
    }
}
