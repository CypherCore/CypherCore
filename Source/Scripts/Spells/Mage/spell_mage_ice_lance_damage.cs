// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[SpellScript(228598)] // 228598 - Ice Lance
internal class spell_mage_ice_lance_damage : SpellScript, IHasSpellEffects, ISpellCalculateMultiplier
{
	public List<ISpellEffect> SpellEffects { get; } = new();

    public double CalcMultiplier(double multiplier)
    {
        if (GetSpell().unitTarget.HasAuraState(AuraStateType.Frozen, GetSpellInfo(), GetCaster()))
            multiplier *= 3.0f;

		return multiplier;
    }

    public override void Register()
	{
		SpellEffects.Add(new EffectHandler(ApplyDamageMultiplier, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}

	private void ApplyDamageMultiplier(int effIndex)
	{
		var spellValue = GetSpellValue();

		if ((spellValue.CustomBasePointsMask & (1 << 1)) != 0)
		{
			var originalDamage = GetHitDamage();
			var targetIndex    = spellValue.EffectBasePoints[1];
			var multiplier     = Math.Pow(GetEffectInfo().CalcDamageMultiplier(GetCaster(), GetSpell()), targetIndex);
			SetHitDamage(originalDamage * multiplier);
		}
	}
}