// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	[Script] // 24858 - Moonkin Form
	internal class spell_dru_glyph_of_stars : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Validate(SpellInfo spell)
		{
			return ValidateSpellInfo(DruidSpellIds.GlyphOfStars, DruidSpellIds.GlyphOfStarsVisual);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 1, AuraType.ModShapeshift, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
			AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 1, AuraType.ModShapeshift, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
		}

		private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
		{
			var target = GetTarget();

			if (target.HasAura(DruidSpellIds.GlyphOfStars))
				target.CastSpell(target, DruidSpellIds.GlyphOfStarsVisual, true);
		}

		private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
		{
			GetTarget().RemoveAura(DruidSpellIds.GlyphOfStarsVisual);
		}
	}
}