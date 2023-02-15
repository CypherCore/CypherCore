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
                victim.HealBySpell(healInfo);
            }
            else
            {
                uint amount = GetSpell().StandardVariance(player.GetTotalSpellPowerValue(SpellSchoolMask.Shadow, false) * 0.65);

                switch (player.GetPrimarySpecialization())
                {
                    case TalentSpecialization.ShamanEnhancement:
                        var damageInfo = new SpellNonMeleeDamage(player, victim,
                            GetSpellInfo(), new(), SpellSchoolMask.Shadow);
                        damageInfo.damage = amount;

                        victim.DealSpellDamage(damageInfo, true);
                        break;
                    case TalentSpecialization.ShamanRestoration:
                        break;
                    case TalentSpecialization.ShamanElemental:
                        break;
                }
            }
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(ShamanSpells.PrimordialWave);
        }
    }
}

