using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(new uint[]
             {
	             178963, 203794, 228532
             })]
public class spell_dh_soul_fragment_heals : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();

	private void HandleHit(uint effIndex)
	{
		if (!GetCaster().HasAura(DemonHunterSpells.SPELL_DH_DEMONIC_APPETITE))
			PreventHitDefaultEffect(effIndex);
	}

	private void HandleHeal(uint UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		var reductionTime = caster.GetAuraEffectAmount(DemonHunterSpells.SPELL_DH_FEAST_ON_THE_SOULS, 0);

		if (reductionTime != 0)
		{
			caster.GetSpellHistory().ModifyCooldown(DemonHunterSpells.SPELL_DH_CHAOS_NOVA, TimeSpan.FromSeconds(-reductionTime));
			caster.GetSpellHistory().ModifyCooldown(DemonHunterSpells.SPELL_DH_EYE_BEAM, TimeSpan.FromSeconds(-reductionTime));
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHeal, 0, SpellEffectName.Any, SpellScriptHookType.EffectHitTarget));
		SpellEffects.Add(new EffectHandler(HandleHit, 1, SpellEffectName.TriggerSpell, SpellScriptHookType.LaunchTarget));
		SpellEffects.Add(new EffectHandler(HandleHit, 1, SpellEffectName.TriggerSpell, SpellScriptHookType.Launch));
	}
}