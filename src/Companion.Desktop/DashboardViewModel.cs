using static Companion.Core.Localization.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Companion.Core;
using Companion.Transport;
using Companion.Core.Localization;

namespace Companion.Desktop;

public abstract class Observable : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}

public sealed record LanguageChoice(string Code, string Name);

public sealed class WidgetViewModel(Widget value, Func<CombatSnapshot?> snapshot, Func<string> source) : Observable
{
    public Widget Value { get; private set; } = value;
    public double X => Value.Layout.X;
    public double Y => Value.Layout.Y;
    public double Width => Value.Layout.Width;
    public double Height => Value.Layout.Height;
    public bool Editing { get; private set; }
    public bool Display => Editing || !Value.Hidden;
    public bool Known => Value.Kind is "damage-players" or "damage-groups";
    public string Title => Value.Kind == "damage-groups" ? T("WidgetGroups") : Known ? T("WidgetPlayers") : T("MissingIntegration");
    public string Source => !Known ? T("MissingWidgetHelp")
        : source();
    public string HideLabel => Value.Hidden ? T("Show") : T("Hide");
    public int Group { get => Value.Settings.Subgroup ?? 0; set { Value = Value with { Settings = Value.Settings with { Subgroup = value == 0 ? null : value } }; Refresh(); } }
    public int Rows { get => Value.Settings.Rows; set { Value = Value with { Settings = Value.Settings with { Rows = Math.Clamp(value, 1, 50) } }; Refresh(); } }
    public IReadOnlyList<DamageRow> Data => Known && snapshot() is { } combat ? combat.Select(Value.Settings, Value.Kind == "damage-groups") : [];
    public string EmptyMessage => Known && snapshot() is not null ? T("NoPlayers") : Source;
    public bool Empty => Data.Count == 0;
    public static IReadOnlyList<int> Groups { get; } = Enumerable.Range(0, 16).ToArray();
    public static IReadOnlyList<int> RowOptions { get; } = [5, 8, 10, 15, 20, 50];
    public void SetEditing(bool editing) { Editing = editing; Changed(nameof(Editing)); Changed(nameof(Display)); }
    public void ToggleHidden() { Value = Value with { Hidden = !Value.Hidden }; Changed(nameof(Display)); Changed(nameof(HideLabel)); }
    public void Move(double dx, double dy)
    {
        Value = Value with { Layout = Value.Layout with { X = Math.Clamp(X + dx, 0, 10000), Y = Math.Clamp(Y + dy, 0, 10000) } };
        Changed(nameof(X)); Changed(nameof(Y));
    }
    public void Resize(double dx, double dy)
    {
        Value = Value with { Layout = Value.Layout with { Width = Math.Clamp(Width + dx, 320, 4000), Height = Math.Clamp(Height + dy, 240, 4000) } };
        Changed(nameof(Width)); Changed(nameof(Height));
    }
    public void Refresh() { Changed(nameof(Data)); Changed(nameof(Source)); Changed(nameof(Empty)); Changed(nameof(EmptyMessage)); Changed(nameof(Group)); Changed(nameof(Rows)); }
}

