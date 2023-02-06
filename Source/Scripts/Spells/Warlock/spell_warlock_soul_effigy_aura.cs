using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    // 205247 - Soul Effigy aura
    [SpellScript(205247)]
    public class spell_warlock_soul_effigy_aura : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            WorldObject owner = caster.ToTempSummon().GetSummoner();
            if (owner == null)
            {
                return;
            }

            if (eventInfo.GetSpellInfo() != null && eventInfo.GetSpellInfo().IsPositive())
            {
                return;
            }

            var damage = MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), aurEff.GetAmount());
            if (damage == 0)
            {
                return;
            }

            ObjectGuid guid = owner.VariableStorage.GetValue<ObjectGuid>("Spells.SoulEffigyTargetGuid", ObjectGuid.Empty);

            Unit target = ObjectAccessor.Instance.GetUnit(owner, guid);
            if (target != null)
            {
                caster.CastSpell(target, WarlockSpells.SOUL_EFFIGY_VISUAL, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCaster(owner.GetGUID()));
                ObjectGuid targetGuid = target.GetGUID();
                ObjectGuid ownerGuid = owner.GetGUID();
                //C++ TO C# CONVERTER TASK: Only lambdas having all locals passed by reference can be converted to C#:
                //ORIGINAL LINE: caster->GetScheduler().Schedule(750ms, [caster, targetGuid, damage, ownerGuid](TaskContext)
                caster.m_Events.AddEvent(() => 
                { 
                    Unit target = ObjectAccessor.Instance.GetUnit(caster, targetGuid);
                    Unit owner = ObjectAccessor.Instance.GetUnit(caster, ownerGuid);
                    if (target == null || owner == null)
                    {
                        return;
                    }
                    CastSpellExtraArgs args = new CastSpellExtraArgs(TriggerCastFlags.FullMask);
                    caster.CastSpell(target, WarlockSpells.SOUL_EFFIGY_DAMAGE, new CastSpellExtraArgs(SpellValueMod.BasePoint0, 0).SetTriggerFlags(TriggerCastFlags.FullMask).SetOriginalCaster(owner.GetGUID()));
                }, TimeSpan.FromMilliseconds(750));
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectProcHandler(OnProc, 0, AuraType.PeriodicDummy, AuraScriptHookType.EffectProc));
        }
    }
}
