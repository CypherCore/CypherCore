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
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using System.Collections.Generic;

namespace Scripts.Northrend.AzjolNerub.Ahnkahet
{
    struct DataTypes
    {
        // Encounter States/Boss GUIDs
        public const uint ElderNadox = 0;
        public const uint PrinceTaldaram = 1;
        public const uint JedogaShadowseeker = 2;
        public const uint Amanitar = 3;
        public const uint HeraldVolazj = 4;

        // Additional Data
        public const uint Sphere1 = 5;
        public const uint Sphere2 = 6;
        public const uint PrinceTaldaramPlatform = 7;
        public const uint PlJedogaTarget = 8;
        public const uint AddJedogaVictim = 9;
        public const uint AddJedogaInitiand = 10;
        public const uint JedogaTriggerSwitch = 11;
        public const uint JedogaResetInitiands = 12;
        public const uint AllInitiandDead = 13;
    }

    struct AKCreatureIds
    {
        public const uint ElderNadox = 29309;
        public const uint PrinceTaldaram = 29308;
        public const uint JedogaShadowseeker = 29310;
        public const uint Amanitar = 30258;
        public const uint HeraldVolazj = 29311;

        // Elder Nadox
        public const uint AhnkaharGuardian = 30176;
        public const uint AhnkaharSwarmer = 30178;

        // Jedoga Shadowseeker
        public const uint Initiand = 30114;
        public const uint JedogaController = 30181;

        // Herald Volazj
        //public const uint TwistedVisage1          = 30621,
        //public const uint TwistedVisage2          = 30622,
        //public const uint TwistedVisage3          = 30623,
        //public const uint TwistedVisage4          = 30624,
        public const uint TwistedVisage = 30625;
    }

    struct GameObjectIds
    {
        public const uint PrinceTaldaramGate = 192236;
        public const uint PrinceTaldaramPlatform = 193564;
        public const uint Sphere1 = 193093;
        public const uint Sphere2 = 193094;
    }

    [Script]
    class instance_ahnkahet : InstanceMapScript
    {
        public instance_ahnkahet() : base("instance_ahnkahet", 619) { }

        class instance_ahnkahet_InstanceScript : InstanceScript
        {
            public instance_ahnkahet_InstanceScript(InstanceMap map) : base(map)
            {
                SetHeaders("AK");
                SetBossNumber(DataTypes.HeraldVolazj + 1);
                LoadDoorData(new DoorData(GameObjectIds.PrinceTaldaramGate, DataTypes.PrinceTaldaram, DoorType.Passage));

                SwitchTrigger = 0;

                SpheresState[0] = 0;
                SpheresState[1] = 0;
            }

            public override void OnCreatureCreate(Creature creature)
            {
                switch (creature.GetEntry())
                {
                    case AKCreatureIds.ElderNadox:
                        ElderNadoxGUID = creature.GetGUID();
                        break;
                    case AKCreatureIds.PrinceTaldaram:
                        PrinceTaldaramGUID = creature.GetGUID();
                        break;
                    case AKCreatureIds.JedogaShadowseeker:
                        JedogaShadowseekerGUID = creature.GetGUID();
                        break;
                    case AKCreatureIds.Amanitar:
                        AmanitarGUID = creature.GetGUID();
                        break;
                    case AKCreatureIds.HeraldVolazj:
                        HeraldVolazjGUID = creature.GetGUID();
                        break;
                    case AKCreatureIds.Initiand:
                        InitiandGUIDs.Add(creature.GetGUID());
                        break;
                    default:
                        break;
                }
            }

            public override void OnGameObjectCreate(GameObject go)
            {
                switch (go.GetEntry())
                {
                    case GameObjectIds.PrinceTaldaramPlatform:
                        PrinceTaldaramPlatformGUID = go.GetGUID();
                        if (GetBossState(DataTypes.PrinceTaldaram) == EncounterState.Done)
                            HandleGameObject(ObjectGuid.Empty, true, go);
                        break;
                    case GameObjectIds.Sphere1:
                        if (SpheresState[0] != 0)
                        {
                            go.SetGoState(GameObjectState.Active);
                            go.SetFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                        }
                        else
                            go.RemoveFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                        break;
                    case GameObjectIds.Sphere2:
                        if (SpheresState[1] != 0)
                        {
                            go.SetGoState(GameObjectState.Active);
                            go.SetFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                        }
                        else
                            go.RemoveFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                        break;
                    case GameObjectIds.PrinceTaldaramGate:
                        AddDoor(go, true);
                        break;
                    default:
                        break;
                }
            }

