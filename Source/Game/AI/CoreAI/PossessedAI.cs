using Framework.Constants;
using Game.Entities;

namespace Game.AI;

public class PossessedAI : CreatureAI
{
	public PossessedAI(Creature creature) : base(creature)
	{
		creature.SetReactState(ReactStates.Passive);
	}

	public override void AttackStart(Unit target)
	{
		me.Attack(target, true);
	}

	public override void UpdateAI(uint diff)
	{
		if (me.GetVictim() != null)
		{
			if (!me.IsValidAttackTarget(me.GetVictim()))
				me.AttackStop();
			else
				DoMeleeAttackIfReady();
		}
	}

	public override void JustDied(Unit unit)
	{
		// We died while possessed, disable our loot
		me.RemoveDynamicFlag(UnitDynFlags.Lootable);
	}

	public override void MoveInLineOfSight(Unit who)
	{
	}

	public override void JustEnteredCombat(Unit who)
	{
		EngagementStart(who);
	}

	public override void JustExitedCombat()
	{
		EngagementOver();
	}

	public override void JustStartedThreateningMe(Unit who)
	{
	}

	public override void EnterEvadeMode(EvadeReason why)
	{
	}
}