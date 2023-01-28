using Framework.Collections;
using Framework.Constants;

namespace Game.Misc;

public class PageTextLocale
{
    public StringArray Text { get; set; } = new((int)Locale.Total);
}