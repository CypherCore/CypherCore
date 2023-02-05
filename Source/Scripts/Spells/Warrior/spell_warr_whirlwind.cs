using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
    // Whirlwind - 190411
    [SpellScript(190411)]
    public class spell_warr_whirlwind : SpellScript, IAfterCast
    {
        public void AfterCast()
        {
            Unit caster = GetCaster();

            if (caster == null)
            {
                return;
            }

            if (caster.HasAura(WarriorSpells.WRECKING_BALL_EFFECT))
            {
                caster.RemoveAura(WarriorSpells.WRECKING_BALL_EFFECT);
            }

            if (caster.HasAura(WarriorSpells.MEAT_CLEAVER))
            {
                if (RandomHelper.randChance(10))
                {
                    caster.CastSpell(null, WarriorSpells.ENRAGE_AURA, true);
                }
            }

            if (caster.HasAura(WarriorSpells.THIRST_FOR_BATTLE))
            {
                caster.AddAura(WarriorSpells.THIRST_FOR_BATTLE_BUFF, caster);
                AuraEffect thirst = caster.GetAura(WarriorSpells.THIRST_FOR_BATTLE_BUFF).GetEffect(0);
                if (thirst != null)
                {
                    thirst.GetAmount();
                }
            }
            caster.AddAura(85739, caster);
        }
    }
}
