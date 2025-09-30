using Nefarius.ViGEm.Client;
using System.Diagnostics;
using System.Net.Http;

public static class ViGEmInstaller
{
    private const string ViGEmDownloadUrl = "https://github.com/nefarius/ViGEmBus/releases/download/v1.22.0/ViGEmBus_1.22.0_x64_x86_arm64.exe";
    private const string InstallerFileName = "ViGEmBus_1.22.0_x64_x86_arm64.exe";

    public static async void Run()
    {
        if (!ViGEmInstaller.IsViGEmInstalled())
        {
            Console.WriteLine("❌ ViGEm Bus Driver non installé");
            Console.WriteLine("🔄 Tentative d'installation automatique...");

            var success = await InstallViGEmAsync();
            if (!success)
            {
                Console.WriteLine("❌ Échec de l'installation automatique");
                Console.WriteLine("💡 Veuillez installer manuellement ViGEm Bus Driver");
                Console.WriteLine("📥 Téléchargez-le depuis : https://github.com/ViGEm/ViGEmBus/releases");
                return;
            }

            Console.WriteLine("✅ ViGEm Bus Driver installé avec succès");
            Console.WriteLine("🔄 Redémarrage recommandé pour finaliser l'installation");
            Console.WriteLine("Voulez-vous redémarrer maintenant ? (o/n)");

            if (Console.ReadKey().Key == ConsoleKey.O)
            {
                Process.Start("shutdown", "/r /t 0");
                return;
            }
        }

    }

    public static bool IsViGEmInstalled()
    {
        try
        {
            using var client = new ViGEmClient();
            var controller = client.CreateDualShock4Controller();
            controller.Disconnect();
            controller.Dispose();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> InstallViGEmAsync()
    {
        try
        {
            Console.WriteLine("📥 Téléchargement de ViGEm Bus Driver...");

            using var httpClient = new HttpClient();
            var installerData = await httpClient.GetByteArrayAsync(ViGEmDownloadUrl);
            await File.WriteAllBytesAsync(InstallerFileName, installerData);

            Console.WriteLine("🔧 Installation de ViGEm Bus Driver...");

            var processStartInfo = new ProcessStartInfo(InstallerFileName)
            {
                UseShellExecute = true,
                Verb = "runas" 
            };

            var process = Process.Start(processStartInfo);
            if (process == null)
            {
                Console.WriteLine("❌ Impossible de lancer l'installateur");
                return false;
            }

            await process.WaitForExitAsync();

            File.Delete(InstallerFileName);

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erreur lors de l'installation: {ex.Message}");
            return false;
        }
    }
}