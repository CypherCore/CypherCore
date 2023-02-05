namespace Game.Scripting.Interfaces.ISpell
{
    public interface ISpellAfterCast : ISpellScript
    {
        public void AfterCast();
    }
}