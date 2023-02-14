// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting.BaseScripts;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin
{
    // Light's Hammer - 122773
    [SpellScript(122773)]
    public class spell_pal_lights_hammer : SpellScript, ISpellAfterCast
    {
        public void AfterCast()
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                List<Creature> tempList = new List<Creature>();
                List<Creature> LightsHammerlist = new List<Creature>();

                LightsHammerlist = caster.GetCreatureListWithEntryInGrid(PaladinNPCs.NPC_PALADIN_LIGHTS_HAMMER, 200.0f);

                tempList = new List<Creature>(LightsHammerlist);

                for (List<Creature>.Enumerator i = tempList.GetEnumerator(); i.MoveNext();)
                {
                    Unit owner = i.Current.GetOwner();
                    if (owner != null && owner.GetGUID() == caster.GetGUID() && i.Current.IsSummon())
                    {
                        continue;
                    }

                    LightsHammerlist.Remove(i.Current);
                }

                foreach (var item in LightsHammerlist)
                {
                    item.CastSpell(item, PaladinSpells.LightHammerPeriodic, true);
                }
            }
        }
    }
}
