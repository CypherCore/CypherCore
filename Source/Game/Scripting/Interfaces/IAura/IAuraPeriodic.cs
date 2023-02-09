using Framework.Constants;
using Game.Spells;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraPeriodic : IAuraEffectHandler
    {
        void HandlePeriodic(AuraEffect aurEff);
    }

    public class AuraEffectPeriodicHandler : AuraEffectHandler, IAuraPeriodic
    {
        public delegate void AuraEffectPeriodicDelegate(AuraEffect aura);

        private readonly AuraEffectPeriodicDelegate _fn;

        public AuraEffectPeriodicHandler(AuraEffectPeriodicDelegate fn, int effectIndex, AuraType auraType) : base(effectIndex, auraType, AuraScriptHookType.EffectPeriodic)
        {
            _fn = fn;
        }

        public void HandlePeriodic(AuraEffect aurEff)
        {
            _fn(aurEff);
        }
    }
}