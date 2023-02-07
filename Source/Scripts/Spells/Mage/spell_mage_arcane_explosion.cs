using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 1449 - Arcane Explosion
internal class spell_mage_arcane_explosion : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		if (!ValidateSpellInfo(MageSpells.ArcaneMage, MageSpells.Reverberate))
			return false;

		if (spellInfo.GetEffects().Count <= 1)
			return false;

		return spellInfo.GetEffect(1).IsEffect(SpellEffectName.SchoolDamage);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(CheckRequiredAuraForBaselineEnergize, 0, SpellEffectName.Energize, SpellScriptHookType.EffectHitTarget));
		SpellEffects.Add(new EffectHandler(HandleReverberate, 2, SpellEffectName.Energize, SpellScriptHookType.EffectHitTarget));
	}

	private void CheckRequiredAuraForBaselineEnergize(uint effIndex)
	{
		if (GetUnitTargetCountForEffect(1) == 0 ||
		    !GetCaster().HasAura(MageSpells.ArcaneMage))
			PreventHitDefaultEffect(effIndex);
	}

	private void HandleReverberate(uint effIndex)
	{
		bool procTriggered = false;

		Unit       caster        = GetCaster();
		AuraEffect triggerChance = caster.GetAuraEffect(MageSpells.Reverberate, 0);

		if (triggerChance != null)
		{
			AuraEffect requiredTargets = caster.GetAuraEffect(MageSpells.Reverberate, 1);

			if (requiredTargets != null)
				procTriggered = GetUnitTargetCountForEffect(1) >= requiredTargets.GetAmount() && RandomHelper.randChance(triggerChance.GetAmount());
		}

		if (!procTriggered)
			PreventHitDefaultEffect(effIndex);
	}
}