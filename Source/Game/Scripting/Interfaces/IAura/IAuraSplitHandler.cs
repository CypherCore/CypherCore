using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraSplitHandler : IAuraEffectHandler
    {
        void Split(AuraEffect aura, DamageInfo damageInfo, uint splitAmount);
    }

    public class EffectSplitHandler : AuraEffectHandler, IAuraSplitHandler
    {
        public delegate void AuraEffectSplitDelegate(AuraEffect aura, DamageInfo damageInfo, uint splitAmount);
        AuraEffectSplitDelegate _fn;

        public EffectSplitHandler(AuraEffectSplitDelegate fn, uint effectIndex) : base(effectIndex, AuraType.SplitDamagePct, AuraScriptHookType.EffectSplit)
        {
            _fn = fn;
        }

        public void Split(AuraEffect aura, DamageInfo damageInfo, uint splitAmount)
        {
            _fn(aura, damageInfo, splitAmount);
        }
    }
}
