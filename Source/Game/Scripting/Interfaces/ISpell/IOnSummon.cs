using Game.Entities;

namespace Game.Scripting.Interfaces.ISpell
{
	public interface IOnSummon : ISpellScript
	{
		void HandleSummon(Creature creature);
	}
}