// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[SpellScript(257537)]
public class spell_mage_ebonbolt : SpellScript, IHasSpellEffects, ISpellOnCast
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(MageSpells.SPELL_MAGE_SPLITTING_ICE, MageSpells.SPELL_MAGE_EBONBOLT_DAMAGE, MageSpells.SPELL_MAGE_BRAIN_FREEZE_AURA);
	}

	public void OnCast()
	{
		GetCaster().CastSpell(GetCaster(), MageSpells.SPELL_MAGE_BRAIN_FREEZE_AURA, true);
	}

	private void DoEffectHitTarget(uint UnnamedParameter)
	{
		var explTarget = GetExplTargetUnit();
		var hitUnit    = GetHitUnit();

		if (hitUnit == null || explTarget == null)
			return;

		if (GetCaster().HasAura(MageSpells.SPELL_MAGE_SPLITTING_ICE))
			GetCaster().VariableStorage.Set<ObjectGuid>("explTarget", explTarget.GetGUID());

		GetCaster().CastSpell(hitUnit, MageSpells.SPELL_MAGE_EBONBOLT_DAMAGE, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(DoEffectHitTarget, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}