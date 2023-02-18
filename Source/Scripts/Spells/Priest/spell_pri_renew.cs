// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(139)]
public class spell_pri_renew : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	public override bool Load()
	{
		return GetCaster() && GetCaster().GetTypeId() == TypeId.Player;
	}

	private void HandleApplyEffect(AuraEffect aurEff, AuraEffectHandleModes UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster != null)
		{
			// Reduse the GCD of Holy Word: Sanctify by 2 seconds
			if (caster.GetSpellHistory().HasCooldown(PriestSpells.HOLY_WORD_SANCTIFY))
				caster.GetSpellHistory().ModifyCooldown(PriestSpells.HOLY_WORD_SANCTIFY, TimeSpan.FromSeconds(-2 * Time.InMilliseconds));

			// Divine Touch
			var empoweredRenewAurEff = caster.GetAuraEffect(PriestSpellIcons.PRIEST_ICON_ID_DIVINE_TOUCH_TALENT, 0);

			if (empoweredRenewAurEff != null)
			{
				var heal = caster.SpellHealingBonusDone(GetTarget(), GetSpellInfo(), (uint)aurEff.GetAmount(), DamageEffectType.DOT, aurEff.GetSpellEffectInfo());
				heal = GetTarget().SpellHealingBonusTaken(caster, GetSpellInfo(), heal, DamageEffectType.DOT);
				var basepoints0 = MathFunctions.CalculatePct((int)heal * aurEff.GetTotalTicks(), empoweredRenewAurEff.GetAmount());
				var args        = new CastSpellExtraArgs();
				args.AddSpellMod(SpellValueMod.BasePoint0, (int)basepoints0);
				args.SetTriggerFlags(TriggerCastFlags.FullMask);
				args.SetTriggeringAura(aurEff);
				caster.CastSpell(GetTarget(), PriestSpells.DIVINE_TOUCH, args);
			}
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleApplyEffect, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.RealOrReapplyMask));
	}
}