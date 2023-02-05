using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{

    //232670
    [SpellScript(232670)]
    public class spell_warr_shadowbolt_affliction : SpellScript, ISpellOnHit
    {
        public void OnHit()
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (caster == null || target == null)
            {
                return;
            }

            if (caster.HasAura(WarlockSpells.SHADOW_EMBRACE))
            {
                caster.AddAura(WarlockSpells.SHADOW_EMBRACE_TARGET_DEBUFF, target);
            }
        }
    }
}
