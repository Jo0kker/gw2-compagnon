using static Companion.Core.Localization.Text;
namespace Companion.Core;

public sealed record WidgetSettings(int? Subgroup = null, int Rows = 8);
public sealed record WidgetLayout(double X, double Y, double Width, double Height);
public sealed record Widget(Guid Id, string Kind, WidgetSettings Settings, WidgetLayout Layout, bool Hidden = false)
{
    public Widget Duplicate() => this with
    {
        Id = Guid.NewGuid(), Settings = Settings with { },
        Layout = Layout with { X = Math.Min(Layout.X + 16, 10000), Y = Math.Min(Layout.Y + 16, 10000) }
    };
}
public sealed record ProfilePage(Guid Id, string Name, List<Widget> Widgets);
public sealed record Profile(Guid Id, string Name, List<ProfilePage> Pages)
{
    public Profile Duplicate(string name) => new(Guid.NewGuid(), name, Pages.Select(p =>
        new ProfilePage(Guid.NewGuid(), p.Name, p.Widgets.Select(w => w.Duplicate() with { Layout = w.Layout with { } }).ToList())).ToList());

    public static Profile Create(string name, bool template = false) => new(Guid.NewGuid(), name,
    [new(Guid.NewGuid(), T("Overview"), template ?
    [new(Guid.NewGuid(), "damage-players", new(), new(0, 0, 760, 480)),
     new(Guid.NewGuid(), "damage-groups", new(), new(776, 0, 400, 480))] : [])]);
}

public sealed record Workspace(int Version, Guid ActiveProfileId, List<Profile> Profiles, bool ReplayEnabled = false)
{
    public static Workspace CreateDefault()
    {
        var profile = Profile.Create(T("WvwProfile"), template: true);
        return new(1, profile.Id, [profile], ReplayEnabled: true);
    }

    public void Validate()
    {
        if (Version != 1) throw new InvalidDataException(T("InvalidProfileVersion"));
        if (Profiles is null || Profiles.Count is < 1 or > 100 || !Profiles.Any(p => p?.Id == ActiveProfileId))
            throw new InvalidDataException(T("InvalidProfileList"));
        var ids = new HashSet<Guid>();
        void Id(Guid id) { if (id == Guid.Empty || !ids.Add(id)) throw new InvalidDataException(T("InvalidId")); }
        void Name(string name) { if (string.IsNullOrWhiteSpace(name) || name.Length > 80) throw new InvalidDataException(T("InvalidName")); }
        foreach (var p in Profiles)
        {
            if (p is null) throw new InvalidDataException(T("EmptyProfile"));
            Id(p.Id); Name(p.Name);
            if (p.Pages is null || p.Pages.Count is < 1 or > 50) throw new InvalidDataException(T("InvalidPages"));
            foreach (var page in p.Pages)
            {
                if (page is null) throw new InvalidDataException(T("EmptyPage"));
                Id(page.Id); Name(page.Name);
                if (page.Widgets is null || page.Widgets.Count > 200) throw new InvalidDataException(T("TooManyWidgets"));
                foreach (var w in page.Widgets)
                {
                    if (w is null || w.Settings is null || w.Layout is null) throw new InvalidDataException(T("InvalidWidget"));
                    Id(w.Id);
                    if (string.IsNullOrWhiteSpace(w.Kind) || w.Kind.Length > 100 || w.Settings.Rows is < 1 or > 50 ||
                        (w.Settings.Subgroup is not null && w.Settings.Subgroup is < 1 or > 15))
                        throw new InvalidDataException(T("InvalidWidgetSettings"));
                    var l = w.Layout;
                    if (!double.IsFinite(l.X + l.Y + l.Width + l.Height) || l.X < 0 || l.Y < 0 || l.X > 10000 || l.Y > 10000 ||
                        l.Width is < 320 or > 4000 || l.Height is < 240 or > 4000)
                        throw new InvalidDataException(T("InvalidLayout"));
                }
            }
        }
    }
}
