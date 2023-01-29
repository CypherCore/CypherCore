using Framework.Constants;
using Game.Entities;

namespace Game.AI;

public class NullCreatureAI : CreatureAI
{
    public NullCreatureAI(Creature creature) : base(creature)
    {
        creature.SetReactState(ReactStates.Passive);
    }

    public override void MoveInLineOfSight(Unit unit)
    {
    }

    public override void AttackStart(Unit unit)
    {
    }

    public override void JustStartedThreateningMe(Unit unit)
    {
    }

    public override void JustEnteredCombat(Unit who)
    {
    }

    public override void UpdateAI(uint diff)
    {
    }

    public override void JustAppeared()
    {
    }

    public override void EnterEvadeMode(EvadeReason why)
    {
    }

    public override void OnCharmed(bool isNew)
    {
    }
}