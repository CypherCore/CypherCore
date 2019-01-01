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

namespace Scripts.Northrend.Nexus.Nexus
{
    struct DataTypes
    {
        public const uint Commander = 0;
        public const uint MagusTelestra = 1;
        public const uint Anomalus = 2;
        public const uint Ormorok = 3;
        public const uint Keristrasza = 4;

        public const uint AnomalusContainmetSphere = 5;
        public const uint OrmorokContainmetSphere = 6;
        public const uint TelestraContainmetSphere = 7;
    }

    struct CreatureIds
    {
        public const uint Anomalus = 26763;
        public const uint Keristrasza = 26723;

        // Alliance
        public const uint AllianceBerserker = 26800;
        public const uint AllianceRanger = 26802;
        public const uint AllianceCleric = 26805;
        public const uint AllianceCommander = 27949;
        public const uint CommanderStoutbeard = 26796;

        // Horde
        public const uint HordeBerserker = 26799;
        public const uint HordeRanger = 26801;
        public const uint HordeCleric = 26803;
        public const uint HordeCommander = 27947;
        public const uint CommanderKolurg = 26798;
    }

    struct GameObjectIds
    {
        public const uint AnomalusContainmetSphere = 188527;
        public const uint OrmoroksContainmetSphere = 188528;
        public const uint TelestrasContainmetSphere = 188526;
    }

    [Script]
    class instance_nexus : InstanceMapScript
    {
        public instance_nexus() : base(nameof(instance_nexus), 576) { }

        class instance_nexus_InstanceMapScript : InstanceScript
        {
            public instance_nexus_InstanceMapScript(InstanceMap map) : base(map)
            {
                SetHeaders("NEX");
                SetBossNumber(DataTypes.Keristrasza + 1);
                _teamInInstance = 0;
            }

            public override void OnPlayerEnter(Player player)
            {
                if (_teamInInstance == 0)
                    _teamInInstance = player.GetTeam();
            }

            public override void OnCreatureCreate(Creature creature)
            {
                switch (creature.GetEntry())
                {
                    case CreatureIds.Anomalus:
                        AnomalusGUID = creature.GetGUID();
                        break;
                    case CreatureIds.Keristrasza:
                        KeristraszaGUID = creature.GetGUID();
                        break;
                    // Alliance npcs are spawned by default, if you are alliance, you will fight against horde npcs.
                    case CreatureIds.AllianceBerserker:
                        if (ServerAllowsTwoSideGroups())
                            creature.SetFaction(16);
                        if (_teamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.HordeBerserker);
                        break;
                    case CreatureIds.AllianceRanger:
                        if (ServerAllowsTwoSideGroups())
                            creature.SetFaction(16);
                        if (_teamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.HordeRanger);
                        break;
                    case CreatureIds.AllianceCleric:
                        if (ServerAllowsTwoSideGroups())
                            creature.SetFaction(16);
                        if (_teamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.HordeCleric);
                        break;
                    case CreatureIds.AllianceCommander:
                        if (ServerAllowsTwoSideGroups())
                            creature.SetFaction(16);
                        if (_teamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.HordeCommander);
                        break;
                    case CreatureIds.CommanderStoutbeard:
                        if (ServerAllowsTwoSideGroups())
                            creature.SetFaction(16);
                        if (_teamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.CommanderKolurg);
                        break;
                    default:
                        break;
                }
            }

            public override void OnGameObjectCreate(GameObject go)
            {
                switch (go.GetEntry())
                {
                    case GameObjectIds.AnomalusContainmetSphere:
                        AnomalusContainmentSphere = go.GetGUID();
                        if (GetBossState(DataTypes.Anomalus) == EncounterState.Done)
                            go.RemoveFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                        break;
                    case GameObjectIds.OrmoroksContainmetSphere:
                        OrmoroksContainmentSphere = go.GetGUID();
                        if (GetBossState(DataTypes.Ormorok) == EncounterState.Done)
                            go.RemoveFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                        break;
                    case GameObjectIds.TelestrasContainmetSphere:
                        TelestrasContainmentSphere = go.GetGUID();
                        if (GetBossState(DataTypes.MagusTelestra) == EncounterState.Done)
                            go.RemoveFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                        break;
                    default:
                        break;
                }
            }

            public override bool SetBossState(uint type, EncounterState state)
            {
                if (!base.SetBossState(type, state))
                    return false;

                switch (type)
                {
                    case DataTypes.MagusTelestra:
                        if (state == EncounterState.Done)
                        {
                            GameObject sphere = instance.GetGameObject(TelestrasContainmentSphere);
                            if (sphere)
                                sphere.RemoveFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                        }
                        break;
                    case DataTypes.Anomalus:
                        if (state == EncounterState.Done)
                        {
                            GameObject sphere = instance.GetGameObject(AnomalusContainmentSphere);
                            if (sphere)
                                sphere.RemoveFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                        }
                        break;
                    case DataTypes.Ormorok:
                        if (state == EncounterState.Done)
                        {
                            GameObject sphere = instance.GetGameObject(OrmoroksContainmentSphere);
                            if (sphere)
                                sphere.RemoveFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                        }
                        break;
                    default:
                        break;
                }

                return true;
            }

            public override ObjectGuid GetGuidData(uint type)
            {
                switch (type)
                {
                    case DataTypes.Anomalus:
                        return AnomalusGUID;
                    case DataTypes.Keristrasza:
                        return KeristraszaGUID;
                    case DataTypes.AnomalusContainmetSphere:
                        return AnomalusContainmentSphere;
                    case DataTypes.OrmorokContainmetSphere:
                        return OrmoroksContainmentSphere;
                    case DataTypes.TelestraContainmetSphere:
                        return TelestrasContainmentSphere;
                    default:
                        break;
                }

                return ObjectGuid.Empty;
            }

            ObjectGuid AnomalusGUID;
            ObjectGuid KeristraszaGUID;
            ObjectGuid AnomalusContainmentSphere;
            ObjectGuid OrmoroksContainmentSphere;
            ObjectGuid TelestrasContainmentSphere;
            Team _teamInInstance;
        }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_nexus_InstanceMapScript(map);
        }
    }
}
