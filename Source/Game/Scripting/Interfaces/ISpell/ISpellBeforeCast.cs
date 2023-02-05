namespace Game.Scripting.Interfaces.ISpell
{
    public interface ISpellBeforeCast : ISpellScript
    {
        public void BeforeCast();
    }
}