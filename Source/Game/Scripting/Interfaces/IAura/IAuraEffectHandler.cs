using Framework.Constants;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraEffectHandler
    {
        int EffectIndex { get; }

        AuraType AuraType { get; }

        AuraScriptHookType HookType { get; }
    }

    public class AuraEffectHandler : IAuraEffectHandler
    {
        public AuraEffectHandler(int effectIndex, AuraType auraType, AuraScriptHookType hookType)
        {
            EffectIndex = effectIndex;
            AuraType = auraType;
            HookType = hookType;
        }

        public int EffectIndex { get; private set; }

        public AuraType AuraType { get; private set; }

        public AuraScriptHookType HookType { get; private set; }
    }
}