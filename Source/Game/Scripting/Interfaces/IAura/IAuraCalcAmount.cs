using Framework.Constants;
using Game.Spells.Auras.EffectHandlers;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraCalcAmount : IAuraEffectHandler
    {
        void HandleCalcAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated);
    }

    public class EffectCalcAmountHandler : AuraEffectHandler, IAuraCalcAmount
    {
        public delegate void AuraEffectCalcAmountDelegate(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated);

        private readonly AuraEffectCalcAmountDelegate _fn;

        public EffectCalcAmountHandler(AuraEffectCalcAmountDelegate fn, uint effectIndex, AuraType auraType) : base(effectIndex, auraType, AuraScriptHookType.EffectCalcAmount)
        {
            _fn = fn;
        }

        public void HandleCalcAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            _fn(aurEff, ref amount, ref canBeRecalculated);
        }
    }
}