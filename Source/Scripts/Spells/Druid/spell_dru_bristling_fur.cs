// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
    [Script] // 155835 - Bristling Fur
    internal class spell_dru_bristling_fur : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(DruidSpellIds.BristlingFurGainRage);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            // BristlingFurRage = 100 * Damage / MaxHealth.
            DamageInfo damageInfo = eventInfo.GetDamageInfo();

            if (damageInfo != null)
            {
                Unit target = GetTarget();
                uint rage = (uint)(target.GetMaxPower(PowerType.Rage) * (float)damageInfo.GetDamage() / (float)target.GetMaxHealth());

                if (rage > 0)
                    target.CastSpell(target, DruidSpellIds.BristlingFurGainRage, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)rage));
            }
        }
    }
}