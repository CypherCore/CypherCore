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

namespace Scripts.Spells.Shaman
{
    // Ascendance (Water) - 114052
    [SpellScript(114052)]
    public class spell_sha_ascendance_water : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        private struct eSpells
        {
            public const uint RestorativeMists = 114083;
        }

        public override bool Validate(SpellInfo UnnamedParameter)
        {
            return ValidateSpellInfo(eSpells.RestorativeMists);
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            if (eventInfo.GetHealInfo() != null && eventInfo.GetSpellInfo() != null && eventInfo.GetSpellInfo().Id == eSpells.RestorativeMists)
            {
                return false;
            }

            if (eventInfo.GetHealInfo() == null)
            {
                return false;
            }

            return true;
        }

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            uint bp0 = eventInfo.GetHealInfo().GetHeal();
            if (bp0 != 0)
            {
                eventInfo.GetActionTarget().CastSpell(eventInfo.GetActor(), eSpells.RestorativeMists, new CastSpellExtraArgs(aurEff).AddSpellMod(SpellValueMod.BasePoint0, (int)bp0));
            }
        }

        public override void Register()
        {

            AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 1, AuraType.PeriodicDummy, AuraScriptHookType.EffectProc));
        }
    }
}
