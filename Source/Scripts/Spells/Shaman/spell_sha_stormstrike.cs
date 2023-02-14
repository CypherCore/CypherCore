// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman
{
	//17364
	[SpellScript(17364)]
	public class spell_sha_stormstrike : SpellScript, ISpellOnHit
	{
		public void OnHit()
		{
			var target = GetHitUnit();

			if (target == null)
				return;

			if (GetCaster().HasAura(ShamanSpells.SPELL_SHAMAN_CRASHING_STORM_DUMMY) && GetCaster().HasAura(ShamanSpells.SPELL_SHAMAN_CRASH_LIGTHNING_AURA))
				GetCaster().CastSpell(target, ShamanSpells.SPELL_SHAMAN_CRASHING_LIGHTNING_DAMAGE, true);

			if (GetCaster() && GetCaster().HasAura(ShamanSpells.SPELL_SHAMAN_CRASH_LIGTHNING_AURA))
				GetCaster().CastSpell(null, ShamanSpells.SPELL_SHAMAN_CRASH_LIGHTNING_PROC, true);
		}
	}
}