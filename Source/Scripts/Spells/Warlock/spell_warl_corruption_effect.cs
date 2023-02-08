using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{

    //146739 - Corruption
    [SpellScript(146739)]
    public class spell_warl_corruption_effect : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        public override bool Validate(SpellInfo UnnamedParameter)
        {
            if (Global.SpellMgr.GetSpellInfo(WarlockSpells.ABSOLUTE_CORRUPTION, Difficulty.None) != null)
            {
                return false;
            }
            return true;
        }

        private void HandleApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit target = GetTarget();
            Unit caster = GetCaster();
            if (target == null || caster == null)
            {
                return;
            }

            //If the target is a player, only cast for the time said in ABSOLUTE_CORRUPTION
            if (caster.HasAura(WarlockSpells.ABSOLUTE_CORRUPTION))
            {
                GetAura().SetDuration(target.GetTypeId() == TypeId.Player ? Global.SpellMgr.GetSpellInfo(WarlockSpells.ABSOLUTE_CORRUPTION, Difficulty.None).GetEffect(0).BasePoints * Time.InMilliseconds : 60 * 60 * Time.InMilliseconds); //If not player, 1 hour
            }
        }

        /*
        Removes the aura if the caster is null, far away or dead.
        */
        private void HandlePeriodic(AuraEffect UnnamedParameter)
        {
            Unit target = GetTarget();
            Unit caster = GetCaster();
            if (target == null)
            {
                return;
            }

            if (caster == null)
            {
                target.RemoveAura(WarlockSpells.CORRUPTION_DAMAGE);
                return;
            }

            if (caster.IsDead())
            {
                target.RemoveAura(WarlockSpells.CORRUPTION_DAMAGE);
            }

            if (!caster.IsInRange(target, 0, 80))
            {
                target.RemoveAura(WarlockSpells.CORRUPTION_DAMAGE);
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.Real));
            AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDamage));
        }
    }
}
