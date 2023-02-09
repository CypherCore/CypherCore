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
    // -51556 - Ancestral Awakening
    [SpellScript(51556)]
    public class spell_sha_ancestral_awakening : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo UnnamedParameter)
        {
            if (Global.SpellMgr.GetSpellInfo(ShamanSpells.SPELL_SHAMAN_TIDAL_WAVES, Difficulty.None) != null)
            {
                return false;
            }
            return true;
        }

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            var heal = MathFunctions.CalculatePct(eventInfo.GetHealInfo().GetHeal(), aurEff.GetAmount());
            GetTarget().CastSpell(GetTarget(), ShamanSpells.SPELL_SHAMAN_ANCESTRAL_AWAKENING, new CastSpellExtraArgs().AddSpellMod(SpellValueMod.BasePoint0, (int)heal));
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }
}
