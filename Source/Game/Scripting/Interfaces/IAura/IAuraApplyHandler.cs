// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Spells;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraApplyHandler : IAuraEffectHandler
    {
        AuraEffectHandleModes Modes { get; }
        void Apply(AuraEffect aura, AuraEffectHandleModes auraMode);
    }

    public class AuraEffectApplyHandler : AuraEffectHandler, IAuraApplyHandler
    {
        public delegate void AuraEffectApplicationModeDelegate(AuraEffect aura, AuraEffectHandleModes auraMode);

        private readonly AuraEffectApplicationModeDelegate _fn;

        public AuraEffectApplyHandler(AuraEffectApplicationModeDelegate fn, uint effectIndex, AuraType auraType, AuraEffectHandleModes mode, AuraScriptHookType hookType = AuraScriptHookType.EffectApply) : base(effectIndex, auraType, hookType)
        {
            _fn = fn;
            Modes = mode;

            if (hookType != AuraScriptHookType.EffectApply &&
                hookType != AuraScriptHookType.EffectRemove &&
                hookType != AuraScriptHookType.EffectAfterApply &&
                hookType != AuraScriptHookType.EffectAfterRemove)
                throw new Exception($"Hook Type {hookType} is not valid for {nameof(AuraEffectApplyHandler)}. Use {AuraScriptHookType.EffectApply}, {AuraScriptHookType.EffectRemove}, {AuraScriptHookType.EffectAfterApply}, or {AuraScriptHookType.EffectAfterRemove}");
        }

        public AuraEffectHandleModes Modes { get; }

        public void Apply(AuraEffect aura, AuraEffectHandleModes auraMode)
        {
            if (Convert.ToBoolean(Modes & auraMode))
                _fn(aura, auraMode);
        }
    }
}