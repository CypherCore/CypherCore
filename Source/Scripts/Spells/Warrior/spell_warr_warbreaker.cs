using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
    // 262161 Warbreaker
    [SpellScript(262161)]
    public class spell_warr_warbreaker : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects => throw new NotImplementedException();

        private void HandleOnHitTarget(uint UnnamedParameter)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                Unit target = GetHitUnit();
                if (target != null)
                {
                    caster.CastSpell(target, WarriorSpells.COLOSSUS_SMASH_BUFF, true);
                }
            }
        }

        private void CountTargets(List<WorldObject> targets)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                Aura inForTheKill = caster.GetAura(248621);
                if (inForTheKill != null) // In For The Kill
                {
                    int hpPct = inForTheKill.GetSpellInfo().GetEffect(2).CalcValue(caster);
                    int hastePct = inForTheKill.GetEffect(0).GetAmount();
                    for (List<WorldObject>.Enumerator itr = targets.GetEnumerator(); itr.MoveNext();)
                    {
                        Unit target = itr.Current.ToUnit();
                        if (target != null)
                        {
                            if (target.HealthBelowPct(hpPct))
                            {
                                hastePct = inForTheKill.GetSpellInfo().GetEffect(1).CalcValue(caster);
                                break;
                            }
                        }
                    }

                    caster.CastSpell(caster, 248622, new CastSpellExtraArgs(SpellValueMod.DurationPct, hastePct)); // In For The Kill
                }
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleOnHitTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(CountTargets, 0, Targets.UnitSrcAreaEnemy));
        }
    }
}
