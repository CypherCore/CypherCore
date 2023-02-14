// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    [SpellScript(205191)] // 205191 - Eye for an Eye
    internal class spell_pal_eye_for_an_eye : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(PaladinSpells.EyeForAnEyeTriggered);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            GetTarget().CastSpell(eventInfo.GetActor(), PaladinSpells.EyeForAnEyeTriggered, true);
        }
    }
}
