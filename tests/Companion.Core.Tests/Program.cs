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
Console.WriteLine($"{tests.Length - failures}/{tests.Length} scénarios réussis.");
return failures == 0 ? 0 : 1;

static void Check(bool condition) { if (!condition) throw new Exception("Assertion échouée"); }
static void Throws(Action action) { try { action(); } catch (InvalidDataException) { return; } throw new Exception("Validation attendue en erreur"); }
static void WithDirectory(Action<string> action)
{
    var dir = Path.Combine(Path.GetTempPath(), "gw2-tests-" + Guid.NewGuid()); Directory.CreateDirectory(dir);
    try { action(dir); } finally { Directory.Delete(dir, true); }
}
