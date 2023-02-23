// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[Script] // 53 - Backstab
internal class spell_rog_backstab_SpellScript : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return spellInfo.GetEffects().Count > 3;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHitDamage, 1, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleHitDamage(int effIndex)
	{
		var hitUnit = GetHitUnit();

		if (!hitUnit)
			return;

		var caster = GetCaster();

		if (hitUnit.IsInBack(caster))
		{
			var currDamage = (double)GetHitDamage();
			MathFunctions.AddPct(ref currDamage, (double)GetEffectInfo(3).CalcValue(caster));
			SetHitDamage((int)currDamage);
		}
	}
}