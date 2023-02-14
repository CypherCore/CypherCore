// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	// Victory Rush (heal) - 118779
	[SpellScript(118779)]
	public class spell_warr_victory_rush_heal : SpellScript, ISpellOnHit
	{
		public override bool Validate(SpellInfo UnnamedParameter)
		{
			return Global.SpellMgr.GetSpellInfo(WarriorSpells.GLYPH_OF_MIGHTY_VICTORY, Difficulty.None) != null;
		}

		public void OnHit()
		{
			var caster = GetCaster();
			var heal   = GetHitHeal();

			var GlyphOfVictoryRush = caster.GetAuraEffect(WarriorSpells.GLYPH_OF_MIGHTY_VICTORY, 0);

			if (GlyphOfVictoryRush != null)
				MathFunctions.AddPct(ref heal, GlyphOfVictoryRush.GetAmount());

			SetHitHeal(heal);
		}
	}
}