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
			player.CastSpell(ShatteredSoulsSpells.SPELL_DH_SHATTERED_SOULS_MISSILE, true);
			victim.CastSpell(ShatteredSoulsSpells.SPELL_DH_SHATTERED_SOULS_DEMON, true);     //at
			player.CastSpell(ShatteredSoulsSpells.SPELL_DH_SOUL_FRAGMENT_DEMON_BONUS, true); //buff
		}

		if (victim.GetCreatureType() != CreatureType.Demon && RandomHelper.randChance(30))
		{
			victim.CastSpell(ShatteredSoulsSpells.SPELL_DH_SHATTERED_SOULS_MISSILE, true);
			player.CastSpell(fragmentPos, ShatteredSoulsSpells.SPELL_DH_SHATTERED_SOULS, true); //10665
		}

		if (player.HasAura(DemonHunterSpells.SPELL_DH_FEED_THE_DEMON))
			player.GetSpellHistory().ModifyCooldown(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_DEMON_SPIKES, Difficulty.None).ChargeCategoryId, TimeSpan.FromMilliseconds(-1000));

		if (player.HasAura(ShatteredSoulsSpells.SPELL_DH_PAINBRINGER))
			player.CastSpell(player, ShatteredSoulsSpells.SPELL_DH_PAINBRINGER_BUFF, true);

		var soulBarrier = player.GetAuraEffect(DemonHunterSpells.SPELL_DH_SOUL_BARRIER, 0);

		if (soulBarrier != null)
		{
			var amount = soulBarrier.GetAmount() + ((float)(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_SOUL_BARRIER, Difficulty.None).GetEffect(1).BasePoints) / 100.0f) * player.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack);
			soulBarrier.SetAmount(amount);
		}
	}
}