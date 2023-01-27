using Game.Entities;

namespace Game.Scripting.Interfaces.IAura
{
	public interface IOnAuraDispel : IAuraScript
	{
		void HandleDispel(DispelInfo dispelInfo);
	}
}