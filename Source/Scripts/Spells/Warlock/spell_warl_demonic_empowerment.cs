using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{

    // Demonic Empowerment - 193396
    [SpellScript(193396)]
    public class spell_warl_demonic_empowerment : SpellScript, IHasSpellEffects, ISpellOnCast
    {
        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

        private void HandleTargets(List<WorldObject> targets)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            targets.RemoveIf((WorldObject target) =>
            {
                if (!target.ToCreature())
                {
                    return true;
                }
                if (!caster.IsFriendlyTo(target.ToUnit()))
                {
                    return true;
                }
                if (target.ToCreature().GetCreatureType() != CreatureType.Demon)
                {
                    return true;
                }
                return false;
            });
        }

        public void OnCast()
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            if (caster.HasAura(WarlockSpells.SHADOWY_INSPIRATION))
            {
                caster.CastSpell(caster, WarlockSpells.SHADOWY_INSPIRATION_EFFECT, true);
            }

            if (caster.HasAura(WarlockSpells.POWER_TRIP) && caster.IsInCombat() && RandomHelper.randChance(50))
            {
                caster.CastSpell(caster, WarlockSpells.POWER_TRIP_ENERGIZE, true);
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(HandleTargets, 255, Targets.UnitCasterAndSummons));

        }
    }
}
