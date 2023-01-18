// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;

namespace Scripts.EasternKingdoms.Gnomeregan
{
    struct GNOGameObjectIds
    {
        public const uint CaveInLeft = 146085;
        public const uint CaveInRight = 146086;
        public const uint RedRocket = 103820;
    }

    struct GNOCreatureIds
    {
        public const uint BlastmasterEmiShortfuse = 7998;
        public const uint CaverndeepAmbusher = 6207;
        public const uint Grubbis = 7361;
        public const uint ViciousFallout = 7079;
        public const uint Chomper = 6215;
        public const uint Electrocutioner = 6235;
        public const uint CrowdPummeler = 6229;
        public const uint Mekgineer = 7800;
    }

    struct DataTypes
    {
        public const uint BlastmasterEvent = 0;
        public const uint ViciousFallout = 1;
        public const uint Electrocutioner = 2;
        public const uint CrowdPummeler = 3;
        public const uint Thermaplugg = 4;

        public const uint MaxEncounter = 5;

        // Additional Objects
        public const uint GoCaveInLeft = 6;
        public const uint GoCaveInRight = 7;
        public const uint NpcBastmasterEmiShortfuse = 8;
    }

    struct DataTypes64
    {
        public const uint GoCaveInLeft = 0;
        public const uint GoCaveInRight = 1;
        public const uint NpcBastmasterEmiShortfuse = 2;
    }

    class instance_gnomeregan : InstanceMapScript
    {
        public instance_gnomeregan() : base(nameof(instance_gnomeregan), 90) { }

        class instance_gnomeregan_InstanceMapScript : InstanceScript
        {
            ObjectGuid uiCaveInLeftGUID;
            ObjectGuid uiCaveInRightGUID;

            ObjectGuid uiBlastmasterEmiShortfuseGUID;

            public instance_gnomeregan_InstanceMapScript(InstanceMap map) : base(map)
            {
                SetHeaders("GNO");
                SetBossNumber(DataTypes.MaxEncounter);
            }

            public override void OnCreatureCreate(Creature creature)
            {
                switch (creature.GetEntry())
                {
                    case GNOCreatureIds.BlastmasterEmiShortfuse:
                        uiBlastmasterEmiShortfuseGUID = creature.GetGUID();
                        break;
                }
            }

            public override void OnGameObjectCreate(GameObject go)
            {
                switch (go.GetEntry())
                {
                    case DataTypes64.GoCaveInLeft:
                        uiCaveInLeftGUID = go.GetGUID();
                        break;
                    case DataTypes64.GoCaveInRight:
                        uiCaveInRightGUID = go.GetGUID();
                        break;
                }
            }

            public override void OnUnitDeath(Unit unit)
            {
                Creature creature = unit.ToCreature();
                if (creature)
                    switch (creature.GetEntry())
                    {
                        case GNOCreatureIds.ViciousFallout:
                            SetBossState(DataTypes.ViciousFallout, EncounterState.Done);
                            break;
                        case GNOCreatureIds.Electrocutioner:
                            SetBossState(DataTypes.Electrocutioner, EncounterState.Done);
                            break;
                        case GNOCreatureIds.CrowdPummeler:
                            SetBossState(DataTypes.CrowdPummeler, EncounterState.Done);
                            break;
                        case GNOCreatureIds.Mekgineer:
                            SetBossState(DataTypes.Thermaplugg, EncounterState.Done);
                            break;
                    }
            }

            public override ObjectGuid GetGuidData(uint uiType)
            {
                switch (uiType)
                {
                    case DataTypes64.GoCaveInLeft: return uiCaveInLeftGUID;
                    case DataTypes64.GoCaveInRight: return uiCaveInRightGUID;
                    case DataTypes64.NpcBastmasterEmiShortfuse: return uiBlastmasterEmiShortfuseGUID;
                }

                return ObjectGuid.Empty;
            }
        }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_gnomeregan_InstanceMapScript(map);
        }
    }
}

