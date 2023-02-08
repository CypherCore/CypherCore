using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    // 980 - Agony
    [SpellScript(980)]
    public class spell_warlock_agony : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void HandleDummyPeriodic(AuraEffect auraEffect)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            float soulShardAgonyTick = caster.VariableStorage.GetValue<float>("SoulShardAgonyTick", RandomHelper.FRand(0.0f, 99.0f));
            soulShardAgonyTick += 16.0f;

            if (soulShardAgonyTick >= 100.0f)
            {
                soulShardAgonyTick = RandomHelper.FRand(0.0f, 99.0f);

                Player player = GetCaster().ToPlayer();
                if (player != null)
                {
                    if (player.GetPower(PowerType.SoulShards) < player.GetMaxPower(PowerType.SoulShards))
                    {
                        player.SetPower(PowerType.SoulShards, player.GetPower(PowerType.SoulShards) + 10);
                    }
                }
            }

            caster.VariableStorage.Set("SoulShardAgonyTick", soulShardAgonyTick);

            // If we have more than maxStackAmount, dont do anything
            if (GetStackAmount() >= auraEffect.GetBase().CalcMaxStackAmount())
            {
                return;
            }

            SetStackAmount((byte)(GetStackAmount() + 1));
        }

        private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            // If last agony removed, remove tick counter
            Unit caster = GetCaster();
            if (caster != null)
            {
                if (caster.GetOwnedAura(WarlockSpells.AGONY) == null)
                {
                    caster.VariableStorage.Remove("SoulShardAgonyTick");
                }
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectPeriodicHandler(HandleDummyPeriodic, 1, AuraType.PeriodicDummy));
            AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 1, AuraType.PeriodicDummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }
    }
}
