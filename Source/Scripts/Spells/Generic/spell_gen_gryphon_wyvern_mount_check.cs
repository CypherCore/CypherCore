using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 69641 - Gryphon/Wyvern Pet - Mounting Check Aura
internal class spell_gen_gryphon_wyvern_mount_check : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
	}

	private void HandleEffectPeriodic(AuraEffect aurEff)
	{
		var target = GetTarget();
		var owner  = target.GetOwner();

		if (owner == null)
			return;

		if (owner.IsMounted())
			target.SetDisableGravity(true);
		else
			target.SetDisableGravity(false);
	}
}