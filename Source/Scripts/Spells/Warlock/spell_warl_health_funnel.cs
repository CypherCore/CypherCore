using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	[SpellScript(755)] // 755 - Health Funnel
	internal class spell_warl_health_funnel : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(ApplyEffect, 0, AuraType.ObsModHealth, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
			AuraEffects.Add(new AuraEffectApplyHandler(RemoveEffect, 0, AuraType.ObsModHealth, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
			AuraEffects.Add(new AuraEffectPeriodicHandler(OnPeriodic, 0, AuraType.ObsModHealth));
		}

		private void ApplyEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
		{
			var caster = GetCaster();

			if (!caster)
				return;

			var target = GetTarget();

			if (caster.HasAura(WarlockSpells.IMPROVED_HEALTH_FUNNEL_R2))
				target.CastSpell(target, WarlockSpells.IMPROVED_HEALTH_FUNNEL_BUFF_R2, true);
			else if (caster.HasAura(WarlockSpells.IMPROVED_HEALTH_FUNNEL_R1))
				target.CastSpell(target, WarlockSpells.IMPROVED_HEALTH_FUNNEL_BUFF_R1, true);
		}

		private void RemoveEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
		{
			var target = GetTarget();
			target.RemoveAurasDueToSpell(WarlockSpells.IMPROVED_HEALTH_FUNNEL_BUFF_R1);
			target.RemoveAurasDueToSpell(WarlockSpells.IMPROVED_HEALTH_FUNNEL_BUFF_R2);
		}

		private void OnPeriodic(AuraEffect aurEff)
		{
			var caster = GetCaster();

			if (!caster)
				return;

			//! HACK for self Damage, is not blizz :/
			var damage = (uint)caster.CountPctFromMaxHealth(aurEff.GetBaseAmount());

			var modOwner = caster.GetSpellModOwner();

			if (modOwner)
				modOwner.ApplySpellMod(GetSpellInfo(), SpellModOp.PowerCost0, ref damage);

			SpellNonMeleeDamage damageInfo = new(caster, caster, GetSpellInfo(), GetAura().GetSpellVisual(), GetSpellInfo().SchoolMask, GetAura().GetCastId());
			damageInfo.periodicLog = true;
			damageInfo.damage      = damage;
			caster.DealSpellDamage(damageInfo, false);
			caster.SendSpellNonMeleeDamageLog(damageInfo);
		}
	}
}