using System;
using Framework.Constants;

namespace Game.AI;

public class AISpellInfoType
{
    public TimeSpan Cooldown;
    public TimeSpan RealCooldown;

    public AISpellInfoType()
    {
        Target = AITarget.Self;
        Condition = AICondition.Combat;
        Cooldown = TimeSpan.FromMilliseconds(SharedConst.AIDefaultCooldown);
    }

    public AICondition Condition { get; set; }
    public byte Effects { get; set; } // set of enum SelectEffect
    public float MaxRange { get; set; }

    public AITarget Target { get; set; }

    public byte Targets { get; set; } // set of enum SelectTarget
}