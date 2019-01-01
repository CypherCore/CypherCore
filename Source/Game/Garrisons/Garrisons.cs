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
using Framework.Dynamic;
using Framework.GameMath;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Network.Packets;
using System;
using System.Collections.Generic;

namespace Game.Garrisons
{
    public class Garrison
    {
        public Garrison(Player owner)
        {
            _owner = owner;
            _followerActivationsRemainingToday = 1;
        }

        public bool LoadFromDB(SQLResult garrison, SQLResult blueprints, SQLResult buildings, SQLResult followers, SQLResult abilities)
        {
            if (garrison.IsEmpty())
                return false;

            _siteLevel = CliDB.GarrSiteLevelStorage.LookupByKey(garrison.Read<uint>(0));
            _followerActivationsRemainingToday = garrison.Read<uint>(1);
            if (_siteLevel == null)
                return false;

            InitializePlots();

            if (!blueprints.IsEmpty())
            {
                do
                {
                    GarrBuildingRecord building = CliDB.GarrBuildingStorage.LookupByKey(blueprints.Read<uint>(0));
                    if (building != null)
                        _knownBuildings.Add(building.Id);

                } while (blueprints.NextRow());
            }

            if (!buildings.IsEmpty())
            {
                do
                {
                    uint plotInstanceId = buildings.Read<uint>(0);
                    uint buildingId = buildings.Read<uint>(1);
                    ulong timeBuilt = buildings.Read<ulong>(2);
                    bool active = buildings.Read<bool>(3);


                    Plot plot = GetPlot(plotInstanceId);
                    if (plot == null)
                        continue;

                    if (!CliDB.GarrBuildingStorage.ContainsKey(buildingId))
                        continue;

                    plot.BuildingInfo.PacketInfo.HasValue = true;
                    plot.BuildingInfo.PacketInfo.Value.GarrPlotInstanceID = plotInstanceId;
                    plot.BuildingInfo.PacketInfo.Value.GarrBuildingID = buildingId;
                    plot.BuildingInfo.PacketInfo.Value.TimeBuilt = (long)timeBuilt;
                    plot.BuildingInfo.PacketInfo.Value.Active = active;

                } while (buildings.NextRow());
            }

            if (!followers.IsEmpty())
            {
                do
                {
                    ulong dbId = followers.Read<ulong>(0);
                    uint followerId = followers.Read<uint>(1);
                    if (!CliDB.GarrFollowerStorage.ContainsKey(followerId))
                        continue;

                    _followerIds.Add(followerId);

                    var follower = new Follower();
                    follower.PacketInfo.DbID = dbId;
                    follower.PacketInfo.GarrFollowerID = followerId;
                    follower.PacketInfo.Quality = followers.Read<uint>(2);
                    follower.PacketInfo.FollowerLevel = followers.Read<uint>(3);
                    follower.PacketInfo.ItemLevelWeapon = followers.Read<uint>(4);
                    follower.PacketInfo.ItemLevelArmor = followers.Read<uint>(5);
                    follower.PacketInfo.Xp = followers.Read<uint>(6);
                    follower.PacketInfo.CurrentBuildingID = followers.Read<uint>(7);
                    follower.PacketInfo.CurrentMissionID = followers.Read<uint>(8);
                    follower.PacketInfo.FollowerStatus = followers.Read<uint>(9);
                    if (!CliDB.GarrBuildingStorage.ContainsKey(follower.PacketInfo.CurrentBuildingID))
                        follower.PacketInfo.CurrentBuildingID = 0;

                    //if (!sGarrMissionStore.LookupEntry(follower.PacketInfo.CurrentMissionID))
                    //    follower.PacketInfo.CurrentMissionID = 0;
                    _followers[followerId] = follower;

                } while (followers.NextRow());

                if (!abilities.IsEmpty())
                {
                    do
                    {
                        ulong dbId = abilities.Read<ulong>(0);
                        GarrAbilityRecord ability = CliDB.GarrAbilityStorage.LookupByKey(abilities.Read<uint>(1));

                        if (ability == null)
                            continue;

                        var garrisonFollower = _followers.LookupByKey(dbId);
                        if (garrisonFollower == null)
                            continue;

                        garrisonFollower.PacketInfo.AbilityID.Add(ability);
                    } while (abilities.NextRow());
                }
            }

            return true;
        }

