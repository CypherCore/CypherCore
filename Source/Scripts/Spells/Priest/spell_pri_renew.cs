using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(139)]
public class spell_pri_renew : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	public override bool Load()
	{
		return GetCaster() && GetCaster().GetTypeId() == TypeId.Player;
	}

	private void HandleApplyEffect(AuraEffect aurEff, AuraEffectHandleModes UnnamedParameter)
	{
		Unit caster = GetCaster();
		if (caster != null)
		{
			// Reduse the GCD of Holy Word: Sanctify by 2 seconds
			if (caster.GetSpellHistory().HasCooldown(PriestSpells.SPELL_PRIEST_HOLY_WORD_SANCTIFY))
			{
				caster.GetSpellHistory().ModifyCooldown(PriestSpells.SPELL_PRIEST_HOLY_WORD_SANCTIFY, TimeSpan.FromSeconds(-2 * Time.InMilliseconds));
			}

			// Divine Touch
			AuraEffect empoweredRenewAurEff = caster.GetAuraEffect(PriestSpellIcons.PRIEST_ICON_ID_DIVINE_TOUCH_TALENT, 0);
			if (empoweredRenewAurEff != null)
			{
				uint heal = caster.SpellHealingBonusDone(GetTarget(), GetSpellInfo(), (uint)aurEff.GetAmount(), DamageEffectType.DOT, aurEff.GetSpellEffectInfo());
				heal = GetTarget().SpellHealingBonusTaken(caster, GetSpellInfo(), heal, DamageEffectType.DOT);
				var                basepoints0 = MathFunctions.CalculatePct((int)heal * aurEff.GetTotalTicks(), empoweredRenewAurEff.GetAmount());
				CastSpellExtraArgs args        = new CastSpellExtraArgs();
				args.AddSpellMod(SpellValueMod.BasePoint0, (int)basepoints0);
				args.SetTriggerFlags(TriggerCastFlags.FullMask);
				args.SetTriggeringAura(aurEff);
				caster.CastSpell(GetTarget(), PriestSpells.SPELL_PRIEST_DIVINE_TOUCH, args);
			}
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleApplyEffect, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.RealOrReapplyMask));
	}
}