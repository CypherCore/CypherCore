using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
    // 203179 - Opportunity Strike
    [SpellScript(203179)]
    public class spell_warr_opportunity_strike : AuraScript, IAuraOnProc
    {
        public void OnProc(ProcEventInfo eventInfo)
        {
            if (!GetCaster())
            {
                return;
            }

            if (eventInfo?.GetDamageInfo()?.GetSpellInfo() != null && eventInfo.GetDamageInfo().GetSpellInfo().Id == WarriorSpells.OPPORTUNITY_STRIKE_DAMAGE)
            {
                return;
            }

            Unit target = eventInfo.GetActionTarget();
            if (target != null)
            {
                Player _player = GetCaster().ToPlayer();
                if (_player != null)
                {
                    Aura aur = GetAura();
                    if (aur != null)
                    {
                        AuraEffect eff = aur.GetEffect(0);
                        if (eff != null)
                        {
                            if (RandomHelper.randChance(eff.GetAmount()))
                            {
                                _player.CastSpell(target, WarriorSpells.OPPORTUNITY_STRIKE_DAMAGE, true);
                            }
                        }
                    }
                }
            }
        }
    }
}
