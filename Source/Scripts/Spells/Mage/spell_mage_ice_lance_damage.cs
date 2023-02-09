using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[Script] // 228598 - Ice Lance
internal class spell_mage_ice_lance_damage : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

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
			var targetIndex    = (float)spellValue.EffectBasePoints[1];
			var multiplier     = MathF.Pow(GetEffectInfo().CalcDamageMultiplier(GetCaster(), GetSpell()), targetIndex);
			SetHitDamage((int)(originalDamage * multiplier));
		}
	}
}