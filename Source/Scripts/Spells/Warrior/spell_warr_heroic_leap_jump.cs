using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	[Script] // Heroic Leap (triggered by Heroic Leap (6544)) - 178368
	internal class spell_warr_heroic_leap_jump : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(WarriorSpells.GLYPH_OF_HEROIC_LEAP,
			                         WarriorSpells.GLYPH_OF_HEROIC_LEAP_BUFF,
			                         WarriorSpells.IMPROVED_HEROIC_LEAP,
			                         WarriorSpells.TAUNT);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(AfterJump, 1, SpellEffectName.JumpDest, SpellScriptHookType.EffectHit));
		}

		private void AfterJump(int effIndex)
		{
			if (GetCaster().HasAura(WarriorSpells.GLYPH_OF_HEROIC_LEAP))
				GetCaster().CastSpell(GetCaster(), WarriorSpells.GLYPH_OF_HEROIC_LEAP_BUFF, true);

			if (GetCaster().HasAura(WarriorSpells.IMPROVED_HEROIC_LEAP))
				GetCaster().GetSpellHistory().ResetCooldown(WarriorSpells.TAUNT, true);
		}
	}
}