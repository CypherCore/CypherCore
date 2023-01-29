using Game.Entities;

namespace Game.AI;

public class TurretAI : CreatureAI
{
    private readonly float _minRange;

    public TurretAI(Creature creature) : base(creature)
    {
        if (creature.Spells[0] == 0)
            Log.outError(LogFilter.Server, $"TurretAI set for creature with spell1=0. AI will do nothing ({creature.GetGUID()})");

        var spellInfo = Global.SpellMgr.GetSpellInfo(creature.Spells[0], creature.GetMap().GetDifficultyID());
        _minRange = spellInfo != null ? spellInfo.GetMinRange(false) : 0;
        creature.CombatDistance = spellInfo != null ? spellInfo.GetMaxRange(false) : 0;
        creature.SightDistance = creature.CombatDistance;
    }

    public override bool CanAIAttack(Unit victim)
    {
        // todo use one function to replace it
        if (!me.IsWithinCombatRange(victim, me.CombatDistance) ||
            (_minRange != 0 && me.IsWithinCombatRange(victim, _minRange)))
            return false;

        return true;
    }

    public override void AttackStart(Unit victim)
    {
        if (victim != null)
            me.Attack(victim, false);
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        DoSpellAttackIfReady(me.Spells[0]);
    }
}