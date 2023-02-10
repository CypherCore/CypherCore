using Game.Entities;
using Game.Scripting.BaseScripts;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Paladin
{
    // 216860 - Judgement of the Pure
    [SpellScript(216860)]
    public class spell_pal_judgement_of_the_pure : AuraScript, IAuraCheckProc
    {
        public bool CheckProc(ProcEventInfo eventInfo)
        {
            var spellinfo = eventInfo.GetSpellInfo();
            return spellinfo != null && eventInfo.GetSpellInfo().Id == PaladinSpells.SPELL_PALADIN_JUDGMENT;
        }
    }
}
