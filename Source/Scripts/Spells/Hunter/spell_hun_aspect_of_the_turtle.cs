using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[SpellScript(186265)]
public class spell_hun_aspect_of_the_turtle : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster != null)
		{
			caster.SetUnitFlag(UnitFlags.Pacified);
			caster.AttackStop();
		}
	}

	private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster != null)
			caster.RemoveUnitFlag(UnitFlags.Pacified);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 1, AuraType.DeflectSpells, AuraEffectHandleModes.Real));
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 1, AuraType.DeflectSpells, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}
}