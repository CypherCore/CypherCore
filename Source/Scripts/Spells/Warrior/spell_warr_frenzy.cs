using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warrior
{
    //206313 Frenzy
    [SpellScript(206313)]
    public class spell_warr_frenzy : AuraScript, IAuraCheckProc
    {
        public bool CheckProc(ProcEventInfo procInfo)
        {
            return procInfo.GetSpellInfo().Id == WarriorSpells.FURIOUS_SLASH;
        }
    }
}