            public override void OnGameObjectRemove(GameObject go)
            {
                switch (go.GetEntry())
                {
                    case GameObjectIds.PrinceTaldaramGate:
                        AddDoor(go, false);
                        break;
                    default:
                        break;
                }
            }

            public override void SetData(uint type, uint data)
            {
                switch (type)
                {
                    case DataTypes.Sphere1:
                    case DataTypes.Sphere2:
                        SpheresState[type - DataTypes.Sphere1] = data;
                        break;
                    case DataTypes.JedogaTriggerSwitch:
                        SwitchTrigger = (byte)data;
                        break;
                    case DataTypes.JedogaResetInitiands:
                        foreach (ObjectGuid guid in InitiandGUIDs)
                        {
                            Creature creature = instance.GetCreature(guid);
                            if (creature)
                            {
                                creature.Respawn();
                                if (!creature.IsInEvadeMode())
                                    creature.GetAI().EnterEvadeMode();
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            public override uint GetData(uint type)
            {
                switch (type)
                {
                    case DataTypes.Sphere1:
                    case DataTypes.Sphere2:
                        return SpheresState[type - DataTypes.Sphere1];
                    case DataTypes.AllInitiandDead:
                        foreach (ObjectGuid guid in InitiandGUIDs)
                        {
                            Creature cr = instance.GetCreature(guid);
                            if (!cr || cr.IsAlive())
                                return 0;
                        }
                        return 1;
                    case DataTypes.JedogaTriggerSwitch:
                        return SwitchTrigger;
                    default:
                        break;
                }
                return 0;
            }

            public override void SetGuidData(uint type, ObjectGuid data)
            {
                switch (type)
                {
                    case DataTypes.AddJedogaVictim:
                        JedogaSacrifices = data;
                        break;
                    case DataTypes.PlJedogaTarget:
                        JedogaTarget = data;
                        break;
                    default:
                        break;
                }
            }

            public override ObjectGuid GetGuidData(uint type)
            {
                switch (type)
                {
                    case DataTypes.ElderNadox:
                        return ElderNadoxGUID;
                    case DataTypes.PrinceTaldaram:
                        return PrinceTaldaramGUID;
                    case DataTypes.JedogaShadowseeker:
                        return JedogaShadowseekerGUID;
                    case DataTypes.Amanitar:
                        return AmanitarGUID;
                    case DataTypes.HeraldVolazj:
                        return HeraldVolazjGUID;
                    case DataTypes.PrinceTaldaramPlatform:
                        return PrinceTaldaramPlatformGUID;
                    case DataTypes.AddJedogaInitiand:
                        {
                            List<ObjectGuid> vInitiands = new List<ObjectGuid>();
                            foreach (ObjectGuid guid in InitiandGUIDs)
                            {
                                Creature cr = instance.GetCreature(guid);
                                if (cr && cr.IsAlive())
                                    vInitiands.Add(guid);
                            }
                            if (vInitiands.Empty())
                                return ObjectGuid.Empty;

                            return vInitiands.SelectRandom();
                        }
                    case DataTypes.AddJedogaVictim:
                        return JedogaSacrifices;
                    case DataTypes.PlJedogaTarget:
                        return JedogaTarget;
                    default:
                        break;
                }
                return ObjectGuid.Empty;
            }

            public override bool SetBossState(uint type, EncounterState state)
            {
                if (!base.SetBossState(type, state))
                    return false;

                switch (type)
                {
                    case DataTypes.JedogaShadowseeker:
                        if (state == EncounterState.Done)
                        {
                            foreach (ObjectGuid guid in InitiandGUIDs)
                            {
                                Creature cr = instance.GetCreature(guid);
                                if (cr)
                                    cr.DespawnOrUnsummon();
                            }
                        }
                        break;
                    default:
                        break;
                }
                return true;
            }

            void WriteSaveDataMore(string data)
            {
                data += SpheresState[0] + ' ' + SpheresState[1];
            }

            void ReadSaveDataMore(string data)
            {
                data += SpheresState[0];
                data += SpheresState[1];
            }

            ObjectGuid ElderNadoxGUID;
            ObjectGuid PrinceTaldaramGUID;
            ObjectGuid JedogaShadowseekerGUID;
            ObjectGuid AmanitarGUID;
            ObjectGuid HeraldVolazjGUID;

            ObjectGuid PrinceTaldaramPlatformGUID;
            ObjectGuid JedogaSacrifices;
            ObjectGuid JedogaTarget;

            List<ObjectGuid> InitiandGUIDs = new List<ObjectGuid>();

            uint[] SpheresState = new uint[2];
            byte SwitchTrigger;
        }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_ahnkahet_InstanceScript(map);
        }
    }
}
