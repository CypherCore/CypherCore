// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_darkflight : SpellScript, ISpellAfterCast
{
	public void AfterCast()
	{
		GetCaster().CastSpell(GetCaster(), GenericSpellIds.AlteredForm, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
	}
}