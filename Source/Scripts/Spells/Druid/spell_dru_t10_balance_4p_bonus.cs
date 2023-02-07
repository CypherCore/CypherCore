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
    [Script] // 70723 - Item - Druid T10 Balance 4P Bonus
    internal class spell_dru_t10_balance_4p_bonus : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(DruidSpellIds.Languish);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            DamageInfo damageInfo = eventInfo.GetDamageInfo();

            if (damageInfo == null ||
                damageInfo.GetDamage() == 0)
                return;

            Unit caster = eventInfo.GetActor();
            Unit target = eventInfo.GetProcTarget();

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(DruidSpellIds.Languish, GetCastDifficulty());
            int amount = (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount());
            amount /= (int)spellInfo.GetMaxTicks();

            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, amount);
            caster.CastSpell(target, DruidSpellIds.Languish, args);
        }
    }
}