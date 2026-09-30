using System.IO;
using System.Windows;
using Companion.Core;

namespace Companion.Desktop;

public partial class App : Application
{
    private Mutex? instance;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        instance = new Mutex(true, "Local\\GW2CompanionDesktop", out var first);
        if (!first)
        {
            MessageBox.Show("Le compagnon est déjà ouvert dans cette session Windows.");
            Shutdown(); return;
        }
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GW2Companion", "profiles.json");
        try
        {
            var store = new ProfileStore(path);
            MainWindow = new MainWindow(new DashboardViewModel(store, store.Load(), path));
            MainWindow.Show();
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            MessageBox.Show($"Les profils ne peuvent pas être ouverts. Le fichier existant est conservé.\n\n{path}\n\n{error.Message}\n\nUne sauvegarde précédente peut être disponible dans profiles.json.bak.", "Profils indisponibles", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
    protected override void OnExit(ExitEventArgs e) { instance?.Dispose(); base.OnExit(e); }
}
