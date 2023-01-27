namespace Game.Scripting.Interfaces.ISpell
{
	public interface ICalculateCastTime : ISpellScript
	{
		public int CalcCastTime(int castTime);
	}
}