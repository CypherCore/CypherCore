using System;

namespace Game.Entities;

public class CreatureBaseStats
{
    public uint AttackPower { get; set; }
    public uint BaseMana { get; set; }
    public uint RangedAttackPower { get; set; }

    // Helpers
    public uint GenerateMana(CreatureTemplate info)
    {
        // Mana can be 0.
        if (BaseMana == 0)
            return 0;

        return (uint)Math.Ceiling(BaseMana * info.ModMana * info.ModManaExtra);
    }
}