// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Spells;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraCalcAmount : IAuraEffectHandler
    {
        void HandleCalcAmount(AuraEffect aurEff, ref double amount, ref bool canBeRecalculated);
    }

    public class AuraEffectCalcAmountHandler : AuraEffectHandler, IAuraCalcAmount
    {
        public delegate void AuraEffectCalcAmountDelegate(AuraEffect aurEff, ref double amount, ref bool canBeRecalculated);

        private readonly AuraEffectCalcAmountDelegate _fn;

        public AuraEffectCalcAmountHandler(AuraEffectCalcAmountDelegate fn, int effectIndex, AuraType auraType) : base(effectIndex, auraType, AuraScriptHookType.EffectCalcAmount)
        {
            _fn = fn;
        }

        public void HandleCalcAmount(AuraEffect aurEff, ref double amount, ref bool canBeRecalculated)
        {
            _fn(aurEff, ref amount, ref canBeRecalculated);
        }
    }
}