using System;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman
{
	[SpellScript(327163)]
	public class spell_sha_primordial_wave : SpellScript, ISpellOnHit
	{
        public void OnHit()
        {
            var player = GetCaster().ToPlayer();
            var victim = GetHitUnit();

            if (player == null || victim == null)
                return;

            uint amount = (uint)GetSpellInfo().GetEffect(0).CalcValue(player, null, victim);

            switch (player.GetPrimarySpecialization())
            {
                case TalentSpecialization.ShamanEnhancement:
                    if (player.IsFriendlyTo(victim))
                    {
                        var healInfo = new HealInfo(player, victim, amount, GetSpellInfo(), SpellSchoolMask.Shadow);
                        victim.HealBySpell(healInfo);
                    }
                    else
                    {
                        var damageInfo = new SpellNonMeleeDamage(player, victim,
                            GetSpellInfo(), new(), SpellSchoolMask.Shadow);
                        damageInfo.damage = amount;

                        victim.DealSpellDamage(damageInfo, true);
                    }
                    break;
                case TalentSpecialization.ShamanRestoration:
                    break;
                case TalentSpecialization.ShamanElemental:
                    break;
            }
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(ShamanSpells.PrimordialWave);
        }


    }
}

