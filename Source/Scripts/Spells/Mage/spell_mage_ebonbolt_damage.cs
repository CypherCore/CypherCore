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

[SpellScript(257538)]
public class spell_mage_ebonbolt_damage : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(MageSpells.SPLITTING_ICE);
	}

	private void DoEffectHitTarget(int effIndex)
	{
		var hitUnit       = GetHitUnit();
		var primaryTarget = GetCaster().VariableStorage.GetValue<ObjectGuid>("explTarget", default);
		var damage        = GetHitDamage();

		if (hitUnit == null || primaryTarget == default)
			return;

		var eff1 = Global.SpellMgr.GetSpellInfo(MageSpells.SPLITTING_ICE, Difficulty.None).GetEffect(1).CalcValue();

		if (eff1 != 0)
			if (hitUnit.GetGUID() != primaryTarget)
				SetHitDamage(MathFunctions.CalculatePct(damage, eff1));
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(DoEffectHitTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}