using Framework.Constants;
using Game.Spells;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface IDestinationTargetSelectHandler : ITargetHookHandler
    {
        void SetDest(ref SpellDestination dest);
    }

    public class DestinationTargetSelectHandler : TargetHookHandler, IDestinationTargetSelectHandler
    {
        public delegate void SpellDestinationTargetSelectFnType(ref SpellDestination dest);

        private readonly SpellDestinationTargetSelectFnType _func;


        public DestinationTargetSelectHandler(SpellDestinationTargetSelectFnType func, uint effectIndex, Targets targetType, SpellScriptHookType hookType = SpellScriptHookType.DestinationTargetSelect) : base(effectIndex, targetType, false, hookType, true)
        {
            _func = func;
        }

        public void SetDest(ref SpellDestination dest)
        {
            _func(ref dest);
        }
    }
}