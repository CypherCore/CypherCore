using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    // Immolate Dot - 157736
    [SpellScript(157736)]
    public class spell_warlock_immolate_dot : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void HandlePeriodic(AuraEffect UnnamedParameter)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            Aura aur = caster.GetAura(WarlockSpells.CHANNEL_DEMONFIRE_ACTIVATOR);
            if (aur != null)
            {
                aur.RefreshDuration();
            }

            if (GetAura() != null)
            {
                int damage = GetEffect(0).GetAmount();
                MathFunctions.AddPct(ref damage, 25);

                GetEffect(0).SetAmount(damage);
                GetAura().SetNeedClientUpdateForTargets();

            }
        }

        private void HandleApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            caster.CastSpell(caster, WarlockSpells.CHANNEL_DEMONFIRE_ACTIVATOR, true);

            // GetAura()->VariableStorage.Remove("Spells.AffectedByRoaringBlaze");
        }

        private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            List<Unit> enemies = new List<Unit>();
            AnyUnfriendlyUnitInObjectRangeCheck check = new AnyUnfriendlyUnitInObjectRangeCheck(caster, caster, 100.0f);
            UnitListSearcher searcher = new UnitListSearcher(caster, enemies, check);
            Cell.VisitAllObjects(caster, searcher, 100.0f);
            var checker = new UnitAuraCheck<Unit>(false, WarlockSpells.IMMOLATE_DOT, caster.GetGUID());
            enemies.RemoveIf(checker);
            if (enemies.Count == 0)
            {
                Aura aur = caster.GetAura(WarlockSpells.CHANNEL_DEMONFIRE_ACTIVATOR);
                if (aur != null)
                {
                    aur.SetDuration(0);
                }
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(HandleApply, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.RealOrReapplyMask));
            AuraEffects.Add(new EffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDamage));
            AuraEffects.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectAfterRemove));
        }
    }
}
