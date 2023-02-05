using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    // 199471 - Soul Flame
    [SpellScript(199471)]
    public class spell_warlock_artifact_soul_flame : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void OnProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
        {
            Unit target = eventInfo.GetActionTarget();
            Unit caster = GetCaster();
            if (caster == null || target == null)
            {
                return;
            }

            Position p = target.GetPosition();
            caster.m_Events.AddEvent(() =>
            {
                caster.CastSpell(p, WarlockSpells.SOUL_FLAME_PROC, true);
            }, TimeSpan.FromMilliseconds(300));
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }
}
