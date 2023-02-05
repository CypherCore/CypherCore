using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    // Call Dreadstalkers - 104316
    [SpellScript(104316)]
    public class spell_warlock_call_dreadstalkers : SpellScript, ISpellAfterCast, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

        private void HandleHit(uint UnnamedParameter)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            for (int i = 0; i < GetEffectValue(); ++i)
            {
                caster.CastSpell(caster, WarlockSpells.CALL_DREADSTALKERS_SUMMON, true);
            }

            Player player = caster.ToPlayer();
            if (player == null)
            {
                return;
            }

            // Check if player has aura with ID 387485
            Aura aura = caster.GetAura(387485);
            if (aura != null)
            {
                var effect = aura.GetEffect(0);

                if (RandomHelper.randChance(effect.GetBaseAmount()))
                {
                    caster.CastSpell(caster, WarlockSpells.CALL_DREADSTALKERS_SUMMON, true);
                }
            }
        }

        public void AfterCast()
        {
            Unit caster = GetCaster();
            Unit target = GetExplTargetUnit();
            if (caster == null || target == null)
            {
                return;
            }

            List<Creature> dreadstalkers = caster.GetCreatureListWithEntryInGrid(98035);
            foreach (Creature dreadstalker in dreadstalkers)
            {
                if (dreadstalker.GetOwner() == caster)
                {
                    dreadstalker.SetLevel(caster.GetLevel());
                    dreadstalker.SetMaxHealth(caster.GetMaxHealth() / 3);
                    dreadstalker.SetHealth(caster.GetHealth() / 3);
                    dreadstalker.GetAI().AttackStart(target);
                }
            }

            var impsToSummon = caster.GetAuraEffectAmount(WarlockSpells.IMPROVED_DREADSTALKERS, 0);
            for (uint i = 0; i < impsToSummon; ++i)
            {
                caster.CastSpell(target.GetRandomNearPosition(3.0f), WarlockSpells.WILD_IMP_SUMMON, true);
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }
    }
}
