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
using Game.DataStorage;
using Game.Entities;
using System;
using System.Collections.Generic;

namespace Game.Network.Packets
{
    class GarrisonCreateResult : ServerPacket
    {
        public GarrisonCreateResult() : base(ServerOpcodes.GarrisonCreateResult, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Result);
            _worldPacket.WriteUInt32(GarrSiteLevelID);
        }

        public uint GarrSiteLevelID;
        public uint Result;
    }

    class GarrisonDeleteResult : ServerPacket
    {
        public GarrisonDeleteResult() : base(ServerOpcodes.GarrisonDeleteResult, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Result);
            _worldPacket.WriteUInt32(GarrSiteID);
        }

        public GarrisonError Result;
        public  uint GarrSiteID;
    }

    class GetGarrisonInfo : ClientPacket
    {
        public GetGarrisonInfo(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class GetGarrisonInfoResult : ServerPacket
    {
        public GetGarrisonInfoResult() : base(ServerOpcodes.GetGarrisonInfoResult, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(FactionIndex);
            _worldPacket.WriteUInt32(Garrisons.Count);
            _worldPacket.WriteUInt32(FollowerSoftCaps.Count);

            foreach (FollowerSoftCapInfo followerSoftCapInfo in FollowerSoftCaps)
                followerSoftCapInfo.Write(_worldPacket);

            foreach (GarrisonInfo garrison in Garrisons)
                garrison.Write(_worldPacket);
        }

        public uint FactionIndex;
        public List<GarrisonInfo> Garrisons = new List<GarrisonInfo>();
        public List<FollowerSoftCapInfo> FollowerSoftCaps = new List<FollowerSoftCapInfo>();
    }

    class GarrisonRemoteInfo : ServerPacket
    {
        public GarrisonRemoteInfo() : base(ServerOpcodes.GarrisonRemoteInfo, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Sites.Count);
            foreach (GarrisonRemoteSiteInfo site in Sites)
                site.Write(_worldPacket);
        }

        public List<GarrisonRemoteSiteInfo> Sites = new List<GarrisonRemoteSiteInfo>();
    }

    class GarrisonPurchaseBuilding : ClientPacket
    {
        public GarrisonPurchaseBuilding(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            NpcGUID = _worldPacket.ReadPackedGuid();
            PlotInstanceID = _worldPacket.ReadUInt32();
            BuildingID = _worldPacket.ReadUInt32();
        }

        public ObjectGuid NpcGUID;
        public uint BuildingID;
        public uint PlotInstanceID;
    }

    class GarrisonPlaceBuildingResult : ServerPacket
    {
        public GarrisonPlaceBuildingResult() : base(ServerOpcodes.GarrisonPlaceBuildingResult, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(GarrTypeID);
            _worldPacket.WriteUInt32(Result);
            BuildingInfo.Write(_worldPacket);
            _worldPacket.WriteBit(PlayActivationCinematic);
            _worldPacket.FlushBits();
        }

        public GarrisonType GarrTypeID;
        public GarrisonError Result;
        public GarrisonBuildingInfo BuildingInfo = new GarrisonBuildingInfo();
        public bool PlayActivationCinematic;
    }

    class GarrisonCancelConstruction : ClientPacket
    {
        public GarrisonCancelConstruction(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            NpcGUID = _worldPacket.ReadPackedGuid();
            PlotInstanceID = _worldPacket.ReadUInt32();
        }

        public ObjectGuid NpcGUID;
        public uint PlotInstanceID;
    }

    class GarrisonBuildingRemoved : ServerPacket
    {
        public GarrisonBuildingRemoved() : base(ServerOpcodes.GarrisonBuildingRemoved, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(GarrTypeID);
            _worldPacket.WriteUInt32(Result);
            _worldPacket.WriteUInt32(GarrPlotInstanceID);
            _worldPacket.WriteUInt32(GarrBuildingID);
        }

        public GarrisonType GarrTypeID;
        public GarrisonError Result;
        public uint GarrPlotInstanceID;
        public uint GarrBuildingID;
    }

    class GarrisonLearnBlueprintResult : ServerPacket
    {
        public GarrisonLearnBlueprintResult() : base(ServerOpcodes.GarrisonLearnBlueprintResult, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(GarrTypeID);
            _worldPacket.WriteUInt32(Result);
            _worldPacket.WriteUInt32(BuildingID);
        }

        public GarrisonType GarrTypeID;
        public uint BuildingID;
        public GarrisonError Result;
    }

    class GarrisonUnlearnBlueprintResult : ServerPacket
    {
        public GarrisonUnlearnBlueprintResult() : base(ServerOpcodes.GarrisonUnlearnBlueprintResult, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(GarrTypeID);
            _worldPacket.WriteUInt32(Result);
            _worldPacket.WriteUInt32(BuildingID);
        }

        public GarrisonType GarrTypeID;
        public uint BuildingID;
        public GarrisonError Result;
    }

    class GarrisonRequestBlueprintAndSpecializationData : ClientPacket
    {
        public GarrisonRequestBlueprintAndSpecializationData(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class GarrisonRequestBlueprintAndSpecializationDataResult : ServerPacket
    {
        public GarrisonRequestBlueprintAndSpecializationDataResult() : base(ServerOpcodes.GarrisonRequestBlueprintAndSpecializationDataResult, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(GarrTypeID);
            _worldPacket.WriteUInt32(BlueprintsKnown != null ? BlueprintsKnown.Count : 0);
            _worldPacket.WriteUInt32(SpecializationsKnown != null ? SpecializationsKnown.Count : 0);
            if (BlueprintsKnown != null)
                foreach (uint blueprint in BlueprintsKnown)
                    _worldPacket.WriteUInt32(blueprint);

            if (SpecializationsKnown != null)
                foreach (uint specialization in SpecializationsKnown)
                    _worldPacket.WriteUInt32(specialization);
        }

        public GarrisonType GarrTypeID;
        public List<uint> SpecializationsKnown = null;
        public List<uint> BlueprintsKnown = null;
    }

    class GarrisonGetBuildingLandmarks : ClientPacket
    {
        public GarrisonGetBuildingLandmarks(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class GarrisonBuildingLandmarks : ServerPacket
    {
        public GarrisonBuildingLandmarks() : base(ServerOpcodes.GarrisonBuildingLandmarks, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Landmarks.Count);
            foreach (GarrisonBuildingLandmark landmark in Landmarks)
                landmark.Write(_worldPacket);
        }

        public List<GarrisonBuildingLandmark> Landmarks = new List<GarrisonBuildingLandmark>();
    }

    class GarrisonPlotPlaced : ServerPacket
    {
        public GarrisonPlotPlaced() : base(ServerOpcodes.GarrisonPlotPlaced, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(GarrTypeID);
            PlotInfo.Write(_worldPacket);
        }

        public GarrisonType GarrTypeID;
        public GarrisonPlotInfo PlotInfo;
    }

    class GarrisonPlotRemoved : ServerPacket
    {
        public GarrisonPlotRemoved() : base(ServerOpcodes.GarrisonPlotRemoved, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(GarrPlotInstanceID);
        }

        public uint GarrPlotInstanceID;
    }

    class GarrisonAddFollowerResult : ServerPacket
    {
        public GarrisonAddFollowerResult() : base(ServerOpcodes.GarrisonAddFollowerResult, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(GarrTypeID);
            _worldPacket .WriteUInt32(Result);
            Follower.Write(_worldPacket);
        }

        public GarrisonType GarrTypeID;
        public GarrisonFollower Follower;
        public GarrisonError Result;
    }

    class GarrisonRemoveFollowerResult : ServerPacket
    {
        public GarrisonRemoveFollowerResult() : base(ServerOpcodes.GarrisonRemoveFollowerResult) { }

        public override void Write()
        {
            _worldPacket.WriteUInt64(FollowerDBID);
            _worldPacket.WriteInt32(GarrTypeID);
            _worldPacket.WriteUInt32(Result);
            _worldPacket.WriteUInt32(Destroyed);
        }

        public ulong FollowerDBID;
        public int GarrTypeID;
        public uint Result;
        public uint Destroyed;
    }

    class GarrisonBuildingActivated : ServerPacket
    {
        public GarrisonBuildingActivated() : base(ServerOpcodes.GarrisonBuildingActivated, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(GarrPlotInstanceID);
        }

        public uint GarrPlotInstanceID;
    }

    //Structs
    public struct GarrisonPlotInfo
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(GarrPlotInstanceID);
            data.WriteXYZO(PlotPos);
            data.WriteUInt32(PlotType);
        }

        public uint GarrPlotInstanceID;
        public Position PlotPos;
        public uint PlotType;
    }

    public class GarrisonBuildingInfo
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(GarrPlotInstanceID);
            data.WriteUInt32(GarrBuildingID);
            data.WriteUInt32(TimeBuilt);
            data.WriteUInt32(CurrentGarSpecID);
            data.WriteUInt32(TimeSpecCooldown);
            data.WriteBit(Active);
            data.FlushBits();
        }

        public uint GarrPlotInstanceID;
        public uint GarrBuildingID;
        public long TimeBuilt;
        public uint CurrentGarSpecID;
        public long TimeSpecCooldown = 2288912640;   // 06/07/1906 18:35:44 - another in the series of magic blizz dates
        public bool Active;
    }

    public class GarrisonFollower
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt64(DbID);
            data.WriteUInt32(GarrFollowerID);
            data.WriteUInt32(Quality);
            data.WriteUInt32(FollowerLevel);
            data.WriteUInt32(ItemLevelWeapon);
            data.WriteUInt32(ItemLevelArmor);
            data.WriteUInt32(Xp);
            data.WriteUInt32(Durability);
            data.WriteUInt32(CurrentBuildingID);
            data.WriteUInt32(CurrentMissionID);
            data.WriteUInt32(AbilityID.Count);
            data.WriteUInt32(ZoneSupportSpellID);
            data.WriteUInt32(FollowerStatus);

            AbilityID.ForEach(ability => data.WriteUInt32(ability.Id));

            data.WriteBits(CustomName.GetByteCount(), 7);
            data.FlushBits();
            data.WriteString(CustomName);
        }

        public ulong DbID;
        public uint GarrFollowerID;
        public uint Quality;
        public uint FollowerLevel;
        public uint ItemLevelWeapon;
        public uint ItemLevelArmor;
        public uint Xp;
        public uint Durability;
        public uint CurrentBuildingID;
        public uint CurrentMissionID;
        public List<GarrAbilityRecord> AbilityID = new List<GarrAbilityRecord>();
        public uint ZoneSupportSpellID;
        public uint FollowerStatus;
        public string CustomName = "";
    }

    class GarrisonMission
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt64(DbID);
            data.WriteUInt32(MissionRecID);
            data.WriteUInt32(OfferTime);
            data.WriteUInt32(OfferDuration);
            data.WriteUInt32(StartTime);
            data.WriteUInt32(TravelDuration);
            data.WriteUInt32(MissionDuration);
            data.WriteUInt32(MissionState);
            data.WriteUInt32(Unknown1);
            data.WriteUInt32(Unknown2);
        }

        public ulong DbID;
        public uint MissionRecID;
        public long OfferTime;
        public uint OfferDuration;
        public long StartTime = 2288912640;
        public uint TravelDuration;
        public uint MissionDuration;
        public uint MissionState;
        public uint Unknown1 = 0;
        public uint Unknown2 = 0;
    }

    struct GarrisonMissionReward
    {
        public void Write(WorldPacket data)
        {
            data.WriteInt32(ItemID);
            data.WriteUInt32(Quantity);
            data.WriteInt32(CurrencyID);
            data.WriteUInt32(CurrencyQuantity);
            data.WriteUInt32(FollowerXP);
            data.WriteUInt32(BonusAbilityID);
            data.WriteInt32(Unknown);
        }

        public int ItemID;
        public uint Quantity;
        public int CurrencyID;
        public uint CurrencyQuantity;
        public uint FollowerXP;
        public uint BonusAbilityID;
        public int Unknown;
    }

    struct GarrisonMissionAreaBonus
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(GarrMssnBonusAbilityID);
            data.WriteUInt32(StartTime);
        }

        public uint GarrMssnBonusAbilityID;
        public long StartTime;
    }

    struct GarrisonTalent
    {
        public void Write(WorldPacket data)
        {
            data.WriteInt32(GarrTalentID);
            data.WriteInt32(ResearchStartTime);
            data.WriteInt32(Flags);
        }

        public int GarrTalentID;
        public long ResearchStartTime;
        public int Flags;
    }

    class GarrisonInfo
    {
        public void Write(WorldPacket data)
        {
            data.WriteInt32(GarrTypeID);
            data.WriteInt32(GarrSiteID);
            data.WriteInt32(GarrSiteLevelID);
            data.WriteUInt32(Buildings.Count);
            data.WriteUInt32(Plots.Count);
            data.WriteUInt32(Followers.Count);
            data.WriteUInt32(Missions.Count);
            data.WriteUInt32(MissionRewards.Count);
            data.WriteUInt32(MissionOvermaxRewards.Count);
            data.WriteUInt32(MissionAreaBonuses.Count);
            data.WriteUInt32(Talents.Count);
            data.WriteUInt32(CanStartMission.Count);
            data.WriteUInt32(ArchivedMissions.Count);
            data.WriteInt32(NumFollowerActivationsRemaining);
            data.WriteUInt32(NumMissionsStartedToday);

            foreach (GarrisonPlotInfo plot in Plots)
                plot.Write(data);

            foreach (GarrisonMission mission in Missions)
                mission.Write(data);

            foreach (List<GarrisonMissionReward> missionReward in MissionRewards)
            {
                data.WriteUInt32(missionReward.Count);
                foreach (GarrisonMissionReward missionRewardItem in missionReward)
                    missionRewardItem.Write(data);
            }

            foreach (List<GarrisonMissionReward> missionReward in MissionOvermaxRewards)
            {
                data.WriteUInt32(missionReward.Count);
                foreach (GarrisonMissionReward missionRewardItem in missionReward)
                    missionRewardItem.Write(data);
            }

            foreach (GarrisonMissionAreaBonus areaBonus in MissionAreaBonuses)
                areaBonus.Write(data);

            foreach (GarrisonTalent talent in Talents)
                talent.Write(data);

            foreach(var id in ArchivedMissions)
                    data.WriteInt32(id);

            foreach (GarrisonBuildingInfo building in Buildings)
                building.Write(data);

            foreach (bool canStartMission in CanStartMission)
                data.WriteBit(canStartMission);

            data.FlushBits();

            foreach (GarrisonFollower follower in Followers)
                follower.Write(data);
        }

        public GarrisonType GarrTypeID;
        public uint GarrSiteID;
        public uint GarrSiteLevelID;
        public uint NumFollowerActivationsRemaining;
        public uint NumMissionsStartedToday;   // might mean something else, but sending 0 here enables follower abilities "Increase success chance of the first mission of the day by %."
        public List<GarrisonPlotInfo> Plots = new List<GarrisonPlotInfo>();
        public List<GarrisonBuildingInfo> Buildings = new List<GarrisonBuildingInfo>();
        public List<GarrisonFollower> Followers = new List<GarrisonFollower>();
        public List<GarrisonMission> Missions = new List<GarrisonMission>();
        public List<List<GarrisonMissionReward>> MissionRewards = new List<List<GarrisonMissionReward>>();
        public List<List<GarrisonMissionReward>> MissionOvermaxRewards = new List<List<GarrisonMissionReward>>();
        public List<GarrisonMissionAreaBonus> MissionAreaBonuses = new List<GarrisonMissionAreaBonus>();
        public List<GarrisonTalent> Talents = new List<GarrisonTalent>();
        public List<bool> CanStartMission = new List<bool>();
        public List<int> ArchivedMissions = new List<int>();
    }

    struct FollowerSoftCapInfo
    {
        public void Write(WorldPacket data)
        {
            data.WriteInt32(GarrFollowerTypeID);
            data.WriteUInt32(Count);
        }

        public int GarrFollowerTypeID;
        public uint Count;
    }

    struct GarrisonRemoteBuildingInfo
    {
        public GarrisonRemoteBuildingInfo(uint plotInstanceId, uint buildingId)
        {
            GarrPlotInstanceID = plotInstanceId;
            GarrBuildingID = buildingId;
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(GarrPlotInstanceID);
            data.WriteUInt32(GarrBuildingID);
        }

        public uint GarrPlotInstanceID;
        public uint GarrBuildingID;
    }

    class GarrisonRemoteSiteInfo
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(GarrSiteLevelID);
            data.WriteUInt32(Buildings.Count);
            foreach (GarrisonRemoteBuildingInfo building in Buildings)
                building.Write(data);
        }

        public uint GarrSiteLevelID;
        public List<GarrisonRemoteBuildingInfo> Buildings = new List<GarrisonRemoteBuildingInfo>();
    }

    struct GarrisonBuildingLandmark
    {
        public GarrisonBuildingLandmark(uint buildingPlotInstId, Position pos)
        {
            GarrBuildingPlotInstID = buildingPlotInstId;
            Pos = pos;
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(GarrBuildingPlotInstID);
            data.WriteXYZ(Pos);
        }

        public uint GarrBuildingPlotInstID;
        public Position Pos;
    }
}