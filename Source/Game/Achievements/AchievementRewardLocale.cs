using Framework.Collections;
using Framework.Constants;

namespace Game.Achievements;

public class AchievementRewardLocale
{
    public StringArray Body { get; set; } = new((int)Locale.Total);
    public StringArray Subject { get; set; } = new((int)Locale.Total);
}