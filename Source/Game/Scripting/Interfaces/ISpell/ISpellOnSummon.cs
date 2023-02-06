using Game.Entities;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface ISpellOnSummon : ISpellScript
    {
        void OnSummon(Creature creature);
    }
}