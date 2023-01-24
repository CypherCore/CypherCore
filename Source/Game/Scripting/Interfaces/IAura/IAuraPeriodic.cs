using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Spells;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraPeriodic : IAuraEffectHandler
    {
        void HandlePeriodic(AuraEffect aurEff);
    }

    public class EffectPeriodicHandler : AuraEffectHandler, IAuraPeriodic
    {
        public delegate void AuraEffectPeriodicDelegate(AuraEffect aura);
        AuraEffectPeriodicDelegate _fn;

        public EffectPeriodicHandler(AuraEffectPeriodicDelegate fn, uint effectIndex, AuraType auraType) : base(effectIndex, auraType, AuraScriptHookType.EffectPeriodic)
        {
            _fn = fn;
        }

        public void HandlePeriodic(AuraEffect aurEff)
        {
            _fn(aurEff);
        }
    }
}
