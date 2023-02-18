// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Monk;

[CreatureScript(new uint[]
                {
	                69791, 69792
                })]
public class npc_monk_sef_spirit : ScriptedAI
{
	public npc_monk_sef_spirit(Creature creature) : base(creature)
	{
	}

	public override void IsSummonedBy(WorldObject summoner)
	{
		me.SetLevel(summoner.ToUnit().GetLevel());
		me.SetMaxHealth(summoner.ToUnit().GetMaxHealth() / 3);
		me.SetFullHealth();
		summoner.CastSpell(me, MonkSpells.TRANSCENDENCE_CLONE_TARGET, true);
		me.CastSpell(me, me.GetEntry() == StormEarthAndFireSpells.NPC_FIRE_SPIRIT ? StormEarthAndFireSpells.SEF_FIRE_VISUAL : StormEarthAndFireSpells.SEF_EARTH_VISUAL, true);
		me.CastSpell(me, StormEarthAndFireSpells.SEF_SUMMONS_STATS, true);
		var attackPower = summoner.ToUnit().m_unitData.AttackPower / 100 * 45.0f;
		var spellPower  = summoner.ToUnit().SpellBaseDamageBonusDone(SpellSchoolMask.Nature) / 100 * 45.0f;

		var target = ObjectAccessor.Instance.GetUnit(summoner, summoner.ToUnit().GetTarget());

		if (target != null)
		{
			me.CastSpell(target, StormEarthAndFireSpells.SEF_CHARGE, true);
		}
		else
		{
			if (me.GetEntry() == StormEarthAndFireSpells.NPC_FIRE_SPIRIT)
				me.GetMotionMaster().MoveFollow(summoner.ToUnit(), SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
			else
				me.GetMotionMaster().MoveFollow(summoner.ToUnit(), SharedConst.PetFollowDist, SharedConst.PetFollowAngle * 3);
		}
	}
}