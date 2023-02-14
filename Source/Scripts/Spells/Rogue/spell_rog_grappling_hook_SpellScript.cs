// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[SpellScript(195457)]
public class spell_rog_grappling_hook_SpellScript : SpellScript, ISpellOnHit
{
	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo((uint)TrueBearingIDs.SPELL_ROGUE_GRAPPLING_HOOK, RogueSpells.SPELL_ROGUE_GRAPPLING_HOOK_TRIGGER);
	}

	public void OnHit()
	{
		var caster = GetCaster();
		var dest   = GetExplTargetDest();

		if (caster == null || dest == null)
			return;

		caster.CastSpell(new Position(dest.GetPositionX(), dest.GetPositionY(), dest.GetPositionZ()), RogueSpells.SPELL_ROGUE_GRAPPLING_HOOK_TRIGGER, true);
	}
}