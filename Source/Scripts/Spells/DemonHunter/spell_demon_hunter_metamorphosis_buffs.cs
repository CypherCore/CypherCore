using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(162264)]
public class spell_demon_hunter_metamorphosis_buffs : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		caster.RemoveAura(DemonHunterSpells.SPELL_DH_DEMONIC_ORIGINS_BUFF);
	}

	private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		if (caster.HasAura(DemonHunterSpells.SPELL_DH_DEMONIC_ORIGINS))
			caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_DEMONIC_ORIGINS_BUFF, true);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Transform, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.Transform, AuraEffectHandleModes.Real));
	}
}