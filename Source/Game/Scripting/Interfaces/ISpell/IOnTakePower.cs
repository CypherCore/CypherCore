using Game.Spells;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface IOnTakePower : ISpellScript
    {
        public void TakePower(SpellPowerCost cost);
    }
}