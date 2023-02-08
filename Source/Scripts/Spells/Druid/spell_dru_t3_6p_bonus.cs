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
    [Script] // 28744 - Regrowth
    internal class spell_dru_t3_6p_bonus : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(DruidSpellIds.BlessingOfTheClaw);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.OverrideClassScripts, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            eventInfo.GetActor().CastSpell(eventInfo.GetProcTarget(), DruidSpellIds.BlessingOfTheClaw, new CastSpellExtraArgs(aurEff));
        }
    }
}