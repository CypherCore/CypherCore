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
    [SpellScript(85043)] // -85043 - Grand Crusader
    internal class spell_pal_grand_crusader : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(PaladinSpells.AvengersShield);
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            return GetTarget().IsTypeId(TypeId.Player);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
        }

        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            GetTarget().GetSpellHistory().ResetCooldown(PaladinSpells.AvengersShield, true);
        }
    }
}
