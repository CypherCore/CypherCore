// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    // Shield of the Righteous - 53600
    [SpellScript(53600)]
    public class spell_pal_shield_of_the_righteous : SpellScript, ISpellOnHit
    {
        public void OnHit()
        {
            Player player = GetCaster().ToPlayer();

            if (player == null || !GetHitUnit())
                return;

            if (player.FindNearestCreature(43499, 8) && player.HasAura(PaladinSpells.CONSECRATION)) //if player is standing in his consecration all effects are increased by 20%
            {
                int previousDuration = 0;

                Aura aur = player.GetAura(PaladinSpells.SHIELD_OF_THE_RIGHTEOUS_PROC);
                if (aur != null)
                {
                    previousDuration = aur.GetDuration();
                }

                double dmg = GetHitDamage();
                dmg += dmg / 5;
                SetHitDamage(dmg); //damage is increased by 20%

                double mastery = player.m_activePlayerData.Mastery;

                double reduction = ((-25 - mastery / 2.0f) * 120.0f) / 100.0f; //damage reduction is increased by 20%
                player.CastSpell(player, PaladinSpells.SHIELD_OF_THE_RIGHTEOUS_PROC, (int)reduction);

                aur = player.GetAura(PaladinSpells.SHIELD_OF_THE_RIGHTEOUS_PROC);
                if (aur != null)
                {
                    aur.SetDuration(aur.GetDuration() + previousDuration);
                }
            }
            else
            {
                int previousDuration = 0;

                Aura aur = player.GetAura(PaladinSpells.SHIELD_OF_THE_RIGHTEOUS_PROC);
                if (aur != null)
                {
                    previousDuration = aur.GetDuration();
                }

                player.CastSpell(player, PaladinSpells.SHIELD_OF_THE_RIGHTEOUS_PROC, true);

                aur = player.GetAura(PaladinSpells.SHIELD_OF_THE_RIGHTEOUS_PROC);
                if (aur != null)
                {
                    aur.SetDuration(aur.GetDuration() + previousDuration);
                }
            }

            Aura aura = player.GetAura(PaladinSpells.RIGHTEOUS_PROTECTOR);
            if (aura != null) //reduce the CD of Light of the Protector and Avenging Wrath by 3
            {
                TimeSpan cooldownReduction = TimeSpan.FromSeconds(aura.GetEffect(0).GetBaseAmount() * Time.InMilliseconds);

                if (player.HasSpell(PaladinSpells.LIGHT_OF_THE_PROTECTOR))
                {
                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(PaladinSpells.LIGHT_OF_THE_PROTECTOR, Difficulty.None);

                    if (spellInfo != null)
                        player.GetSpellHistory().ModifySpellCooldown(spellInfo.Id, cooldownReduction, false);
                }

                if (player.HasSpell(PaladinSpells.HAND_OF_THE_PROTECTOR))
                {
                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(PaladinSpells.HAND_OF_THE_PROTECTOR, Difficulty.None);

                    if (spellInfo != null)
                        player.GetSpellHistory().ModifySpellCooldown(spellInfo.Id, cooldownReduction, false);
                }

                SpellInfo spellInfoAR = Global.SpellMgr.GetSpellInfo(PaladinSpells.AvengingWrath, Difficulty.None);

                if (spellInfoAR != null)
                    player.GetSpellHistory().ModifySpellCooldown(spellInfoAR.Id, cooldownReduction, false);
            }
        }
    }
}
