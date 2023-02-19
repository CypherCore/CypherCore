// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Spells;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraUpdatePeriodic : IAuraEffectHandler
    {
        void UpdatePeriodic(AuraEffect aurEff);
    }

    public class AuraEffectUpdatePeriodicHandler : AuraEffectHandler, IAuraUpdatePeriodic
    {
        public delegate void AuraEffectUpdatePeriodicDelegate(AuraEffect aura);

        private readonly AuraEffectUpdatePeriodicDelegate _fn;

        public AuraEffectUpdatePeriodicHandler(AuraEffectUpdatePeriodicDelegate fn, int effectIndex, AuraType auraType) : base(effectIndex, auraType, AuraScriptHookType.EffectUpdatePeriodic)
        {
            _fn = fn;
        }

        public void UpdatePeriodic(AuraEffect aurEff)
        {
            _fn(aurEff);
        }
    }
}