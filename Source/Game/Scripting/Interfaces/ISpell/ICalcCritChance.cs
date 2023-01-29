using Game.Entities;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface ICalcCritChance : ISpellScript
    {
        void CalcCritChance(Unit victim, ref float chance);
    }
}