public sealed class DashboardViewModel : Observable
{
    private readonly ProfileStore store;
    private Profile selected;
    private ProfilePage page;
    private bool editing;
    private bool replayEnabled;
    private bool useLocalBridge;
    private static readonly CombatSnapshot DemoCombat = CombatReplay.Demo();
    private BridgeStatus bridge = new(BridgeConnection.Waiting, null, null, null, 0, false, null);
    private string status = "";
    private string selectedLanguage = Language;
    public static IReadOnlyList<LanguageChoice> Languages { get; } = [new("en", "English"), new("fr", "Français")];
    public string SelectedLanguage
    {
        get => selectedLanguage;
        set
        {
            if (value == selectedLanguage || value is not ("en" or "fr")) return;
            try
            {
                LanguagePreferences.Save(Path.Combine(Path.GetDirectoryName(StoragePath)!, "language.txt"), value);
                selectedLanguage = value; Changed(); Status = T("LanguageRestart");
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            { Status = T("LanguageSaveFailed", e.Message); Changed(); }
        }
    }
    public ObservableCollection<Profile> Profiles { get; }
    public ObservableCollection<WidgetViewModel> Widgets { get; } = [];
    public string StoragePath { get; }
    public string Status { get => status; private set { status = value; Changed(); } }
    public Profile Selected
    {
        get => selected;
        set
        {
            if (value is null || value.Id == selected.Id) return;
            Capture(); selected = value; page = selected.Pages[0]; LoadWidgets();
            Changed(); Changed(nameof(Pages)); Changed(nameof(Page)); Changed(nameof(PageName)); Changed(nameof(ProfileName)); Save();
        }
    }
    public IReadOnlyList<ProfilePage> Pages => selected.Pages.ToArray();
    public ProfilePage Page
    {
        get => page;
        set { if (value is null || value.Id == page.Id) return; Capture(); page = value; LoadWidgets(); Changed(); Changed(nameof(PageName)); Save(); }
    }
    public string ProfileName => selected.Name;
    public string PageName => page.Name;
    public bool Editing
    {
        get => editing;
        set { editing = value; foreach (var w in Widgets) w.SetEditing(value); Changed(); Changed(nameof(EditLabel)); Save(); }
    }
    public string EditLabel => Editing ? T("FinishEditing") : T("EditDashboard");
    public bool ReplayEnabled
    {
        get => replayEnabled;
        set { replayEnabled = value; foreach (var w in Widgets) w.Refresh(); Changed(); Changed(nameof(ConnectionLabel)); Save(); }
    }
    public bool UseLocalBridge
    {
        get => useLocalBridge;
        set { useLocalBridge = value; Changed(); RefreshSource(); }
    }
    public string BridgeSummary => bridge.Connection switch
    {
        BridgeConnection.Waiting => T("WaitingSimulator"),
        BridgeConnection.Connected => bridge.Finished ? T("SimulationEnded") : T("SimulatorConnected"),
        BridgeConnection.Stale => T("SimulatorStale"),
        BridgeConnection.Disconnected => bridge.Finished ? T("SimulationClosed") : T("LocalDisconnected"),
        BridgeConnection.Rejected => T("MessagesRejected") + bridge.Error,
        BridgeConnection.Faulted => T("LocalUnavailable") + bridge.Error,
        _ => T("ListenerStopped")
    };
    public string ConnectionLabel => UseLocalBridge
        ? T("LocalConnectionLabel", BridgeSummary) + (bridge.Combat?.Partial == true ? T("PartialSuffix") : "")
        : ReplayEnabled ? T("DemoConnected") : T("NoSource");
    private CombatSnapshot? CurrentSnapshot() => UseLocalBridge ? bridge.Combat : ReplayEnabled ? DemoCombat : null;
    private string SourceDescription()
    {
        if (!UseLocalBridge) return ReplayEnabled ? T("DemoSource") : T("DisabledSource");
        var duration = bridge.Combat is { } combat ? T("DurationSuffix", TimeSpan.FromMilliseconds(combat.DurationMs).ToString("c", Culture)) : "";
        var received = bridge.LastReceivedAt is { } at ? T("ReceivedSuffix", at.ToLocalTime().ToString("HH:mm:ss", Culture)) : "";
        return T("LocalSource", BridgeSummary, duration, received) +
            (bridge.Combat?.Partial == true ? T("IncompleteSuffix", bridge.DroppedEvents) : "");
    }
    public void UpdateBridge(BridgeStatus status)
    {
        bridge = status; Changed(nameof(BridgeSummary));
        if (UseLocalBridge) RefreshSource();
    }
    private void RefreshSource()
    {
        foreach (var w in Widgets) w.Refresh();
        Changed(nameof(ConnectionLabel));
    }
    public double CanvasWidth => Math.Max(1180, Widgets.Where(w => w.Display).Select(w => w.X + w.Width + 16).DefaultIfEmpty(0).Max());
    public double CanvasHeight => Math.Max(520, Widgets.Where(w => w.Display).Select(w => w.Y + w.Height + 16).DefaultIfEmpty(0).Max());
    public bool NoWidgets => Widgets.All(w => !w.Display);

    public DashboardViewModel(ProfileStore store, Workspace state, string path)
    {
        this.store = store; StoragePath = path;
        Profiles = new(state.Profiles); selected = Profiles.First(p => p.Id == state.ActiveProfileId);
        page = selected.Pages[0]; replayEnabled = state.ReplayEnabled; LoadWidgets();
    }
    private void Capture() { page.Widgets.Clear(); page.Widgets.AddRange(Widgets.Select(w => w.Value)); }
    private void LoadWidgets()
    {
        Widgets.Clear(); foreach (var w in page.Widgets) { var vm = new WidgetViewModel(w, CurrentSnapshot, SourceDescription); vm.SetEditing(Editing); Widgets.Add(vm); }
        LayoutChanged();
    }
    public void LayoutChanged() { Changed(nameof(CanvasWidth)); Changed(nameof(CanvasHeight)); Changed(nameof(NoWidgets)); }
    public bool Save()
    {
        Capture(); LayoutChanged();
        try { store.Save(new(1, selected.Id, Profiles.ToList(), ReplayEnabled)); Status = T("Saved"); return true; }
        catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException)
        { Status = T("SaveFailed", e.Message); return false; }
    }
    public void NewProfile(bool duplicate)
    {
        Capture(); if (Profiles.Count >= 100) { Status = T("MaxProfiles"); return; }
        var next = duplicate ? selected.Duplicate(selected.Name.Length > 70 ? T("ProfileCopy") : selected.Name + T("CopySuffix")) : Profile.Create(T("NewProfile"));
        Profiles.Add(next); Selected = next; Editing = true;
    }
    public void Rename(string name)
    {
        name = name.Trim(); if (name.Length is < 1 or > 80) { Status = T("NameLength"); return; }
        Capture(); var index = Profiles.IndexOf(selected); selected = selected with { Name = name }; Profiles[index] = selected;
        Changed(nameof(Selected)); Changed(nameof(ProfileName)); Save();
    }
    public void DeleteProfile()
    {
        var old = selected;
        if (Profiles.Count == 1) Profiles.Add(Profile.Create(T("MyProfile")));
        Selected = Profiles.First(p => p.Id != old.Id); Profiles.Remove(old); Save();
    }
    public void AddPage()
    {
        if (selected.Pages.Count >= 50) { Status = T("MaxPages"); return; }
        var next = new ProfilePage(Guid.NewGuid(), T("PageNumber", selected.Pages.Count + 1), []);
        selected.Pages.Add(next); Changed(nameof(Pages)); Page = next;
    }
    public void RenamePage(string name)
    {
        name = name.Trim();
        if (name.Length is < 1 or > 80) { Status = T("NameLength"); return; }
        Capture(); var index = selected.Pages.IndexOf(page);
        page = page with { Name = name }; selected.Pages[index] = page;
        Changed(nameof(Pages)); Changed(nameof(Page)); Changed(nameof(PageName)); Save();
    }
    public void DuplicatePage()
    {
        if (selected.Pages.Count >= 50) { Status = T("MaxPages"); return; }
        Capture();
        var copy = new ProfilePage(Guid.NewGuid(), page.Name.Length > 70 ? T("PageCopy") : page.Name + T("CopySuffix"),
            page.Widgets.Select(w => w.Duplicate() with { Layout = w.Layout with { } }).ToList());
        selected.Pages.Add(copy); Changed(nameof(Pages)); Page = copy;
    }
    public void DeletePage()
    {
        var old = page;
        if (selected.Pages.Count == 1) selected.Pages.Add(new(Guid.NewGuid(), T("Overview"), []));
        Page = selected.Pages.First(p => p.Id != old.Id);
        selected.Pages.Remove(old); Changed(nameof(Pages)); Changed(nameof(Page)); Save();
    }
    public byte[] ExportProfile() { Capture(); return ProfileTransfer.Export(selected); }
    public void ImportProfile(ProfileImport imported)
    {
        if (Profiles.Count >= 100) throw new InvalidDataException(T("MaxProfiles"));
        // Regenerate once more so reusing an import result cannot introduce duplicate IDs.
        var copy = imported.Profile.Duplicate(imported.Profile.Name);
        new Workspace(1, copy.Id, [copy]).Validate();
        Profiles.Add(copy); Selected = copy;
    }
    public void AddWidget(string kind, WidgetViewModel? source = null)
    {
        if (Widgets.Count >= 200) { Status = T("MaxWidgets"); return; }
        var w = source?.Value.Duplicate() ?? new Widget(Guid.NewGuid(), kind, new(), new(0, Widgets.Count * 32, 600, 480));
        var vm = new WidgetViewModel(w, CurrentSnapshot, SourceDescription); vm.SetEditing(Editing); Widgets.Add(vm); Save();
    }
    public void RemoveWidget(WidgetViewModel widget) { Widgets.Remove(widget); Save(); }
}
