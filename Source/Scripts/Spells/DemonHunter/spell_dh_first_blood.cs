using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.DemonHunter;

[Script] // 206416 - First Blood
internal class spell_dh_first_blood : AuraScript
{
	private ObjectGuid _firstTargetGUID;

	public ObjectGuid GetFirstTarget()
	{
		return _firstTargetGUID;
	}

	public void SetFirstTarget(ObjectGuid targetGuid)
	{
		_firstTargetGUID = targetGuid;
	}

	public override void Register()
	{
	}
}