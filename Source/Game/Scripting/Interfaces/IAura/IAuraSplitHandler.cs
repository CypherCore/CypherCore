using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraSplitHandler : IAuraEffectHandler
    {
        void Split(AuraEffect aura, DamageInfo damageInfo, ref uint splitAmount);
    }

    public class EffectSplitHandler : AuraEffectHandler, IAuraSplitHandler
    {
        public delegate void AuraEffectSplitDelegate(AuraEffect aura, DamageInfo damageInfo, ref uint splitAmount);

        private readonly AuraEffectSplitDelegate _fn;

        public EffectSplitHandler(AuraEffectSplitDelegate fn, uint effectIndex) : base(effectIndex, AuraType.SplitDamagePct, AuraScriptHookType.EffectSplit)
        {
            _fn = fn;
        }

        public void Split(AuraEffect aura, DamageInfo damageInfo, ref uint splitAmount)
        {
            _fn(aura, damageInfo, ref splitAmount);
        }
    }
}