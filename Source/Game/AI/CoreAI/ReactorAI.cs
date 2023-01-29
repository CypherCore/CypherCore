using Game.Entities;

namespace Game.AI;

public class ReactorAI : CreatureAI
{
    public ReactorAI(Creature c) : base(c)
    {
    }

    public override void MoveInLineOfSight(Unit who)
    {
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        DoMeleeAttackIfReady();
    }
}