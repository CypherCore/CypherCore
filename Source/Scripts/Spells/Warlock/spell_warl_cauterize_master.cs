using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
    // Cauterize Master - 119905
    [SpellScript(119905)]
    public class spell_warl_cauterize_master : SpellScript, ISpellCheckCast, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

        public SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();
            Guardian pet = caster.GetGuardianPet();
            if (caster == null || pet == null)
            {
                return SpellCastResult.DontReport;
            }

            if (pet.GetSpellHistory().HasCooldown(WarlockSpells.IMP_CAUTERIZE_MASTER))
            {
                return SpellCastResult.CantDoThatRightNow;
            }

            return SpellCastResult.SpellCastOk;
        }

        private void HandleHit(uint UnnamedParameter)
        {
            Unit caster = GetCaster();
            Guardian pet = caster.GetGuardianPet();
            if (caster == null || pet == null)
            {
                return;
            }

            /*if (pet->GetEntry() != PET_ENTRY_IMP)
			    return;*/

            pet.CastSpell(caster, WarlockSpells.IMP_CAUTERIZE_MASTER, true);
            caster.ToPlayer().GetSpellHistory().ModifyCooldown(GetSpellInfo().Id, TimeSpan.FromSeconds(30));
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));

        }
    }
}