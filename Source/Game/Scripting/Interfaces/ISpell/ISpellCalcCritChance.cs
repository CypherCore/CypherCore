using Game.Entities;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface ISpellCalcCritChance : ISpellScript
    {
        void CalcCritChance(Unit victim, ref float chance);
    }
}