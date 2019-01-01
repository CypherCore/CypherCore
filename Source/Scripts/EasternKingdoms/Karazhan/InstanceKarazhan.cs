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
using Framework.IO;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.AI;

namespace Scripts.EasternKingdoms.Karazhan
{
    struct karazhanConst
    {
        public const uint MaxEncounter = 12;

        public const uint BossAttumen = 1;
        public const uint BossMoroes = 2;
        public const uint BossMaiden = 3;
        public const uint OptionalBoss = 4;
        public const uint BossOpera = 5;
        public const uint Curator = 6;
        public const uint Aran = 7;
        public const uint Terestian = 8;
        public const uint Netherspite = 9;
        public const uint Chess = 10;
        public const uint Malchezzar = 11;
        public const uint Nightbane = 12;

        public static Dialogue[] OzDialogue =
        {
            new Dialogue(0, 6000),
            new Dialogue(1, 18000),
            new Dialogue(2, 9000),
            new Dialogue(3, 15000)
        };

        public static Dialogue[] HoodDialogue =
        {
            new Dialogue(4, 6000),
            new Dialogue(5, 10000),
            new Dialogue(6, 14000),
            new Dialogue(7, 15000)
        };

        public static Dialogue[] RAJDialogue =
        {
            new Dialogue(8, 5000),
            new Dialogue(9, 7000),
            new Dialogue(10, 14000),
            new Dialogue(11, 14000)
        };

        // Entries and spawn locations for creatures in Oz event
        public static float[][] Spawns =
        {
            new float[] { 17535, -10896},                                        // Dorothee
            new float[] { 17546, -10891},                                        // Roar
            new float[] { 17547, -10884},                                        // Tinhead
            new float[] { 17543, -10902},                                        // Strawman
            new float[] { 17603, -10892},                                        // Grandmother
            new float[] { 17534, -10900},                                        // Julianne
        };

        public static float SPAWN_Z = 90.5f;
        public static float SPAWN_Y = -1758f;
        public static float SPAWN_O = 4.738f;

        public static string SAY_READY = "Splendid, I'm going to get the audience ready. Break a leg!";
        public static string SAY_OZ_INTRO1 = "Finally, everything is in place. Are you ready for your big stage debut?";
        public static string OZ_GOSSIP1 = "I'm not an actor.";
        public static string SAY_OZ_INTRO2 = "Don't worry, you'll be fine. You look like a natural!";
        public static string OZ_GOSSIP2 = "Ok, I'll give it a try, then.";

        public static string SAY_RAJ_INTRO1 = "The romantic plays are really tough, but you'll do better this time. You have TALENT. Ready?";
        public static string RAJ_GOSSIP1 = "I've never been more ready.";

        public static string OZ_GM_GOSSIP1 = "[GM] Change event to EVENT_OZ";
        public static string OZ_GM_GOSSIP2 = "[GM] Change event to EVENT_HOOD";
        public static string OZ_GM_GOSSIP3 = "[GM] Change event to EVENT_RAJ";

        // Barnes
        public const uint SpellSpotlight = 25824;
        public const uint SpellTuxedo = 32616;

        // Berthold
        public const uint SpellTeleport = 39567;

        // Image of Medivh
        public const uint SpellFireBall = 30967;
        public const uint SpellUberFireball = 30971;
        public const uint SpellConflagrationBlast = 30977;
        public const uint SpellManaShield = 31635;

        public const uint NpcArcanagos = 17652;
        public const uint NpcSpotlight = 19525;
    }

    struct Dialogue
    {
        public Dialogue(int textid, uint timer)
        {
            TextId = textid;
            Timer = timer;
        }

        public int TextId;
        public uint Timer;
    }

    struct DataTypes
    {
        public const uint OperaPerformance = 13;
        public const uint OperaOzDeathcount = 14;

