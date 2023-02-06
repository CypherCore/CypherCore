using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    // 212623 - Singe Magic
    [SpellScript(212623)]
    public class spell_warlock_singe_magic : SpellScript, ISpellCheckCast, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

        private void HandleHit(uint UnnamedParameter)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (caster == null || target == null)
            {
                return;
            }

            Pet pet = caster.ToPlayer().GetPet();
            if (pet != null)
            {
                pet.CastSpell(target, WarlockSpells.SINGE_MAGIC, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)GetEffectInfo(0).BasePoints));
            }
        }

        public SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();
            if (caster == null || !caster.ToPlayer())
            {
                return SpellCastResult.BadTargets;
            }

            if (caster.ToPlayer().GetPet() && caster.ToPlayer().GetPet().GetEntry() == 416)
            {
                return SpellCastResult.SpellCastOk;
            }

            return SpellCastResult.CantDoThatRightNow;
        }

        public override void Register()
        {

            SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }
    }
}
