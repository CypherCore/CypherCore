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
    [Script] // 77220 - Mastery: Chaotic Energies
    internal class spell_warl_chaotic_energies : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectAbsorbHandler(HandleAbsorb, 2, false, AuraScriptHookType.EffectAbsorb));
        }

        private void HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            AuraEffect auraEffect = GetEffect(1);

            if (auraEffect == null ||
                !GetTargetApplication().HasEffect(1))
            {
                PreventDefaultAction();

                return;
            }

            // You take ${$s2/3}% reduced Damage
            float damageReductionPct = (float)auraEffect.GetAmount() / 3;
            // plus a random amount of up to ${$s2/3}% additional reduced Damage
            damageReductionPct += RandomHelper.FRand(0.0f, damageReductionPct);

            absorbAmount = MathFunctions.CalculatePct(dmgInfo.GetDamage(), damageReductionPct);
        }
    }
}