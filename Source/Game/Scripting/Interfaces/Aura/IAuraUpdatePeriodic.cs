using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Spells;

namespace Game.Scripting.Interfaces.Aura
{
    public interface IAuraUpdatePeriodic : IAuraEffectHandler
    {
        void UpdatePeriodic(AuraEffect aurEff);
    }

    public class EffectUpdatePeriodicHandler : AuraEffectHandler, IAuraUpdatePeriodic
    {
        public delegate void AuraEffectUpdatePeriodicDelegate(AuraEffect aura);
        AuraEffectUpdatePeriodicDelegate _fn;

        public EffectUpdatePeriodicHandler(AuraEffectUpdatePeriodicDelegate fn, uint effectIndex, AuraType auraType) : base(effectIndex, auraType, AuraScriptHookType.EffectUpdatePeriodic)
        {
            _fn = fn;
        }

        public void UpdatePeriodic(AuraEffect aurEff)
        {
            _fn(aurEff);
        }
    }
}
