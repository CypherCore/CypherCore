using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    // 234877 - Curse of Shadows
    [SpellScript(234877)]
    public class spell_warlock_curse_of_shadows : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            SpellInfo spellInfo = eventInfo.GetDamageInfo().GetSpellInfo();
            if (spellInfo == null || (spellInfo.GetSchoolMask() & SpellSchoolMask.Shadow) == 0)
            {
                return;
            }

            var damage = MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), aurEff.GetAmount());
            caster.CastSpell(eventInfo.GetActionTarget(), WarlockSpells.CURSE_OF_SHADOWS_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)damage));
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }
}
