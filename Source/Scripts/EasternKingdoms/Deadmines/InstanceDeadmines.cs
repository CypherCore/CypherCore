// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Mails;
using Game.Maps;
using System.Linq;
using Game.Scripting.BaseScripts;
using Game.Scripting.Interfaces.IMap;
using Game.Scripting;

namespace Scripts.EasternKingdoms.Deadmines
{

    //[Script]
    internal class instance_deadmines : InstanceMapScript, IInstanceMapGetInstanceScript
    {
        private class instance_deadmines_InstanceMapScript : InstanceScript
        {
            public static readonly Position NoteSpawn = new Position(-74.36111f, -820.0139f, 40.67145f, 4.014257f);
            public const string NOTE_TEXT = "A note falls to the floor!";

            private ObjectGuid _vanessa;
            private ObjectGuid _vanessaNote;
            private ObjectGuid _vanessaBoss;
            private ObjectGuid _glubtokGUID;

            private Team _teamInInstance;

            /// https://wowpedia.fandom.com/wiki/DungeonEncounterID
            private static readonly DungeonEncounterData[] _encounters =
            {
                new(DMData.DATA_HELIX, 1065),
                // new(DMData.DATA_NIGHTMARE_HELIX, 1065),
                new(DMData.DATA_GLUBTOK, 1064),
                new(DMData.DATA_COOKIE, 1060),
                new(DMData.DATA_FOEREAPER, 1063),
                new(DMData.DATA_RIPSNARL, 1062),
                new(DMData.DATA_VANESSA, 1081),
                // new(DMData.DATA_VANESSA_NIGHTMARE, 1081)
            };

            private static DoorData[] _doorData =
            {
                new(DMGameObjects.GO_FACTORY_DOOR, DMData.DATA_GLUBTOK, DoorType.Passage),
                new(DMGameObjects.GO_HEAVY_DOOR_HELIX, DMData.DATA_HELIX, DoorType.Passage),
                new(DMGameObjects.GO_FOUNDRY_DOOR, DMData.DATA_FOEREAPER, DoorType.Passage),
                new(DMGameObjects.GO_IRONCLAD_DOOR, DMData.DATA_FOEREAPER, DoorType.Passage),

            };

            private static ObjectData[] _creatureData =
            {
                new(DMCreatures.NPC_HELIX_GEARBREAKER, DMData.DATA_HELIX), 
                new(DMCreatures.NPC_HELIX_NIGHTMARE, DMData.DATA_NIGHTMARE_HELIX), 
                new(DMCreatures.NPC_GLUBTOK, DMData.DATA_GLUBTOK), 
                new(DMCreatures.NPC_CAPTAIN_COOKIE, DMData.DATA_COOKIE), 
                new(DMCreatures.NPC_FOE_REAPER_5000, DMData.DATA_FOEREAPER), 
                new(DMCreatures.NPC_ADMIRAL_RIPSNARL, DMData.DATA_RIPSNARL), 
                new(DMCreatures.NPC_VANESSA_NIGHTMARE, DMData.DATA_VANESSA_NIGHTMARE), 
                new(DMCreatures.NPC_VANESSA_BOSS, DMData.DATA_VANESSA),
                new(DMCreatures.NPC_GLUBTOK_NIGHTMARE, DMData.DATA_NIGHTMARE_MECHANICAL)
            };

            private static ObjectData[] _gameObjectData =
            {
                
            };


            public instance_deadmines_InstanceMapScript(InstanceMap map) : base(map)
            {
                SetBossNumber((uint)DMData.MAX_BOSSES);
                SetHeaders("DM");
                LoadDoorData(_doorData);
                LoadObjectData(_creatureData, _gameObjectData);
                LoadDungeonEncounterData(_encounters);
            }

            public override void OnCreatureCreate(Creature creature)
            {
                var players = instance.GetPlayers();
                if (!players.Empty())
                {
                   
                        _teamInInstance = players.First().GetTeam();
                }
                switch (creature.GetEntry())
                {
                    case 46889: // Kagtha
                        if (_teamInInstance == Team.Alliance)
                        {
                            creature.UpdateEntry(42308); // Lieutenant Horatio Laine
                        }
                        break;
                    case 46902: // Miss Mayhem
                        if (_teamInInstance == Team.Alliance)
                        {
                            creature.UpdateEntry(491); // Quartermaster Lewis <Quartermaster>
                        }
                        break;
                    case 46903: // Mayhem Reaper Prototype
                        if (_teamInInstance == Team.Alliance)
                        {
                            creature.UpdateEntry(1); // GM WAYPOINT
                        }
                        break;
                    case 46906: // Slinky Sharpshiv
                        if (_teamInInstance == Team.Alliance)
                        {
                            creature.UpdateEntry(46612); // Lieutenant Horatio Laine
                        }
                        break;
                    case 46613: // Crime Scene Alarm-O-Bot
                        if (_teamInInstance == Team.Horde)
                        {
                            creature.UpdateEntry(1); // GM WAYPOINT
                        }
                        break;
                    case 50595: // Stormwind Defender
                        if (_teamInInstance == Team.Horde)
                        {
                            creature.UpdateEntry(46890); // Shattered Hand Assassin
                        }
                        break;
                    case 46614: // Stormwind Investigator
                        if (_teamInInstance == Team.Horde)
                        {
                            creature.UpdateEntry(1); // GM WAYPOINT
                        }
                        break;
                    case DMCreatures.NPC_VANESSA_VANCLEEF:
                        _vanessa = creature.GetGUID();
                        break;
                    case DMCreatures.NPC_VANESSA_BOSS:
                        _vanessaBoss = creature.GetGUID();
                        break;
                    case DMCreatures.NPC_VANESSA_NOTE:
                        _vanessaNote = creature.GetGUID();
                        break;
                    case DMCreatures.NPC_GLUBTOK:
                        _glubtokGUID = creature.GetGUID();
                        break;
                }
            }

            public override bool SetBossState(uint id, EncounterState state)
            {
                if (!base.SetBossState(id, state))
                {
                    return false;
                }

                switch (id)
                {
                    case DMData.DATA_COOKIE:
                        if (state == EncounterState.Done)
                        {
                            if (instance.IsHeroic())
                            {
                                SummonNote();
                            }
                        }
                        break;
                    case DMData.DATA_VANESSA_NIGHTMARE:
                        if (state == EncounterState.Fail)
                        {
                            SummonNote();
                        }
                        break;
                    default: 
                        break;
                       
                }

                return true;
            }

            private void SummonNote()
            {
                Creature Note = instance.SummonCreature(DMCreatures.NPC_VANESSA_NOTE, NoteSpawn);

                if (Note != null)
                    Note.TextEmote(NOTE_TEXT, null, true);
            }

            public override ulong GetData64(uint data)
            {
                switch (data)
                {
                    case DMCreatures.NPC_VANESSA_VANCLEEF:
                        return _vanessa.GetCounter();
                      
                    case DMCreatures.NPC_VANESSA_BOSS:
                        return _vanessaBoss.GetCounter();
               
                    case DMCreatures.NPC_VANESSA_NOTE:
                        return _vanessaNote.GetCounter();

                    case DMCreatures.NPC_GLUBTOK:
                        return _glubtokGUID.GetCounter();
               
                }

                return 0;
            }

     
        }

        public instance_deadmines() : base(nameof(instance_deadmines), 36)
        {
        }

        public InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_deadmines_InstanceMapScript(map);
        }
    }
}