using static Companion.Core.Localization.Text;
using System.IO;
using System.Windows;
using Companion.Core;
using Companion.Core.Localization;

namespace Companion.Desktop;

public partial class App : Application
{
    private Mutex? instance;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GW2Companion");
        SetLanguage(LanguagePreferences.Load(Path.Combine(directory, "language.txt")));
        instance = new Mutex(true, "Local\\GW2CompanionDesktop", out var first);
        if (!first)
        {
            MessageBox.Show(T("AlreadyOpen"));
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
            MessageBox.Show(T("ProfileLoadFailed", path, error.Message), T("ProfilesUnavailable"), MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
    protected override void OnExit(ExitEventArgs e) { instance?.Dispose(); base.OnExit(e); }
}
