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
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using System.Collections.Generic;

namespace Scripts.Northrend.FrozenHalls.ForgeOfSouls
{
    struct DataType
    {
        // Encounter states and GUIDs
        public const uint Bronjahm = 0;
        public const uint DevourerOfSouls = 1;

        // Additional Data
        public const uint TeamInInstance = 2;
    }

    struct CreatureIds
    {
        public const uint Bronjahm = 36497;
        public const uint Devourer = 36502;
        public const uint CorruptedSoulFragment = 36535;

        public const uint SylvanasPart1 = 37596;
        public const uint SylvanasPart2 = 38161;
        public const uint JainaPart1 = 37597;
        public const uint JainaPart2 = 38160;
        public const uint Kalira = 37583;
        public const uint Elandra = 37774;
        public const uint Loralen = 37779;
        public const uint Koreln = 37582;
        public const uint Champion1Horde = 37584;
        public const uint Champion2Horde = 37587;
        public const uint Champion3Horde = 37588;
        public const uint Champion1Alliance = 37496;
        public const uint Champion2Alliance = 37497;
        public const uint CrucibleOfSouls = 37094;
    }

    struct EventIds
    {
        public const uint None = 0;

        // Jaina/Sylvanas Intro
        public const uint Intro1 = 1;
        public const uint Intro2 = 2;
        public const uint Intro3 = 3;
        public const uint Intro4 = 4;
        public const uint Intro5 = 5;
        public const uint Intro6 = 6;
        public const uint Intro7 = 7;
        public const uint Intro8 = 8;
    }

    struct TextIds
    {
        public const uint SayJainaIntro1 = 0;
        public const uint SayJainaIntro2 = 1;
        public const uint SayJainaIntro3 = 2;
        public const uint SayJainaIntro4 = 3;
        public const uint SayJainaIntro5 = 4;
        public const uint SayJainaIntro6 = 5;
        public const uint SayJainaIntro7 = 6;
        public const uint SayJainaIntro8 = 7;

        public const uint SaySylvanasIntro1 = 0;
        public const uint SaySylvanasIntro2 = 1;
        public const uint SaySylvanasIntro3 = 2;
        public const uint SaySylvanasIntro4 = 3;
        public const uint SaySylvanasIntro5 = 4;
        public const uint SaySylvanasIntro6 = 5;
    }

    struct Misc
    {
        public const uint MenuIdJaina = 10943;
        public const uint MenuIdSylvanas = 10971;
        public const uint GossipOptionId = 0;
    }

    enum Phase
    {
        Normal,
        Intro,
    }

    [Script]
    class instance_forge_of_souls : InstanceMapScript
    {
        public instance_forge_of_souls() : base(nameof(instance_forge_of_souls), 632) { }

        class instance_forge_of_souls_InstanceScript : InstanceScript
        {
            public instance_forge_of_souls_InstanceScript(InstanceMap map) : base(map)
            {
                SetHeaders("FOS");
                SetBossNumber(2);

                teamInInstance = 0;
            }

            public override void OnPlayerEnter(Player player)
            {
                if (teamInInstance == 0)
                    teamInInstance = player.GetTeam();
            }

            public override void OnCreatureCreate(Creature creature)
            {
                if (teamInInstance == 0)
                {
                    var players = instance.GetPlayers();
                    if (!players.Empty())
                    {
                        Player player = players[0];
                        if (player)
                            teamInInstance = player.GetTeam();
                    }
                }

                switch (creature.GetEntry())
                {
                    case CreatureIds.Bronjahm:
                        bronjahm = creature.GetGUID();
                        break;
                    case CreatureIds.Devourer:
                        devourerOfSouls = creature.GetGUID();
                        break;
                    case CreatureIds.SylvanasPart1:
                        if (teamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.JainaPart1);
                        break;
                    case CreatureIds.Loralen:
                        if (teamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.Elandra);
                        break;
                    case CreatureIds.Kalira:
                        if (teamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.Koreln);
                        break;
                }
            }

            public override uint GetData(uint type)
            {
                switch (type)
                {
                    case DataType.TeamInInstance:
                        return (uint)teamInInstance;
                    default:
                        break;
                }

                return 0;
            }

            public override ObjectGuid GetGuidData(uint type)
            {
                switch (type)
                {
                    case DataType.Bronjahm:
                        return bronjahm;
                    case DataType.DevourerOfSouls:
                        return devourerOfSouls;
                    default:
                        break;
                }

                return ObjectGuid.Empty;
            }

            ObjectGuid bronjahm;
            ObjectGuid devourerOfSouls;

