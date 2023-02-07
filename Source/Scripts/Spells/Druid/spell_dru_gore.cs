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
    [Script] // 210706 - Gore
    internal class spell_dru_gore : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(DruidSpellIds.GoreProc, DruidSpellIds.Mangle);
        }

        public override void Register()
        {
            AuraEffects.Add(new CheckEffectProcHandler(CheckEffectProc, 0, AuraType.Dummy));
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private bool CheckEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return RandomHelper.randChance(aurEff.GetAmount());
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            Unit owner = GetTarget();
            owner.CastSpell(owner, DruidSpellIds.GoreProc);
            owner.GetSpellHistory().ResetCooldown(DruidSpellIds.Mangle, true);
        }
    }
}