using Framework.Constants;
using Game.Entities;
using Game.Scripting.BaseScripts;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Shaman
{
    // Flametongue aura - 194084
    [SpellScript(194084)]
    public class bfa_spell_flametongue_proc_attack : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            Unit attacker = eventInfo.GetActionTarget();
            Unit caster = GetCaster();

            if (caster == null || attacker == null)
            {
                return;
            }
            caster.CastSpell(attacker, ShamanSpells.SPELL_SHAMAN_FLAMETONGUE_ATTACK, true);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
        }
    }
}
