using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warrior
{
    // Taste for Blood - 206333
    [SpellScript(206333)]
    public class spell_warr_taste_for_blood : AuraScript, IAuraCheckProc
    {
        public bool CheckProc(ProcEventInfo eventInfo)
        {
            if ((eventInfo.GetHitMask() & ProcFlagsHit.Critical) != 0 && eventInfo.GetSpellInfo().Id == WarriorSpells.BLOODTHIRST_DAMAGE)
            {
                GetAura().SetDuration(0);
                return true;
            }
            return false;
        }
    }
}
