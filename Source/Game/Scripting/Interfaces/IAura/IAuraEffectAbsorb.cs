// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraEffectAbsorb : IAuraEffectHandler
    {
        void HandleAbsorb(AuraEffect aura, DamageInfo damageInfo, ref double absorbAmount);
    }

    public class AuraEffectAbsorbHandler : AuraEffectHandler, IAuraEffectAbsorb
    {
        public delegate void AuraEffectAbsorbDelegate(AuraEffect aura, DamageInfo damageInfo, ref double absorbAmount);

        private readonly AuraEffectAbsorbDelegate _fn;

        public AuraEffectAbsorbHandler(AuraEffectAbsorbDelegate fn, int effectIndex, bool overkill = false, AuraScriptHookType hookType = AuraScriptHookType.EffectAbsorb) : base(effectIndex, overkill ? AuraType.SchoolAbsorbOverkill : AuraType.SchoolAbsorb, hookType)
        {
            _fn = fn;

            if (hookType != AuraScriptHookType.EffectAbsorb &&
                hookType != AuraScriptHookType.EffectAfterAbsorb)
                throw new Exception($"Hook Type {hookType} is not valid for {nameof(AuraEffectAbsorbHandler)}. Use {AuraScriptHookType.EffectAbsorb} or {AuraScriptHookType.EffectAfterAbsorb}");
        }

        public void HandleAbsorb(AuraEffect aura, DamageInfo damageInfo, ref double absorbAmount)
        {
            _fn(aura, damageInfo, ref absorbAmount);
        }
    }
}