using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_pony_mount_check : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
	}

	private void HandleEffectPeriodic(AuraEffect aurEff)
	{
		var caster = GetCaster();

		if (!caster)
			return;

		var owner = caster.GetOwner().ToPlayer();

		if (!owner ||
		    !owner.HasAchieved(GenericSpellIds.AchievementPonyup))
			return;

		if (owner.IsMounted())
		{
			caster.Mount(GenericSpellIds.MountPony);
			caster.SetSpeedRate(UnitMoveType.Run, owner.GetSpeedRate(UnitMoveType.Run));
		}
		else if (caster.IsMounted())
		{
			caster.Dismount();
			caster.SetSpeedRate(UnitMoveType.Run, owner.GetSpeedRate(UnitMoveType.Run));
		}
	}
}