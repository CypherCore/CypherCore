using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(127757)]
public class aura_dru_charm_woodland_creature : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		// Make targeted creature follow the player - Using pet's default dist and angle
		//if (Unit* caster = GetCaster())
		//if (Unit* target = GetTarget())
		//target->GetMotionMaster()->MoveFollow(caster, PET_FOLLOW_DIST, PET_FOLLOW_ANGLE);

		var caster = GetCaster();
		var target = GetTarget();

		if (caster != null && target != null)
		{
			target.GetMotionMaster().MoveFollow(caster, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
		}
	}

	private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		//if (Unit* target = GetTarget())
		//if (target->GetMotionMaster()->GetCurrentMovementGeneratorType() == FOLLOW_MOTION_TYPE)
		//target->GetMotionMaster()->MovementExpired(true); // reset movement
		var target = GetTarget();

		if (target != null)
		{
			if (target.GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Follow)
			{
				target.GetMotionMaster().Initialize();
			}
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new EffectApplyHandler(OnApply, 0, AuraType.AoeCharm, AuraEffectHandleModes.Real));
		AuraEffects.Add(new EffectApplyHandler(OnRemove, 0, AuraType.AoeCharm, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}
}