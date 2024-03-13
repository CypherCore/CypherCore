// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using System;
using System.Collections.Generic;

namespace Scripts.EasternKingdoms.BaradinHold
{
    enum DataTypes
    {
        Argaloth = 0,
        Occuthar = 1,
        Alizabal = 2,

        // Encounter Related
        ExtinuishFelFlames
    }

    enum CreatureIds
    {
        // Bosses
        Argaloth = 47120,
        Occuthar = 52363,
        Alizabal = 55869,

        // Encounter Related Creatures
        /*Argaloth*/
        FelFlames = 47829,

        EyeOfOccuthar = 52389,
        FocusFireDummy = 52369,
        OccutharEye = 52368
    }

    enum GameObjectIds
    {
        ArgalothDoor = 207619,
        OccutharDoor = 208953,
        AlizabalDoor = 209849
    }

    enum SpellIds
    {
        // Fel Flames
        FelFlames = 88999
    }

    [Script]
    class instance_baradin_hold : InstanceMapScript
    {
        static DoorData[] doorData =
        {
            new DoorData((uint)GameObjectIds.ArgalothDoor,  (uint)DataTypes.Argaloth, EncounterDoorBehavior.OpenWhenNotInProgress),
            new DoorData((uint)GameObjectIds.OccutharDoor,  (uint)DataTypes.Occuthar, EncounterDoorBehavior.OpenWhenNotInProgress),
            new DoorData((uint)GameObjectIds.AlizabalDoor,  (uint)DataTypes.Alizabal, EncounterDoorBehavior.OpenWhenNotInProgress),
        };

        static ObjectData[] creatureData =
        {
            new ObjectData((uint)CreatureIds.Argaloth, (uint)DataTypes.Argaloth),
            new ObjectData((uint)CreatureIds.Occuthar, (uint)DataTypes.Occuthar),
            new ObjectData((uint)CreatureIds.Alizabal, (uint)DataTypes.Alizabal),
        };

        static DungeonEncounterData[] encounters =
        {
            new DungeonEncounterData((uint)DataTypes.Argaloth, 1033),
            new DungeonEncounterData((uint)DataTypes.Occuthar, 1250),
            new DungeonEncounterData((uint)DataTypes.Alizabal, 1332)
        };

        public instance_baradin_hold() : base(nameof(instance_baradin_hold), 757) { }

        class instance_baradin_hold_InstanceMapScript : InstanceScript
        {
            List<ObjectGuid> _felFlameGUIDs = new();

            public instance_baradin_hold_InstanceMapScript(InstanceMap map) : base(map)
            {
                SetHeaders("BH");
                SetBossNumber(3);
                LoadObjectData(creatureData, null);
                LoadDoorData(doorData);
                LoadDungeonEncounterData(encounters);
            }

            public override void OnCreatureCreate(Creature creature)
            {
                switch ((CreatureIds)creature.GetEntry())
                {
                    case CreatureIds.FelFlames:
                        _felFlameGUIDs.Add(creature.GetGUID());
                        creature.m_Events.AddEventAtOffset(() => creature.CastSpell(null, (uint)SpellIds.FelFlames), TimeSpan.FromSeconds(1));
                        break;
                }
            }

            public override void SetData(uint type, uint value)
            {
                switch ((DataTypes)type)
                {
                    case DataTypes.ExtinuishFelFlames:
                        foreach (ObjectGuid guid in _felFlameGUIDs)
                        {
                            Creature felFlame = instance.GetCreature(guid);
                            if (felFlame != null)
                                felFlame.RemoveAllAuras();
                        }

                        _felFlameGUIDs.Clear();
                        break;
                    default:
                        break;
                }
            }
        }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_baradin_hold_InstanceMapScript(map);
        }
    }
}

