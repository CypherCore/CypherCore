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
using System.Collections.Generic;
using System.Text;

namespace Scripts.Northrend.Gundrak
{
    struct GDInstanceMisc
    {
        public const uint TimerStatueActivation = 3500;

        public static DoorData[] doorData =
        {
            new DoorData(GDGameObjectIds.GalDarahDoor1,               GDDataTypes.GalDarah,           DoorType.Passage),
            new DoorData(GDGameObjectIds.GalDarahDoor2,               GDDataTypes.GalDarah,           DoorType.Passage),
            new DoorData(GDGameObjectIds.GalDarahDoor3,               GDDataTypes.GalDarah,           DoorType.Room),
            new DoorData(GDGameObjectIds.EckTheFerociousDoor,         GDDataTypes.Moorabi,            DoorType.Passage),
            new DoorData(GDGameObjectIds.EckTheFerociousDoorBehind,   GDDataTypes.EckTheFerocious,    DoorType.Passage),
        };

        public static ObjectData[] creatureData =
        {
            new ObjectData(GDCreatureIds.DrakkariColossus, GDDataTypes.DrakkariColossus),
        };

        public static ObjectData[] gameObjectData =
        {
            new ObjectData(GDGameObjectIds.SladRanAltar,            GDDataTypes.SladRanAltar),
            new ObjectData(GDGameObjectIds.MoorabiAltar,            GDDataTypes.MoorabiAltar),
            new ObjectData(GDGameObjectIds.DrakkariColossusAltar,   GDDataTypes.DrakkariColossusAltar),
            new ObjectData(GDGameObjectIds.SladRanStatue,           GDDataTypes.SladRanStatue),
            new ObjectData(GDGameObjectIds.MoorabiStatue,           GDDataTypes.MoorabiStatue),
            new ObjectData(GDGameObjectIds.DrakkariColossusStatue,  GDDataTypes.DrakkariColossusStatue),
            new ObjectData(GDGameObjectIds.GalDarahStatue,          GDDataTypes.GalDarahStatue),
            new ObjectData(GDGameObjectIds.Trapdoor,                GDDataTypes.Trapdoor),
            new ObjectData(GDGameObjectIds.Collision,               GDDataTypes.Collision)
        };

        public static Position EckSpawnPoint = new Position(1643.877930f, 936.278015f, 107.204948f, 0.668432f);
    }

    struct GDDataTypes
    {
        // Encounter Ids // Encounter States // Boss Guids
        public const uint SladRan = 0;
        public const uint DrakkariColossus = 1;
        public const uint Moorabi = 2;
        public const uint GalDarah = 3;
        public const uint EckTheFerocious = 4;

        // Additional Objects
        public const uint SladRanAltar = 5;
        public const uint DrakkariColossusAltar = 6;
        public const uint MoorabiAltar = 7;

        public const uint SladRanStatue = 8;
        public const uint DrakkariColossusStatue = 9;
        public const uint MoorabiStatue = 10;
        public const uint GalDarahStatue = 11;

        public const uint Trapdoor = 12;
        public const uint Collision = 13;
        public const uint Bridge = 14;

        public const uint StatueActivate = 15;
    }

    struct GDCreatureIds
    {
        public const uint SladRan = 29304;
        public const uint Moorabi = 29305;
        public const uint GalDarah = 29306;
        public const uint DrakkariColossus = 29307;
        public const uint RuinDweller = 29920;
        public const uint EckTheFerocious = 29932;
        public const uint AltarTrigger = 30298;
    }

    struct GDGameObjectIds
    {
        public const uint SladRanAltar = 192518;
        public const uint MoorabiAltar = 192519;
        public const uint DrakkariColossusAltar = 192520;
        public const uint SladRanStatue = 192564;
        public const uint MoorabiStatue = 192565;
        public const uint GalDarahStatue = 192566;
        public const uint DrakkariColossusStatue = 192567;
        public const uint EckTheFerociousDoor = 192632;
        public const uint EckTheFerociousDoorBehind = 192569;
        public const uint GalDarahDoor1 = 193208;
        public const uint GalDarahDoor2 = 193209;
        public const uint GalDarahDoor3 = 192568;
        public const uint Trapdoor = 193188;
        public const uint Collision = 192633;
    }

    struct GDSpellIds
    {
        public const uint FireBeamMammoth = 57068;
        public const uint FireBeamSnake = 57071;
        public const uint FireBeamElemental = 57072;
    }

    [Script]
    class instance_gundrak : InstanceMapScript
    {
        public instance_gundrak() : base(nameof(instance_gundrak), 604) { }

        class instance_gundrak_InstanceMapScript : InstanceScript
        {
            public instance_gundrak_InstanceMapScript(InstanceMap map) : base(map)
            {
                SetHeaders("GD");
                SetBossNumber(5);
                LoadDoorData(GDInstanceMisc.doorData);
                LoadObjectData(GDInstanceMisc.creatureData, GDInstanceMisc.gameObjectData);

                SladRanStatueState = GameObjectState.Active;
                DrakkariColossusStatueState = GameObjectState.Active;
                MoorabiStatueState = GameObjectState.Active;
            }