        public const uint Kilrek = 15;
        public const uint Terestian = 16;
        public const uint Moroes = 17;
        public const uint GoCurtains = 18;
        public const uint GoStagedoorleft = 19;
        public const uint GoStagedoorright = 20;
        public const uint GoLibraryDoor = 21;
        public const uint GoMassiveDoor = 22;
        public const uint GoNetherDoor = 23;
        public const uint GoGameDoor = 24;
        public const uint GoGameExitDoor = 25;

        public const uint ImageOfMedivh = 26;
        public const uint MastersTerraceDoor1 = 27;
        public const uint MastersTerraceDoor2 = 28;
        public const uint GoSideEntranceDoor = 29;
    }

    struct OperaEvents
    {
        public const uint Oz = 1;
        public const uint Hood = 2;
        public const uint RAJ = 3;
    }

    [Script]
    public class instance_karazhan : InstanceMapScript
    {
        public instance_karazhan() : base("instance_karazhan", 532) { }

        public class instance_karazhan_InstanceMapScript : InstanceScript
        {
            public instance_karazhan_InstanceMapScript(InstanceMap map) : base(map)
            {
                SetHeaders("KZ");

                // 1 - OZ, 2 - HOOD, 3 - RAJ, this never gets altered.
                m_uiOperaEvent = RandomHelper.URand(1, 3);
                m_uiOzDeathCount = 0;
            }

            public override bool IsEncounterInProgress()
            {
                for (byte i = 0; i < karazhanConst.MaxEncounter; ++i)
                    if (m_auiEncounter[i] == (uint)EncounterState.InProgress)
                        return true;

                return false;
            }

            public override void OnCreatureCreate(Creature creature)
            {
                switch (creature.GetEntry())
                {
                    case 17229:
                        m_uiKilrekGUID = creature.GetGUID();
                        break;
                    case 15688:
                        m_uiTerestianGUID = creature.GetGUID();
                        break;
                    case 15687:
                        m_uiMoroesGUID = creature.GetGUID();
                        break;
                }
            }

            public override void SetData(uint type, uint uiData)
            {
                switch (type)
                {
                    case karazhanConst.BossAttumen:
                        m_auiEncounter[0] = uiData;
                        break;
                    case karazhanConst.BossMoroes:
                        if (m_auiEncounter[1] == (uint)EncounterState.Done)
                            break;
                        m_auiEncounter[1] = uiData;
                        break;
                    case karazhanConst.BossMaiden:
                        m_auiEncounter[2] = uiData;
                        break;
                    case karazhanConst.OptionalBoss:
                        m_auiEncounter[3] = uiData;
                        break;
                    case karazhanConst.BossOpera:
                        m_auiEncounter[4] = uiData;
                        if (uiData == (uint)EncounterState.Done)
                            UpdateEncounterStateForKilledCreature(16812, null);
                        break;
                    case karazhanConst.Curator:
                        m_auiEncounter[5] = uiData;
                        break;
                    case karazhanConst.Aran:
                        m_auiEncounter[6] = uiData;
                        break;
                    case karazhanConst.Terestian:
                        m_auiEncounter[7] = uiData;
                        break;
                    case karazhanConst.Netherspite:
                        m_auiEncounter[8] = uiData;
                        break;
                    case karazhanConst.Chess:
                        if (uiData == (uint)EncounterState.Done)
                            DoRespawnGameObject(DustCoveredChest, Time.Day);
                        m_auiEncounter[9] = uiData;
                        break;
                    case karazhanConst.Malchezzar:
                        m_auiEncounter[10] = uiData;
                        break;
                    case karazhanConst.Nightbane:
                        if (m_auiEncounter[11] != (uint)EncounterState.Done)
                            m_auiEncounter[11] = uiData;
                        break;
                    case DataTypes.OperaOzDeathcount:
                        if (uiData == (uint)EncounterState.Special)
                            ++m_uiOzDeathCount;
                        else if (uiData == (uint)EncounterState.InProgress)
                            m_uiOzDeathCount = 0;
                        break;
                }

                if (uiData == (uint)EncounterState.Done)
                {
                    OUT_SAVE_INST_DATA();

                    strSaveData =
                        $"{m_auiEncounter[0]} {m_auiEncounter[1]} {m_auiEncounter[2]} {m_auiEncounter[3]} {m_auiEncounter[4]} {m_auiEncounter[5]} {m_auiEncounter[6]} {m_auiEncounter[7]} {m_auiEncounter[8]} {m_auiEncounter[9]} {m_auiEncounter[10]} {m_auiEncounter[11]}";

                    SaveToDB();
                    OUT_SAVE_INST_DATA_COMPLETE();
                }
            }

