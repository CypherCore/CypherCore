using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warrior
{
    // Enrage - 184361
    [SpellScript(184361)]
    public class spell_warr_enrage : AuraScript, IAuraCheckProc
    {
        public bool CheckProc(ProcEventInfo eventInfo)
        {
            if (eventInfo.GetSpellInfo().Id == WarriorSpells.BLOODTHIRST_DAMAGE && (eventInfo.GetHitMask() & ProcFlagsHit.Critical) != 0)
            {
                return true;
            }
            return false;
        }
    }
}
