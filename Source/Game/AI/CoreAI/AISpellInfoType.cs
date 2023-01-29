using System;
using Framework.Constants;

namespace Game.AI;

public class AISpellInfoType
{
	public AICondition Condition { get; set; }
	public TimeSpan Cooldown;
	public byte Effects { get; set; } // set of enum SelectEffect
	public float MaxRange { get; set; }
	public TimeSpan RealCooldown;

	public AITarget Target { get; set; }

	public byte Targets { get; set; } // set of enum SelectTarget

	public AISpellInfoType()
	{
		Target    = AITarget.Self;
		Condition = AICondition.Combat;
		Cooldown  = TimeSpan.FromMilliseconds(SharedConst.AIDefaultCooldown);
	}
}