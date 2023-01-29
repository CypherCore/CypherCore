using Framework.Constants;
using Game.Entities;

namespace Game.AI;

public class CritterAI : PassiveAI
{
	public CritterAI(Creature c) : base(c)
	{
		me.SetReactState(ReactStates.Passive);
	}

	public override void JustEngagedWith(Unit who)
	{
		if (!me.HasUnitState(UnitState.Fleeing))
			me.SetControlled(true, UnitState.Fleeing);
	}

	public override void MovementInform(MovementGeneratorType type, uint id)
	{
		if (type == MovementGeneratorType.TimedFleeing)
			EnterEvadeMode(EvadeReason.Other);
	}

	public override void EnterEvadeMode(EvadeReason why)
	{
		if (me.HasUnitState(UnitState.Fleeing))
			me.SetControlled(false, UnitState.Fleeing);

		base.EnterEvadeMode(why);
	}
}