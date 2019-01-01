/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Game.AI;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Northrend.IcecrownCitadel
{
    [Script]
    class icecrown_citadel_teleport : GameObjectScript
    {
        public icecrown_citadel_teleport() : base("icecrown_citadel_teleport") { }

        public class icecrown_citadel_teleportAI : GameObjectAI
        {
            public icecrown_citadel_teleportAI(GameObject go) : base(go) { }

            public override bool GossipSelect(Player player, uint menuId, uint gossipListId)
            {
                if (gossipListId >= TeleportSpells.Length)
                    return false;

                player.PlayerTalkClass.ClearMenus();
                player.CLOSE_GOSSIP_MENU();
                SpellInfo spell = Global.SpellMgr.GetSpellInfo(TeleportSpells[gossipListId]);
                if (spell == null)
                    return false;

                if (player.IsInCombat())
                {
                    ObjectGuid castId = ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, player.GetMapId(), spell.Id, player.GetMap().GenerateLowGuid(HighGuid.Cast));
                    Spell.SendCastResult(player, spell, 0, castId, SpellCastResult.AffectingCombat);
                    return true;
                }

                return true;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return GetInstanceAI<icecrown_citadel_teleportAI>(go);
        }

        public const uint GOSSIP_SENDER_ICC_PORT = 631;

        static uint[] TeleportSpells =
        {
            TeleporterSpells.LIGHT_S_HAMMER_TELEPORT,        // 0
            TeleporterSpells.ORATORY_OF_THE_DAMNED_TELEPORT, // 1
            0,                              // 2
            TeleporterSpells.RAMPART_OF_SKULLS_TELEPORT,     // 3
            TeleporterSpells.DEATHBRINGER_S_RISE_TELEPORT,   // 4
            TeleporterSpells.UPPER_SPIRE_TELEPORT,           // 5
            TeleporterSpells.SINDRAGOSA_S_LAIR_TELEPORT      // 6
        };
    }

    [Script]
    class at_frozen_throne_teleport : AreaTriggerScript
    {
        public at_frozen_throne_teleport() : base("at_frozen_throne_teleport") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger, bool entered)
        {
            if (player.IsInCombat())
            {
                SpellInfo spell = Global.SpellMgr.GetSpellInfo(TeleporterSpells.FROZEN_THRONE_TELEPORT);
                if (spell != null)
                {
                    ObjectGuid castId = ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, player.GetMapId(), spell.Id, player.GetMap().GenerateLowGuid(HighGuid.Cast));
                    Spell.SendCastResult(player, spell, 0, castId, SpellCastResult.AffectingCombat);
                }
                return true;
            }
            InstanceScript instance = player.GetInstanceScript();
            if (instance != null)
                if (instance.GetBossState(Bosses.ProfessorPutricide) == EncounterState.Done &&
                    instance.GetBossState(Bosses.BloodQueenLanaThel) == EncounterState.Done &&
                    instance.GetBossState(Bosses.Sindragosa) == EncounterState.Done &&
                    instance.GetBossState(Bosses.TheLichKing) != EncounterState.InProgress)
                    player.CastSpell(player, TeleporterSpells.FROZEN_THRONE_TELEPORT, true);

            return true;
        }
    }
}
