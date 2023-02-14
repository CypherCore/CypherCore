// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(new uint[]
             {
	             883, 83242, 83243, 83244, 83245
             })]
public class spell_hun_call_pet : SpellScript, ISpellCheckCast
{
	public SpellCastResult CheckCast()
	{
		return GetCaster().HasAura(HunterSpells.SPELL_HUNTER_LONE_WOLF) ? SpellCastResult.SpellUnavailable : SpellCastResult.SpellCastOk;
	}
}