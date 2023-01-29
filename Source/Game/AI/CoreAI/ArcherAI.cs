using Framework.Constants;
using Game.Entities;

namespace Game.AI;

public class ArcherAI : CreatureAI
{
	private float _minRange;

	public ArcherAI(Creature creature) : base(creature)
	{
		if (creature.Spells[0] == 0)
			Log.outError(LogFilter.ScriptsAi, $"ArcherAI set for creature with spell1=0. AI will do nothing ({me.GetGUID()})");

		var spellInfo = Global.SpellMgr.GetSpellInfo(creature.Spells[0], creature.GetMap().GetDifficultyID());
		_minRange = spellInfo != null ? spellInfo.GetMinRange(false) : 0;

		if (_minRange == 0)
			_minRange = SharedConst.MeleeRange;

		creature.CombatDistance = spellInfo != null ? spellInfo.GetMaxRange(false) : 0;
		creature.SightDistance  = creature.CombatDistance;
	}

	public override void AttackStart(Unit who)
	{
		if (who == null)
			return;

		if (me.IsWithinCombatRange(who, _minRange))
		{
			if (me.Attack(who, true) &&
			    !who.IsFlying())
				me.GetMotionMaster().MoveChase(who);
		}
		else
		{
			if (me.Attack(who, false) &&
			    !who.IsFlying())
				me.GetMotionMaster().MoveChase(who, me.CombatDistance);
		}

		if (who.IsFlying())
			me.GetMotionMaster().MoveIdle();
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		if (!me.IsWithinCombatRange(me.GetVictim(), _minRange))
			DoSpellAttackIfReady(me.Spells[0]);
		else
			DoMeleeAttackIfReady();
	}
}