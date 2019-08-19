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

        public static Position[] OptionalSpawn =
        {
            new Position(-10960.981445f, -1940.138428f, 46.178097f, 4.12f), // Hyakiss the Lurker
            new Position(-10945.769531f, -2040.153320f, 49.474438f, 0.077f), // Shadikith the Glider
            new Position(-10899.903320f, -2085.573730f, 49.474449f, 1.38f)  // Rokad the Ravager
        };

        public const uint OptionalBossRequiredDeathCount = 50;
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
        public const uint Attumen = 1;
        public const uint Moroes = 2;
        public const uint MaidenOfVirtue = 3;
        public const uint OptionalBoss = 4;
        public const uint OperaPerformance = 5;
        public const uint Curator = 6;
        public const uint Aran = 7;
        public const uint Terestian = 8;
        public const uint Netherspite = 9;
        public const uint Chess = 10;
        public const uint Malchezzar = 11;
        public const uint Nightbane = 12;

        public const uint OperaOzDeathcount = 14;

        public const uint Kilrek = 15;
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
        public const uint GoBlackenedUrn = 30;
    }

    struct OperaEvents
    {
        public const uint Oz = 1;
        public const uint Hood = 2;
        public const uint RAJ = 3;
    }

    struct CreatureIds
    {
        public const uint HyakissTheLurker = 16179;
        public const uint RokadTheRavager = 16181;
        public const uint ShadikithTheGlider = 16180;
        public const uint TerestianIllhoof = 15688;
        public const uint Moroes = 15687;
        public const uint Nightbane = 17225;
        public const uint AttumenUnmounted = 15550;
        public const uint AttumenMounted = 16152;
        public const uint Midnight = 16151;

        // Trash
        public const uint ColdmistWidow = 16171;
        public const uint ColdmistStalker = 16170;
        public const uint Shadowbat = 16173;
        public const uint VampiricShadowbat = 16175;
        public const uint GreaterShadowbat = 16174;
        public const uint PhaseHound = 16178;
        public const uint Dreadbeast = 16177;
        public const uint Shadowbeast = 16176;
        public const uint Kilrek = 17229;
    }

    struct GameObjectIds
    {
        public const uint StageCurtain = 183932;
        public const uint StageDoorLeft = 184278;
        public const uint StageDoorRight = 184279;
        public const uint PrivateLibraryDoor = 184517;
        public const uint MassiveDoor = 185521;
        public const uint GamesmanHallDoor = 184276;
        public const uint GamesmanHallExitDoor = 184277;
        public const uint NetherspaceDoor = 185134;
        public const uint MastersTerraceDoor = 184274;
        public const uint MastersTerraceDoor2 = 184280;
        public const uint SideEntranceDoor = 184275;
        public const uint DustCoveredChest = 185119;
        public const uint BlackenedUrn = 194092;
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
                SetBossNumber(karazhanConst.MaxEncounter);

                // 1 - OZ, 2 - HOOD, 3 - RAJ, this never gets altered.
                OperaEvent = RandomHelper.URand(1, 3);
                OzDeathCount = 0;
                OptionalBossCount = 0;
            }

            public override void OnCreatureCreate(Creature creature)
            {
                switch (creature.GetEntry())
                {
                    case CreatureIds.Kilrek:
                        KilrekGUID = creature.GetGUID();
                        break;
                    case CreatureIds.TerestianIllhoof:
                        TerestianGUID = creature.GetGUID();
                        break;
                    case CreatureIds.Moroes:
                        MoroesGUID = creature.GetGUID();
                        break;
                    case CreatureIds.Nightbane:
                        NightbaneGUID = creature.GetGUID();
                        break;
                }
            }

            public override void OnUnitDeath(Unit unit)
            {
                Creature creature = unit.ToCreature();
                if (creature == null)
                    return;

                switch (creature.GetEntry())
                {
                    case CreatureIds.ColdmistWidow:
                    case CreatureIds.ColdmistStalker:
                    case CreatureIds.Shadowbat:
                    case CreatureIds.VampiricShadowbat:
                    case CreatureIds.GreaterShadowbat:
                    case CreatureIds.PhaseHound:
                    case CreatureIds.Dreadbeast:
                    case CreatureIds.Shadowbeast:
                        if (GetBossState(DataTypes.OptionalBoss) == EncounterState.ToBeDecided)
                        {
                            ++OptionalBossCount;
                            if (OptionalBossCount == karazhanConst.OptionalBossRequiredDeathCount)
                            {
                                switch (RandomHelper.URand(CreatureIds.HyakissTheLurker, CreatureIds.RokadTheRavager))
                                {
                                    case CreatureIds.HyakissTheLurker:
                                        instance.SummonCreature(CreatureIds.HyakissTheLurker, karazhanConst.OptionalSpawn[0]);
                                        break;
                                    case CreatureIds.ShadikithTheGlider:
                                        instance.SummonCreature(CreatureIds.ShadikithTheGlider, karazhanConst.OptionalSpawn[1]);
                                        break;
                                    case CreatureIds.RokadTheRavager:
                                        instance.SummonCreature(CreatureIds.RokadTheRavager, karazhanConst.OptionalSpawn[2]);
                                        break;
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            public override void SetData(uint type, uint uiData)
            {
                switch (type)
                {
                    case DataTypes.OperaOzDeathcount:
                        if (uiData == (uint)EncounterState.Special)
                            ++OzDeathCount;
                        else if (uiData == (uint)EncounterState.InProgress)
                            OzDeathCount = 0;
                        break;
                }
            }

            public override bool SetBossState(uint id, EncounterState state)
            {
                if (!base.SetBossState(id, state))
                    return false;

                switch (id)
                {
                    case DataTypes.OperaPerformance:
                        if (state == EncounterState.Done)
                        {
                            HandleGameObject(StageDoorLeftGUID, true);
                            HandleGameObject(StageDoorRightGUID, true);
                            GameObject sideEntrance = instance.GetGameObject(SideEntranceDoor);
                            if (sideEntrance != null)
                                sideEntrance.RemoveFlag(GameObjectFlags.Locked);
                            UpdateEncounterStateForKilledCreature(16812, null);
                        }
                        break;
                    case DataTypes.Chess:
                        if (state == EncounterState.Done)
                            DoRespawnGameObject(DustCoveredChest, Time.Day);
                        break;
                    default:
                        break;
                }

                return true;
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
                    case GameObjectIds.StageCurtain:
                        CurtainGUID = go.GetGUID();
                        break;
                    case GameObjectIds.StageDoorLeft:
                        StageDoorLeftGUID = go.GetGUID();
                        if (GetBossState(DataTypes.OperaPerformance) == EncounterState.Done)
                            go.SetGoState(GameObjectState.Active);
                        break;
                    case GameObjectIds.StageDoorRight:
                        StageDoorRightGUID = go.GetGUID();
                        if (GetBossState(DataTypes.OperaPerformance) == EncounterState.Done)
                            go.SetGoState(GameObjectState.Active);
                        break;
                    case GameObjectIds.PrivateLibraryDoor:
                        LibraryDoor = go.GetGUID();
                        break;
                    case GameObjectIds.MassiveDoor:
                        MassiveDoor = go.GetGUID();
                        break;
                    case GameObjectIds.GamesmanHallDoor:
                        GamesmansDoor = go.GetGUID();
                        break;
                    case GameObjectIds.GamesmanHallExitDoor:
                        GamesmansExitDoor = go.GetGUID();
                        break;
                    case GameObjectIds.NetherspaceDoor:
                        NetherspaceDoor = go.GetGUID();
                        break;
                    case GameObjectIds.MastersTerraceDoor:
                        MastersTerraceDoor[0] = go.GetGUID();
                        break;
                    case GameObjectIds.MastersTerraceDoor2:
                        MastersTerraceDoor[1] = go.GetGUID();
                        break;
                    case GameObjectIds.SideEntranceDoor:
                        SideEntranceDoor = go.GetGUID();
                        if (GetBossState(DataTypes.OperaPerformance) == EncounterState.Done)
                            go.AddFlag(GameObjectFlags.Locked);
                        else
                            go.RemoveFlag(GameObjectFlags.Locked);
                        break;
                    case GameObjectIds.DustCoveredChest:
                        DustCoveredChest = go.GetGUID();
                        break;
                    case GameObjectIds.BlackenedUrn:
                        BlackenedUrnGUID = go.GetGUID();
                        break;
                }

                switch (OperaEvent)
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

            public override uint GetData(uint uiData)
            {
                switch (uiData)
                {
                    case DataTypes.OperaPerformance:
                        return OperaEvent;
                    case DataTypes.OperaOzDeathcount:
                        return OzDeathCount;
                }

                return 0;
            }

            public override ObjectGuid GetGuidData(uint uiData)
            {
                switch (uiData)
                {
                    case DataTypes.Kilrek:
                        return KilrekGUID;
                    case DataTypes.Terestian:
                        return TerestianGUID;
                    case DataTypes.Moroes:
                        return MoroesGUID;
                    case DataTypes.Nightbane:
                        return NightbaneGUID;
                    case DataTypes.GoStagedoorleft:
                        return StageDoorLeftGUID;
                    case DataTypes.GoStagedoorright:
                        return StageDoorRightGUID;
                    case DataTypes.GoCurtains:
                        return CurtainGUID;
                    case DataTypes.GoLibraryDoor:
                        return LibraryDoor;
                    case DataTypes.GoMassiveDoor:
                        return MassiveDoor;
                    case DataTypes.GoSideEntranceDoor:
                        return SideEntranceDoor;
                    case DataTypes.GoGameDoor:
                        return GamesmansDoor;
                    case DataTypes.GoGameExitDoor:
                        return GamesmansExitDoor;
                    case DataTypes.GoNetherDoor:
                        return NetherspaceDoor;
                    case DataTypes.MastersTerraceDoor1:
                        return MastersTerraceDoor[0];
                    case DataTypes.MastersTerraceDoor2:
                        return MastersTerraceDoor[1];
                    case DataTypes.ImageOfMedivh:
                        return ImageGUID;
                    case DataTypes.GoBlackenedUrn:
                        return BlackenedUrnGUID;
                }

                return ObjectGuid.Empty;
            }

            uint OperaEvent;
            uint OzDeathCount;
            uint OptionalBossCount;
            ObjectGuid CurtainGUID;
            ObjectGuid StageDoorLeftGUID;
            ObjectGuid StageDoorRightGUID;
            ObjectGuid KilrekGUID;
            ObjectGuid TerestianGUID;
            ObjectGuid MoroesGUID;
            ObjectGuid NightbaneGUID;
            ObjectGuid LibraryDoor;                 // Door at Shade of Aran
            ObjectGuid MassiveDoor;                 // Door at Netherspite
            ObjectGuid SideEntranceDoor;            // Side Entrance
            ObjectGuid GamesmansDoor;               // Door before Chess
            ObjectGuid GamesmansExitDoor;           // Door after Chess
            ObjectGuid NetherspaceDoor;             // Door at Malchezaar
            ObjectGuid[] MastersTerraceDoor = new ObjectGuid[2];
            ObjectGuid ImageGUID;
            ObjectGuid DustCoveredChest;
            ObjectGuid BlackenedUrnGUID;
        }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_karazhan_InstanceMapScript(map);
        }
    }
}
