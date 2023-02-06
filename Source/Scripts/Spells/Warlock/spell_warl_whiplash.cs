using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
    // Whiplash - 119909
    [SpellScript(119909)]
    public class spell_warl_whiplash : SpellScript, ISpellAfterHit, ISpellCheckCast
    {


        public void AfterHit()
        {
            Unit caster = GetCaster();
            WorldLocation dest = GetExplTargetDest();
            Guardian pet = caster.GetGuardianPet();
            if (caster == null || pet == null || dest == null)
            {
                return;
            }

            /*if (pet->GetEntry() != PET_ENTRY_SUCCUBUS)
			    return;*/

            pet.CastSpell(new Position(dest.GetPositionX(), dest.GetPositionY(), dest.GetPositionZ()), WarlockSpells.SUCCUBUS_WHIPLASH, true);
            caster.ToPlayer().GetSpellHistory().ModifyCooldown(GetSpellInfo().Id, TimeSpan.FromSeconds(25));
        }

        public SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();
            Guardian pet = caster.GetGuardianPet();
            if (caster == null || pet == null)
            {
                return SpellCastResult.DontReport;
            }

            if (pet.GetSpellHistory().HasCooldown(WarlockSpells.SUCCUBUS_WHIPLASH))
            {
                return SpellCastResult.CantDoThatRightNow;
            }

            return SpellCastResult.SpellCastOk;
        }
    }
}
