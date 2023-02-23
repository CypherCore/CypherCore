// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Bgs.Protocol.Notification.V1;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman
{
    [SpellScript(new uint[] { 51505, 188196, 77472 })]
	public class aura_sha_primordial_wave : SpellScript, ISpellAfterCast, ISpellCalculateMultiplier
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(ShamanSpells.PrimordialWaveAura, ShamanSpells.LavaBurst, ShamanSpells.LightningBolt, ShamanSpells.HealingWave);
        }

        public void AfterCast()
        {
            if (GetSpell().IsTriggered())
                return;

            var player = GetCaster().ToPlayer();

            if (player == null || !player.HasAura(ShamanSpells.PrimordialWaveAura))
                return;

            var spec = player.GetPrimarySpecialization();
            var spellId = GetSpellInfo().Id;

            uint procSpell = 0;
            List<Unit> targets = new();
            if (spec == TalentSpecialization.ShamanElemental && spellId == ShamanSpells.LavaBurst)
            {
                player.GetEnemiesWithinRangeWithOwnedAura(targets, 100.0f, ShamanSpells.FlameShock);
                procSpell = ShamanSpells.LavaBurst;
            }
            else if (spec == TalentSpecialization.ShamanEnhancement && spellId == ShamanSpells.LightningBolt)
            {
                player.GetEnemiesWithinRangeWithOwnedAura(targets, 100.0f, ShamanSpells.FlameShock);
                procSpell = ShamanSpells.LightningBolt;
            }
            else if (spec == TalentSpecialization.ShamanRestoration && spellId == ShamanSpells.HealingWave)
            {
                player.GetAlliesWithinRangeWithOwnedAura(targets, 100.0f, ShamanSpells.Riptide);
                procSpell = ShamanSpells.Riptide;
            }

            if (procSpell != 0)
            {
                foreach (var target in targets)
                    player.CastSpell(target, procSpell, true);

                player.RemoveAura(ShamanSpells.PrimordialWaveAura);
            }
        }

        public double CalcMultiplier(double multiplier)
        {
            var player = GetCaster().ToPlayer();

            if (player == null || !player.HasAura(ShamanSpells.PrimordialWaveAura))
                return multiplier;

            var spec = player.GetPrimarySpecialization();
            var spellId = GetSpellInfo().Id;

            if (spec == TalentSpecialization.ShamanElemental && spellId == ShamanSpells.LavaBurst)
            {
                var primordialWave = SpellManager.Instance.GetSpellInfo(ShamanSpells.PrimordialWave);
                double pct = primordialWave.GetEffect(2).BasePoints * 0.01f;
                multiplier *= 1f + pct;
            }
            else if (spec == TalentSpecialization.ShamanEnhancement && spellId == ShamanSpells.LightningBolt)
            {
                var primordialWave = SpellManager.Instance.GetSpellInfo(ShamanSpells.PrimordialWave);
                double pct = primordialWave.GetEffect(3).BasePoints * 0.01f;
                multiplier *= 1f + pct;
            }
            else if (spec == TalentSpecialization.ShamanRestoration && spellId == ShamanSpells.HealingWave)
            {
                var primordialWave = SpellManager.Instance.GetSpellInfo(ShamanSpells.PrimordialWave);
                double pct = primordialWave.GetEffect(1).BasePoints * 0.01f;
                multiplier *= 1f + pct;
            }

            return multiplier;
        }
    }
}

