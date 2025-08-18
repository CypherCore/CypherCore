// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using System;
using System.Collections.Generic;
using Framework.Dynamic;

namespace Game.Networking.Packets
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
            _worldPacket.WriteUInt32((uint)Result);
            _worldPacket.WriteUInt32(GarrSiteID);
        }

        public GarrisonError Result;
        public uint GarrSiteID;
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
            _worldPacket.WriteInt8(FactionIndex);
            _worldPacket.WriteInt32(Garrisons.Count);
            _worldPacket.WriteInt32(FollowerSoftCaps.Count);

            foreach (FollowerSoftCapInfo followerSoftCapInfo in FollowerSoftCaps)
                followerSoftCapInfo.Write(_worldPacket);

            foreach (GarrisonInfo garrison in Garrisons)
                garrison.Write(_worldPacket);
        }

        public sbyte FactionIndex;
        public List<GarrisonInfo> Garrisons = new();
        public List<FollowerSoftCapInfo> FollowerSoftCaps = new();
    }

    class GarrisonRemoteInfo : ServerPacket
    {
        public GarrisonRemoteInfo() : base(ServerOpcodes.GarrisonRemoteInfo, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Sites.Count);
            foreach (GarrisonRemoteSiteInfo site in Sites)
                site.Write(_worldPacket);
        }

        public List<GarrisonRemoteSiteInfo> Sites = new();
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
            _worldPacket.WriteUInt8((byte)GarrTypeID);
            _worldPacket.WriteUInt32((uint)Result);
            BuildingInfo.Write(_worldPacket);
            _worldPacket.WriteBit(PlayActivationCinematic);
            _worldPacket.FlushBits();
        }

        public GarrisonType GarrTypeID;
        public GarrisonError Result;
        public GarrisonBuildingInfo BuildingInfo = new();
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
            _worldPacket.WriteUInt8((byte)GarrTypeID);
            _worldPacket.WriteUInt32((uint)Result);
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
            _worldPacket.WriteUInt8((byte)GarrTypeID);
            _worldPacket.WriteUInt32((uint)Result);
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
            _worldPacket.WriteUInt8((byte)GarrTypeID);
            _worldPacket.WriteUInt32((uint)Result);
            _worldPacket.WriteUInt32(BuildingID);
        }

        public GarrisonType GarrTypeID;
        public GarrisonError Result;
        public uint BuildingID;
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
            _worldPacket.WriteUInt8((byte)GarrTypeID);
            _worldPacket.WriteInt32(BlueprintsKnown != null ? BlueprintsKnown.Count : 0);
            _worldPacket.WriteInt32(SpecializationsKnown != null ? SpecializationsKnown.Count : 0);
            if (BlueprintsKnown != null)
                foreach (uint blueprint in BlueprintsKnown)
                    _worldPacket.WriteUInt32(blueprint);

            if (SpecializationsKnown != null)
                foreach (uint specialization in SpecializationsKnown)
                    _worldPacket.WriteUInt32(specialization);
        }

        public GarrisonType GarrTypeID;
        public List<uint> BlueprintsKnown = null;
        public List<uint> SpecializationsKnown = null;
    }

    class GarrisonGetMapData : ClientPacket
    {
        public GarrisonGetMapData(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class GarrisonMapDataResponse : ServerPacket
    {
        public GarrisonMapDataResponse() : base(ServerOpcodes.GarrisonMapDataResponse, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Buildings.Count);
            foreach (GarrisonBuildingMapData landmark in Buildings)
                landmark.Write(_worldPacket);
        }

        public List<GarrisonBuildingMapData> Buildings = new();
    }

    class GarrisonPlotPlaced : ServerPacket
    {
        public GarrisonPlotPlaced() : base(ServerOpcodes.GarrisonPlotPlaced, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt8((byte)GarrTypeID);
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
            _worldPacket.WriteUInt8((byte)GarrTypeID);
            _worldPacket.WriteUInt32((uint)Result);
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
            _worldPacket.WriteUInt8(GarrTypeID);
            _worldPacket.WriteUInt32(Result);
            _worldPacket.WriteUInt64(FollowerDBID);
            _worldPacket.WriteUInt32(Destroyed);
        }

        public byte GarrTypeID;
        public uint Result;
        public ulong FollowerDBID;
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
            data.WriteUInt8(PlotType);
        }

        public uint GarrPlotInstanceID;
        public Position PlotPos;
        public byte PlotType;
    }

    public class GarrisonBuildingInfo
    {
        public long TimeBuilt;
        public uint GarrPlotInstanceID;
        public uint GarrBuildingID;
        public uint CurrentGarSpecID;
        public long TimeSpecCooldown = 2288912640;   // 06/07/1906 18:35:44 - another in the series of magic blizz dates
        public bool Active;

        public void Write(WorldPacket data)
        {
            data.WriteInt64(TimeBuilt);
            data.WriteUInt32(GarrPlotInstanceID);
            data.WriteUInt32(GarrBuildingID);
            data.WriteUInt32(CurrentGarSpecID);
            data.WriteInt64(TimeSpecCooldown);
            data.WriteBit(Active);
            data.FlushBits();
        }
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
            data.WriteInt32(AbilityID.Count);
            data.WriteUInt32(ZoneSupportSpellID);
            data.WriteUInt32(FollowerStatus);
            data.WriteInt32(Health);
            data.WriteInt8(BoardIndex);
            data.WriteInt64(HealingTimestamp);

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
        public List<GarrAbilityRecord> AbilityID = new();
        public uint ZoneSupportSpellID;
        public uint FollowerStatus;
        public int Health;
        public long HealingTimestamp;
        public sbyte BoardIndex;
        public string CustomName = "";
    }

    class GarrisonEncounter
    {
        public int GarrEncounterID;
        public List<int> Mechanics = new();
        public int GarrAutoCombatantID;
        public int Health;
        public int MaxHealth;
        public int Attack;
        public sbyte BoardIndex;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(GarrEncounterID);
            data.WriteInt32(Mechanics.Count);
            data.WriteInt32(GarrAutoCombatantID);
            data.WriteInt32(Health);
            data.WriteInt32(MaxHealth);
            data.WriteInt32(Attack);
            data.WriteInt8(BoardIndex);

            if (!Mechanics.Empty())
                Mechanics.ForEach(id => data.WriteInt32(id));
        }
    }

    struct GarrisonMissionReward
    {
        public void Write(WorldPacket data)
        {
            data.WriteInt32(ItemID);
            data.WriteUInt32(ItemQuantity);
            data.WriteInt32(CurrencyID);
            data.WriteUInt32(CurrencyQuantity);
            data.WriteUInt32(FollowerXP);
            data.WriteUInt32(GarrMssnBonusAbilityID);
            data.WriteInt32(ItemFileDataID);
            data.WriteBit(ItemInstance != null);
            data.FlushBits();

            if (ItemInstance != null)
                ItemInstance.Write(data);
        }

        public int ItemID;
        public uint ItemQuantity;
        public int CurrencyID;
        public uint CurrencyQuantity;
        public uint FollowerXP;
        public uint GarrMssnBonusAbilityID;
        public int ItemFileDataID;
        public ItemInstance ItemInstance;
    }

    class GarrisonMission
    {
        public ulong DbID;
        public int MissionRecID;
        public long OfferTime;
        public long OfferDuration;
        public long StartTime = 2288912640;
        public long TravelDuration;
        public long MissionDuration;
        public int MissionState = 0;
        public int SuccessChance = 0;
        public uint Flags = 0;
        public float MissionScalar = 1.0f;
        public int ContentTuningID = 0;
        public List<GarrisonEncounter> Encounters = new();
        public List<GarrisonMissionReward> Rewards = new();
        public List<GarrisonMissionReward> OvermaxRewards = new();

        public void Write(WorldPacket data)
        {
            data.WriteUInt64(DbID);
            data.WriteInt32(MissionRecID);
            data.WriteInt64(OfferTime);
            data.WriteInt64(OfferDuration);
            data.WriteInt64(StartTime);
            data.WriteInt64(TravelDuration);
            data.WriteInt64(MissionDuration);
            data.WriteInt32(MissionState);
            data.WriteInt32(SuccessChance);
            data.WriteUInt32(Flags);
            data.WriteFloat(MissionScalar);
            data.WriteInt32(ContentTuningID);
            data.WriteInt32(Encounters.Count);
            data.WriteInt32(Rewards.Count);
            data.WriteInt32(OvermaxRewards.Count);

            foreach (GarrisonEncounter encounter in Encounters)
                encounter.Write(data);

            foreach (GarrisonMissionReward missionRewardItem in Rewards)
                missionRewardItem.Write(data);

            foreach (GarrisonMissionReward missionRewardItem in OvermaxRewards)
                missionRewardItem.Write(data);
        }
    }

    struct GarrisonMissionBonusAbility
    {
        public void Write(WorldPacket data)
        {
            data.WriteInt64(StartTime);
            data.WriteUInt32(GarrMssnBonusAbilityID);
        }

        public uint GarrMssnBonusAbilityID;
        public long StartTime;
    }

    struct GarrisonTalentSocketData
    {
        public int SoulbindConduitID;
        public int SoulbindConduitRank;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(SoulbindConduitID);
            data.WriteInt32(SoulbindConduitRank);
        }
    }

    struct GarrisonTalent
    {
        public void Write(WorldPacket data)
        {
            data.WriteInt32(GarrTalentID);
            data.WriteInt32(Rank);
            data.WriteInt64(ResearchStartTime);
            data.WriteInt32(Flags);
            data.WriteBit(Socket.HasValue);
            data.FlushBits();

            if (Socket.HasValue)
                Socket.Value.Write(data);
        }

        public int GarrTalentID;
        public int Rank;
        public long ResearchStartTime;
        public int Flags;
        public GarrisonTalentSocketData? Socket;
    }

    struct GarrisonCollectionEntry
    {
        public int EntryID;
        public int Rank;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(EntryID);
            data.WriteInt32(Rank);
        }
    }

    class GarrisonCollection
    {
        public int Type;
        public List<GarrisonCollectionEntry> Entries = new();

        public void Write(WorldPacket data)
        {
            data.WriteInt32(Type);
            data.WriteInt32(Entries.Count);
            foreach (GarrisonCollectionEntry collectionEntry in Entries)
                collectionEntry.Write(data);
        }
    }

    struct GarrisonEventEntry
    {
        public int EntryID;
        public long EventValue;

        public void Write(WorldPacket data)
        {
            data.WriteInt64(EventValue);
            data.WriteInt32(EntryID);
        }
    }

    class GarrisonEventList
    {
        public int Type;
        public List<GarrisonEventEntry> Events = new();

        public void Write(WorldPacket data)
        {
            data.WriteInt32(Type);
            data.WriteInt32(Events.Count);
            foreach (GarrisonEventEntry eventEntry in Events)
                eventEntry.Write(data);
        }
    }

    struct GarrisonSpecGroup
    {
        public int ChrSpecializationID;
        public int SoulbindID;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(ChrSpecializationID);
            data.WriteInt32(SoulbindID);
        }
    }

    class GarrisonInfo
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt8((byte)GarrTypeID);
            data.WriteUInt32(GarrSiteID);
            data.WriteUInt32(GarrSiteLevelID);
            data.WriteInt32(Buildings.Count);
            data.WriteInt32(Plots.Count);
            data.WriteInt32(Followers.Count);
            data.WriteInt32(AutoTroops.Count);
            data.WriteInt32(Missions.Count);
            data.WriteInt32(MissionRewards.Count);
            data.WriteInt32(MissionOvermaxRewards.Count);
            data.WriteInt32(MissionAreaBonuses.Count);
            data.WriteInt32(Talents.Count);
            data.WriteInt32(Collections.Count);
            data.WriteInt32(EventLists.Count);
            data.WriteInt32(SpecGroups.Count);
            data.WriteInt32(CanStartMission.Count);
            data.WriteInt32(ArchivedMissions.Count);
            data.WriteUInt32(NumFollowerActivationsRemaining);
            data.WriteUInt32(NumMissionsStartedToday);
            data.WriteInt32(MinAutoTroopLevel);

            foreach (GarrisonPlotInfo plot in Plots)
                plot.Write(data);

            foreach (GarrisonMission mission in Missions)
                mission.Write(data);

            foreach (List<GarrisonMissionReward> missionReward in MissionRewards)
                data.WriteInt32(missionReward.Count);

            foreach (List<GarrisonMissionReward> missionReward in MissionOvermaxRewards)
                data.WriteInt32(missionReward.Count);

            foreach (GarrisonMissionBonusAbility areaBonus in MissionAreaBonuses)
                areaBonus.Write(data);

            foreach (GarrisonCollection collection in Collections)
                collection.Write(data);

            foreach (GarrisonEventList eventList in EventLists)
                eventList.Write(data);

            foreach (var specGroup in SpecGroups)
                specGroup.Write(data);

            foreach (var id in ArchivedMissions)
                data.WriteInt32(id);

            foreach (GarrisonBuildingInfo building in Buildings)
                building.Write(data);

            foreach (bool canStartMission in CanStartMission)
                data.WriteBit(canStartMission);

            data.FlushBits();

            foreach (GarrisonFollower follower in Followers)
                follower.Write(data);

            foreach (GarrisonFollower follower in AutoTroops)
                follower.Write(data);

            foreach (GarrisonTalent talent in Talents)
                talent.Write(data);

            foreach (List<GarrisonMissionReward> missionReward in MissionRewards)
                foreach (GarrisonMissionReward missionRewardItem in missionReward)
                    missionRewardItem.Write(data);

            foreach (List<GarrisonMissionReward> missionReward in MissionOvermaxRewards)
                foreach (GarrisonMissionReward missionRewardItem in missionReward)
                    missionRewardItem.Write(data);
        }

        public GarrisonType GarrTypeID;
        public uint GarrSiteID;
        public uint GarrSiteLevelID;
        public uint NumFollowerActivationsRemaining;
        public uint NumMissionsStartedToday;   // might mean something else, but sending 0 here enables follower abilities "Increase success chance of the first mission of the day by %."
        public int MinAutoTroopLevel;
        public List<GarrisonPlotInfo> Plots = new();
        public List<GarrisonBuildingInfo> Buildings = new();
        public List<GarrisonFollower> Followers = new();
        public List<GarrisonFollower> AutoTroops = new();
        public List<GarrisonMission> Missions = new();
        public List<List<GarrisonMissionReward>> MissionRewards = new();
        public List<List<GarrisonMissionReward>> MissionOvermaxRewards = new();
        public List<GarrisonMissionBonusAbility> MissionAreaBonuses = new();
        public List<GarrisonTalent> Talents = new();
        public List<GarrisonCollection> Collections = new();
        public List<GarrisonEventList> EventLists = new();
        public List<GarrisonSpecGroup> SpecGroups = new();
        public List<bool> CanStartMission = new();
        public List<int> ArchivedMissions = new();
    }

    struct FollowerSoftCapInfo
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt8(GarrFollowerTypeID);
            data.WriteUInt32(Count);
        }

        public byte GarrFollowerTypeID;
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
            data.WriteInt32(Buildings.Count);
            foreach (GarrisonRemoteBuildingInfo building in Buildings)
                building.Write(data);
        }

        public uint GarrSiteLevelID;
        public List<GarrisonRemoteBuildingInfo> Buildings = new();
    }

    struct GarrisonBuildingMapData
    {
        public GarrisonBuildingMapData(uint buildingPlotInstId, Position pos)
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