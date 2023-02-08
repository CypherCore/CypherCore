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
    [Script] // 203974 - Earthwarden
    internal class spell_dru_earthwarden : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(DruidSpellIds.ThrashCat, DruidSpellIds.ThrashBear, DruidSpellIds.EarthwardenAura);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Unit target = GetTarget();
            target.CastSpell(target, DruidSpellIds.EarthwardenAura, true);
        }
    }
}