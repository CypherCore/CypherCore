using Framework.Constants;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface ISpellEffectHandler : ISpellEffect
    {
        SpellEffectName EffectName { get; }

        void CallEffect(int effIndex);
    }

    public class EffectHandler : SpellEffect, ISpellEffectHandler
    {
        public delegate void SpellEffectFn(int index);

        private readonly SpellEffectFn _callEffect;

        public EffectHandler(SpellEffectFn callEffect, int effectIndex, SpellEffectName spellEffectName, SpellScriptHookType hookType) : base(effectIndex, hookType)
        {
            EffectName = spellEffectName;
            _callEffect = callEffect;
        }

        public SpellEffectName EffectName { get; private set; }

        public void CallEffect(int effIndex)
        {
            _callEffect(EffectIndex);
        }
    }
}