            Team teamInInstance;
        }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_forge_of_souls_InstanceScript(map);
        }
    }

    [Script]
    class npc_sylvanas_fos : ScriptedAI
    {
        public npc_sylvanas_fos(Creature creature) : base(creature)
        {
            Initialize();
            me.SetFlag(UnitFields.NpcFlags, NPCFlags.Gossip);
        }

        void Initialize()
        {
            phase = Phase.Normal;
        }

        public override void Reset()
        {
            _events.Reset();
            Initialize();
        }

        public override void sGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            if (menuId == Misc.MenuIdSylvanas && gossipListId == Misc.GossipOptionId)
            {
                player.CLOSE_GOSSIP_MENU();
                phase = Phase.Intro;
                me.RemoveFlag(UnitFields.NpcFlags, NPCFlags.Gossip);

                _events.Reset();
                _events.ScheduleEvent(EventIds.Intro1, 1000);
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (phase == Phase.Intro)
            {
                _events.Update(diff);
                switch (_events.ExecuteEvent())
                {
                    case EventIds.Intro1:
                        Talk(TextIds.SaySylvanasIntro1);
                        _events.ScheduleEvent(EventIds.Intro2, 11500);
                        break;

                    case EventIds.Intro2:
                        Talk(TextIds.SaySylvanasIntro2);
                        _events.ScheduleEvent(EventIds.Intro3, 10500);
                        break;

                    case EventIds.Intro3:
                        Talk(TextIds.SaySylvanasIntro3);
                        _events.ScheduleEvent(EventIds.Intro4, 9500);
                        break;

                    case EventIds.Intro4:
                        Talk(TextIds.SaySylvanasIntro4);
                        _events.ScheduleEvent(EventIds.Intro5, 10500);
                        break;

                    case EventIds.Intro5:
                        Talk(TextIds.SaySylvanasIntro5);
                        _events.ScheduleEvent(EventIds.Intro6, 9500);
                        break;

                    case EventIds.Intro6:
                        Talk(TextIds.SaySylvanasIntro6);
                        // End of Intro
                        phase = Phase.Normal;
                        break;
                }
            }

            //Return since we have no target
            if (!UpdateVictim())
                return;

            _events.Update(diff);
            DoMeleeAttackIfReady();
        }

        Phase phase;
    }

    [Script]
    class npc_jaina_fos : ScriptedAI
    {
        public npc_jaina_fos(Creature creature) : base(creature)
        {
            Initialize();
            me.SetFlag(UnitFields.NpcFlags, NPCFlags.Gossip);
        }

        void Initialize()
        {
            phase = Phase.Normal;
        }

        public override void Reset()
        {
            _events.Reset();
            Initialize();
        }

        public override void sGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            if (menuId == Misc.MenuIdJaina && gossipListId == Misc.GossipOptionId)
            {
                player.CLOSE_GOSSIP_MENU();
                phase = Phase.Intro;
                me.RemoveFlag(UnitFields.NpcFlags, NPCFlags.Gossip);
                _events.Reset();
                _events.ScheduleEvent(EventIds.Intro1, 1000);
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (phase == Phase.Intro)
            {
                _events.Update(diff);
                switch (_events.ExecuteEvent())
                {
                    case EventIds.Intro1:
                        Talk(TextIds.SayJainaIntro1);
                        _events.ScheduleEvent(EventIds.Intro2, 8000);
                        break;

                    case EventIds.Intro2:
                        Talk(TextIds.SayJainaIntro2);
                        _events.ScheduleEvent(EventIds.Intro3, 8500);
                        break;

                    case EventIds.Intro3:
                        Talk(TextIds.SayJainaIntro3);
                        _events.ScheduleEvent(EventIds.Intro4, 8000);
                        break;

                    case EventIds.Intro4:
                        Talk(TextIds.SayJainaIntro4);
                        _events.ScheduleEvent(EventIds.Intro5, 10000);
                        break;

                    case EventIds.Intro5:
                        Talk(TextIds.SayJainaIntro5);
                        _events.ScheduleEvent(EventIds.Intro6, 8000);
                        break;

                    case EventIds.Intro6:
                        Talk(TextIds.SayJainaIntro6);
                        _events.ScheduleEvent(EventIds.Intro7, 12000);
                        break;

                    case EventIds.Intro7:
                        Talk(TextIds.SayJainaIntro7);
                        _events.ScheduleEvent(EventIds.Intro8, 8000);
                        break;

                    case EventIds.Intro8:
                        Talk(TextIds.SayJainaIntro8);
                        // End of Intro
                        phase = Phase.Normal;
                        break;
                }
            }

            //Return since we have no target
            if (!UpdateVictim())
                return;

            _events.Update(diff);

            DoMeleeAttackIfReady();
        }

        Phase phase;
    }
}
