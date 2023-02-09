using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(191587)]
public class aura_dk_virulent_plague : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private void HandlePeriodic(AuraEffect UnnamedParameter)
	{
		var eruptionChances = GetEffectInfo(1).BasePoints;

		if (RandomHelper.randChance(eruptionChances))
			GetAura().Remove(AuraRemoveMode.Death);
	}

	private void HandleEffectRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var removeMode = GetTargetApplication().GetRemoveMode();

		if (removeMode == AuraRemoveMode.Death)
		{
			var caster = GetCaster();

			if (caster != null)
				caster.CastSpell(GetTarget(), DeathKnightSpells.SPELL_DK_VIRULENT_ERUPTION, true);
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDamage));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectRemove, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}
}