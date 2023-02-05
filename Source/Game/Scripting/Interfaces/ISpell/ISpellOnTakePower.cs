using Game.Spells;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface ISpellOnTakePower : ISpellScript
    {
        public void TakePower(SpellPowerCost cost);
    }
}