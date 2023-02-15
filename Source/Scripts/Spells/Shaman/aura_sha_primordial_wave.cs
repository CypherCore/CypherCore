using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman
{
    [SpellScript(new uint[] { 51505, 188196, 77472 })]
	public class aura_sha_primordial_wave : SpellScript, ISpellOnCast
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(ShamanSpells.PrimordialWaveAura, ShamanSpells.LavaBurst, ShamanSpells.LightningBolt, ShamanSpells.HealingWave);
        }

        public void OnCast()
        {
            var player = GetCaster().ToPlayer();

            if (player == null || !player.HasAura(ShamanSpells.PrimordialWaveAura))
                return;

            var spec = player.GetPrimarySpecialization();
            var spellId = GetSpellInfo().Id;

            uint procSpell = 0;
            List<Creature> targets = new();
            if (spec == TalentSpecialization.ShamanElemental && spellId == ShamanSpells.LavaBurst)
            {
                player.GetEnemiesWithOwnedAura(targets, 100, ShamanSpells.FlameShock);
                procSpell = ShamanSpells.LavaBurst;
            }
            else if (spec == TalentSpecialization.ShamanEnhancement && spellId == ShamanSpells.LightningBolt)
            {
                player.GetEnemiesWithOwnedAura(targets, 100, ShamanSpells.FlameShock);
                procSpell = ShamanSpells.LightningBolt;
            }
            else if (spec == TalentSpecialization.ShamanRestoration && spellId == ShamanSpells.HealingWave)
            {
                player.GetAlliesWithOwnedAura(targets, 100, ShamanSpells.Riptide);
                procSpell = ShamanSpells.Riptide;
            }

            if (procSpell != 0)
            {
                foreach (var target in targets)
                    player.CastSpell(target, procSpell, true);

                player.RemoveAura(ShamanSpells.PrimordialWaveAura);
            }
        }
    }
}

