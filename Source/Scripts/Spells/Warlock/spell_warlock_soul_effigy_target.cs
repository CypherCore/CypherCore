using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    // 205178 - Soul Effigy target
    [SpellScript(205178)]
    public class spell_warlock_soul_effigy_target : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void PeriodicTick(AuraEffect UnnamedParameter)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            if (!caster.VariableStorage.Exist("Spells.SoulEffigyGuid"))
            {
                Remove();
                return;
            }

            ObjectGuid guid = caster.VariableStorage.GetValue<ObjectGuid>("Spells.SoulEffigyGuid", ObjectGuid.Empty);
            if (!ObjectAccessor.Instance.GetUnit(caster, guid))
            {
                Remove();
            }
        }

        private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            ObjectGuid guid = caster.VariableStorage.GetValue<ObjectGuid>("Spells.SoulEffigyGuid", ObjectGuid.Empty);

            Unit effigy = ObjectAccessor.Instance.GetUnit(caster, guid);
            if (effigy != null)
            {
                effigy.ToTempSummon().DespawnOrUnsummon();
            }
        }

        private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit caster = GetCaster();
            Unit target = GetTarget();
            if (caster == null || target == null)
            {
                return;
            }

            caster.VariableStorage.Set("Spells.SoulEffigyTargetGuid", target.GetGUID());
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask));
            AuraEffects.Add(new EffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
            AuraEffects.Add(new EffectPeriodicHandler(PeriodicTick, 0, AuraType.Dummy));
        }
    }
}
