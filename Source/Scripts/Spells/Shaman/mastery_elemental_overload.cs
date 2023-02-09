using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using Game.Scripting.Interfaces.IPlayer;

namespace Scripts.Spells.Shaman
{
    //168534
    [Script]
    public class mastery_elemental_overload : ScriptObjectAutoAdd, IPlayerOnSpellCast
    {
        public mastery_elemental_overload() : base("mastery_elemental_overload")
        {
        }

        public void OnSpellCast(Player player, Spell spell, bool UnnamedParameter)
        {
            if (player.GetPrimarySpecialization() != TalentSpecialization.ShamanElemental)
            {
                return;
            }

            if (player.HasAura(ShamanSpells.SPELL_SHAMAN_MASTERY_ELEMENTAL_OVERLOAD) && RandomHelper.randChance(15))
            {
                SpellInfo spellInfo = spell.GetSpellInfo();

                if (spellInfo != null)
                {
                    switch (spell.GetSpellInfo().Id)
                    {
                        case ShamanSpells.SPELL_SHAMAN_LIGHTNING_BOLT_ELEM:
                            player.CastSpell(player.GetSelectedUnit(), ShamanSpells.SPELL_SHAMAN_LIGHTNING_BOLT_ELEM, true);
                            break;
                        case ShamanSpells.SPELL_SHAMAN_ELEMENTAL_BLAST:
                            player.CastSpell(player.GetSelectedUnit(), ShamanSpells.SPELL_SHAMAN_ELEMENTAL_BLAST, true);
                            break;
                        case ShamanSpells.SPELL_SHAMAN_LAVA_BURST:
                            player.CastSpell(player.GetSelectedUnit(), ShamanSpells.SPELL_SHAMAN_LAVA_BURST, true);
                            break;
                        case ShamanSpells.SPELL_SHAMAN_CHAIN_LIGHTNING:
                            player.CastSpell(player.GetSelectedUnit(), ShamanSpells.SPELL_SHAMAN_LAVA_BURST, true);
                            break;
                    }
                }
            }
        }
    }
}
