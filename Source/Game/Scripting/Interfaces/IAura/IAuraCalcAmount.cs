using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Spells;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraCalcAmount : IAuraEffectHandler
    {
        void HandleCalcAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated);
    }

    public class EffectCalcAmountHandler : AuraEffectHandler, IAuraCalcAmount
    {
        public delegate void AuraEffectCalcAmountDelegate(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated);
        AuraEffectCalcAmountDelegate _fn;

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
