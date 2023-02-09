using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(132464)]
public class spell_monk_chi_wave_heal_missile : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes UnnamedParameter)
	{
		Unit caster = GetCaster();
		Unit target = GetTarget();
		if (target == null || caster == null)
		{
			return;
		}

		caster.CastSpell(target, 132463, true);
		// rerun target selector
		caster.CastSpell(target, 132466, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint1, aurEff.GetAmount() - 1).SetTriggeringAura(aurEff));
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}
}