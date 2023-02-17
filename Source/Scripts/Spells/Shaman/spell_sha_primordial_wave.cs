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
            {
                uint amount = GetSpell().StandardVariance(player.GetTotalSpellPowerValue(SpellSchoolMask.Shadow, true) * 0.65);
                var healInfo = new HealInfo(player, victim, amount, GetSpellInfo(), SpellSchoolMask.Shadow);
                victim.HealBySpell(healInfo, IsHitCrit());
            }
            else
            {
                uint amount = GetSpell().StandardVariance(player.GetTotalSpellPowerValue(SpellSchoolMask.Shadow, false) * 0.65);
                var damageInfo = new SpellNonMeleeDamage(player, victim,
                    GetSpellInfo(), new(), SpellSchoolMask.Shadow);
                damageInfo.damage = IsHitCrit() ? amount * 2 : amount;

                victim.DealSpellDamage(damageInfo, true);
                player.AddAura(ShamanSpells.FlameShock, victim);

                if (!victim.IsInCombatWith(player))
                    victim.SetInCombatWith(player);
            }

            player.AddAura(ShamanSpells.PrimordialWaveAura, player);
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(ShamanSpells.PrimordialWave);
        }
    }
}

