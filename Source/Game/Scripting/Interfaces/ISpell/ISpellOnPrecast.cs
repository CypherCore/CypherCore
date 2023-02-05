namespace Game.Scripting.Interfaces.ISpell
{
    public interface ISpellOnPrecast : ISpellScript
    {
        void OnPrecast();
    }
}