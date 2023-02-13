using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    // Beacon of Faith - 156910
    [SpellScript(156910)]
    public class spell_pal_beacon_of_faith : SpellScript, ISpellCheckCast
    {
        public SpellCastResult CheckCast()
        {
            Unit target = GetExplTargetUnit();

            if (target == null)
            {
                return SpellCastResult.DontReport;
            }

            if (target.HasAura(PaladinSpells.BeaconOfLight))
            {
                return SpellCastResult.BadTargets;
            }

            return SpellCastResult.SpellCastOk;
        }
    }
}
