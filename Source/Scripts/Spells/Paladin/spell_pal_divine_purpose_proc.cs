using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    // Divine Purpose Proc
    // Called by Seal of Light - 202273, Justicar's Vengeance - 215661, Word of Glory - 210191, Divine Storm - 53385, Templar's Verdict - 85256
    // Called by Holy Shock - 20473, Light of Dawn - 85222
    [SpellScript(new uint[] { 202273, 215661, 210191, 53385, 85256, 20473, 85222 })]
    public class spell_pal_divine_purpose_proc : SpellScript, ISpellAfterCast
    {
        public void AfterCast()
        {
            Player player = GetCaster().ToPlayer();
            if (player != null)
            {
                if (player.HasSpell(PaladinSpells.SPELL_PALADIN_DIVINE_PURPOSE_RET) || player.HasSpell(PaladinSpells.SPELL_PALADIN_DIVINE_PURPOSE_HOLY))
                {
                    uint spec = player.GetPrimarySpecialization();
                    uint activateSpell = GetSpellInfo().Id;

                    switch (spec)
                    {
                        case TalentSpecialization.PaladinRetribution:
                            {
                                if (RandomHelper.randChance(20))
                                {
                                    if (activateSpell == (uint)PaladinSpells.SPELL_PALADIN_JUSTICARS_VENGEANCE || activateSpell == (uint)PaladinSpells.SPELL_PALADIN_WORD_OF_GLORY || activateSpell == (uint)PaladinSpells.SPELL_PALADIN_DIVINE_STORM || activateSpell == (uint)PaladinSpells.SPELL_PALADIN_TEMPLARS_VERDICT)
                                    {
                                        player.CastSpell(player, PaladinSpells.SPELL_PALADIN_DIVINE_PURPOSE_RET_AURA);
                                    }
                                }
                                break;
                            }
                        case TalentSpecialization.PaladinHoly:
                            {
                                if (RandomHelper.randChance(15))
                                {
                                    if (activateSpell == (uint)PaladinSpells.SPELL_PALADIN_HOLY_SHOCK_GENERIC)
                                    {
                                        player.CastSpell(player, PaladinSpells.SPELL_PALADIN_DIVINE_PURPOSE_HOLY_AURA_1);

                                        if (player.GetSpellHistory().HasCooldown(PaladinSpells.SPELL_PALADIN_HOLY_SHOCK_GENERIC))
                                        {
                                            player.GetSpellHistory().ResetCooldown(PaladinSpells.SPELL_PALADIN_HOLY_SHOCK_GENERIC, true);
                                        }
                                    }

                                    if (activateSpell == (uint)PaladinSpells.SPELL_PALADIN_LIGHT_OF_DAWN)
                                    {
                                        player.CastSpell(player, PaladinSpells.SPELL_PALADIN_DIVINE_PURPOSE_HOLY_AURA_2);

                                        if (player.GetSpellHistory().HasCooldown(PaladinSpells.SPELL_PALADIN_LIGHT_OF_DAWN))
                                        {
                                            player.GetSpellHistory().ResetCooldown(PaladinSpells.SPELL_PALADIN_LIGHT_OF_DAWN, true);
                                        }
                                    }
                                }
                                break;
                            }
                    }
                }
            }
        }
    }
}
