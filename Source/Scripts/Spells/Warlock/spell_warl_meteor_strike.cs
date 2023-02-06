using System;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
    // Meteor Strike - 171152
    [SpellScript(171152)]
    public class spell_warl_meteor_strike : SpellScript, ISpellAfterHit, ISpellCheckCast
    {


        public void AfterHit()
        {
            Unit caster = GetCaster();
            Guardian pet = caster.GetGuardianPet();
            if (caster == null || pet == null)
            {
                return;
            }

            /*if (pet->GetEntry() != PET_ENTRY_INFERNAL)
			    return;*/

            pet.CastSpell(pet, WarlockSpells.INFERNAL_METEOR_STRIKE, true);

            caster.ToPlayer().GetSpellHistory().ModifyCooldown(GetSpellInfo().Id, TimeSpan.FromSeconds(60));
        }

        public SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();
            Guardian pet = caster.GetGuardianPet();
            if (caster == null || pet == null)
            {
                return SpellCastResult.DontReport;
            }

            if (pet.GetSpellHistory().HasCooldown(WarlockSpells.INFERNAL_METEOR_STRIKE))
            {
                return SpellCastResult.CantDoThatRightNow;
            }

            return SpellCastResult.SpellCastOk;
        }
    }
}
