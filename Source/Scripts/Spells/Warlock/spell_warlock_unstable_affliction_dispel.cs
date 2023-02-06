using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    // 233490 - Unstable Affliction dispel
    [SpellScript(233490)]
    public class spell_warlock_unstable_affliction_dispel : AuraScript, IHasAuraEffects, IAuraOnDispel
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        public void OnDispel(DispelInfo dispelInfo)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            Unit dispeller = dispelInfo.GetDispeller().ToUnit();
            if (dispeller != null)
            {
                int damage = GetAura().GetEffect(0).GetAmount() * 4;
                CastSpellExtraArgs args = new CastSpellExtraArgs();
                args.AddSpellMod(SpellValueMod.BasePoint0, (int)damage);
                args.SetTriggerFlags(TriggerCastFlags.FullMask);
                caster.CastSpell(dispeller, WarlockSpells.UNSTABLE_AFFLICTION_DISPEL, args);
            }
        }

        private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit caster = GetCaster();
            Unit target = GetUnitOwner();
            if (caster == null || target == null || !caster.ToPlayer())
            {
                return;
            }

            if (caster.HasAura(WarlockSpells.UNSTABLE_AFFLICTION_RANK2))
            {
                if (GetTargetApplication() != null && GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Death)
                {
                    if (caster.VariableStorage.Exist("_uaLockout"))
                    {
                        return;
                    }

                    caster.CastSpell(caster, WarlockSpells.UNSTABLE_AFFLICTION_ENERGIZE, true);

                    caster.VariableStorage.Set("_uaLockout", 0);


                    caster.m_Events.AddEventAtOffset(() =>
                    {
                        caster.VariableStorage.Remove("_uaLockout");
                    }, TimeSpan.FromMilliseconds(100));
                }
            }

            // When Unstable Affliction expires, it has a 6% chance to reapply itself.
            if (GetTargetApplication() != null && GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Expire)
            {
                if (RandomHelper.randChance(caster.GetAuraEffectAmount(WarlockSpells.FATAL_ECHOES, 0)))
                {


                    caster.m_Events.AddEventAtOffset(() =>
                    {
                        caster.CastSpell(target, GetSpellInfo().Id, true);
                    }, TimeSpan.FromMilliseconds(100));
                }
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }
    }
}
