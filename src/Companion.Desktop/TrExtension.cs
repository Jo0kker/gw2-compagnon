using System.Windows.Markup;
using static Companion.Core.Localization.Text;

namespace Companion.Desktop;

[MarkupExtensionReturnType(typeof(string))]
public sealed class TrExtension(string key) : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => T(key);
}
