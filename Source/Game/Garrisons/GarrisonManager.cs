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
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Garrisons
{
    public class GarrisonManager : Singleton<GarrisonManager>
    {
        GarrisonManager() { }

        public void Initialize()
        {
            foreach (GarrSiteLevelPlotInstRecord siteLevelPlotInst in CliDB.GarrSiteLevelPlotInstStorage.Values)
                _garrisonPlotInstBySiteLevel.Add(siteLevelPlotInst.GarrSiteLevelID, siteLevelPlotInst);

            foreach (GameObjectsRecord gameObject in CliDB.GameObjectsStorage.Values)
            {
                if (gameObject.TypeID == GameObjectTypes.GarrisonPlot)
                {
                    if (!_garrisonPlots.ContainsKey(gameObject.OwnerID))
                        _garrisonPlots[gameObject.OwnerID] = new Dictionary<uint, GameObjectsRecord>();

                    _garrisonPlots[gameObject.OwnerID][(uint)gameObject.PropValue[0]] = gameObject;
                }
            }

            foreach (GarrPlotBuildingRecord plotBuilding in CliDB.GarrPlotBuildingStorage.Values)
                _garrisonBuildingsByPlot.Add(plotBuilding.GarrPlotID, plotBuilding.GarrBuildingID);

            foreach (GarrBuildingPlotInstRecord buildingPlotInst in CliDB.GarrBuildingPlotInstStorage.Values)
                _garrisonBuildingPlotInstances[MathFunctions.MakePair64(buildingPlotInst.GarrBuildingID, buildingPlotInst.GarrSiteLevelPlotInstID)] = buildingPlotInst.Id;

            foreach (GarrBuildingRecord building in CliDB.GarrBuildingStorage.Values)
                _garrisonBuildingsByType.Add(building.BuildingType, building.Id);

            for (var i = 0; i < 2; ++i)
                _garrisonFollowerAbilities[i] = new Dictionary<uint, GarrAbilities>();

            foreach (GarrFollowerXAbilityRecord followerAbility in CliDB.GarrFollowerXAbilityStorage.Values)
            {
                GarrAbilityRecord ability = CliDB.GarrAbilityStorage.LookupByKey(followerAbility.GarrAbilityID);
                if (ability != null)
                {
                    if (ability.GarrFollowerTypeID != (uint)GarrisonFollowerType.Garrison)
                        continue;

                    if (!ability.Flags.HasAnyFlag(GarrisonAbilityFlags.CannotRoll) && ability.Flags.HasAnyFlag(GarrisonAbilityFlags.Trait))
                        _garrisonFollowerRandomTraits.Add(ability);

                    if (followerAbility.FactionIndex < 2)
                    {
                        var dic = _garrisonFollowerAbilities[followerAbility.FactionIndex];

                        if (!dic.ContainsKey(followerAbility.GarrFollowerID))
                            dic[followerAbility.GarrFollowerID] = new GarrAbilities();

                        if (ability.Flags.HasAnyFlag(GarrisonAbilityFlags.Trait))
                            dic[followerAbility.GarrFollowerID].Traits.Add(ability);
                        else
                            dic[followerAbility.GarrFollowerID].Counters.Add(ability);
                    }
                }
            }

            InitializeDbIdSequences();
            LoadPlotFinalizeGOInfo();
            LoadFollowerClassSpecAbilities();
        }

        public GarrSiteLevelRecord GetGarrSiteLevelEntry(uint garrSiteId, uint level)
        {
            foreach (GarrSiteLevelRecord siteLevel in CliDB.GarrSiteLevelStorage.Values)
                if (siteLevel.GarrSiteID == garrSiteId && siteLevel.GarrLevel == level)
                    return siteLevel;

            return null;
        }

        public List<GarrSiteLevelPlotInstRecord> GetGarrPlotInstForSiteLevel(uint garrSiteLevelId)
        {
            return _garrisonPlotInstBySiteLevel.LookupByKey(garrSiteLevelId);
        }

        public GameObjectsRecord GetPlotGameObject(uint mapId, uint garrPlotInstanceId)
        {
            var pair = _garrisonPlots.LookupByKey(mapId);
            if (pair != null)
            {
                var gameobjectsRecord = pair.LookupByKey(garrPlotInstanceId);
                if (gameobjectsRecord != null)
                    return gameobjectsRecord;
            }

            return null;
        }

        public bool IsPlotMatchingBuilding(uint garrPlotId, uint garrBuildingId)
        {
            var plotList = _garrisonBuildingsByPlot.LookupByKey(garrPlotId);
            if (!plotList.Empty())
                return plotList.Contains(garrBuildingId);

            return false;
        }

        public uint GetGarrBuildingPlotInst(uint garrBuildingId, uint garrSiteLevelPlotInstId)
        {
            return _garrisonBuildingPlotInstances.LookupByKey(MathFunctions.MakePair64(garrBuildingId, garrSiteLevelPlotInstId));
        }

        public uint GetPreviousLevelBuilding(uint buildingType, uint currentLevel)
        {
            var list = _garrisonBuildingsByType.LookupByKey(buildingType);
            if (!list.Empty())
            {
                foreach (uint buildingId in list)
                    if (CliDB.GarrBuildingStorage.LookupByKey(buildingId).UpgradeLevel == currentLevel - 1)
                        return buildingId;
            }

            return 0;
        }

        public FinalizeGarrisonPlotGOInfo GetPlotFinalizeGOInfo(uint garrPlotInstanceID)
        {
            return _finalizePlotGOInfo.LookupByKey(garrPlotInstanceID);
        }

        public ulong GenerateFollowerDbId()
        {
            if (_followerDbIdGenerator >= ulong.MaxValue)
            {
                Log.outFatal(LogFilter.Server, "Garrison follower db id overflow! Can't continue, shutting down server. ");
                Global.WorldMgr.StopNow();
            }

            return _followerDbIdGenerator++;
        }

        // Counters, Traits
        uint[,] AbilitiesForQuality =
        {
            { 0, 0 },
            { 1, 0 },
            { 1, 1 },   // Uncommon
            { 1, 2 },   // Rare
            { 2, 3 },   // Epic
            { 2, 3 }    // Legendary
        };

        //todo check this method, might be slow.....
        public List<GarrAbilityRecord> RollFollowerAbilities(uint garrFollowerId, GarrFollowerRecord follower, uint quality, uint faction, bool initial)
        {
            Cypher.Assert(faction< 2);

            bool hasForcedExclusiveTrait = false;
            List<GarrAbilityRecord> result = new List<GarrAbilityRecord>();
            uint[] slots = { AbilitiesForQuality[quality, 0], AbilitiesForQuality[quality, 1] };

            GarrAbilities garrAbilities = null;
            var abilities = _garrisonFollowerAbilities[faction].LookupByKey(garrFollowerId);
            if (abilities != null)
                garrAbilities = abilities;

            List<GarrAbilityRecord> abilityList = new List<GarrAbilityRecord>();
            List<GarrAbilityRecord> forcedAbilities = new List<GarrAbilityRecord>();
            List<GarrAbilityRecord> traitList = new List<GarrAbilityRecord>();
            List<GarrAbilityRecord> forcedTraits = new List<GarrAbilityRecord>();
            if (garrAbilities != null)
            {
                foreach (GarrAbilityRecord ability in garrAbilities.Counters)
                {
                    if (ability.Flags.HasAnyFlag(GarrisonAbilityFlags.HordeOnly) && faction != GarrisonFactionIndex.Horde)
                        continue;
                    else if (ability.Flags.HasAnyFlag(GarrisonAbilityFlags.AllianceOnly) && faction != GarrisonFactionIndex.Alliance)
                        continue;

                    if (ability.Flags.HasAnyFlag(GarrisonAbilityFlags.CannotRemove))
                        forcedAbilities.Add(ability);
                    else
                        abilityList.Add(ability);
                }

                foreach (GarrAbilityRecord ability in garrAbilities.Traits)
                {
                    if (ability.Flags.HasAnyFlag(GarrisonAbilityFlags.HordeOnly) && faction != GarrisonFactionIndex.Horde)
                        continue;
                    else if (ability.Flags.HasAnyFlag(GarrisonAbilityFlags.AllianceOnly) && faction != GarrisonFactionIndex.Alliance)
                        continue;

                    if (ability.Flags.HasAnyFlag(GarrisonAbilityFlags.CannotRemove))
                        forcedTraits.Add(ability);
                    else
                        traitList.Add(ability);
                }
            }

            abilityList.RandomResize((uint)Math.Max(0, slots[0] - forcedAbilities.Count));
            traitList.RandomResize((uint)Math.Max(0, slots[1] - forcedTraits.Count));

            // Add abilities specified in GarrFollowerXAbility.db2 before generic classspec ones on follower creation
            if (initial)
            {
                forcedAbilities.AddRange(abilityList);
                forcedTraits.AddRange(traitList);
            }

            forcedAbilities.Sort();
            abilityList.Sort();
            forcedTraits.Sort();
            traitList.Sort();

            // check if we have a trait from exclusive category
            foreach (GarrAbilityRecord ability in forcedTraits)
            {
                if (ability.Flags.HasAnyFlag(GarrisonAbilityFlags.Exclusive))
                {
                    hasForcedExclusiveTrait = true;
                    break;
                }
            }

            if (slots[0] > forcedAbilities.Count + abilityList.Count)
            {
                List<GarrAbilityRecord> classSpecAbilities = GetClassSpecAbilities(follower, faction);
                IEnumerable<GarrAbilityRecord> classSpecAbilitiesTemp = classSpecAbilities.Except(forcedAbilities);

                abilityList = classSpecAbilitiesTemp.Union(abilityList).ToList();
                abilityList.RandomResize((uint)Math.Max(0, slots[0] - forcedAbilities.Count));
            }

            if (slots[1] > forcedTraits.Count + traitList.Count)
            {
                List<GarrAbilityRecord> genericTraitsTemp = new List<GarrAbilityRecord>();
                foreach (GarrAbilityRecord ability in _garrisonFollowerRandomTraits)
                {
                    if (ability.Flags.HasAnyFlag(GarrisonAbilityFlags.HordeOnly) && faction != GarrisonFactionIndex.Horde)
                        continue;
                    else if (ability.Flags.HasAnyFlag(GarrisonAbilityFlags.AllianceOnly) && faction != GarrisonFactionIndex.Alliance)
                        continue;

                    // forced exclusive trait exists, skip other ones entirely
                    if (hasForcedExclusiveTrait && ability.Flags.HasAnyFlag(GarrisonAbilityFlags.Exclusive))
                        continue;

                    genericTraitsTemp.Add(ability);
                }

                List<GarrAbilityRecord> genericTraits = genericTraitsTemp.Except(forcedTraits).ToList();
                genericTraits.AddRange(traitList);
                genericTraits.Sort((GarrAbilityRecord a1, GarrAbilityRecord a2) =>
                {
                    int e1 = (int)(a1.Flags & GarrisonAbilityFlags.Exclusive);
                    int e2 = (int)(a2.Flags & GarrisonAbilityFlags.Exclusive);
                    if (e1 != e2)
                        return e1.CompareTo(e2);

                    return a1.Id.CompareTo(a2.Id);
                });
                genericTraits = genericTraits.Distinct().ToList();

                int firstExclusive = 0;
                int total = genericTraits.Count;
                for (var i = 0; i < total; ++i, ++firstExclusive)
                    if (genericTraits[i].Flags.HasAnyFlag(GarrisonAbilityFlags.Exclusive))
                        break;

                while (traitList.Count < Math.Max(0, slots[1] - forcedTraits.Count) && total != 0)
                {
                    var garrAbility = genericTraits[RandomHelper.IRand(0, total-- - 1)];
                    if (garrAbility.Flags.HasAnyFlag(GarrisonAbilityFlags.Exclusive))
                        total = firstExclusive; // selected exclusive trait - no other can be selected now
                    else
                        --firstExclusive;

                    traitList.Add(garrAbility);
                    genericTraits.Remove(garrAbility);
                }
            }

            result.AddRange(forcedAbilities);
            result.AddRange(abilityList);
            result.AddRange(forcedTraits);
            result.AddRange(traitList);

            return result;
        }

        List<GarrAbilityRecord> GetClassSpecAbilities(GarrFollowerRecord follower, uint faction)
        {
            List<GarrAbilityRecord> abilities = new List<GarrAbilityRecord>();
            uint classSpecId;
            switch (faction)
            {
                case GarrisonFactionIndex.Horde:
                    classSpecId = follower.HordeGarrClassSpecID;
                    break;
                case GarrisonFactionIndex.Alliance:
                    classSpecId = follower.AllianceGarrClassSpecID;
                    break;
                default:
                    return abilities;
            }

            if (!CliDB.GarrClassSpecStorage.ContainsKey(classSpecId))
                return abilities;

            var garrAbility = _garrisonFollowerClassSpecAbilities.LookupByKey(classSpecId);
            if (!garrAbility.Empty())
                abilities = garrAbility;

            return abilities;
        }

        void InitializeDbIdSequences()
        {
            SQLResult result = DB.Characters.Query("SELECT MAX(dbId) FROM character_garrison_followers");
            if (!result.IsEmpty())
                _followerDbIdGenerator = result.Read<ulong>(0) + 1;
        }

        void LoadPlotFinalizeGOInfo()
        {
            //                                                                0                  1       2       3       4       5               6
            SQLResult result = DB.World.Query("SELECT garrPlotInstanceId, hordeGameObjectId, hordeX, hordeY, hordeZ, hordeO, hordeAnimKitId, " +
                //                      7          8          9         10         11                 12
                "allianceGameObjectId, allianceX, allianceY, allianceZ, allianceO, allianceAnimKitId FROM garrison_plot_finalize_info");

            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 garrison follower class spec abilities. DB table `garrison_plot_finalize_info` is empty.");
                return;
            }

            uint msTime = Time.GetMSTime();
            do
            {
                uint garrPlotInstanceId = result.Read<uint>(0);
                uint hordeGameObjectId = result.Read<uint>(1);
                uint allianceGameObjectId = result.Read<uint>(7);
                ushort hordeAnimKitId = result.Read<ushort>(6);
                ushort allianceAnimKitId = result.Read<ushort>(12);

                if (!CliDB.GarrPlotInstanceStorage.ContainsKey(garrPlotInstanceId))
                {
                    Log.outError(LogFilter.Sql, "Non-existing GarrPlotInstance.db2 entry {0} was referenced in `garrison_plot_finalize_info`.", garrPlotInstanceId);
                    continue;
                }

                GameObjectTemplate goTemplate = Global.ObjectMgr.GetGameObjectTemplate(hordeGameObjectId);
                if (goTemplate == null)
                {
                    Log.outError(LogFilter.Sql, "Non-existing gameobject_template entry {0} was referenced in `garrison_plot_finalize_info`.`hordeGameObjectId` for garrPlotInstanceId {1}.",
                        hordeGameObjectId, garrPlotInstanceId);
                    continue;
                }

                if (goTemplate.type != GameObjectTypes.Goober)
                {
                    Log.outError(LogFilter.Sql, "Invalid gameobject type {0} (entry {1}) was referenced in `garrison_plot_finalize_info`.`hordeGameObjectId` for garrPlotInstanceId {2}.",
                        goTemplate.type, hordeGameObjectId, garrPlotInstanceId);
                    continue;
                }

                goTemplate = Global.ObjectMgr.GetGameObjectTemplate(allianceGameObjectId);
                if (goTemplate == null)
                {
                    Log.outError(LogFilter.Sql, "Non-existing gameobject_template entry {0} was referenced in `garrison_plot_finalize_info`.`allianceGameObjectId` for garrPlotInstanceId {1}.",
                        allianceGameObjectId, garrPlotInstanceId);
                    continue;
                }

                if (goTemplate.type != GameObjectTypes.Goober)
                {
                    Log.outError(LogFilter.Sql, "Invalid gameobject type {0} (entry {1}) was referenced in `garrison_plot_finalize_info`.`allianceGameObjectId` for garrPlotInstanceId {2}.",
                        goTemplate.type, allianceGameObjectId, garrPlotInstanceId);
                    continue;
                }

                if (hordeAnimKitId != 0 && !CliDB.AnimKitStorage.ContainsKey(hordeAnimKitId))
                {
                    Log.outError(LogFilter.Sql, "Non-existing AnimKit.dbc entry {0} was referenced in `garrison_plot_finalize_info`.`hordeAnimKitId` for garrPlotInstanceId {1}.",
                        hordeAnimKitId, garrPlotInstanceId);
                    continue;
                }

                if (allianceAnimKitId != 0 && !CliDB.AnimKitStorage.ContainsKey(allianceAnimKitId))
                {
                    Log.outError(LogFilter.Sql, "Non-existing AnimKit.dbc entry {0} was referenced in `garrison_plot_finalize_info`.`allianceAnimKitId` for garrPlotInstanceId {1}.",
                        allianceAnimKitId, garrPlotInstanceId);
                    continue;
                }

                FinalizeGarrisonPlotGOInfo info = new FinalizeGarrisonPlotGOInfo();
                info.factionInfo[GarrisonFactionIndex.Horde].GameObjectId = hordeGameObjectId;
                info.factionInfo[GarrisonFactionIndex.Horde].Pos.Relocate(result.Read<float>(2), result.Read<float>(3), result.Read<float>(4), result.Read<float>(5));
                info.factionInfo[GarrisonFactionIndex.Horde].AnimKitId = hordeAnimKitId;

                info.factionInfo[GarrisonFactionIndex.Alliance].GameObjectId = allianceGameObjectId;
                info.factionInfo[GarrisonFactionIndex.Alliance].Pos.Relocate(result.Read<float>(8), result.Read<float>(9), result.Read<float>(10), result.Read<float>(11));
                info.factionInfo[GarrisonFactionIndex.Alliance].AnimKitId = allianceAnimKitId;

                _finalizePlotGOInfo[garrPlotInstanceId] = info;

            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} garrison plot finalize entries in {1}.", _finalizePlotGOInfo.Count, Time.GetMSTimeDiffToNow(msTime));
        }

        void LoadFollowerClassSpecAbilities()
        {
            SQLResult result = DB.World.Query("SELECT classSpecId, abilityId FROM garrison_follower_class_spec_abilities");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 garrison follower class spec abilities. DB table `garrison_follower_class_spec_abilities` is empty.");
                return;
            }

            uint msTime = Time.GetMSTime();
            uint count = 0;
            do
            {
                uint classSpecId = result.Read<uint>(0);
                uint abilityId = result.Read<uint>(1);

                if (!CliDB.GarrClassSpecStorage.ContainsKey(classSpecId))
                {
                    Log.outError(LogFilter.Sql, "Non-existing GarrClassSpec.db2 entry {0} was referenced in `garrison_follower_class_spec_abilities` by row ({1}, {2}).", classSpecId, classSpecId, abilityId);
                    continue;
                }

                GarrAbilityRecord ability = CliDB.GarrAbilityStorage.LookupByKey(abilityId);
                if (ability == null)
                {
                    Log.outError(LogFilter.Sql, "Non-existing GarrAbility.db2 entry {0} was referenced in `garrison_follower_class_spec_abilities` by row ({1}, {2}).", abilityId, classSpecId, abilityId);
                    continue;
                }

                _garrisonFollowerClassSpecAbilities.Add(classSpecId, ability);
                ++count;

            } while (result.NextRow());

            //foreach (var key in _garrisonFollowerClassSpecAbilities.Keys)
            //_garrisonFollowerClassSpecAbilities[key].Sort();

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} garrison follower class spec abilities in {1}.", count, Time.GetMSTimeDiffToNow(msTime));
        }

        MultiMap<uint, GarrSiteLevelPlotInstRecord> _garrisonPlotInstBySiteLevel = new MultiMap<uint, GarrSiteLevelPlotInstRecord>();
        Dictionary<uint, Dictionary<uint, GameObjectsRecord>> _garrisonPlots = new Dictionary<uint, Dictionary<uint, GameObjectsRecord>>();
        MultiMap<uint, uint> _garrisonBuildingsByPlot = new MultiMap<uint, uint>();
        Dictionary<ulong, uint> _garrisonBuildingPlotInstances = new Dictionary<ulong, uint>();
        MultiMap<uint, uint> _garrisonBuildingsByType = new MultiMap<uint, uint>();
        Dictionary<uint, FinalizeGarrisonPlotGOInfo> _finalizePlotGOInfo = new Dictionary<uint, FinalizeGarrisonPlotGOInfo>();
        Dictionary<uint, GarrAbilities>[] _garrisonFollowerAbilities = new Dictionary<uint, GarrAbilities>[2];
        MultiMap<uint, GarrAbilityRecord> _garrisonFollowerClassSpecAbilities = new MultiMap<uint, GarrAbilityRecord>();
        List<GarrAbilityRecord> _garrisonFollowerRandomTraits = new List<GarrAbilityRecord>();

        ulong _followerDbIdGenerator = 1;
    }

    class GarrAbilities
    {
        public List<GarrAbilityRecord> Counters = new List<GarrAbilityRecord>();
        public List<GarrAbilityRecord> Traits = new List<GarrAbilityRecord>();
    }

    public class FinalizeGarrisonPlotGOInfo
    {
        public FactionInfo[] factionInfo = new FactionInfo[2];

        public struct FactionInfo
        {
            public uint GameObjectId;
            public Position Pos;
            public ushort AnimKitId;
        }
    }
}
