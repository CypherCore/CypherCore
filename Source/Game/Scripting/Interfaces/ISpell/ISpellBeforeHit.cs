using Framework.Constants;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface ISpellBeforeHit : ISpellScript
    {
        public void BeforeHit(SpellMissInfo missInfo);
    }
}