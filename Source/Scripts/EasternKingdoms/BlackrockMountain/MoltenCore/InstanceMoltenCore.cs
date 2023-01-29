// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.BaseScripts;
using Game.Scripting.Interfaces.IMap;

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore
{
    internal struct DataTypes
    {
        public const uint Lucifron = 0;
        public const uint Magmadar = 1;
        public const uint Gehennas = 2;
        public const uint Garr = 3;
        public const uint Shazzrah = 4;
        public const uint BaronGeddon = 5;
        public const uint SulfuronHarbinger = 6;
        public const uint GolemaggTheIncinerator = 7;
        public const uint MajordomoExecutus = 8;
        public const uint Ragnaros = 9;

        public const uint MaxEncounter = 10;
    }

    internal struct ActionIds
    {
        public const int StartRagnaros = 0;
        public const int StartRagnarosAlt = 1;
    }

    internal struct MCCreatureIds
    {
        public const uint Lucifron = 12118;
        public const uint Magmadar = 11982;
        public const uint Gehennas = 12259;
        public const uint Garr = 12057;
        public const uint Shazzrah = 12264;
        public const uint BaronGeddon = 12056;
        public const uint SulfuronHarbinger = 12098;
        public const uint GolemaggTheIncinerator = 11988;
        public const uint MajordomoExecutus = 12018;
        public const uint Ragnaros = 11502;
        public const uint FlamewakerHealer = 11663;
        public const uint FlamewakerElite = 11664;
    }

    internal struct MCGameObjectIds
    {
        public const uint CacheOfTheFirelord = 179703;
    }

    internal struct MCMiscConst
    {
        public const uint DataRagnarosAdds = 0;

        public static Position[] SummonPositions =
        {
            new(737.850f, -1145.35f, -120.288f, 4.71368f), new(744.162f, -1151.63f, -119.726f, 4.58204f), new(751.247f, -1152.82f, -119.744f, 4.49673f), new(759.206f, -1155.09f, -120.051f, 4.30104f), new(755.973f, -1152.33f, -120.029f, 4.25588f), new(731.712f, -1147.56f, -120.195f, 4.95955f), new(726.499f, -1149.80f, -120.156f, 5.24055f), new(722.408f, -1152.41f, -120.029f, 5.33087f), new(718.994f, -1156.36f, -119.805f, 5.75738f), new(838.510f, -829.840f, -232.000f, 2.00000f)
        };

        public static Position RagnarosTelePos = new(829.159f, -815.773f, -228.972f, 5.30500f);
        public static Position RagnarosSummonPos = new(838.510f, -829.840f, -232.000f, 2.00000f);
    }

    [Script]
    internal class instance_molten_core : InstanceMapScript, IInstanceMapGetInstanceScript
    {
        private class instance_molten_core_InstanceMapScript : InstanceScript
        {
            private ObjectGuid _cacheOfTheFirelordGUID;
            private bool _executusSchedule;
            private ObjectGuid _golemaggTheIncineratorGUID;
            private ObjectGuid _majordomoExecutusGUID;
            private byte _ragnarosAddDeaths;

            public instance_molten_core_InstanceMapScript(InstanceMap map) : base(map)
            {
                SetHeaders("MC");
                SetBossNumber(DataTypes.MaxEncounter);
                LoadDungeonEncounterData(encounters);
                _executusSchedule = false;
                _ragnarosAddDeaths = 0;
            }

            public override void OnPlayerEnter(Player player)
            {
                if (_executusSchedule)
                    SummonMajordomoExecutus();
            }

            public override void OnCreatureCreate(Creature creature)
            {
                switch (creature.GetEntry())
                {
                    case MCCreatureIds.GolemaggTheIncinerator:
                        _golemaggTheIncineratorGUID = creature.GetGUID();

                        break;
                    case MCCreatureIds.MajordomoExecutus:
                        _majordomoExecutusGUID = creature.GetGUID();

                        break;
                    default:
                        break;
                }
            }

            public override void OnGameObjectCreate(GameObject go)
            {
                switch (go.GetEntry())
                {
                    case MCGameObjectIds.CacheOfTheFirelord:
                        _cacheOfTheFirelordGUID = go.GetGUID();

                        break;
                    default:
                        break;
                }
            }

            public override void SetData(uint type, uint data)
            {
                if (type == MCMiscConst.DataRagnarosAdds)
                {
                    if (data == 1)
                        ++_ragnarosAddDeaths;
                    else if (data == 0)
                        _ragnarosAddDeaths = 0;
                }
            }

            public override uint GetData(uint type)
            {
                switch (type)
                {
                    case MCMiscConst.DataRagnarosAdds:
                        return _ragnarosAddDeaths;
                }

                return 0;
            }

            public override ObjectGuid GetGuidData(uint type)
            {
                switch (type)
                {
                    case DataTypes.GolemaggTheIncinerator:
                        return _golemaggTheIncineratorGUID;
                    case DataTypes.MajordomoExecutus:
                        return _majordomoExecutusGUID;
                }

                return ObjectGuid.Empty;
            }

            public override bool SetBossState(uint bossId, EncounterState state)
            {
                if (!base.SetBossState(bossId, state))
                    return false;

                if (state == EncounterState.Done &&
                    bossId < DataTypes.MajordomoExecutus)
                    if (CheckMajordomoExecutus())
                        SummonMajordomoExecutus();

                if (bossId == DataTypes.MajordomoExecutus &&
                    state == EncounterState.Done)
                    DoRespawnGameObject(_cacheOfTheFirelordGUID, TimeSpan.FromDays(7));

                return true;
            }

            public override void AfterDataLoad()
            {
                if (CheckMajordomoExecutus())
                    _executusSchedule = true;
            }

            private void SummonMajordomoExecutus()
            {
                _executusSchedule = false;

                if (!_majordomoExecutusGUID.IsEmpty())
                    return;

                if (GetBossState(DataTypes.MajordomoExecutus) != EncounterState.Done)
                {
                    instance.SummonCreature(MCCreatureIds.MajordomoExecutus, MCMiscConst.SummonPositions[0]);
                    instance.SummonCreature(MCCreatureIds.FlamewakerHealer, MCMiscConst.SummonPositions[1]);
                    instance.SummonCreature(MCCreatureIds.FlamewakerHealer, MCMiscConst.SummonPositions[2]);
                    instance.SummonCreature(MCCreatureIds.FlamewakerHealer, MCMiscConst.SummonPositions[3]);
                    instance.SummonCreature(MCCreatureIds.FlamewakerHealer, MCMiscConst.SummonPositions[4]);
                    instance.SummonCreature(MCCreatureIds.FlamewakerElite, MCMiscConst.SummonPositions[5]);
                    instance.SummonCreature(MCCreatureIds.FlamewakerElite, MCMiscConst.SummonPositions[6]);
                    instance.SummonCreature(MCCreatureIds.FlamewakerElite, MCMiscConst.SummonPositions[7]);
                    instance.SummonCreature(MCCreatureIds.FlamewakerElite, MCMiscConst.SummonPositions[8]);
                }
                else
                {
                    TempSummon summon = instance.SummonCreature(MCCreatureIds.MajordomoExecutus, MCMiscConst.RagnarosTelePos);

                    if (summon)
                        summon.GetAI().DoAction(ActionIds.StartRagnarosAlt);
                }
            }

            private bool CheckMajordomoExecutus()
            {
                if (GetBossState(DataTypes.Ragnaros) == EncounterState.Done)
                    return false;

                for (byte i = 0; i < DataTypes.MajordomoExecutus; ++i)
                    if (GetBossState(i) != EncounterState.Done)
                        return false;

                return true;
            }
        }

        private static readonly DungeonEncounterData[] encounters =
        {
            new(DataTypes.Lucifron, 663), new(DataTypes.Magmadar, 664), new(DataTypes.Gehennas, 665), new(DataTypes.Garr, 666), new(DataTypes.Shazzrah, 667), new(DataTypes.BaronGeddon, 668), new(DataTypes.SulfuronHarbinger, 669), new(DataTypes.GolemaggTheIncinerator, 670), new(DataTypes.MajordomoExecutus, 671), new(DataTypes.Ragnaros, 672)
        };

        public instance_molten_core() : base(nameof(instance_molten_core), 409)
        {
        }

        public InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_molten_core_InstanceMapScript(map);
        }
    }
}