            public override void OnCreatureCreate(Creature creature)
            {
                switch (creature.GetEntry())
                {
                    case GDCreatureIds.RuinDweller:
                        if (creature.IsAlive())
                            DwellerGUIDs.Add(creature.GetGUID());
                        break;
                    default:
                        break;
                }

                base.OnCreatureCreate(creature);
            }

            public override void OnGameObjectCreate(GameObject go)
            {
                switch (go.GetEntry())
                {
                    case GDGameObjectIds.SladRanAltar:
                        if (GetBossState(GDDataTypes.SladRan) == EncounterState.Done)
                        {
                            if (SladRanStatueState == GameObjectState.Active)
                                go.RemoveFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                            else
                                go.SetGoState(GameObjectState.Active);
                        }
                        break;
                    case GDGameObjectIds.MoorabiAltar:
                        if (GetBossState(GDDataTypes.Moorabi) == EncounterState.Done)
                        {
                            if (MoorabiStatueState == GameObjectState.Active)
                                go.RemoveFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                            else
                                go.SetGoState(GameObjectState.Active);
                        }
                        break;
                    case GDGameObjectIds.DrakkariColossusAltar:
                        if (GetBossState(GDDataTypes.DrakkariColossus) == EncounterState.Done)
                        {
                            if (DrakkariColossusStatueState == GameObjectState.Active)
                                go.RemoveFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                            else
                                go.SetGoState(GameObjectState.Active);
                        }
                        break;
                    case GDGameObjectIds.SladRanStatue:
                        go.SetGoState(SladRanStatueState);
                        break;
                    case GDGameObjectIds.MoorabiStatue:
                        go.SetGoState(MoorabiStatueState);
                        break;
                    case GDGameObjectIds.GalDarahStatue:
                        go.SetGoState(CheckRequiredBosses(GDDataTypes.GalDarah) ? GameObjectState.ActiveAlternative : GameObjectState.Ready);
                        break;
                    case GDGameObjectIds.DrakkariColossusStatue:
                        go.SetGoState(DrakkariColossusStatueState);
                        break;
                    case GDGameObjectIds.EckTheFerociousDoor:
                        // Don't store door on non-heroic
                        if (!instance.IsHeroic())
                            return;
                        break;
                    case GDGameObjectIds.Trapdoor:
                        go.SetGoState(CheckRequiredBosses(GDDataTypes.GalDarah) ? GameObjectState.Ready : GameObjectState.Active);
                        break;
                    case GDGameObjectIds.Collision:
                        go.SetGoState(CheckRequiredBosses(GDDataTypes.GalDarah) ? GameObjectState.Active : GameObjectState.Ready);
                        break;
                    default:
                        break;
                }

                base.OnGameObjectCreate(go);
            }

            public override void OnUnitDeath(Unit unit)
            {
                if (unit.GetEntry() == GDCreatureIds.RuinDweller)
                {
                    DwellerGUIDs.Remove(unit.GetGUID());

                    if (DwellerGUIDs.Empty())
                        unit.SummonCreature(GDCreatureIds.EckTheFerocious, GDInstanceMisc.EckSpawnPoint, TempSummonType.CorpseTimedDespawn, 300 * Time.InMilliseconds);
                }
            }

            public override bool SetBossState(uint type, EncounterState state)
            {
                if (!base.SetBossState(type, state))
                    return false;

                switch (type)
                {
                    case GDDataTypes.SladRan:
                        if (state == EncounterState.Done)
                        {
                            GameObject go = GetGameObject(GDDataTypes.SladRanAltar);
                            if (go)
                                go.RemoveFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                        }
                        break;
                    case GDDataTypes.DrakkariColossus:
                        if (state == EncounterState.Done)
                        {
                            GameObject go = GetGameObject(GDDataTypes.DrakkariColossusAltar);
                            if (go)
                                go.RemoveFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                        }
                        break;
                    case GDDataTypes.Moorabi:
                        if (state == EncounterState.Done)
                        {
                            GameObject go = GetGameObject(GDDataTypes.MoorabiAltar);
                            if (go)
                                go.RemoveFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                        }
                        break;
                    default:
                        break;
                }

                return true;
            }

            public override bool CheckRequiredBosses(uint bossId, Player player = null)
            {
                if (_SkipCheckRequiredBosses(player))
                    return true;

                switch (bossId)
                {
                    case GDDataTypes.EckTheFerocious:
                        if (!instance.IsHeroic() || GetBossState(GDDataTypes.Moorabi) != EncounterState.Done)
                            return false;
                        break;
                    case GDDataTypes.GalDarah:
                        if (SladRanStatueState != GameObjectState.ActiveAlternative
                            || DrakkariColossusStatueState != GameObjectState.ActiveAlternative
                            || MoorabiStatueState != GameObjectState.ActiveAlternative)
                            return false;
                        break;
                    default:
                        break;
                }

                return true;
            }

