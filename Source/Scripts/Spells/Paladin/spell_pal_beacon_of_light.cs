using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin
{
    // 53563 - Beacon of Light
    [SpellScript(53563)]
    public class spell_pal_beacon_of_light : SpellScript, ISpellCheckCast
    {
        public SpellCastResult CheckCast()
        {
            Unit target = GetExplTargetUnit();

            if (target == null)
            {
                return SpellCastResult.DontReport;
            }

            if (target.HasAura(PaladinSpells.BEACON_OF_FAITH))
            {
                return SpellCastResult.BadTargets;
            }

            return SpellCastResult.SpellCastOk;
        }
    }
}
