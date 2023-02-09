using Framework.Constants;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface ITargetHookHandler : ISpellEffect
    {
        Targets TargetType { get; }
        bool Area { get { return true; } }
        bool Dest { get { return false; } }
    }

    public class TargetHookHandler : SpellEffect, ITargetHookHandler
    {
        public TargetHookHandler(uint effectIndex, Targets targetType, bool area, SpellScriptHookType hookType, bool dest = false) : base(effectIndex, hookType)
        {
            TargetType = targetType;
            Area = area;
            Dest = dest;
        }

        public Targets TargetType { get; }

        public bool Area { get; }

        public bool Dest { get; }
    }
}