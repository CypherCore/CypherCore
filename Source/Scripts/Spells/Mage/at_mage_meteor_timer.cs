// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Mage;

[Script]
public class at_mage_meteor_timer : AreaTriggerAI
{
	public at_mage_meteor_timer(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	public override void OnCreate()
	{
		var caster = at.GetCaster();

		if (caster == null)
			return;

		var tempSumm = caster.SummonCreature(12999, at.GetPosition(), TempSummonType.TimedDespawn, TimeSpan.FromSeconds(5));

		if (tempSumm != null)
		{
			tempSumm.SetFaction(caster.GetFaction());
			tempSumm.SetSummonerGUID(caster.GetGUID());
			PhasingHandler.InheritPhaseShift(tempSumm, caster);
			caster.CastSpell(tempSumm, MageSpells.SPELL_MAGE_METEOR_VISUAL, true);
		}
	}

	public override void OnRemove()
	{
		var caster = at.GetCaster();

		if (caster == null)
			return;

		var tempSumm = caster.SummonCreature(12999, at.GetPosition(), TempSummonType.TimedDespawn, TimeSpan.FromSeconds(5));

		if (tempSumm != null)
		{
			tempSumm.SetFaction(caster.GetFaction());
			tempSumm.SetSummonerGUID(caster.GetGUID());
			PhasingHandler.InheritPhaseShift(tempSumm, caster);
			caster.CastSpell(tempSumm, MageSpells.SPELL_MAGE_METEOR_DAMAGE, true);
		}
	}
}