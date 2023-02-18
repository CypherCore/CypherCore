// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman
{
	// 60103 - Lava Lash
	[SpellScript(60103)]
	public class spell_sha_lava_lash : SpellScript, ISpellOnHit
	{
		public override bool Load()
		{
			return GetCaster().IsPlayer();
		}

		public void OnHit()
		{
			GetCaster().CastSpell(GetHitUnit(), ShamanSpells.LAVA_LASH_SPREAD_FLAME_SHOCK, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.MaxTargets, GetEffectValue()));

			GetCaster().RemoveAurasDueToSpell(ShamanSpells.HOT_HAND);

			var target = GetHitUnit();

			if (target == null)
				return;

			if (GetCaster().HasAura(ShamanSpells.CRASHING_STORM_DUMMY) && GetCaster().HasAura(ShamanSpells.CRASH_LIGTHNING_AURA))
				GetCaster().CastSpell(target, ShamanSpells.CRASHING_LIGHTNING_DAMAGE, true);

			if (GetCaster() && GetCaster().HasAura(ShamanSpells.CRASH_LIGTHNING_AURA))
				GetCaster().CastSpell(null, ShamanSpells.CRASH_LIGHTNING_PROC, true);
		}
	}
}