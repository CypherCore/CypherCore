using System;
using Framework.Constants;
using Game.Entities;
using Game.Spells.Auras.EffectHandlers;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IEffectAbsorbHeal : IAuraEffectHandler
    {
        void HandleAbsorb(AuraEffect aura, HealInfo healInfo, ref uint absorbAmount);
    }

    public class EffectAbsorbHealHandler : AuraEffectHandler, IEffectAbsorbHeal
    {
        public delegate void AuraEffectAbsorbHealDelegate(AuraEffect aura, HealInfo healInfo, ref uint absorbAmount);

        private readonly AuraEffectAbsorbHealDelegate _fn;

        public EffectAbsorbHealHandler(AuraEffectAbsorbHealDelegate fn, uint effectIndex, AuraType auraType, AuraScriptHookType hookType) : base(effectIndex, auraType, hookType)
        {
            _fn = fn;

            if (hookType != AuraScriptHookType.EffectAbsorbHeal &&
                hookType != AuraScriptHookType.EffectAfterAbsorbHeal &&
                hookType != AuraScriptHookType.EffectManaShield &&
                hookType != AuraScriptHookType.EffectAfterManaShield)
                throw new Exception($"Hook Type {hookType} is not valid for {nameof(EffectAbsorbHealHandler)}. Use {AuraScriptHookType.EffectAbsorbHeal}, {AuraScriptHookType.EffectAfterManaShield}, {AuraScriptHookType.EffectManaShield} or {AuraScriptHookType.EffectAfterAbsorbHeal}");
        }

        public void HandleAbsorb(AuraEffect aura, HealInfo healInfo, ref uint absorbAmount)
        {
            _fn(aura, healInfo, ref absorbAmount);
        }
    }
}