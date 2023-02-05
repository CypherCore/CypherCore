using Game.Entities;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface ISpellOnSummon : ISpellScript
    {
        void HandleSummon(Creature creature);
    }
}