using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Warlock
{
    [SpellScript(198590)] // 198590 - Drain Soul
    internal class spell_warl_drain_soul : SpellScript, ISpellCalculateMultiplier
    {
        public float CalcMultiplier(float multiplier)
        {
            if (GetCaster().HasAuraState(AuraStateType.Wounded20Percent))
                multiplier *= 2;

            return multiplier;
        }
    }
}
