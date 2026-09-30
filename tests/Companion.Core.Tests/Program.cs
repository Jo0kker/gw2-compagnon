using Companion.Core;
using Companion.Desktop;

var tests = new (string Name, Action Run)[]
{
    ("Duplication indépendante des profils, pages et filtres", () =>
    {
        var original = Profile.Create("Source", true);
        var copy = original.Duplicate("Copie");
        Check(original.Id != copy.Id && original.Pages[0].Id != copy.Pages[0].Id);
        var w = copy.Pages[0].Widgets[0];
        copy.Pages[0].Widgets[0] = w with { Settings = w.Settings with { Subgroup = 2 } };
        Check(original.Pages[0].Widgets[0].Settings.Subgroup is null);
        Check(original.Pages[0].Widgets[0].Id != w.Id);
        var edge = w with { Layout = w.Layout with { X = 10000, Y = 10000 } };
        Check(edge.Duplicate().Layout.X == 10000 && edge.Duplicate().Layout.Y == 10000);
    }),
    ("DPS exact, filtres indépendants, durée inconnue sans faux zéro", () =>
    {
        var combat = CombatReplay.Read([new(1, 1, "A", 1, 1000), new(2, 2, "B", 2, 3000)], 2000);
        Check(combat.Select(new(1))[0].Dps == 500);
        Check(combat.Select(new(2))[0].Dps == 1500);
        Check(combat.Select(new())[0].Player == "B");
        Check(CombatReplay.Read([new(1, 0, "A", 1, 0)], 0).Players[0].Dps is null);
        Check(CombatReplay.Read([], 1000).Players.Count == 0);
    }),
    ("Doublons ignorés, pertes et interruption visibles", () =>
    {
        var a = new DamageEvent(1, 1, "A", 1, 100);
        var full = CombatReplay.Read([a, a], 1000);
        Check(!full.Partial && full.Players[0].Damage == 100);
        Check(CombatReplay.Read([a, new(3, 2, "A", 1, 200)], 1000).Partial);
        Check(CombatReplay.Read([a], 1000, interrupted: true).Partial);
    }),
    ("Stockage, sauvegarde précédente, source inconnue conservée", () => WithDirectory(dir =>
    {
        var path = Path.Combine(dir, "profiles.json");
        var store = new ProfileStore(path);
        var state = Workspace.CreateDefault() with { ReplayEnabled = false };
        var w = state.Profiles[0].Pages[0].Widgets[0];
        state.Profiles[0].Pages[0].Widgets[0] = w with { Kind = "missing.integration", Hidden = true };
        store.Save(state);
        store.Save(state with { ReplayEnabled = true });
        var restored = store.Load();
        Check(restored.ActiveProfileId == state.ActiveProfileId);
        Check(restored.Profiles[0].Pages[0].Widgets[0].Kind == "missing.integration");
        Check(restored.Profiles[0].Pages[0].Widgets[0].Hidden && restored.ReplayEnabled);
        Check(File.Exists(path + ".bak"));
        Check(!new ProfileStore(path + ".bak").Load().ReplayEnabled);
    })),
    ("Validation refusée sans écraser le fichier existant", () => WithDirectory(dir =>
    {
        var path = Path.Combine(dir, "profiles.json"); var store = new ProfileStore(path);
        var state = Workspace.CreateDefault(); store.Save(state); var before = File.ReadAllText(path);
        Throws(() => store.Save(state with { Version = 99 }));
        Check(before == File.ReadAllText(path));
        var w = state.Profiles[0].Pages[0].Widgets[0];
        state.Profiles[0].Pages[0].Widgets[0] = w with { Layout = w.Layout with { X = double.NaN } };
        Throws(() => store.Save(state)); Check(before == File.ReadAllText(path));
    })),
    ("Pages, navigation et désactivation conservent les réglages", () => WithDirectory(dir =>
    {
        var path = Path.Combine(dir, "profiles.json"); var store = new ProfileStore(path);
        var vm = new DashboardViewModel(store, Workspace.CreateDefault(), path);
        var firstPage = vm.Page; var firstId = vm.Widgets[0].Value.Id;
        vm.Widgets[0].Group = 2;
        vm.ReplayEnabled = false;
        Check(vm.Widgets[0].Data.Count == 0 && vm.Widgets[0].Group == 2);
        vm.AddPage(); Check(vm.Widgets.Count == 0);
        vm.AddWidget("damage-players"); vm.Widgets[0].Group = 3;
        vm.Page = firstPage;
        Check(vm.Widgets[0].Group == 2 && vm.Widgets[0].Value.Id == firstId);
        vm.ReplayEnabled = true;
        Check(vm.Widgets[0].Data.Count == 2);
        vm.NewProfile(true); vm.Widgets[0].Group = 4; vm.Save();
        var restored = store.Load();
        Check(restored.Profiles[0].Pages[0].Widgets[0].Settings.Subgroup == 2);
        Check(restored.Profiles[1].Pages[0].Widgets[0].Settings.Subgroup == 4);
        vm.DeleteProfile(); Check(vm.Profiles.Count == 1);
        vm.DeleteProfile(); Check(vm.Profiles.Count == 1 && vm.Widgets.Count == 0);
    })),
    ("Profil portable : aller-retour sans identité machine ni référence partagée", () =>
    {
        var original = Profile.Create("Portable", true);
        var widget = original.Pages[0].Widgets[0];
        original.Pages[0].Widgets[0] = widget with { Hidden = true, Settings = new(2, 15) };
        var bytes = ProfileTransfer.Export(original);
        using var document = System.Text.Json.JsonDocument.Parse(bytes);
        Check(document.RootElement.EnumerateObject().Select(p => p.Name).Order().SequenceEqual(new[] { "Name", "Pages", "Version" }));
        var imported = ProfileTransfer.Import(bytes);
        Check(imported.Profile.Id != original.Id && imported.Profile.Pages[0].Id != original.Pages[0].Id);
        var copy = imported.Profile.Pages[0].Widgets[0];
        Check(copy.Id != widget.Id && copy.Hidden && copy.Settings == new WidgetSettings(2, 15));
        Check(copy.Layout == widget.Layout && imported.MissingWidgetKinds.Count == 0);
        imported.Profile.Pages[0].Widgets.Clear();
        Check(original.Pages[0].Widgets.Count == 2);
    }),
    ("Import : widgets inconnus conservés, formats dangereux ou futurs refusés", () =>
    {
        var profile = Profile.Create("Manquant", true);
        var widget = profile.Pages[0].Widgets[0];
        profile.Pages[0].Widgets[0] = widget with { Kind = "third-party.unknown", Settings = new(9, 50) };
        var bytes = ProfileTransfer.Export(profile);
        var imported = ProfileTransfer.Import(bytes);
        Check(imported.MissingWidgetKinds.Single() == "third-party.unknown");
        Check(imported.Profile.Pages[0].Widgets[0].Layout == widget.Layout);
        Check(imported.Profile.Pages[0].Widgets[0].Settings == new WidgetSettings());
        var text = System.Text.Encoding.UTF8.GetString(bytes);
        Throws(() => ProfileTransfer.Import(System.Text.Encoding.UTF8.GetBytes(text.Replace("\"Version\": 1", "\"Version\": 99"))));
        Throws(() => ProfileTransfer.Import(System.Text.Encoding.UTF8.GetBytes(text.Replace("\"Version\": 1", "\"token\": \"secret-sentinel\", \"Version\": 1"))));
        Throws(() => ProfileTransfer.Import(new byte[ProfileTransfer.MaxBytes + 1]));
        Throws(() => ProfileTransfer.Import("{}"u8));
    }),
    ("Pages : renommer, dupliquer et supprimer sans altérer les autres pages", () => WithDirectory(dir =>
    {
        var path = Path.Combine(dir, "profiles.json"); var store = new ProfileStore(path);
        var vm = new DashboardViewModel(store, Workspace.CreateDefault(), path);
        vm.Widgets[0].Group = 2; vm.RenamePage("Escouade"); var first = vm.Page;
        vm.DuplicatePage(); Check(vm.Page.Id != first.Id && vm.PageName == "Escouade — copie");
        vm.Widgets[0].Group = 3; vm.Save();
        Check(first.Widgets[0].Settings.Subgroup == 2);
        vm.DeletePage(); Check(vm.Pages.Count == 1 && vm.Page.Id == first.Id);
        vm.DeletePage(); Check(vm.Pages.Count == 1 && vm.Widgets.Count == 0);
        Check(store.Load().Profiles[0].Pages.Count == 1);
    })),
    ("Import répété indépendant et lecture de fichier bornée", () => WithDirectory(dir =>
    {
        var path = Path.Combine(dir, "profiles.json"); var store = new ProfileStore(path);
        var vm = new DashboardViewModel(store, Workspace.CreateDefault(), path);
        var portable = Path.Combine(dir, "portable.json"); File.WriteAllBytes(portable, vm.ExportProfile());
        var imported = ProfileTransfer.ReadFile(portable);
        vm.ImportProfile(imported); var first = vm.Selected;
        vm.ImportProfile(imported); Check(vm.Selected.Id != first.Id && vm.Profiles.Count == 3);
        store.Load().Validate();
        File.WriteAllBytes(portable, new byte[ProfileTransfer.MaxBytes + 1]);
        Throws(() => ProfileTransfer.ReadFile(portable));
        Check(vm.Profiles.Count == 3);
    })),
    ("Fichier invalide ou futur jamais remplacé silencieusement", () => WithDirectory(dir =>
    {
        var path = Path.Combine(dir, "profiles.json"); File.WriteAllText(path, "{invalid");
        try { new ProfileStore(path).Load(); throw new Exception("Parse attendu en erreur"); }
        catch (System.Text.Json.JsonException) { }
        Check(File.ReadAllText(path) == "{invalid");
    }))
};
var failures = 0;
foreach (var (name, run) in tests)
{
    try { run(); Console.WriteLine($"PASS {name}"); }
    catch (Exception error) { failures++; Console.Error.WriteLine($"FAIL {name}: {error.Message}"); }
}
var bridgeTests = await BridgeTests.RunAsync();
failures += bridgeTests.Failed;
var count = tests.Length + bridgeTests.Total;
Console.WriteLine($"{count - failures}/{count} scénarios réussis.");
return failures == 0 ? 0 : 1;

static void Check(bool condition) { if (!condition) throw new Exception("Assertion échouée"); }
static void Throws(Action action) { try { action(); } catch (InvalidDataException) { return; } throw new Exception("Validation attendue en erreur"); }
static void WithDirectory(Action<string> action)
{
    var dir = Path.Combine(Path.GetTempPath(), "gw2-tests-" + Guid.NewGuid()); Directory.CreateDirectory(dir);
    try { action(dir); } finally { Directory.Delete(dir, true); }
}
