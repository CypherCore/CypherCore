using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman
{
	// 51490 - Thunderstorm
	[SpellScript(51490)]
	public class spell_sha_thunderstorm : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		private void HandleKnockBack(int effIndex)
		{
			// Glyph of Thunderstorm
			if (GetCaster().HasAura(ShamanSpells.SPELL_SHAMAN_GLYPH_OF_THUNDERSTORM))
				PreventHitDefaultEffect(effIndex);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleKnockBack, 1, SpellEffectName.KnockBack, SpellScriptHookType.EffectHitTarget));
		}
	}
}