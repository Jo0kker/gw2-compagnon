using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Companion.Desktop;

public partial class MainWindow : Window
{
    private readonly DashboardViewModel model;
    public MainWindow(DashboardViewModel model)
    {
        InitializeComponent(); this.model = model; DataContext = model;
        Closing += SaveBeforeClose;
    }
    private void SaveBeforeClose(object? sender, CancelEventArgs e)
    {
        if (!model.Save()) e.Cancel = MessageBox.Show(this,
            "L’enregistrement a échoué. Fermer et perdre les changements non enregistrés ?",
            "Profils non enregistrés", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes;
    }
    private void ToggleEdit(object sender, RoutedEventArgs e) => model.Editing = !model.Editing;
    private void NewProfile(object sender, RoutedEventArgs e) => model.NewProfile(false);
    private void DuplicateProfile(object sender, RoutedEventArgs e) => model.NewProfile(true);
    private void RenameProfile(object sender, RoutedEventArgs e) => model.Rename(ProfileNameInput.Text);
    private void AddPage(object sender, RoutedEventArgs e) => model.AddPage();
    private void DeleteProfile(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, $"Supprimer le profil « {model.Selected.Name} » ?", "Supprimer le profil",
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
