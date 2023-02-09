using Game.Entities;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Shaman
{
    // Crash Lightning aura - 187878
    [SpellScript(187878)]
    public class spell_sha_crash_lightning_aura : AuraScript, IAuraCheckProc
    {
        public bool CheckProc(ProcEventInfo eventInfo)
        {
            if (eventInfo.GetSpellInfo().Id == ShamanSpells.SPELL_SHAMAN_STORMSTRIKE_MAIN || eventInfo.GetSpellInfo().Id == ShamanSpells.SPELL_SHAMAN_LAVA_LASH)
            {
                return true;
            }
            return false;
        }
    }
}
