namespace Game.Scripting.Interfaces.IAura
{
	public interface IAuraEnterLeaveCombat : IAuraScript
	{
		void EnterLeaveCombat(bool isNowInCombat);
	}
}