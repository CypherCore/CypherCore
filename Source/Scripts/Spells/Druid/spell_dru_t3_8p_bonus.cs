// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
    [Script] // 28719 - Healing Touch
    internal class spell_dru_t3_8p_bonus : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(DruidSpellIds.Exhilarate);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Spell spell = eventInfo.GetProcSpell();

            if (spell == null)
                return;

            Unit caster = eventInfo.GetActor();
            var spellPowerCostList = spell.GetPowerCost();
            var spellPowerCost = spellPowerCostList.First(cost => cost.Power == PowerType.Mana);

            if (spellPowerCost == null)
                return;

            int amount = MathFunctions.CalculatePct(spellPowerCost.Amount, aurEff.GetAmount());
            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, amount);
            caster.CastSpell((Unit)null, DruidSpellIds.Exhilarate, args);
        }
    }
}