        public void SaveToDB(SQLTransaction trans)
        {
            DeleteFromDB(_owner.GetGUID().GetCounter(), trans);

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_GARRISON);
            stmt.AddValue(0, _owner.GetGUID().GetCounter());
            stmt.AddValue(1, _siteLevel.Id);
            stmt.AddValue(2, _followerActivationsRemainingToday);
            trans.Append(stmt);

            foreach (uint building in _knownBuildings)
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_GARRISON_BLUEPRINTS);
                stmt.AddValue(0, _owner.GetGUID().GetCounter());
                stmt.AddValue(1, building);
                trans.Append(stmt);
            }

            foreach (var plot in _plots.Values)
            {
                if (plot.BuildingInfo.PacketInfo.HasValue)
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_GARRISON_BUILDINGS);
                    stmt.AddValue(0, _owner.GetGUID().GetCounter());
                    stmt.AddValue(1, plot.BuildingInfo.PacketInfo.Value.GarrPlotInstanceID);
                    stmt.AddValue(2, plot.BuildingInfo.PacketInfo.Value.GarrBuildingID);
                    stmt.AddValue(3, plot.BuildingInfo.PacketInfo.Value.TimeBuilt);
                    stmt.AddValue(4, plot.BuildingInfo.PacketInfo.Value.Active);
                    trans.Append(stmt);
                }
            }

            foreach (var follower in _followers.Values)
            {
                byte index = 0;
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_GARRISON_FOLLOWERS);
                stmt.AddValue(index++, follower.PacketInfo.DbID);
                stmt.AddValue(index++, _owner.GetGUID().GetCounter());
                stmt.AddValue(index++, follower.PacketInfo.GarrFollowerID);
                stmt.AddValue(index++, follower.PacketInfo.Quality);
                stmt.AddValue(index++, follower.PacketInfo.FollowerLevel);
                stmt.AddValue(index++, follower.PacketInfo.ItemLevelWeapon);
                stmt.AddValue(index++, follower.PacketInfo.ItemLevelArmor);
                stmt.AddValue(index++, follower.PacketInfo.Xp);
                stmt.AddValue(index++, follower.PacketInfo.CurrentBuildingID);
                stmt.AddValue(index++, follower.PacketInfo.CurrentMissionID);
                stmt.AddValue(index++, follower.PacketInfo.FollowerStatus);
                trans.Append(stmt);

                byte slot = 0;
                foreach (GarrAbilityRecord ability in follower.PacketInfo.AbilityID)
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_GARRISON_FOLLOWER_ABILITIES);
                    stmt.AddValue(0, follower.PacketInfo.DbID);
                    stmt.AddValue(1, ability.Id);
                    stmt.AddValue(2, slot++);
                    trans.Append(stmt);
                }
            }
        }

        public static void DeleteFromDB(ulong ownerGuid, SQLTransaction trans)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_GARRISON);
            stmt.AddValue(0, ownerGuid);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_GARRISON_BLUEPRINTS);
            stmt.AddValue(0, ownerGuid);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_GARRISON_BUILDINGS);
            stmt.AddValue(0, ownerGuid);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_GARRISON_FOLLOWERS);
            stmt.AddValue(0, ownerGuid);
            trans.Append(stmt);
        }

        public bool Create(uint garrSiteId)
        {
            GarrSiteLevelRecord siteLevel = Global.GarrisonMgr.GetGarrSiteLevelEntry(garrSiteId, 1);
            if (siteLevel == null)
                return false;

            _siteLevel = siteLevel;

            InitializePlots();

            GarrisonCreateResult garrisonCreateResult = new GarrisonCreateResult();
            garrisonCreateResult.GarrSiteLevelID = _siteLevel.Id;
            _owner.SendPacket(garrisonCreateResult);
            PhasingHandler.OnConditionChange(_owner);
            SendRemoteInfo();
            return true;
        }

        public void Delete()
        {
            SQLTransaction trans = new SQLTransaction();
            DeleteFromDB(_owner.GetGUID().GetCounter(), trans);
            DB.Characters.CommitTransaction(trans);

            GarrisonDeleteResult garrisonDelete = new GarrisonDeleteResult();
            garrisonDelete.Result = GarrisonError.Success;
            garrisonDelete.GarrSiteID = _siteLevel.GarrSiteID;
            _owner.SendPacket(garrisonDelete);
        }

        void InitializePlots()
        {
            var plots = Global.GarrisonMgr.GetGarrPlotInstForSiteLevel(_siteLevel.Id);

            for (var i = 0; i < plots.Count; ++i)
            {
                uint garrPlotInstanceId = plots[i].GarrPlotInstanceID;
                GarrPlotInstanceRecord plotInstance = CliDB.GarrPlotInstanceStorage.LookupByKey(garrPlotInstanceId);
                GameObjectsRecord gameObject = Global.GarrisonMgr.GetPlotGameObject(_siteLevel.MapID, garrPlotInstanceId);
                if (plotInstance == null || gameObject == null)
                    continue;

                GarrPlotRecord plot = CliDB.GarrPlotStorage.LookupByKey(plotInstance.GarrPlotID);
                if (plot == null)
                    continue;

                Plot plotInfo = _plots[garrPlotInstanceId];
                plotInfo.PacketInfo.GarrPlotInstanceID = garrPlotInstanceId;
                plotInfo.PacketInfo.PlotPos.Relocate(gameObject.Pos.X, gameObject.Pos.Y, gameObject.Pos.Z, 2 * (float)Math.Acos(gameObject.Rot[3]));
                plotInfo.PacketInfo.PlotType = plot.PlotType;
                plotInfo.Rotation = new Quaternion(gameObject.Rot[0], gameObject.Rot[1], gameObject.Rot[2], gameObject.Rot[3]);
                plotInfo.EmptyGameObjectId = gameObject.Id;
                plotInfo.GarrSiteLevelPlotInstId = plots[i].Id;
            }
        }

        void Upgrade()
        {
        }

        void Enter()
        {
            WorldLocation loc = new WorldLocation(_siteLevel.MapID);
            loc.Relocate(_owner);
            _owner.TeleportTo(loc, TeleportToOptions.Seamless);
        }

        void Leave()
        {
            MapRecord map = CliDB.MapStorage.LookupByKey(_siteLevel.MapID);
            if (map != null)
            {
                WorldLocation loc = new WorldLocation((uint)map.ParentMapID);
                loc.Relocate(_owner);
                _owner.TeleportTo(loc, TeleportToOptions.Seamless);
            }
        }

        public uint GetFaction()
        {
            return _owner.GetTeam() == Team.Horde ? GarrisonFactionIndex.Horde : GarrisonFactionIndex.Alliance;
        }

        public ICollection<Plot> GetPlots()
        {
            return _plots.Values;
        }

        Plot GetPlot(uint garrPlotInstanceId)
        {
            return _plots.LookupByKey(garrPlotInstanceId);
        }

        public void LearnBlueprint(uint garrBuildingId)
        {
            GarrisonLearnBlueprintResult learnBlueprintResult = new GarrisonLearnBlueprintResult();
            learnBlueprintResult.GarrTypeID = GarrisonType.Garrison;
            learnBlueprintResult.BuildingID = garrBuildingId;
            learnBlueprintResult.Result = GarrisonError.Success;

            if (!CliDB.GarrBuildingStorage.ContainsKey(garrBuildingId))
                learnBlueprintResult.Result = GarrisonError.InvalidBuildingId;
            else if (_knownBuildings.Contains(garrBuildingId))
                learnBlueprintResult.Result = GarrisonError.BlueprintExists;
            else
                _knownBuildings.Add(garrBuildingId);

            _owner.SendPacket(learnBlueprintResult);
        }

        void UnlearnBlueprint(uint garrBuildingId)
        {
            GarrisonUnlearnBlueprintResult unlearnBlueprintResult = new GarrisonUnlearnBlueprintResult();
            unlearnBlueprintResult.GarrTypeID = GarrisonType.Garrison;
            unlearnBlueprintResult.BuildingID = garrBuildingId;
            unlearnBlueprintResult.Result = GarrisonError.Success;

            if (!CliDB.GarrBuildingStorage.ContainsKey(garrBuildingId))
                unlearnBlueprintResult.Result = GarrisonError.InvalidBuildingId;
            else if (!_knownBuildings.Contains(garrBuildingId))
                unlearnBlueprintResult.Result = GarrisonError.RequiresBlueprint;
            else
                _knownBuildings.Remove(garrBuildingId);

            _owner.SendPacket(unlearnBlueprintResult);
        }

        public void PlaceBuilding(uint garrPlotInstanceId, uint garrBuildingId)
        {
            GarrisonPlaceBuildingResult placeBuildingResult = new GarrisonPlaceBuildingResult();
            placeBuildingResult.GarrTypeID = GarrisonType.Garrison;
            placeBuildingResult.Result = CheckBuildingPlacement(garrPlotInstanceId, garrBuildingId);
            if (placeBuildingResult.Result == GarrisonError.Success)
            {
                placeBuildingResult.BuildingInfo.GarrPlotInstanceID = garrPlotInstanceId;
                placeBuildingResult.BuildingInfo.GarrBuildingID = garrBuildingId;
                placeBuildingResult.BuildingInfo.TimeBuilt = Time.UnixTime;

                Plot plot = GetPlot(garrPlotInstanceId);
                uint oldBuildingId = 0;
                Map map = FindMap();
                GarrBuildingRecord building = CliDB.GarrBuildingStorage.LookupByKey(garrBuildingId);
                if (map)
                    plot.DeleteGameObject(map);

                if (plot.BuildingInfo.PacketInfo.HasValue)
                {
                    oldBuildingId = plot.BuildingInfo.PacketInfo.Value.GarrBuildingID;
                    if (CliDB.GarrBuildingStorage.LookupByKey(oldBuildingId).BuildingType != building.BuildingType)
                        plot.ClearBuildingInfo(_owner);
                }

                plot.SetBuildingInfo(placeBuildingResult.BuildingInfo, _owner);
                if (map)
                {
                    GameObject go = plot.CreateGameObject(map, GetFaction());
                    if (go)
                        map.AddToMap(go);
                }

                _owner.ModifyCurrency((CurrencyTypes)building.CurrencyTypeID, -building.CurrencyQty, false, true);
                _owner.ModifyMoney(-building.GoldCost * MoneyConstants.Gold, false);

                if (oldBuildingId != 0)
                {
                    GarrisonBuildingRemoved buildingRemoved = new GarrisonBuildingRemoved();
                    buildingRemoved.GarrTypeID = GarrisonType.Garrison;
                    buildingRemoved.Result = GarrisonError.Success;
                    buildingRemoved.GarrPlotInstanceID = garrPlotInstanceId;
                    buildingRemoved.GarrBuildingID = oldBuildingId;
                    _owner.SendPacket(buildingRemoved);
                }

                _owner.UpdateCriteria(CriteriaTypes.PlaceGarrisonBuilding, garrBuildingId);
            }

            _owner.SendPacket(placeBuildingResult);
        }

        public void CancelBuildingConstruction(uint garrPlotInstanceId)
        {
            GarrisonBuildingRemoved buildingRemoved = new GarrisonBuildingRemoved();
            buildingRemoved.GarrTypeID = GarrisonType.Garrison;
            buildingRemoved.Result = CheckBuildingRemoval(garrPlotInstanceId);
            if (buildingRemoved.Result == GarrisonError.Success)
            {
                Plot plot = GetPlot(garrPlotInstanceId);

                buildingRemoved.GarrPlotInstanceID = garrPlotInstanceId;
                buildingRemoved.GarrBuildingID = plot.BuildingInfo.PacketInfo.Value.GarrBuildingID;

                Map map = FindMap();
                if (map)
                    plot.DeleteGameObject(map);

                plot.ClearBuildingInfo(_owner);
                _owner.SendPacket(buildingRemoved);

                GarrBuildingRecord constructing = CliDB.GarrBuildingStorage.LookupByKey(buildingRemoved.GarrBuildingID);
                // Refund construction/upgrade cost
                _owner.ModifyCurrency((CurrencyTypes)constructing.CurrencyTypeID, constructing.CurrencyQty, false, true);
                _owner.ModifyMoney(constructing.GoldCost * MoneyConstants.Gold, false);

                if (constructing.UpgradeLevel > 1)
                {
                    // Restore previous level building
                    uint restored = Global.GarrisonMgr.GetPreviousLevelBuilding(constructing.BuildingType, constructing.UpgradeLevel);
                    Cypher.Assert(restored != 0);

                    GarrisonPlaceBuildingResult placeBuildingResult = new GarrisonPlaceBuildingResult();
                    placeBuildingResult.GarrTypeID = GarrisonType.Garrison;
                    placeBuildingResult.Result = GarrisonError.Success;
                    placeBuildingResult.BuildingInfo.GarrPlotInstanceID = garrPlotInstanceId;
                    placeBuildingResult.BuildingInfo.GarrBuildingID = restored;
                    placeBuildingResult.BuildingInfo.TimeBuilt = Time.UnixTime;
                    placeBuildingResult.BuildingInfo.Active = true;

                    plot.SetBuildingInfo(placeBuildingResult.BuildingInfo, _owner);
                    _owner.SendPacket(placeBuildingResult);
                }

                if (map)
                {
                    GameObject go = plot.CreateGameObject(map, GetFaction());
                    if (go)
                        map.AddToMap(go);
                }
            }
            else
                _owner.SendPacket(buildingRemoved);
        }

        public void ActivateBuilding(uint garrPlotInstanceId)
        {
            Plot plot = GetPlot(garrPlotInstanceId);
            if (plot != null)
            {
                if (plot.BuildingInfo.CanActivate() && plot.BuildingInfo.PacketInfo.HasValue && !plot.BuildingInfo.PacketInfo.Value.Active)
                {
                    plot.BuildingInfo.PacketInfo.Value.Active = true;
                    Map map = FindMap();
                    if (map)
                    {
                        plot.DeleteGameObject(map);
                        GameObject go = plot.CreateGameObject(map, GetFaction());
                        if (go)
                            map.AddToMap(go);
                    }

                    GarrisonBuildingActivated buildingActivated = new GarrisonBuildingActivated();
                    buildingActivated.GarrPlotInstanceID = garrPlotInstanceId;
                    _owner.SendPacket(buildingActivated);
                }
            }
        }

        public void AddFollower(uint garrFollowerId)
        {
            GarrisonAddFollowerResult addFollowerResult = new GarrisonAddFollowerResult();
            addFollowerResult.GarrTypeID = GarrisonType.Garrison;
            GarrFollowerRecord followerEntry = CliDB.GarrFollowerStorage.LookupByKey(garrFollowerId);
            if (_followerIds.Contains(garrFollowerId) || followerEntry == null)
            {
                addFollowerResult.Result = GarrisonError.FollowerExists;
                _owner.SendPacket(addFollowerResult);
                return;
            }

            _followerIds.Add(garrFollowerId);
            ulong dbId = Global.GarrisonMgr.GenerateFollowerDbId();

            Follower follower = new Follower();
            follower.PacketInfo.DbID = dbId;
            follower.PacketInfo.GarrFollowerID = garrFollowerId;
            follower.PacketInfo.Quality = followerEntry.Quality;   // TODO: handle magic upgrades
            follower.PacketInfo.FollowerLevel = followerEntry.FollowerLevel;
            follower.PacketInfo.ItemLevelWeapon = followerEntry.ItemLevelWeapon;
            follower.PacketInfo.ItemLevelArmor = followerEntry.ItemLevelArmor;
            follower.PacketInfo.Xp = 0;
            follower.PacketInfo.CurrentBuildingID = 0;
            follower.PacketInfo.CurrentMissionID = 0;
            follower.PacketInfo.AbilityID = Global.GarrisonMgr.RollFollowerAbilities(garrFollowerId, followerEntry, follower.PacketInfo.Quality, GetFaction(), true);
            follower.PacketInfo.FollowerStatus = 0;

            _followers[dbId] = follower;
            addFollowerResult.Follower = follower.PacketInfo;
            _owner.SendPacket(addFollowerResult);

            _owner.UpdateCriteria(CriteriaTypes.RecruitGarrisonFollower, follower.PacketInfo.DbID);
        }

        public Follower GetFollower(ulong dbId)
        {
            return _followers.LookupByKey(dbId);
        }

        public void SendInfo()
        {
            GetGarrisonInfoResult garrisonInfo = new GetGarrisonInfoResult();
            garrisonInfo.FactionIndex = GetFaction();

            GarrisonInfo garrison = new GarrisonInfo();
            garrison.GarrTypeID = GarrisonType.Garrison;
            garrison.GarrSiteID = _siteLevel.GarrSiteID;
            garrison.GarrSiteLevelID = _siteLevel.Id;
            garrison.NumFollowerActivationsRemaining = _followerActivationsRemainingToday;
            foreach (var plot in _plots.Values)
            {
                garrison.Plots.Add(plot.PacketInfo);
                if (plot.BuildingInfo.PacketInfo.HasValue)
                    garrison.Buildings.Add(plot.BuildingInfo.PacketInfo.Value);
            }

            foreach (var follower in _followers.Values)
                garrison.Followers.Add(follower.PacketInfo);

            garrisonInfo.Garrisons.Add(garrison);

            _owner.SendPacket(garrisonInfo);
        }

        public void SendRemoteInfo()
        {
            MapRecord garrisonMap = CliDB.MapStorage.LookupByKey(_siteLevel.MapID);
            if (garrisonMap == null || _owner.GetMapId() != garrisonMap.ParentMapID)
                return;

            GarrisonRemoteInfo remoteInfo = new GarrisonRemoteInfo();

            GarrisonRemoteSiteInfo remoteSiteInfo = new GarrisonRemoteSiteInfo();
            remoteSiteInfo.GarrSiteLevelID = _siteLevel.Id;
            foreach (var p in _plots)
                if (p.Value.BuildingInfo.PacketInfo.HasValue)
                    remoteSiteInfo.Buildings.Add(new GarrisonRemoteBuildingInfo(p.Key, p.Value.BuildingInfo.PacketInfo.Value.GarrBuildingID));

            remoteInfo.Sites.Add(remoteSiteInfo);
            _owner.SendPacket(remoteInfo);
        }

        public void SendBlueprintAndSpecializationData()
        {
            GarrisonRequestBlueprintAndSpecializationDataResult data = new GarrisonRequestBlueprintAndSpecializationDataResult();
            data.GarrTypeID = GarrisonType.Garrison;
            data.BlueprintsKnown = _knownBuildings;
            _owner.SendPacket(data);
        }

        public void SendBuildingLandmarks(Player receiver)
        {
            GarrisonBuildingLandmarks buildingLandmarks = new GarrisonBuildingLandmarks();

            foreach (var plot in _plots.Values)
            {
                if (plot.BuildingInfo.PacketInfo.HasValue)
                {
                    uint garrBuildingPlotInstId = Global.GarrisonMgr.GetGarrBuildingPlotInst(plot.BuildingInfo.PacketInfo.Value.GarrBuildingID, plot.GarrSiteLevelPlotInstId);
                    if (garrBuildingPlotInstId != 0)
                        buildingLandmarks.Landmarks.Add(new GarrisonBuildingLandmark(garrBuildingPlotInstId, plot.PacketInfo.PlotPos));
                }
            }

            receiver.SendPacket(buildingLandmarks);
        }

        Map FindMap()
        {
            return Global.MapMgr.FindMap(_siteLevel.MapID, (uint)_owner.GetGUID().GetCounter());
        }

        GarrisonError CheckBuildingPlacement(uint garrPlotInstanceId, uint garrBuildingId)
        {
            GarrPlotInstanceRecord plotInstance = CliDB.GarrPlotInstanceStorage.LookupByKey(garrPlotInstanceId);
            Plot plot = GetPlot(garrPlotInstanceId);
            if (plotInstance == null || plot == null)
                return GarrisonError.InvalidPlotInstanceId;

            GarrBuildingRecord building = CliDB.GarrBuildingStorage.LookupByKey(garrBuildingId);
            if (building == null)
                return GarrisonError.InvalidBuildingId;

            if (!Global.GarrisonMgr.IsPlotMatchingBuilding(plotInstance.GarrPlotID, garrBuildingId))
                return GarrisonError.InvalidPlotBuilding;

            // Cannot place buldings of higher level than garrison level
            if (building.UpgradeLevel > _siteLevel.MaxBuildingLevel)
                return GarrisonError.InvalidBuildingId;

            if (building.Flags.HasAnyFlag(GarrisonBuildingFlags.NeedsPlan))
            {
                if (!_knownBuildings.Contains(garrBuildingId))
                    return GarrisonError.RequiresBlueprint;
            }
            else // Building is built as a quest reward
                return GarrisonError.InvalidBuildingId;

            // Check all plots to find if we already have this building
            GarrBuildingRecord existingBuilding;
            foreach (var p in _plots)
            {
                if (p.Value.BuildingInfo.PacketInfo.HasValue)
                {
                    existingBuilding = CliDB.GarrBuildingStorage.LookupByKey(p.Value.BuildingInfo.PacketInfo.Value.GarrBuildingID);
                    if (existingBuilding.BuildingType == building.BuildingType)
                        if (p.Key != garrPlotInstanceId || existingBuilding.UpgradeLevel + 1 != building.UpgradeLevel)    // check if its an upgrade in same plot
                            return GarrisonError.BuildingExists;
                }
            }

            if (!_owner.HasCurrency(building.CurrencyTypeID, (uint)building.CurrencyQty))
                return GarrisonError.NotEnoughCurrency;

            if (!_owner.HasEnoughMoney(building.GoldCost * MoneyConstants.Gold))
                return GarrisonError.NotEnoughGold;

            // New building cannot replace another building currently under construction
            if (plot.BuildingInfo.PacketInfo.HasValue)
                if (!plot.BuildingInfo.PacketInfo.Value.Active)
                    return GarrisonError.NoBuilding;

            return GarrisonError.Success;
        }

        GarrisonError CheckBuildingRemoval(uint garrPlotInstanceId)
        {
            Plot plot = GetPlot(garrPlotInstanceId);
            if (plot == null)
                return GarrisonError.InvalidPlotInstanceId;

            if (!plot.BuildingInfo.PacketInfo.HasValue)
                return GarrisonError.NoBuilding;

            if (plot.BuildingInfo.CanActivate())
                return GarrisonError.BuildingExists;

            return GarrisonError.Success;
        }

        public void ResetFollowerActivationLimit() { _followerActivationsRemainingToday = 1; }

        Player _owner;
        GarrSiteLevelRecord _siteLevel;
        uint _followerActivationsRemainingToday;

        Dictionary<uint, Plot> _plots = new Dictionary<uint, Plot>();
        List<uint> _knownBuildings = new List<uint>();
        Dictionary<ulong, Follower> _followers = new Dictionary<ulong, Follower>();
        List<uint> _followerIds = new List<uint>();

        public class Building
        {
            public bool CanActivate()
            {
                if (PacketInfo.HasValue)
                {
                    GarrBuildingRecord building = CliDB.GarrBuildingStorage.LookupByKey(PacketInfo.Value.GarrBuildingID);
                    if (PacketInfo.Value.TimeBuilt + building.BuildSeconds <= Time.UnixTime)
                        return true;
                }

                return false;
            }

            public ObjectGuid Guid;
            public List<ObjectGuid> Spawns = new List<ObjectGuid>();
            public Optional<GarrisonBuildingInfo> PacketInfo;
        }

        public class Plot
        {
            public GameObject CreateGameObject(Map map, uint faction)
            {
                uint entry = EmptyGameObjectId;
                if (BuildingInfo.PacketInfo.HasValue)
                {
                    GarrPlotInstanceRecord plotInstance = CliDB.GarrPlotInstanceStorage.LookupByKey(PacketInfo.GarrPlotInstanceID);
                    GarrPlotRecord plot = CliDB.GarrPlotStorage.LookupByKey(plotInstance.GarrPlotID);
                    GarrBuildingRecord building = CliDB.GarrBuildingStorage.LookupByKey(BuildingInfo.PacketInfo.Value.GarrBuildingID);

                    entry = faction == GarrisonFactionIndex.Horde ? plot.HordeConstructObjID : plot.AllianceConstructObjID;
                    if (BuildingInfo.PacketInfo.Value.Active || entry == 0)
                        entry = faction == GarrisonFactionIndex.Horde ? building.HordeGameObjectID : building.AllianceGameObjectID;
                }

                if (Global.ObjectMgr.GetGameObjectTemplate(entry) == null)
                {
                    Log.outError(LogFilter.Garrison, "Garrison attempted to spawn gameobject whose template doesn't exist ({0})", entry);
                    return null;
                }

                GameObject go = GameObject.CreateGameObject(entry, map, PacketInfo.PlotPos, Rotation, 255, GameObjectState.Ready);
                if (!go)
                    return null;

                if (BuildingInfo.CanActivate() && BuildingInfo.PacketInfo.HasValue && !BuildingInfo.PacketInfo.Value.Active)
                {
                    FinalizeGarrisonPlotGOInfo finalizeInfo = Global.GarrisonMgr.GetPlotFinalizeGOInfo(PacketInfo.GarrPlotInstanceID);
                    if (finalizeInfo != null)
                    {
                        Position pos2 = finalizeInfo.factionInfo[faction].Pos;
                        GameObject finalizer = GameObject.CreateGameObject(finalizeInfo.factionInfo[faction].GameObjectId, map, pos2, Quaternion.fromEulerAnglesZYX(pos2.GetOrientation(), 0.0f, 0.0f), 255, GameObjectState.Ready);
                        if (finalizer)
                        {
                            // set some spell id to make the object delete itself after use
                            finalizer.SetSpellId(finalizer.GetGoInfo().Goober.spell);
                            finalizer.SetRespawnTime(0);

                            ushort animKit = finalizeInfo.factionInfo[faction].AnimKitId;
                            if (animKit != 0)
                                finalizer.SetAnimKitId(animKit, false);

                            map.AddToMap(finalizer);
                        }
                    }
                }

                if (go.GetGoType() == GameObjectTypes.GarrisonBuilding && go.GetGoInfo().garrisonBuilding.SpawnMap != 0)
                {
                    foreach (var cellGuids in Global.ObjectMgr.GetMapObjectGuids((uint)go.GetGoInfo().garrisonBuilding.SpawnMap, (byte)map.GetDifficultyID()))
                    {
                        foreach (var spawnId in cellGuids.Value.creatures)
                        {
                            Creature spawn = BuildingSpawnHelper<Creature>(go, spawnId, map);
                            if (spawn)
                                BuildingInfo.Spawns.Add(spawn.GetGUID());
                        }

                        foreach (var spawnId in cellGuids.Value.gameobjects)
                        {
                            GameObject spawn = BuildingSpawnHelper<GameObject>(go, spawnId, map);
                            if (spawn)
                                BuildingInfo.Spawns.Add(spawn.GetGUID());
                        }
                    }
                }

                BuildingInfo.Guid = go.GetGUID();
                return go;
            }

            public void DeleteGameObject(Map map)
            {
                if (BuildingInfo.Guid.IsEmpty())
                    return;

                foreach (var guid in BuildingInfo.Spawns)
                {
                    WorldObject obj = null;
                    switch (guid.GetHigh())
                    {
                        case HighGuid.Creature:
                            obj = map.GetCreature(guid);
                            break;
                        case HighGuid.GameObject:
                            obj = map.GetGameObject(guid);
                            break;
                        default:
                            continue;
                    }

                    if (obj)
                        obj.AddObjectToRemoveList();
                }

                BuildingInfo.Spawns.Clear();

                GameObject oldBuilding = map.GetGameObject(BuildingInfo.Guid);
                if (oldBuilding)
                    oldBuilding.Delete();

                BuildingInfo.Guid.Clear();
            }

            public void ClearBuildingInfo(Player owner)
            {
                GarrisonPlotPlaced plotPlaced = new GarrisonPlotPlaced();
                plotPlaced.GarrTypeID = GarrisonType.Garrison;
                plotPlaced.PlotInfo = PacketInfo;
                owner.SendPacket(plotPlaced);

                BuildingInfo.PacketInfo.Clear();
            }

            public void SetBuildingInfo(GarrisonBuildingInfo buildingInfo, Player owner)
            {
                if (!BuildingInfo.PacketInfo.HasValue)
                {
                    GarrisonPlotRemoved plotRemoved = new GarrisonPlotRemoved();
                    plotRemoved.GarrPlotInstanceID = PacketInfo.GarrPlotInstanceID;
                    owner.SendPacket(plotRemoved);
                }

                BuildingInfo.PacketInfo.Set(buildingInfo);
            }

            T BuildingSpawnHelper<T>(GameObject building, ulong spawnId, Map map) where T : WorldObject, new()
            {
                T spawn = new T();
                if (!spawn.LoadFromDB(spawnId, map))
                    return null;

                float x = spawn.GetPositionX();
                float y = spawn.GetPositionY();
                float z = spawn.GetPositionZ();
                float o = spawn.GetOrientation();
                TransportPosHelper.CalculatePassengerPosition(ref x, ref y, ref z, ref o, building.GetPositionX(), building.GetPositionY(), building.GetPositionZ(), building.GetOrientation());

                spawn.Relocate(x, y, z, o);
                switch (spawn.GetTypeId())
                {
                    case TypeId.Unit:
                        spawn.ToCreature().SetHomePosition(x, y, z, o);
                        break;
                    case TypeId.GameObject:
                        spawn.ToGameObject().RelocateStationaryPosition(x, y, z, o);
                        break;
                }

                if (!spawn.IsPositionValid())
                    return null;

                if (!map.AddToMap(spawn))
                    return null;

                return spawn;
            }

            public GarrisonPlotInfo PacketInfo;
            public Quaternion Rotation;
            public uint EmptyGameObjectId;
            public uint GarrSiteLevelPlotInstId;
            public Building BuildingInfo;
        }

        public class Follower
        {
            public uint GetItemLevel()
            {
                return (PacketInfo.ItemLevelWeapon + PacketInfo.ItemLevelArmor) / 2;
            }

            public GarrisonFollower PacketInfo = new GarrisonFollower();
        }
    }
}
