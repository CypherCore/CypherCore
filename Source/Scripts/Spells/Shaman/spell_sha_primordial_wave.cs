// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman
{
	[SpellScript(375982)]
	public class spell_sha_primordial_wave : SpellScript, ISpellOnHit
	{
        public void OnHit()
        {
            var player = GetCaster().ToPlayer();
            var victim = GetHitUnit();

            if (player == null || victim == null)
                return;

            if (player.IsFriendlyTo(victim))
                player.CastSpell(victim, ShamanSpells.PrimordialWaveHealing, true);
            else
            {
                player.CastSpell(victim, ShamanSpells.PrimordialWaveDamage, true);
                player.AddAura(ShamanSpells.FlameShock, victim);
            }

            player.AddAura(ShamanSpells.PrimordialWaveAura, player);
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(ShamanSpells.PrimordialWave);
        }
    }
}