            public override void SetGuidData(uint identifier, ObjectGuid data)
            {
                if (identifier == DataTypes.ImageOfMedivh)
                    ImageGUID = data;
            }

            public override void OnGameObjectCreate(GameObject go)
            {
                switch (go.GetEntry())
                {
                    case 183932:
                        m_uiCurtainGUID = go.GetGUID();
                        break;
                    case 184278:
                        m_uiStageDoorLeftGUID = go.GetGUID();
                        if (m_auiEncounter[4] == (uint)EncounterState.Done)
                            go.SetGoState(GameObjectState.Active);
                        break;
                    case 184279:
                        m_uiStageDoorRightGUID = go.GetGUID();
                        if (m_auiEncounter[4] == (uint)EncounterState.Done)
                            go.SetGoState(GameObjectState.Active);
                        break;
                    case 184517:
                        m_uiLibraryDoor = go.GetGUID();
                        break;
                    case 185521:
                        m_uiMassiveDoor = go.GetGUID();
                        break;
                    case 184276:
                        m_uiGamesmansDoor = go.GetGUID();
                        break;
                    case 184277:
                        m_uiGamesmansExitDoor = go.GetGUID();
                        break;
                    case 185134:
                        m_uiNetherspaceDoor = go.GetGUID();
                        break;
                    case 184274:
                        MastersTerraceDoor[0] = go.GetGUID();
                        break;
                    case 184280:
                        MastersTerraceDoor[1] = go.GetGUID();
                        break;
                    case 184275:
                        m_uiSideEntranceDoor = go.GetGUID();
                        if (m_auiEncounter[4] == (uint)EncounterState.Done)
                            go.SetFlag(GameObjectFields.Flags, GameObjectFlags.Locked);
                        else
                            go.RemoveFlag(GameObjectFields.Flags, GameObjectFlags.Locked);
                        break;
                    case 185119:
                        DustCoveredChest = go.GetGUID();
                        break;
                }

                switch (m_uiOperaEvent)
                {
                    // @todo Set Object visibilities for Opera based on performance
                    case OperaEvents.Oz:
                        break;

                    case OperaEvents.Hood:
                        break;

                    case OperaEvents.RAJ:
                        break;
                }
            }

            public override string GetSaveData()
            {
                return strSaveData;
            }

            public override uint GetData(uint uiData)
            {
                switch (uiData)
                {
                    case karazhanConst.BossAttumen:
                        return m_auiEncounter[0];
                    case karazhanConst.BossMoroes:
                        return m_auiEncounter[1];
                    case karazhanConst.BossMaiden:
                        return m_auiEncounter[2];
                    case karazhanConst.OptionalBoss:
                        return m_auiEncounter[3];
                    case karazhanConst.BossOpera:
                        return m_auiEncounter[4];
                    case karazhanConst.Curator:
                        return m_auiEncounter[5];
                    case karazhanConst.Aran:
                        return m_auiEncounter[6];
                    case karazhanConst.Terestian:
                        return m_auiEncounter[7];
                    case karazhanConst.Netherspite:
                        return m_auiEncounter[8];
                    case karazhanConst.Chess:
                        return m_auiEncounter[9];
                    case karazhanConst.Malchezzar:
                        return m_auiEncounter[10];
                    case karazhanConst.Nightbane:
                        return m_auiEncounter[11];
                    case DataTypes.OperaPerformance:
                        return m_uiOperaEvent;
                    case DataTypes.OperaOzDeathcount:
                        return m_uiOzDeathCount;
                }

                return 0;
            }

