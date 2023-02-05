using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warrior
{

    // Rampage damage dealers - 218617, 184707, 184709, 201364, 201363
    [SpellScript(new uint[] { 218617, 184707, 184709, 201364, 201363 })]
    public class spell_warr_rampage : SpellScript, ISpellOnHit, ISpellOnCast
    {
        public void OnCast()
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            Aura enrage = caster.GetAura(WarriorSpells.ENRAGE_AURA);
            if (enrage != null)
            {
                enrage.RefreshDuration();
            }
            else
            {
                caster.CastSpell(caster, WarriorSpells.ENRAGE_AURA, true);
            }
        }

        public void OnHit()
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (caster == null || target == null)
            {
                return;
            }

            if (target != ObjectAccessor.Instance.GetUnit(caster, caster.GetTarget()))
            {
                SetHitDamage(GetHitDamage() / 2);
            }

            if (caster == target)
            {
                SetHitDamage(0);
            }
        }
    }
}
