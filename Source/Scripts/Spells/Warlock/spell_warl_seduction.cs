using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	[SpellScript(6358)] // 6358 - Seduction (Special Ability)
	internal class spell_warl_seduction : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(WarlockSpells.GLYPH_OF_SUCCUBUS, WarlockSpells.PRIEST_SHADOW_WORD_DEATH);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
		}

		private void HandleScriptEffect(int effIndex)
		{
			var caster = GetCaster();
			var target = GetHitUnit();

			if (target)
				if (caster.GetOwner() &&
				    caster.GetOwner().HasAura(WarlockSpells.GLYPH_OF_SUCCUBUS))
				{
					target.RemoveAurasByType(AuraType.PeriodicDamage, ObjectGuid.Empty, target.GetAura(WarlockSpells.PRIEST_SHADOW_WORD_DEATH)); // SW:D shall not be Removed.
					target.RemoveAurasByType(AuraType.PeriodicDamagePercent);
					target.RemoveAurasByType(AuraType.PeriodicLeech);
				}
		}
	}
}