// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.DeathKnight;

[SpellScript(77606)]
public class dark_simulacrum : AuraScript, IAuraOnProc
{
	public void OnProc(ProcEventInfo info)
	{
		var spellInfo = info.GetSpellInfo();
		var player    = GetCaster().ToPlayer();
		var target    = GetTarget();

		if (spellInfo != null && player != null && target != null && target.IsValidAttackTarget(player, spellInfo))
			player.CastSpell(target, spellInfo.Id, true);
	}
}