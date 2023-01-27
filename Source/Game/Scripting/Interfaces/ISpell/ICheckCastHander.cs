using Framework.Constants;

namespace Game.Scripting.Interfaces.ISpell
{
	public interface ICheckCastHander : ISpellScript
	{
		SpellCastResult CheckCast();
	}
}