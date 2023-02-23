// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(263648)]
public class spell_dh_soul_barrier : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	private void CalcAmount(AuraEffect UnnamedParameter, ref double amount, ref bool UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		var player = caster.ToPlayer();

		if (player != null)
		{
			var coeff          = amount / 100.0f;
			var soulShardCoeff = GetSpellInfo().GetEffect(1).BasePoints / 100.0f;
			var ap             = player.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack);

			amount = (int)(coeff * ap);

			// Consume all soul fragments in 25 yards;
			var fragments = new List<List<AreaTrigger>>();
			fragments.Add(caster.GetAreaTriggers(ShatteredSoulsSpells.SHATTERED_SOULS));
			fragments.Add(caster.GetAreaTriggers(ShatteredSoulsSpells.SHATTERED_SOULS_DEMON));
			fragments.Add(caster.GetAreaTriggers(ShatteredSoulsSpells.LESSER_SOUL_SHARD));
			var range = 25.0f;

			foreach (var vec in fragments)
			{
				foreach (var at in vec)
				{
					if (!caster.IsWithinDist(at, range))
						continue;

					var tempSumm = caster.SummonCreature(SharedConst.WorldTrigger, at.GetPositionX(), at.GetPositionY(), at.GetPositionZ(), 0, TempSummonType.TimedDespawn, TimeSpan.FromSeconds(100));

					if (tempSumm != null)
					{
						tempSumm.SetFaction(caster.GetFaction());
						tempSumm.SetSummonerGUID(caster.GetGUID());
						var bp = 0;

						switch (at.GetTemplate().Id.Id)
						{
							case 6007:
							case 5997:
								bp = (int)ShatteredSoulsSpells.SOUL_FRAGMENT_HEAL_VENGEANCE;

								break;
							case 6710:
								bp = (int)ShatteredSoulsSpells.LESSER_SOUL_SHARD_HEAL;

								break;
						}

						caster.CastSpell(tempSumm, ShatteredSoulsSpells.CONSUME_SOUL_MISSILE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)bp));

						if (at.GetTemplate().Id.Id == 6007)
							caster.CastSpell(caster, ShatteredSoulsSpells.SOUL_FRAGMENT_DEMON_BONUS, true);

						if (caster.HasAura(DemonHunterSpells.FEED_THE_DEMON))
							caster.GetSpellHistory().ModifyCooldown(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.DEMON_SPIKES, Difficulty.None).ChargeCategoryId, TimeSpan.FromMilliseconds(-1000));

						if (caster.HasAura(ShatteredSoulsSpells.PAINBRINGER))
							caster.CastSpell(caster, ShatteredSoulsSpells.PAINBRINGER_BUFF, true);

						amount += (int)(soulShardCoeff * ap);

						at.SetDuration(0);
					}
				}
			}
		}

		var appList = caster.GetAuraApplication(DemonHunterSpells.SOUL_BARRIER);

		if (appList != null)
			foreach (var app in appList)
				app.ClientUpdate();
	}

	private void HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref double absorbAmount)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		var threshold = caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 0.4914f;

		if (absorbAmount < dmgInfo.GetDamage())
			aurEff.SetAmount(absorbAmount + threshold);

		var appList = caster.GetAuraApplication(DemonHunterSpells.SOUL_BARRIER);

		if (appList != null)
			foreach (var app in appList)
				app.ClientUpdate();
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAmount, 0, AuraType.SchoolAbsorb));
		AuraEffects.Add(new AuraEffectAbsorbHandler(HandleAbsorb, 0));
	}
}