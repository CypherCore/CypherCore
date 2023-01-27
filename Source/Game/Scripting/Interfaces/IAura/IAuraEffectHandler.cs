using Framework.Constants;

namespace Game.Scripting.Interfaces.IAura
{
	public interface IAuraEffectHandler
	{
		uint EffectIndex { get; }

		AuraType AuraType { get; }

		AuraScriptHookType HookType { get; }
	}

	public class AuraEffectHandler : IAuraEffectHandler
	{
		public AuraEffectHandler(uint effectIndex, AuraType auraType, AuraScriptHookType hookType)
		{
			EffectIndex = effectIndex;
			AuraType    = auraType;
			HookType    = hookType;
		}

		public uint EffectIndex { get; private set; }

		public AuraType AuraType { get; private set; }

		public AuraScriptHookType HookType { get; private set; }
	}
}