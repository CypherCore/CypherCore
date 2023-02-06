using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
    // Suffering - 119907
    [SpellScript(119907)]
    public class spell_warl_suffering : SpellScript, ISpellCheckCast, IHasSpellEffects
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

            if (pet.GetSpellHistory().HasCooldown(WarlockSpells.VOIDWALKER_SUFFERING))
            {
                return SpellCastResult.CantDoThatRightNow;
            }

            return SpellCastResult.SpellCastOk;
        }

        private void HandleHit(uint UnnamedParameter)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            Guardian pet = caster.GetGuardianPet();
            if (caster == null || pet == null || target == null)
            {
                return;
            }

            /*if (pet->GetEntry() != PET_ENTRY_VOIDWALKER)
			    return;*/

            pet.CastSpell(target, WarlockSpells.VOIDWALKER_SUFFERING, true);
            caster.ToPlayer().GetSpellHistory().ModifyCooldown(GetSpellInfo().Id, TimeSpan.FromSeconds(10));
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));

        }
    }
}
