using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
    // 32216 - Victorious
    // 82368 - Victorious
    [SpellScript(new uint[] { 32216, 82368 })]
    public class spell_warr_victorious : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void HandleEffectProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
        {
            PreventDefaultAction();
            GetTarget().RemoveAura(GetId());
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.AddPctModifier, AuraScriptHookType.EffectProc));
            AuraEffects.Add(new EffectProcHandler(HandleEffectProc, 1, AuraType.AddFlatModifier, AuraScriptHookType.EffectProc));
        }
    }
}
