using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    // The fires of Justice - 203316
    [SpellScript(203316)]
    public class spell_pal_the_fire_of_justice : AuraScript, IAuraCheckProc
    {
        public bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetSpellInfo().Id == PaladinSpells.SPELL_PALADIN_CRUSADER_STRIKE;
        }
    }
}
