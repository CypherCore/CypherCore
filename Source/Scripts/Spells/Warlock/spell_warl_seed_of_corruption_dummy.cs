// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;
using Game.Spells.Auras.EffectHandlers;

namespace Scripts.Spells.Warlock
{
    [SpellScript(27243)] // 27243 - Seed of Corruption
    internal class spell_warl_seed_of_corruption_dummy : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SEED_OF_CORRUPTION_DAMAGE);
        }

        public override void Register()
        {
            Effects.Add(new EffectCalcAmountHandler(CalculateBuffer, 2, AuraType.Dummy));
            Effects.Add(new EffectProcHandler(HandleProc, 2, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void CalculateBuffer(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit caster = GetCaster();

            if (caster == null)
                return;

            amount = caster.SpellBaseDamageBonusDone(GetSpellInfo().GetSchoolMask()) * GetEffectInfo(0).CalcValue(caster) / 100;
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            DamageInfo damageInfo = eventInfo.GetDamageInfo();

            if (damageInfo == null ||
                damageInfo.GetDamage() == 0)
                return;

            int amount = (int)(aurEff.GetAmount() - damageInfo.GetDamage());

            if (amount > 0)
            {
                aurEff.SetAmount(amount);

                if (!GetTarget().HealthBelowPctDamaged(1, damageInfo.GetDamage()))
                    return;
            }

            Remove();

            Unit caster = GetCaster();

            if (!caster)
                return;

            caster.CastSpell(eventInfo.GetActionTarget(), SpellIds.SEED_OF_CORRUPTION_DAMAGE, true);
        }
    }
}