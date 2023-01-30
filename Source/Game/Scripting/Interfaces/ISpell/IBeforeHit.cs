using Framework.Constants;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface IBeforeHit : ISpellScript
    {
        public void BeforeHit(SpellMissInfo missInfo);
    }
}