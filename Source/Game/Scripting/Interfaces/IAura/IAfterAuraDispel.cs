using Game.Entities;

namespace Game.Scripting.Interfaces.IAura
{
	public interface IAfterAuraDispel : IAuraScript
	{
		void HandleDispel(DispelInfo dispelInfo);
	}
}