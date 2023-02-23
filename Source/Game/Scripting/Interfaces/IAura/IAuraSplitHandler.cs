// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraSplitHandler : IAuraEffectHandler
    {
        void Split(AuraEffect aura, DamageInfo damageInfo, ref double splitAmount);
    }

    public class AuraEffectSplitHandler : AuraEffectHandler, IAuraSplitHandler
    {
        public delegate void AuraEffectSplitDelegate(AuraEffect aura, DamageInfo damageInfo, ref double splitAmount);

        private readonly AuraEffectSplitDelegate _fn;

        public AuraEffectSplitHandler(AuraEffectSplitDelegate fn, int effectIndex) : base(effectIndex, AuraType.SplitDamagePct, AuraScriptHookType.EffectSplit)
        {
            _fn = fn;
        }

        public void Split(AuraEffect aura, DamageInfo damageInfo, ref double splitAmount)
        {
            _fn(aura, damageInfo, ref splitAmount);
        }
    }
}