// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[SpellScript(190928)]
public class spell_hun_mongoose_bite : SpellScript, ISpellAfterHit
{
	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (Global.SpellMgr.GetSpellInfo(HunterSpells.SPELL_HUNTER_MONGOOSE_BITE, Difficulty.None) != null)
			return false;

		return true;
	}

	public void AfterHit()
	{
		var dur = 0;
		var aur = GetCaster().GetAura(HunterSpells.SPELL_HUNTER_MONGOOSE_FURY);

		if (aur != null)
			dur = aur.GetDuration();

		GetCaster().CastSpell(GetCaster(), HunterSpells.SPELL_HUNTER_MONGOOSE_FURY, true);

		aur = GetCaster().GetAura(HunterSpells.SPELL_HUNTER_MONGOOSE_FURY);

		if (aur != null)
			if (dur != 0)
				aur.SetDuration(dur);
	}
}