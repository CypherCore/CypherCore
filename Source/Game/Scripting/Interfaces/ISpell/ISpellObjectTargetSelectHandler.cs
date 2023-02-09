using Framework.Constants;
using Game.Entities;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface ISpellObjectTargetSelectHandler : ITargetHookHandler
    {
        void TargetSelect(ref WorldObject targets);
    }

    public class ObjectTargetSelectHandler : TargetHookHandler, ISpellObjectTargetSelectHandler
    {
        public delegate void SpellObjectTargetSelectFnType(ref WorldObject targets);

        private readonly SpellObjectTargetSelectFnType _func;


        public ObjectTargetSelectHandler(SpellObjectTargetSelectFnType func, int effectIndex, Targets targetType, SpellScriptHookType hookType = SpellScriptHookType.ObjectTargetSelect) : base(effectIndex, targetType, false, hookType)
        {
            _func = func;
        }

        public void TargetSelect(ref WorldObject targets)
        {
            _func(ref targets);
        }
    }
}