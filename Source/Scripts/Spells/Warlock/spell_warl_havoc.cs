using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    //80240 - Havoc
    [SpellScript(80240)]
    internal class spell_warl_havoc : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            Unit victim = procInfo.GetActionTarget();
            if (victim != null)
            {
                Unit target = procInfo.GetProcTarget();
                if (target != null)
                {
                    if (victim != target)
                    {
                        SpellInfo spellInfo = aurEff.GetSpellInfo();
                        if (spellInfo != null) 
						{
                            uint dmg = procInfo.GetDamageInfo().GetDamage();
                            SpellNonMeleeDamage spell = new SpellNonMeleeDamage(caster, target, spellInfo, new SpellCastVisual(spellInfo.GetSpellVisual(caster), 0), SpellSchoolMask.Shadow);
                            spell.damage = dmg;
                            spell.cleanDamage = spell.damage;
                            caster.DealSpellDamage(spell, false);
                            caster.SendSpellNonMeleeDamageLog(spell);
                        }
                    }
                }
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }
}
