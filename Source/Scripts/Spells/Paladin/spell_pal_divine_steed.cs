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
    [SpellScript(190784)] // 190784 - Divine Steed
    internal class spell_pal_divine_steed : SpellScript, ISpellOnCast
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(PaladinSpells.DivineSteedHuman, PaladinSpells.DivineSteedDwarf, PaladinSpells.DivineSteedDraenei,
                PaladinSpells.DivineSteedDarkIronDwarf, PaladinSpells.DivineSteedBloodelf, PaladinSpells.DivineSteedTauren,
                PaladinSpells.DivineSteedZandalariTroll);
        }

        public void OnCast()
        {
            Unit caster = GetCaster();

            uint spellId = PaladinSpells.DivineSteedHuman;

            switch (caster.GetRace())
            {
                case Race.Human:
                    spellId = PaladinSpells.DivineSteedHuman;

                    break;
                case Race.Dwarf:
                    spellId = PaladinSpells.DivineSteedDwarf;

                    break;
                case Race.Draenei:
                case Race.LightforgedDraenei:
                    spellId = PaladinSpells.DivineSteedDraenei;

                    break;
                case Race.DarkIronDwarf:
                    spellId = PaladinSpells.DivineSteedDarkIronDwarf;

                    break;
                case Race.BloodElf:
                    spellId = PaladinSpells.DivineSteedBloodelf;

                    break;
                case Race.Tauren:
                    spellId = PaladinSpells.DivineSteedTauren;

                    break;
                case Race.ZandalariTroll:
                    spellId = PaladinSpells.DivineSteedZandalariTroll;

                    break;
                default:
                    break;
            }

            caster.CastSpell(caster, spellId, true);
        }
    }
}