            public override ObjectGuid GetGuidData(uint uiData)
            {
                switch (uiData)
                {
                    case DataTypes.Kilrek:
                        return m_uiKilrekGUID;
                    case DataTypes.Terestian:
                        return m_uiTerestianGUID;
                    case DataTypes.Moroes:
                        return m_uiMoroesGUID;
                    case DataTypes.GoStagedoorleft:
                        return m_uiStageDoorLeftGUID;
                    case DataTypes.GoStagedoorright:
                        return m_uiStageDoorRightGUID;
                    case DataTypes.GoCurtains:
                        return m_uiCurtainGUID;
                    case DataTypes.GoLibraryDoor:
                        return m_uiLibraryDoor;
                    case DataTypes.GoMassiveDoor:
                        return m_uiMassiveDoor;
                    case DataTypes.GoSideEntranceDoor:
                        return m_uiSideEntranceDoor;
                    case DataTypes.GoGameDoor:
                        return m_uiGamesmansDoor;
                    case DataTypes.GoGameExitDoor:
                        return m_uiGamesmansExitDoor;
                    case DataTypes.GoNetherDoor:
                        return m_uiNetherspaceDoor;
                    case DataTypes.MastersTerraceDoor1:
                        return MastersTerraceDoor[0];
                    case DataTypes.MastersTerraceDoor2:
                        return MastersTerraceDoor[1];
                    case DataTypes.ImageOfMedivh:
                        return ImageGUID;
                }

                return ObjectGuid.Empty;
            }

            public override void Load(string str)
            {
                if (string.IsNullOrEmpty(str))
                {
                    OUT_LOAD_INST_DATA_FAIL();
                    return;
                }

                OUT_LOAD_INST_DATA(str);
                StringArguments loadStream = new StringArguments(str);

                for (byte i = 0; i < karazhanConst.MaxEncounter; ++i)
                {
                    var state = (EncounterState)loadStream.NextUInt32();
                    // Do not load an encounter as "In Progress" - reset it instead.
                    m_auiEncounter[i] = (uint)(state == EncounterState.InProgress ? EncounterState.NotStarted : state);
                }

                OUT_LOAD_INST_DATA_COMPLETE();
            }

            uint[] m_auiEncounter = new uint[karazhanConst.MaxEncounter];
            string strSaveData;

            uint m_uiOperaEvent;
            uint m_uiOzDeathCount;

            ObjectGuid m_uiCurtainGUID;
            ObjectGuid m_uiStageDoorLeftGUID;
            ObjectGuid m_uiStageDoorRightGUID;
            ObjectGuid m_uiKilrekGUID;
            ObjectGuid m_uiTerestianGUID;
            ObjectGuid m_uiMoroesGUID;
            ObjectGuid m_uiLibraryDoor;                                     // Door at Shade of Aran
            ObjectGuid m_uiMassiveDoor;                                     // Door at Netherspite
            ObjectGuid m_uiSideEntranceDoor;                                // Side Entrance
            ObjectGuid m_uiGamesmansDoor;                                   // Door before Chess
            ObjectGuid m_uiGamesmansExitDoor;                               // Door after Chess
            ObjectGuid m_uiNetherspaceDoor;                                // Door at Malchezaar
            ObjectGuid[] MastersTerraceDoor = new ObjectGuid[2];
            ObjectGuid ImageGUID;
            ObjectGuid DustCoveredChest;
        }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_karazhan_InstanceMapScript(map);
        }
    }
}
