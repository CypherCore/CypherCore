using Framework.Constants;
using Game.Entities;
using Game.Spells.Auras.EffectHandlers;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraCalcCritChance : IAuraEffectHandler
    {
        void CalcCritChance(AuraEffect aura, Unit victim, ref float critChance);
    }

    public class EffectCalcCritChanceHandler : AuraEffectHandler, IAuraCalcCritChance
    {
        public delegate void AuraEffectCalcCritChanceFnType(AuraEffect aura, Unit victim, ref float critChance);

        private readonly AuraEffectCalcCritChanceFnType _fn;

        public EffectCalcCritChanceHandler(AuraEffectCalcCritChanceFnType fn, uint effectIndex, AuraType auraType) : base(effectIndex, auraType, AuraScriptHookType.EffectCalcCritChance)
        {
            _fn = fn;
        }

        public void CalcCritChance(AuraEffect aura, Unit victim, ref float critChance)
        {
            _fn(aura, victim, ref critChance);
        }
    }
}