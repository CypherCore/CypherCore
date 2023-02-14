// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Spells;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraCalcPeriodic : IAuraEffectHandler
    {
        void CalcPeriodic(AuraEffect aura, ref bool isPeriodic, ref int amplitude);
    }

    public class AuraEffectCalcPeriodicHandler : AuraEffectHandler, IAuraCalcPeriodic
    {
        public delegate void AuraEffectCalcPeriodicDelegate(AuraEffect aura, ref bool isPeriodic, ref int amplitude);

        private readonly AuraEffectCalcPeriodicDelegate _fn;

        public AuraEffectCalcPeriodicHandler(AuraEffectCalcPeriodicDelegate fn, uint effectIndex, AuraType auraType) : base(effectIndex, auraType, AuraScriptHookType.EffectCalcPeriodic)
        {
            _fn = fn;
        }

        public void CalcPeriodic(AuraEffect aura, ref bool isPeriodic, ref int amplitude)
        {
            _fn(aura, ref isPeriodic, ref amplitude);
        }
    }
}