using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Spells;
using static Game.ScriptInfo;

namespace Game.Scripting.Interfaces.Aura
{
    public interface IAuraApplyHandler : IAuraEffectHandler
    {
        AuraEffectHandleModes Modes { get; }
        void Apply(AuraEffect aura, AuraEffectHandleModes auraMode);
    }

    public class EffectApplyHandler : AuraEffectHandler, IAuraApplyHandler
    {
        public delegate void AuraEffectApplicationModeDelegate(AuraEffect aura, AuraEffectHandleModes auraMode);
        AuraEffectApplicationModeDelegate _fn;

        public AuraEffectHandleModes Modes { get; }

        public EffectApplyHandler(AuraEffectApplicationModeDelegate fn, uint effectIndex, AuraType auraType, AuraEffectHandleModes mode, AuraScriptHookType hookType) : base(effectIndex, auraType, hookType)
        {
            _fn = fn;
            Modes = mode;

            if (hookType != AuraScriptHookType.EffectApply &&
                hookType != AuraScriptHookType.EffectRemove &&
                hookType != AuraScriptHookType.EffectAfterApply &&
                hookType != AuraScriptHookType.EffectAfterRemove)
                throw new Exception($"Hook Type {hookType} is not valid for {nameof(EffectApplyHandler)}. Use {AuraScriptHookType.EffectApply}, {AuraScriptHookType.EffectRemove}, {AuraScriptHookType.EffectAfterApply}, or {AuraScriptHookType.EffectAfterRemove}");
        }

        public void Apply(AuraEffect aura, AuraEffectHandleModes auraMode)
        {
            if (Convert.ToBoolean(Modes & auraMode))
                _fn(aura, auraMode);
        }
    }
}
