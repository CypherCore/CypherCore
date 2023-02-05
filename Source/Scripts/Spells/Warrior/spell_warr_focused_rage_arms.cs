using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warrior
{
    //207982 - Focused Rage
    [SpellScript(207982)]
    public class spell_warr_focused_rage_arms : AuraScript, IAuraCheckProc
    {
        public bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetSpellInfo().Id == WarriorSpells.MORTAL_STRIKE;
        }
    }
}
