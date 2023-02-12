using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// Life Tap - 1454
	[SpellScript(1454)]
	public class spell_warl_life_tap : SpellScript, IHasSpellEffects, ISpellCheckCast
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public struct lifeTap
		{
			public const uint LIFE_TAP = 1454;
			public const uint LIFE_TAP_GLYPH = 63320;
		}

		public SpellCastResult CheckCast()
		{
			if (GetCaster().GetHealthPct() > 15.0f || GetCaster().HasAura(lifeTap.LIFE_TAP_GLYPH))
				return SpellCastResult.SpellCastOk;

			return SpellCastResult.Fizzle;
		}

		private void HandleOnHitTarget(uint effIndex)
		{
			PreventHitDefaultEffect(effIndex);
			// if (!GetCaster()->HasAura(LIFE_TAP_GLYPH))
			//   GetCaster()->EnergizeBySpell(GetCaster(), LIFE_TAP, int32(GetCaster()->GetMaxHealth() * GetSpellInfo()->GetEffect(uint::0).BasePoints / 100), PowerType.Mana); TODO REWRITE
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleOnHitTarget, 0, SpellEffectName.Energize, SpellScriptHookType.EffectHitTarget));
		}
	}
}