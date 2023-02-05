using Game.Entities;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface ISpellCalculateResistAbsorb : ISpellScript
    {
        void CalculateResistAbsorb(DamageInfo damageInfo, ref uint resistAmount, ref int absorbAmount);
    }
}