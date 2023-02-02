// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(ApplyEffect, 0, AuraType.ObsModHealth, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
            Effects.Add(new EffectApplyHandler(RemoveEffect, 0, AuraType.ObsModHealth, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
            Effects.Add(new EffectPeriodicHandler(OnPeriodic, 0, AuraType.ObsModHealth));
        }

        private void ApplyEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();

            if (!caster)
                return;

            Unit target = GetTarget();

            if (caster.HasAura(SpellIds.IMPROVED_HEALTH_FUNNEL_R2))
                target.CastSpell(target, SpellIds.IMPROVED_HEALTH_FUNNEL_BUFF_R2, true);
            else if (caster.HasAura(SpellIds.IMPROVED_HEALTH_FUNNEL_R1))
                target.CastSpell(target, SpellIds.IMPROVED_HEALTH_FUNNEL_BUFF_R1, true);
        }

        private void RemoveEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.RemoveAurasDueToSpell(SpellIds.IMPROVED_HEALTH_FUNNEL_BUFF_R1);
            target.RemoveAurasDueToSpell(SpellIds.IMPROVED_HEALTH_FUNNEL_BUFF_R2);
        }

        private void OnPeriodic(AuraEffect aurEff)
        {
            Unit caster = GetCaster();

            if (!caster)
                return;

            //! HACK for self Damage, is not blizz :/
            uint damage = (uint)caster.CountPctFromMaxHealth(aurEff.GetBaseAmount());

            Player modOwner = caster.GetSpellModOwner();

            if (modOwner)
                modOwner.ApplySpellMod(GetSpellInfo(), SpellModOp.PowerCost0, ref damage);

            SpellNonMeleeDamage damageInfo = new(caster, caster, GetSpellInfo(), GetAura().GetSpellVisual(), GetSpellInfo().SchoolMask, GetAura().GetCastId());
            damageInfo.periodicLog = true;
            damageInfo.damage = damage;
            caster.DealSpellDamage(damageInfo, false);
            caster.SendSpellNonMeleeDamageLog(damageInfo);
        }
    }
}