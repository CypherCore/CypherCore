using Framework.Constants;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface ISpellEffect
    {
        uint EffectIndex { get; }

        SpellScriptHookType HookType { get; }
    }

    public class SpellEffect : ISpellEffect
    {
        public SpellEffect(uint effectIndex, SpellScriptHookType hookType)
        {
            EffectIndex = effectIndex;
            HookType = hookType;
        }

        public uint EffectIndex { get; private set; }

        public SpellScriptHookType HookType { get; private set; }
    }
}