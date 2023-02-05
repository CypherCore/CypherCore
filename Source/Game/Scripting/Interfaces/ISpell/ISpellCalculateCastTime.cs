namespace Game.Scripting.Interfaces.ISpell
{
    public interface ISpellCalculateCastTime : ISpellScript
    {
        public int CalcCastTime(int castTime);
    }
}