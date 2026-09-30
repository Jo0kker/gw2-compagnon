using static Companion.Core.Localization.Text;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Companion.Transport;
using Companion.Core;
using Microsoft.Win32;

namespace Companion.Desktop;

public partial class MainWindow : Window
{
    private readonly DashboardViewModel model;
    private readonly BridgeReceiver receiver = new();
    private readonly CancellationTokenSource receiverStop = new();
    private readonly DispatcherTimer refreshTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly Task receiverTask;
    public MainWindow(DashboardViewModel model)
    {
        InitializeComponent(); this.model = model; DataContext = model;
        Language = System.Windows.Markup.XmlLanguage.GetLanguage(Culture.Name);
        receiverTask = Task.Run(() => receiver.RunAsync(receiverStop.Token));
        refreshTimer.Tick += (_, _) => model.UpdateBridge(receiver.Snapshot());
        refreshTimer.Start();
        Closing += SaveBeforeClose;
        Closed += StopReceiver;
    }
    private async void StopReceiver(object? sender, EventArgs e)
    {
        refreshTimer.Stop();
        await receiverStop.CancelAsync();
        try { await receiverTask; }
        finally { receiverStop.Dispose(); }
    }
    private void SaveBeforeClose(object? sender, CancelEventArgs e)
    {
        if (!model.Save()) e.Cancel = MessageBox.Show(this,
            T("DiscardChanges"),
            T("UnsavedProfiles"), MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes;
    }
    private void ToggleEdit(object sender, RoutedEventArgs e) => model.Editing = !model.Editing;
    private void NewProfile(object sender, RoutedEventArgs e) => model.NewProfile(false);
    private void DuplicateProfile(object sender, RoutedEventArgs e) => model.NewProfile(true);
    private void RenameProfile(object sender, RoutedEventArgs e) => model.Rename(ProfileNameInput.Text);
    private void AddPage(object sender, RoutedEventArgs e) => model.AddPage();
    private void RenamePage(object sender, RoutedEventArgs e) => model.RenamePage(PageNameInput.Text);
    private void DuplicatePage(object sender, RoutedEventArgs e) => model.DuplicatePage();
    private void DeletePage(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, T("DeletePageQuestion", model.Page.Name), T("DeletePage"),
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) model.DeletePage();
    }
    private void ExportProfile(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = T("ProfileFileFilter"), FileName = "profil.gw2profile.json", AddExtension = true };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            if (Path.GetFullPath(dialog.FileName).Equals(Path.GetFullPath(model.StoragePath), StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(T("ExportInternalPath"));
            var bytes = model.ExportProfile();
            File.WriteAllBytes(dialog.FileName, bytes);
            MessageBox.Show(this, T("ExportCompletedMessage"), T("ExportCompleted"));
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException)
        { MessageBox.Show(this, error.Message, T("ExportFailed"), MessageBoxButton.OK, MessageBoxImage.Error); }
    }
    private void ImportProfile(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = T("JsonFileFilter"), Multiselect = false };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var imported = ProfileTransfer.ReadFile(dialog.FileName);
            model.ImportProfile(imported);
            if (imported.MissingWidgetKinds.Count > 0)
                MessageBox.Show(this, T("MissingWidgetsMessage", string.Join(", ", imported.MissingWidgetKinds)), T("MissingDependencies"));
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException)
        { MessageBox.Show(this, error.Message, T("ImportFailed"), MessageBoxButton.OK, MessageBoxImage.Error); }
    }
    private void DeleteProfile(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, T("DeleteProfileQuestion", model.Selected.Name), T("DeleteProfile"),
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) model.DeleteProfile();
    }
    private void AddWidget(object sender, RoutedEventArgs e) => model.AddWidget((string)((Button)sender).Tag);
    private static WidgetViewModel Widget(object sender) => (WidgetViewModel)((FrameworkElement)sender).DataContext;
    private void DuplicateWidget(object sender, RoutedEventArgs e) => model.AddWidget(Widget(sender).Value.Kind, Widget(sender));
    private void RemoveWidget(object sender, RoutedEventArgs e) => model.RemoveWidget(Widget(sender));
    private void HideWidget(object sender, RoutedEventArgs e) { Widget(sender).ToggleHidden(); model.Save(); }
    private void MoveWidget(object sender, DragDeltaEventArgs e) { Widget(sender).Move(e.HorizontalChange, e.VerticalChange); model.LayoutChanged(); }
    private void ResizeWidget(object sender, DragDeltaEventArgs e) { Widget(sender).Resize(e.HorizontalChange, e.VerticalChange); model.LayoutChanged(); }
    private void FinishDrag(object sender, DragCompletedEventArgs e) => model.Save();
    private void MoveWithKeyboard(object sender, KeyEventArgs e) => ChangeWithKeyboard(sender, e, resize: false);
    private void ResizeWithKeyboard(object sender, KeyEventArgs e) => ChangeWithKeyboard(sender, e, resize: true);
    private void ChangeWithKeyboard(object sender, KeyEventArgs e, bool resize)
    {
        var (x, y) = e.Key switch { Key.Left => (-16, 0), Key.Right => (16, 0), Key.Up => (0, -16), Key.Down => (0, 16), _ => (0, 0) };
        if (x == 0 && y == 0) return;
        if (resize) Widget(sender).Resize(x, y); else Widget(sender).Move(x, y);
        e.Handled = true; model.Save();
    }
}