            bool IsBridgeReady()
            {
                return SladRanStatueState == GameObjectState.Ready && DrakkariColossusStatueState == GameObjectState.Ready && MoorabiStatueState == GameObjectState.Ready;
            }

            public override void SetData(uint type, uint data)
            {
                if (type == GDDataTypes.StatueActivate)
                {
                    switch (data)
                    {
                        case GDGameObjectIds.SladRanAltar:
                            _events.ScheduleEvent(GDDataTypes.SladRanStatue, GDInstanceMisc.TimerStatueActivation);
                            break;
                        case GDGameObjectIds.DrakkariColossusAltar:
                            _events.ScheduleEvent(GDDataTypes.DrakkariColossusStatue, GDInstanceMisc.TimerStatueActivation);
                            break;
                        case GDGameObjectIds.MoorabiAltar:
                            _events.ScheduleEvent(GDDataTypes.MoorabiStatue, GDInstanceMisc.TimerStatueActivation);
                            break;
                        default:
                            break;
                    }
                }
            }

            public override void WriteSaveDataMore(StringBuilder data)
            {
                data.AppendFormat("{0} {1} {2} ", (uint)SladRanStatueState, (uint)DrakkariColossusStatueState, (uint)MoorabiStatueState);
            }

            public override void ReadSaveDataMore(StringArguments data)
            {
                SladRanStatueState = (GameObjectState)data.NextUInt32();
                DrakkariColossusStatueState = (GameObjectState)data.NextUInt32();
                MoorabiStatueState = (GameObjectState)data.NextUInt32();

                if (IsBridgeReady())
                    _events.ScheduleEvent(GDDataTypes.Bridge, GDInstanceMisc.TimerStatueActivation);
            }

            void ToggleGameObject(uint type, GameObjectState state)
            {
                GameObject go = GetGameObject(type);
                if (go)
                    go.SetGoState(state);

                switch (type)
                {
                    case GDDataTypes.SladRanStatue:
                        SladRanStatueState = state;
                        break;
                    case GDDataTypes.DrakkariColossusStatue:
                        DrakkariColossusStatueState = state;
                        break;
                    case GDDataTypes.MoorabiStatue:
                        MoorabiStatueState = state;
                        break;
                    default:
                        break;
                }
            }

            public override void Update(uint diff)
            {
                _events.Update(diff);

                _events.ExecuteEvents(eventId =>
                {
                    uint spellId = 0;
                    uint altarId = 0;
                    switch (eventId)
                    {
                        case GDDataTypes.SladRanStatue:
                            spellId = GDSpellIds.FireBeamSnake;
                            altarId = GDDataTypes.SladRanAltar;
                            break;
                        case GDDataTypes.DrakkariColossusStatue:
                            spellId = GDSpellIds.FireBeamElemental;
                            altarId = GDDataTypes.DrakkariColossusAltar;
                            break;
                        case GDDataTypes.MoorabiStatue:
                            spellId = GDSpellIds.FireBeamMammoth;
                            altarId = GDDataTypes.MoorabiAltar;
                            break;
                        case GDDataTypes.Bridge:
                            for (uint type = GDDataTypes.SladRanStatue; type <= GDDataTypes.GalDarahStatue; ++type)
                                ToggleGameObject(type, GameObjectState.ActiveAlternative);
                            ToggleGameObject(GDDataTypes.Trapdoor, GameObjectState.Ready);
                            ToggleGameObject(GDDataTypes.Collision, GameObjectState.Active);
                            SaveToDB();
                            return;
                        default:
                            return;
                    }

                    GameObject altar = GetGameObject(altarId);
                    if (altar)
                    {
                        Creature trigger = altar.FindNearestCreature(GDCreatureIds.AltarTrigger, 10.0f);
                        if (trigger)
                            trigger.CastSpell((Unit)null, spellId, true);
                    }

                    // eventId equals statueId
                    ToggleGameObject(eventId, GameObjectState.Ready);

                    if (IsBridgeReady())
                        _events.ScheduleEvent(GDDataTypes.Bridge, GDInstanceMisc.TimerStatueActivation);

                    SaveToDB();
                });
            }

            List<ObjectGuid> DwellerGUIDs = new List<ObjectGuid>();

            GameObjectState SladRanStatueState;
            GameObjectState DrakkariColossusStatueState;
            GameObjectState MoorabiStatueState;
        }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_gundrak_InstanceMapScript(map);
        }
    }

    [Script]
    class go_gundrak_altar : GameObjectScript
    {
        public go_gundrak_altar() : base("go_gundrak_altar") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            go.SetFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
            go.SetGoState(GameObjectState.Active);

            InstanceScript instance = go.GetInstanceScript();
            if (instance != null)
            {
                instance.SetData(GDDataTypes.StatueActivate, go.GetEntry());
                return true;
            }

            return false;
        }
    }
}
