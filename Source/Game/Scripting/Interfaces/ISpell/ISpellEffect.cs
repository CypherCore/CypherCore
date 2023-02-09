using Framework.Constants;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface ISpellEffect
    {
        int EffectIndex { get; }

        SpellScriptHookType HookType { get; }
    }

    public class SpellEffect : ISpellEffect
    {
        public SpellEffect(int effectIndex, SpellScriptHookType hookType)
        {
            EffectIndex = effectIndex;
            HookType = hookType;
        }

        public int EffectIndex { get; private set; }

        public SpellScriptHookType HookType { get; private set; }
    }
}