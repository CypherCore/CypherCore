using Framework.Collections;
using Framework.Constants;

namespace Game.Entities;

public class CreatureLocale
{
    public StringArray Name { get; set; } = new((int)Locale.Total);
    public StringArray NameAlt { get; set; } = new((int)Locale.Total);
    public StringArray Title { get; set; } = new((int)Locale.Total);
    public StringArray TitleAlt { get; set; } = new((int)Locale.Total);
}