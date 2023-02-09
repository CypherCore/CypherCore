using Game.Entities;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Shaman
{
    //210643 Totem Mastery
    [SpellScript(210643)]
    public class spell_sha_totem_mastery : SpellScript, ISpellOnCast
    {
        public void OnCast()
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            Player player = caster.ToPlayer();
            if (player == null)
            {
                return;
            }

            //Unsummon any Resonance Totem that the player already has. ID : 102392
            List<Creature> totemResoList = player.GetCreatureListWithEntryInGrid(102392, 500.0f);
            totemResoList.RemoveAll(creature =>
            {
                Unit owner = creature.GetOwner();
                return owner == null || owner != player || !creature.IsSummon();
            });

            if ((int)totemResoList.Count > 0)
            {
                totemResoList.Last().ToTempSummon().UnSummon();
            }

            //Unsummon any Storm Totem that the player already has. ID : 106317
            List<Creature> totemStormList = player.GetCreatureListWithEntryInGrid(106317, 500.0f);
            totemStormList.RemoveAll(creature =>
            {
                Unit owner = creature.GetOwner();
                return owner == null || owner != player || !creature.IsSummon();
            });

            if ((int)totemStormList.Count > 0)
            {
                totemStormList.Last().ToTempSummon().UnSummon();
            }

            //Unsummon any Ember Totem that the player already has. ID : 106319
            List<Creature> totemEmberList = player.GetCreatureListWithEntryInGrid(106319, 500.0f);
            totemEmberList.RemoveAll(creature =>
            {
                Unit owner = creature.GetOwner();
                return owner == null || owner != player || !creature.IsSummon();
            });

            if ((int)totemEmberList.Count > 0)
            {
                totemEmberList.Last().ToTempSummon().UnSummon();
            }

            //Unsummon any Tailwind Totem that the player already has. ID : 106321
            List<Creature> totemTailwindList = player.GetCreatureListWithEntryInGrid(106321, 500.0f);
            totemTailwindList.RemoveAll(creature =>
            {
                Unit owner = creature.GetOwner();
                return owner == null || owner != player || !creature.IsSummon();
            });

            if ((int)totemTailwindList.Count > 0)
            {
                totemTailwindList.Last().ToTempSummon().UnSummon();
            }
        }
    }
}
