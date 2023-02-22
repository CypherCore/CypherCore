// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IPlayer;

namespace Scripts.Spells.DemonHunter;

[Script]
public class dh_shattered_souls : ScriptObjectAutoAdd, IPlayerOnCreatureKill
{
	public dh_shattered_souls() : base("dh_shattered_souls")
	{
	}

	public void OnCreatureKill(Player player, Creature victim)
	{
		if (player.GetClass() != Class.DemonHunter)
			return;

		var fragmentPos = victim.GetRandomNearPosition(5.0f);

		if (victim.GetCreatureType() == CreatureType.Demon && RandomHelper.randChance(30))
		{
			player.CastSpell(ShatteredSoulsSpells.SHATTERED_SOULS_MISSILE, true);
			victim.CastSpell(ShatteredSoulsSpells.SHATTERED_SOULS_DEMON, true);     //at
			player.CastSpell(ShatteredSoulsSpells.SOUL_FRAGMENT_DEMON_BONUS, true); //buff
		}

		if (victim.GetCreatureType() != CreatureType.Demon && RandomHelper.randChance(30))
		{
			victim.CastSpell(ShatteredSoulsSpells.SHATTERED_SOULS_MISSILE, true);
			player.CastSpell(fragmentPos, ShatteredSoulsSpells.SHATTERED_SOULS, true); //10665
		}

		if (player.HasAura(DemonHunterSpells.FEED_THE_DEMON))
			player.GetSpellHistory().ModifyCooldown(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.DEMON_SPIKES, Difficulty.None).ChargeCategoryId, TimeSpan.FromMilliseconds(-1000));

		if (player.HasAura(ShatteredSoulsSpells.PAINBRINGER))
			player.CastSpell(player, ShatteredSoulsSpells.PAINBRINGER_BUFF, true);

		var soulBarrier = player.GetAuraEffect(DemonHunterSpells.SOUL_BARRIER, 0);

		if (soulBarrier != null)
		{
			var amount = soulBarrier.GetAmount() + ((double)(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SOUL_BARRIER, Difficulty.None).GetEffect(1).BasePoints) / 100.0f) * player.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack);
			soulBarrier.SetAmount(amount);
		}
	}
}