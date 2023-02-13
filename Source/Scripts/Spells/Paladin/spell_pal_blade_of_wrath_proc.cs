using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    //231832 - Blade of Wrath! (proc)
    [SpellScript(231832)]
    public class spell_pal_blade_of_wrath_proc : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster == null)
                return;

            caster.GetSpellHistory().ResetCooldown(PaladinSpells.BLADE_OF_JUSTICE, true);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }
}
