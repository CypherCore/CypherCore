// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	//190456 - Ignore Pain
	[SpellScript(190456)]
	public class aura_warr_ignore_pain : AuraScript, IHasAuraEffects
	{
		private int m_ExtraSpellCost;

		public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

		public override bool Load()
		{
			var caster = GetCaster();
			// In this phase the initial 20 Rage cost is removed already
			// We just check for bonus.
			m_ExtraSpellCost = Math.Min(caster.GetPower(PowerType.Rage), 400);

			return true;
		}

		private void CalcAmount(AuraEffect UnnamedParameter, ref double amount, ref bool UnnamedParameter2)
		{
			var caster = GetCaster();

			if (caster != null)
			{
				amount = (int)((double)(22.3f * caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack)) * ((double)(m_ExtraSpellCost + 200) / 600.0f));
				var m_newRage = caster.GetPower(PowerType.Rage) - m_ExtraSpellCost;

				if (m_newRage < 0)
					m_newRage = 0;

				caster.SetPower(PowerType.Rage, m_newRage);
				/*if (Player* player = caster->ToPlayer())
				    player->SendPowerUpdate(PowerType.Rage, m_newRage);*/
			}
		}

		private void OnAbsorb(AuraEffect UnnamedParameter, DamageInfo dmgInfo, ref double UnnamedParameter2)
		{
			var caster = GetCaster();

			if (caster != null)
			{
				var spell = new SpellNonMeleeDamage(caster, caster, GetSpellInfo(), new SpellCastVisual(0, 0), SpellSchoolMask.Normal);
				spell.damage = dmgInfo.GetDamage() - dmgInfo.GetDamage() * 0.9f;
				spell.cleanDamage = spell.damage;
				caster.DealSpellDamage(spell, false);
				caster.SendSpellNonMeleeDamageLog(spell);
			}
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAmount, 0, AuraType.SchoolAbsorb));
			AuraEffects.Add(new AuraEffectAbsorbHandler(OnAbsorb, 0));
		}
	}
}