using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Scripting.Interfaces.ISpellManager;
using Game.Spells;

namespace Scripts.SpellFixes
{
    public class BuringRushSpellFix : ISpellManagerSpellLateFix
    {
        public int[] SpellIds => new[] { 111400 };

        public void ApplySpellFix(SpellInfo spellInfo)
        {
            spellInfo.NegativeEffects = new BitSet(SpellConst.MaxEffects); // no negitive effects for burning rush
        }
    }
}
