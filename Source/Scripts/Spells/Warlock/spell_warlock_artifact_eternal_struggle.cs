using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    // 196305 - Eternal Struggle
    [SpellScript(196305)]
    public class spell_warlock_artifact_eternal_struggle : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void OnProc(AuraEffect aurEff, ProcEventInfo UnnamedParameter)
        {
            PreventDefaultAction();
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            caster.CastSpell(caster, WarlockSpells.ETERNAL_STRUGGLE_PROC, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)aurEff.GetAmount()));
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.ProcTriggerSpellWithValue, AuraScriptHookType.EffectProc));
        }
    }
}
