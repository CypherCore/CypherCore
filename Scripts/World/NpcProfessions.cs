/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.World
{
    enum GossipOptionIds
    {
        Alchemy = 0,
        Blacksmithing = 1,
        Enchanting = 2,
        Engineering = 3,
        Herbalism = 4,
        Inscription = 5,
        Jewelcrafting = 6,
        Leatherworking = 7,
        Mining = 8,
        Skinning = 9,
        Tailoring = 10,
        Multi = 11,
    }

    enum GossipMenuIds
    {
        Herbalism = 12188,
        Mining = 12189,
        Skinning = 12190,
        Alchemy = 12191,
        Blacksmithing = 12192,
        Enchanting = 12193,
        Engineering = 12195,
        Inscription = 12196,
        Jewelcrafting = 12197,
        Leatherworking = 12198,
        Tailoring = 12199,
    }

    [Script] //start menu multi profession trainer
    class npc_multi_profession_trainer : ScriptedAI
    {
        public npc_multi_profession_trainer(Creature creature) : base(creature) { }

        public override void sGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            switch ((GossipOptionIds)gossipListId)
            {
                case GossipOptionIds.Alchemy:
                case GossipOptionIds.Blacksmithing:
                case GossipOptionIds.Enchanting:
                case GossipOptionIds.Engineering:
                case GossipOptionIds.Herbalism:
                case GossipOptionIds.Inscription:
                case GossipOptionIds.Jewelcrafting:
                case GossipOptionIds.Leatherworking:
                case GossipOptionIds.Mining:
                case GossipOptionIds.Skinning:
                case GossipOptionIds.Tailoring:
                    SendTrainerList(player, (GossipOptionIds)gossipListId);
                    break;
                case GossipOptionIds.Multi:
                    {
                        switch ((GossipMenuIds)menuId)
                        {
                            case GossipMenuIds.Herbalism:
                                SendTrainerList(player, GossipOptionIds.Herbalism);
                                break;
                            case GossipMenuIds.Mining:
                                SendTrainerList(player, GossipOptionIds.Mining);
                                break;
                            case GossipMenuIds.Skinning:
                                SendTrainerList(player, GossipOptionIds.Skinning);
                                break;
                            case GossipMenuIds.Alchemy:
                                SendTrainerList(player, GossipOptionIds.Alchemy);
                                break;
                            case GossipMenuIds.Blacksmithing:
                                SendTrainerList(player, GossipOptionIds.Blacksmithing);
                                break;
                            case GossipMenuIds.Enchanting:
                                SendTrainerList(player, GossipOptionIds.Enchanting);
                                break;
                            case GossipMenuIds.Engineering:
                                SendTrainerList(player, GossipOptionIds.Engineering);
                                break;
                            case GossipMenuIds.Inscription:
                                SendTrainerList(player, GossipOptionIds.Inscription);
                                break;
                            case GossipMenuIds.Jewelcrafting:
                                SendTrainerList(player, GossipOptionIds.Jewelcrafting);
                                break;
                            case GossipMenuIds.Leatherworking:
                                SendTrainerList(player, GossipOptionIds.Leatherworking);
                                break;
                            case GossipMenuIds.Tailoring:
                                SendTrainerList(player, GossipOptionIds.Tailoring);
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        void SendTrainerList(Player player, GossipOptionIds Index)
        {
            player.GetSession().SendTrainerList(me.GetGUID(), (uint)Index + 1);
        }
    }
}