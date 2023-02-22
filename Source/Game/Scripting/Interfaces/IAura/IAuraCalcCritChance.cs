// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraCalcCritChance : IAuraEffectHandler
    {
        void CalcCritChance(AuraEffect aura, Unit victim, ref double critChance);
    }

    public class AuraEffectCalcCritChanceHandler : AuraEffectHandler, IAuraCalcCritChance
    {
        public delegate void AuraEffectCalcCritChanceFnType(AuraEffect aura, Unit victim, ref double critChance);

        private readonly AuraEffectCalcCritChanceFnType _fn;

        public AuraEffectCalcCritChanceHandler(AuraEffectCalcCritChanceFnType fn, int effectIndex, AuraType auraType) : base(effectIndex, auraType, AuraScriptHookType.EffectCalcCritChance)
        {
            _fn = fn;
        }

        public void CalcCritChance(AuraEffect aura, Unit victim, ref double critChance)
        {
            _fn(aura, victim, ref critChance);
        }
    }
}