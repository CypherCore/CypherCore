// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Game.Networking.Packets
{
    public class BindPointUpdate : ServerPacket
    {
        public BindPointUpdate() : base(ServerOpcodes.BindPointUpdate, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteVector3(BindPosition);
            _worldPacket.WriteUInt32(BindMapID);
            _worldPacket.WriteUInt32(BindAreaID);
        }

        public uint BindMapID = 0xFFFFFFFF;
        public Vector3 BindPosition;
        public uint BindAreaID;
    }

    public class PlayerBound : ServerPacket
    {
        public PlayerBound(ObjectGuid binderId, uint areaId) : base(ServerOpcodes.PlayerBound)
        {
            BinderID = binderId;
            AreaID = areaId;
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(BinderID);
            _worldPacket.WriteUInt32(AreaID);
        }

        ObjectGuid BinderID;
        uint AreaID;
    }

    public class InvalidatePlayer : ServerPacket
    {
        public InvalidatePlayer() : base(ServerOpcodes.InvalidatePlayer) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
        }

        public ObjectGuid Guid;
    }

    public class LoginSetTimeSpeed : ServerPacket
    {
        public LoginSetTimeSpeed() : base(ServerOpcodes.LoginSetTimeSpeed, ConnectionType.Instance) { }

        public override void Write()
        {
            ServerTime.Write(_worldPacket);
            GameTime.Write(_worldPacket);
            _worldPacket.WriteFloat(NewSpeed);
            _worldPacket.WriteInt32(ServerTimeHolidayOffset);
            _worldPacket.WriteInt32(GameTimeHolidayOffset);
        }

        public float NewSpeed;
        public int ServerTimeHolidayOffset;
        public WowTime GameTime;
        public WowTime ServerTime;
        public int GameTimeHolidayOffset;
    }

    public class ResetWeeklyCurrency : ServerPacket
    {
        public ResetWeeklyCurrency() : base(ServerOpcodes.ResetWeeklyCurrency, ConnectionType.Instance) { }

        public override void Write() { }
    }

    public class SetCurrency : ServerPacket
    {
        public SetCurrency() : base(ServerOpcodes.SetCurrency, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Type);
            _worldPacket.WriteInt32(Quantity);
            _worldPacket.WriteUInt32((uint)Flags);
            _worldPacket.WriteInt32(Toasts.Count);

            foreach (var toast in Toasts)
                toast.Write(_worldPacket);

            _worldPacket.WriteBit(WeeklyQuantity.HasValue);
            _worldPacket.WriteBit(TrackedQuantity.HasValue);
            _worldPacket.WriteBit(MaxQuantity.HasValue);
            _worldPacket.WriteBit(TotalEarned.HasValue);
            _worldPacket.WriteBit(SuppressChatLog);
            _worldPacket.WriteBit(QuantityChange.HasValue);
            _worldPacket.WriteBit(QuantityGainSource.HasValue);
            _worldPacket.WriteBit(QuantityLostSource.HasValue);
            _worldPacket.WriteBit(FirstCraftOperationID.HasValue);
            _worldPacket.WriteBit(NextRechargeTime.HasValue);
            _worldPacket.WriteBit(RechargeCycleStartTime.HasValue);
            _worldPacket.WriteBit(OverflownCurrencyID.HasValue);
            _worldPacket.FlushBits();

            if (WeeklyQuantity.HasValue)
                _worldPacket.WriteInt32(WeeklyQuantity.Value);

            if (TrackedQuantity.HasValue)
                _worldPacket.WriteInt32(TrackedQuantity.Value);

            if (MaxQuantity.HasValue)
                _worldPacket.WriteInt32(MaxQuantity.Value);

            if (TotalEarned.HasValue)
                _worldPacket.WriteInt32(TotalEarned.Value);

            if (QuantityChange.HasValue)
                _worldPacket.WriteInt32(QuantityChange.Value);

            if (QuantityGainSource.HasValue)
                _worldPacket.WriteInt32((int)QuantityGainSource.Value);

            if (QuantityLostSource.HasValue)
                _worldPacket.WriteInt32((int)QuantityLostSource.Value);

            if (FirstCraftOperationID.HasValue)
                _worldPacket.WriteUInt32(FirstCraftOperationID.Value);

            if (NextRechargeTime.HasValue)
                _worldPacket.WriteInt64(NextRechargeTime.Value);

            if (RechargeCycleStartTime.HasValue)
                _worldPacket.WriteInt64(RechargeCycleStartTime.Value);

            if (OverflownCurrencyID.HasValue)
                _worldPacket.WriteInt32(OverflownCurrencyID.Value);
        }

        public uint Type;
        public int Quantity;
        public CurrencyGainFlags Flags;
        public List<UiEventToast> Toasts = new();
        public int? WeeklyQuantity;
        public int? TrackedQuantity;
        public int? MaxQuantity;
        public int? TotalEarned;
        public int? QuantityChange;
        public CurrencyGainSource? QuantityGainSource;
        public CurrencyDestroyReason? QuantityLostSource;
        public uint? FirstCraftOperationID;
        public long? NextRechargeTime;
        public long? RechargeCycleStartTime;
        public int? OverflownCurrencyID;    // what currency was originally changed but couldn't be incremented because of a cap
        public bool SuppressChatLog;
    }

    public class SetMaxWeeklyQuantity : ServerPacket
    {
        public SetMaxWeeklyQuantity() : base(ServerOpcodes.SetMaxWeeklyQuantity, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Type);
            _worldPacket.WriteUInt32(MaxWeeklyQuantity);
        }

        public uint MaxWeeklyQuantity;
        public uint Type;
    }

    public class SetSelection : ClientPacket
    {
        public SetSelection(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Selection = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Selection; // Target
    }

    public class SetupCurrency() : ServerPacket(ServerOpcodes.SetupCurrency, ConnectionType.Instance)
    {
        public override void Write()
        {
            _worldPacket.WriteInt32(Data.Count);

            foreach (Record data in Data)
            {
                _worldPacket.WriteUInt32(data.Type);
                _worldPacket.WriteUInt32(data.Quantity);
                _worldPacket.WriteUInt8(data.Flags);

                _worldPacket.WriteBit(data.WeeklyQuantity.HasValue);
                _worldPacket.WriteBit(data.MaxWeeklyQuantity.HasValue);
                _worldPacket.WriteBit(data.TrackedQuantity.HasValue);
                _worldPacket.WriteBit(data.MaxQuantity.HasValue);
                _worldPacket.WriteBit(data.TotalEarned.HasValue);
                _worldPacket.WriteBit(data.NextRechargeTime.HasValue);
                _worldPacket.WriteBit(data.RechargeCycleStartTime.HasValue);
                _worldPacket.FlushBits();

                if (data.WeeklyQuantity.HasValue)
                    _worldPacket.WriteUInt32(data.WeeklyQuantity.Value);
                if (data.MaxWeeklyQuantity.HasValue)
                    _worldPacket.WriteUInt32(data.MaxWeeklyQuantity.Value);
                if (data.TrackedQuantity.HasValue)
                    _worldPacket.WriteUInt32(data.TrackedQuantity.Value);
                if (data.MaxQuantity.HasValue)
                    _worldPacket.WriteInt32(data.MaxQuantity.Value);
                if (data.TotalEarned.HasValue)
                    _worldPacket.WriteInt32(data.TotalEarned.Value);
                if (data.NextRechargeTime.HasValue)
                    _worldPacket.WriteInt64(data.NextRechargeTime.Value);
                if (data.RechargeCycleStartTime.HasValue)
                    _worldPacket.WriteInt64(data.RechargeCycleStartTime.Value);
            }
        }

        public List<Record> Data = new();

        public struct Record
        {
            public uint Type;
            public uint Quantity;
            public uint? WeeklyQuantity;       // Currency count obtained this Week.  
            public uint? MaxWeeklyQuantity;    // Weekly Currency cap.
            public uint? TrackedQuantity;
            public int? MaxQuantity;
            public int? TotalEarned;
            public long? NextRechargeTime;
            public long? RechargeCycleStartTime;
            public byte Flags;
        }
    }

    public class ViolenceLevel : ClientPacket
    {
        public ViolenceLevel(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            violenceLevel = _worldPacket.ReadInt8();
        }

        public sbyte violenceLevel; // 0 - no combat effects, 1 - display some combat effects, 2 - blood, 3 - bloody, 4 - bloodier, 5 - bloodiest
    }

    public class TimeSyncRequest : ServerPacket
    {
        public TimeSyncRequest() : base(ServerOpcodes.TimeSyncRequest, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SequenceIndex);
        }

        public uint SequenceIndex;
    }

    public class TimeSyncResponse : ClientPacket
    {
        public TimeSyncResponse(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            SequenceIndex = _worldPacket.ReadUInt32();
            ClientTime = _worldPacket.ReadUInt32();
        }

        public DateTime GetReceivedTime() { return _worldPacket.GetReceivedTime(); }

        public uint ClientTime; // Client ticks in ms
        public uint SequenceIndex; // Same index as in request
    }

    public class TriggerCinematic : ServerPacket
    {
        public TriggerCinematic() : base(ServerOpcodes.TriggerCinematic) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(CinematicID);
            _worldPacket.WritePackedGuid(ConversationGuid);
        }

        public uint CinematicID;
        public ObjectGuid ConversationGuid;
    }

    public class TriggerMovie : ServerPacket
    {
        public TriggerMovie() : base(ServerOpcodes.TriggerMovie) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(MovieID);
        }

        public uint MovieID;
    }

    public class ServerTimeOffsetRequest : ClientPacket
    {
        public ServerTimeOffsetRequest(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class ServerTimeOffset : ServerPacket
    {
        public ServerTimeOffset() : base(ServerOpcodes.ServerTimeOffset) { }

        public override void Write()
        {
            _worldPacket.WriteInt64(Time);
        }

        public long Time;
    }

    public class TutorialFlags : ServerPacket
    {
        public TutorialFlags() : base(ServerOpcodes.TutorialFlags) { }

        public override void Write()
        {
            for (byte i = 0; i < (int)Tutorials.Max; ++i)
                _worldPacket.WriteUInt32(TutorialData[i]);
        }

        public uint[] TutorialData = new uint[SharedConst.MaxAccountTutorialValues];
    }

    public class TutorialSetFlag : ClientPacket
    {
        public TutorialSetFlag(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Action = (TutorialAction)_worldPacket.ReadBits<byte>(2);
            if (Action == TutorialAction.Update)
                TutorialBit = _worldPacket.ReadUInt32();
        }

        public TutorialAction Action;
        public uint TutorialBit;
    }

    public class WorldServerInfo : ServerPacket
    {
        public WorldServerInfo() : base(ServerOpcodes.WorldServerInfo, ConnectionType.Instance)
        {
            InstanceGroupSize = new uint?();

            RestrictedAccountMaxLevel = new uint?();
            RestrictedAccountMaxMoney = new ulong?();
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(DifficultyID);
            _worldPacket.WriteBit(IsTournamentRealm);
            _worldPacket.WriteBit(XRealmPvpAlert);
            _worldPacket.WriteBit(BlockExitingLoadingScreen);
            _worldPacket.WriteBit(RestrictedAccountMaxLevel.HasValue);
            _worldPacket.WriteBit(RestrictedAccountMaxMoney.HasValue);
            _worldPacket.WriteBit(InstanceGroupSize.HasValue);
            _worldPacket.FlushBits();

            if (RestrictedAccountMaxLevel.HasValue)
                _worldPacket.WriteUInt32(RestrictedAccountMaxLevel.Value);

            if (RestrictedAccountMaxMoney.HasValue)
                _worldPacket.WriteUInt64(RestrictedAccountMaxMoney.Value);

            if (InstanceGroupSize.HasValue)
                _worldPacket.WriteUInt32(InstanceGroupSize.Value);
        }

        public uint DifficultyID;
        public bool IsTournamentRealm;
        public bool XRealmPvpAlert;
        public bool BlockExitingLoadingScreen;     // when set to true, sending SMSG_UPDATE_OBJECT with CreateObject Self bit = true will not hide loading screen
                                                   // instead it will be done after this packet is sent again with false in this bit and SMSG_UPDATE_OBJECT Values for player
        public uint? RestrictedAccountMaxLevel;
        public ulong? RestrictedAccountMaxMoney;
        public uint? InstanceGroupSize;
    }

    public class SetDungeonDifficulty : ClientPacket
    {
        public SetDungeonDifficulty(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            DifficultyID = _worldPacket.ReadUInt32();
        }

        public uint DifficultyID;
    }

    public class SetRaidDifficulty : ClientPacket
    {
        public int DifficultyID;
        public int Legacy;

        public SetRaidDifficulty(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Legacy = _worldPacket.ReadInt32();
            DifficultyID = _worldPacket.ReadInt32();
        }
    }

    public class DungeonDifficultySet : ServerPacket
    {
        public DungeonDifficultySet() : base(ServerOpcodes.SetDungeonDifficulty) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(DifficultyID);
        }

        public int DifficultyID;
    }

    public class RaidDifficultySet : ServerPacket
    {
        public int DifficultyID;
        public int Legacy;

        public RaidDifficultySet() : base(ServerOpcodes.RaidDifficultySet) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Legacy);
            _worldPacket.WriteInt32(DifficultyID);
        }
    }

    public class CorpseReclaimDelay : ServerPacket
    {
        public CorpseReclaimDelay() : base(ServerOpcodes.CorpseReclaimDelay, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Remaining);
        }

        public uint Remaining;
    }

    public class DeathReleaseLoc : ServerPacket
    {
        public DeathReleaseLoc() : base(ServerOpcodes.DeathReleaseLoc) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(MapID);
            _worldPacket.WriteXYZ(Loc);
        }

        public int MapID;
        public WorldLocation Loc;
    }

    public class PortGraveyard : ClientPacket
    {
        public PortGraveyard(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class PreRessurect : ServerPacket
    {
        public PreRessurect() : base(ServerOpcodes.PreRessurect) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(PlayerGUID);
        }

        public ObjectGuid PlayerGUID;
    }

    public class ReclaimCorpse : ClientPacket
    {
        public ReclaimCorpse(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            CorpseGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid CorpseGUID;
    }

    public class RepopRequest : ClientPacket
    {
        public RepopRequest(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            CheckInstance = _worldPacket.HasBit();
        }

        public bool CheckInstance;
    }

    public class RequestCemeteryList : ClientPacket
    {
        public RequestCemeteryList(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class RequestCemeteryListResponse : ServerPacket
    {
        public RequestCemeteryListResponse() : base(ServerOpcodes.RequestCemeteryListResponse, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteBit(IsGossipTriggered);
            _worldPacket.FlushBits();

            _worldPacket.WriteInt32(CemeteryID.Count);
            foreach (uint cemetery in CemeteryID)
                _worldPacket.WriteUInt32(cemetery);
        }

        public bool IsGossipTriggered;
        public List<uint> CemeteryID = new();
    }

    public class ResurrectResponse : ClientPacket
    {
        public ResurrectResponse(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Resurrecter = _worldPacket.ReadPackedGuid();
            Response = _worldPacket.ReadUInt32();
        }

        public ObjectGuid Resurrecter;
        public uint Response;
    }

    public class WeatherPkt : ServerPacket
    {
        public WeatherPkt(WeatherState weatherID = 0, float intensity = 0.0f, bool abrupt = false) : base(ServerOpcodes.Weather, ConnectionType.Instance)
        {
            WeatherID = weatherID;
            Intensity = intensity;
            Abrupt = abrupt;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32((uint)WeatherID);
            _worldPacket.WriteFloat(Intensity);
            _worldPacket.WriteBit(Abrupt);

            _worldPacket.FlushBits();
        }

        bool Abrupt;
        float Intensity;
        WeatherState WeatherID;
    }

    public class StandStateChange : ClientPacket
    {
        public UnitStandStateType StandState;

        public StandStateChange(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            StandState = (UnitStandStateType)_worldPacket.ReadUInt8();
        }
    }

    public class StandStateUpdate : ServerPacket
    {
        uint AnimKitID;
        UnitStandStateType State;

        public StandStateUpdate(UnitStandStateType state, uint animKitId) : base(ServerOpcodes.StandStateUpdate)
        {
            State = state;
            AnimKitID = animKitId;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt8((byte)State);
            _worldPacket.WriteUInt32(AnimKitID);
        }
    }

    public class SetAnimTier : ServerPacket
    {
        public SetAnimTier() : base(ServerOpcodes.SetAnimTier, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Unit);
            _worldPacket.WriteUInt8(Tier);
        }

        public ObjectGuid Unit;
        public byte Tier;
    }

    public class StartMirrorTimer : ServerPacket
    {
        public MirrorTimerType Timer;
        public int Scale;
        public int MaxValue;
        public int SpellID;
        public int Value;
        public bool Paused;

        public StartMirrorTimer(MirrorTimerType timer, int value, int maxValue, int scale, int spellID, bool paused) : base(ServerOpcodes.StartMirrorTimer)
        {
            Timer = timer;
            Value = value;
            MaxValue = maxValue;
            Scale = scale;
            SpellID = spellID;
            Paused = paused;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt8((byte)Timer);
            _worldPacket.WriteInt32(Value);
            _worldPacket.WriteInt32(MaxValue);
            _worldPacket.WriteInt32(Scale);
            _worldPacket.WriteInt32(SpellID);
            _worldPacket.WriteBit(Paused);
            _worldPacket.FlushBits();
        }
    }

    public class PauseMirrorTimer : ServerPacket
    {
        public MirrorTimerType Timer;
        public bool Paused = true;

        public PauseMirrorTimer(MirrorTimerType timer, bool paused) : base(ServerOpcodes.PauseMirrorTimer)
        {
            Timer = timer;
            Paused = paused;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt8((byte)Timer);
            _worldPacket.WriteBit(Paused);
            _worldPacket.FlushBits();
        }
    }

    public class StopMirrorTimer : ServerPacket
    {
        public MirrorTimerType Timer;

        public StopMirrorTimer(MirrorTimerType timer) : base(ServerOpcodes.StopMirrorTimer)
        {
            Timer = timer;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt8((byte)Timer);
        }
    }

    public class ExplorationExperience : ServerPacket
    {
        public ExplorationExperience(uint experience, uint areaID) : base(ServerOpcodes.ExplorationExperience)
        {
            Experience = experience;
            AreaID = areaID;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(AreaID);
            _worldPacket.WriteUInt32(Experience);
        }

        public uint Experience;
        public uint AreaID;
    }

    public class LevelUpInfo : ServerPacket
    {
        public LevelUpInfo() : base(ServerOpcodes.LevelUpInfo) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Level);
            _worldPacket.WriteUInt32(HealthDelta);

            foreach (int power in PowerDelta)
                _worldPacket.WriteInt32(power);

            foreach (int stat in StatDelta)
                _worldPacket.WriteInt32(stat);

            _worldPacket.WriteInt32(NumNewTalents);
            _worldPacket.WriteInt32(NumNewPvpTalentSlots);
        }

        public uint Level = 0;
        public uint HealthDelta = 0;
        public int[] PowerDelta = new int[(int)PowerType.MaxPerClass];
        public int[] StatDelta = new int[(int)Stats.Max];
        public int NumNewTalents;
        public int NumNewPvpTalentSlots;
    }

    public class PlayMusic : ServerPacket
    {
        public PlayMusic(uint soundKitID) : base(ServerOpcodes.PlayMusic)
        {
            SoundKitID = soundKitID;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SoundKitID);
        }

        uint SoundKitID;
    }

    public class RandomRollClient : ClientPacket
    {
        public RandomRollClient(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            bool hasPartyIndex = _worldPacket.HasBit();
            Min = _worldPacket.ReadUInt32();
            Max = _worldPacket.ReadUInt32();
            if (hasPartyIndex)
                PartyIndex = _worldPacket.ReadUInt8();
        }

        public uint Min;
        public uint Max;
        public byte? PartyIndex;
    }

    public class RandomRoll : ServerPacket
    {
        public ObjectGuid Roller;
        public ObjectGuid RollerWowAccount;
        public int Min;
        public int Max;
        public int Result;

        public RandomRoll() : base(ServerOpcodes.RandomRoll) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Roller);
            _worldPacket.WritePackedGuid(RollerWowAccount);
            _worldPacket.WriteInt32(Min);
            _worldPacket.WriteInt32(Max);
            _worldPacket.WriteInt32(Result);
        }
    }

    public class EnableBarberShop : ServerPacket
    {
        public uint CustomizationFeatureMask;

        public EnableBarberShop() : base(ServerOpcodes.EnableBarberShop) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(CustomizationFeatureMask);
        }
    }

    class PhaseShiftChange : ServerPacket
    {
        public PhaseShiftChange() : base(ServerOpcodes.PhaseShiftChange) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Client);
            Phaseshift.Write(_worldPacket);
            _worldPacket.WriteInt32(VisibleMapIDs.Count * 2);           // size in bytes
            foreach (ushort visibleMapId in VisibleMapIDs)
                _worldPacket.WriteUInt16(visibleMapId);                   // Active terrain swap map id

            _worldPacket.WriteInt32(PreloadMapIDs.Count * 2);           // size in bytes
            foreach (ushort preloadMapId in PreloadMapIDs)
                _worldPacket.WriteUInt16(preloadMapId);                            // Inactive terrain swap map id

            _worldPacket.WriteInt32(UiMapPhaseIDs.Count * 2);   // size in bytes
            foreach (ushort uiMapPhaseId in UiMapPhaseIDs)
                _worldPacket.WriteUInt16(uiMapPhaseId);          // UI map id, WorldMapArea.db2, controls map display
        }

        public ObjectGuid Client;
        public PhaseShiftData Phaseshift = new();
        public List<ushort> PreloadMapIDs = new();
        public List<ushort> UiMapPhaseIDs = new();
        public List<ushort> VisibleMapIDs = new();
    }

    public class ZoneUnderAttack : ServerPacket
    {
        public ZoneUnderAttack() : base(ServerOpcodes.ZoneUnderAttack, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(AreaID);
        }

        public int AreaID;
    }

    class DurabilityDamageDeath : ServerPacket
    {
        public DurabilityDamageDeath() : base(ServerOpcodes.DurabilityDamageDeath) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Percent);
        }

        public uint Percent;
    }

    class ObjectUpdateFailed : ClientPacket
    {
        public ObjectUpdateFailed(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ObjectGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid ObjectGUID;
    }

    class ObjectUpdateRescued : ClientPacket
    {
        public ObjectUpdateRescued(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ObjectGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid ObjectGUID;
    }

    class PlayObjectSound : ServerPacket
    {
        public PlayObjectSound() : base(ServerOpcodes.PlayObjectSound) { }

        public PlayObjectSound(ObjectGuid targetObjectGUID, ObjectGuid sourceObjectGUID, uint soundKitID, Vector3 position, int broadcastTextID) : base(ServerOpcodes.PlayObjectSound)
        {
            TargetObjectGUID = targetObjectGUID;
            SourceObjectGUID = sourceObjectGUID;
            SoundKitID = soundKitID;
            Position = position;
            BroadcastTextID = broadcastTextID;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SoundKitID);
            _worldPacket.WritePackedGuid(SourceObjectGUID);
            _worldPacket.WritePackedGuid(TargetObjectGUID);
            _worldPacket.WriteVector3(Position);
            _worldPacket.WriteInt32(BroadcastTextID);
        }

        public ObjectGuid TargetObjectGUID;
        public ObjectGuid SourceObjectGUID;
        public uint SoundKitID;
        public Vector3 Position;
        public int BroadcastTextID;
    }

    class PlaySound : ServerPacket
    {
        public PlaySound(ObjectGuid sourceObjectGuid, uint soundKitID, uint broadcastTextId) : base(ServerOpcodes.PlaySound)
        {
            SourceObjectGuid = sourceObjectGuid;
            SoundKitID = soundKitID;
            BroadcastTextID = broadcastTextId;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SoundKitID);
            _worldPacket.WritePackedGuid(SourceObjectGuid);
            _worldPacket.WriteUInt32(BroadcastTextID);
        }

        public ObjectGuid SourceObjectGuid;
        public uint SoundKitID;
        public uint BroadcastTextID;
    }

    class PlaySpeakerBoxSound : ServerPacket
    {
        public PlaySpeakerBoxSound(ObjectGuid sourceObjectGuid, uint soundKitID) : base(ServerOpcodes.PlaySpeakerbotSound)
        {
            SourceObjectGUID = sourceObjectGuid;
            SoundKitID = soundKitID;
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(SourceObjectGUID);
            _worldPacket.WriteUInt32(SoundKitID);
        }

        public ObjectGuid SourceObjectGUID;
        public uint SoundKitID;
    }

    class StopSpeakerbotSound : ServerPacket
    {
        ObjectGuid SourceObjectGUID;

        public StopSpeakerbotSound(ObjectGuid sourceObjectGUID) : base(ServerOpcodes.StopSpeakerbotSound)
        {
            SourceObjectGUID = sourceObjectGUID;
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(SourceObjectGUID);
        }
    }

    class OpeningCinematic : ClientPacket
    {
        public OpeningCinematic(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class CompleteCinematic : ClientPacket
    {
        public CompleteCinematic(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class NextCinematicCamera : ClientPacket
    {
        public NextCinematicCamera(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class CompleteMovie : ClientPacket
    {
        public CompleteMovie(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class FarSight : ClientPacket
    {
        public FarSight(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Enable = _worldPacket.HasBit();
        }

        public bool Enable;
    }

    class SaveCUFProfiles : ClientPacket
    {
        public SaveCUFProfiles(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint count = _worldPacket.ReadUInt32();
            for (byte i = 0; i < count && i < PlayerConst.MaxCUFProfiles; i++)
            {
                CUFProfile cufProfile = new();

                byte strLen = _worldPacket.ReadBits<byte>(7);

                // Bool Options
                for (byte option = 0; option < (int)CUFBoolOptions.BoolOptionsCount; option++)
                    cufProfile.BoolOptions.Set(option, _worldPacket.HasBit());

                // Other Options
                cufProfile.FrameHeight = _worldPacket.ReadUInt16();
                cufProfile.FrameWidth = _worldPacket.ReadUInt16();

                cufProfile.SortBy = _worldPacket.ReadUInt8();
                cufProfile.HealthText = _worldPacket.ReadUInt8();

                cufProfile.TopPoint = _worldPacket.ReadUInt8();
                cufProfile.BottomPoint = _worldPacket.ReadUInt8();
                cufProfile.LeftPoint = _worldPacket.ReadUInt8();

                cufProfile.TopOffset = _worldPacket.ReadUInt16();
                cufProfile.BottomOffset = _worldPacket.ReadUInt16();
                cufProfile.LeftOffset = _worldPacket.ReadUInt16();

                cufProfile.ProfileName = _worldPacket.ReadString(strLen);

                CUFProfiles.Add(cufProfile);
            }
        }

        public List<CUFProfile> CUFProfiles = new();
    }

    class LoadCUFProfiles : ServerPacket
    {
        public LoadCUFProfiles() : base(ServerOpcodes.LoadCufProfiles, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(CUFProfiles.Count);

            foreach (CUFProfile cufProfile in CUFProfiles)
            {
                _worldPacket.WriteBits(cufProfile.ProfileName.GetByteCount(), 7);

                // Bool Options
                for (byte option = 0; option < (int)CUFBoolOptions.BoolOptionsCount; option++)
                    _worldPacket.WriteBit(cufProfile.BoolOptions[option]);

                // Other Options
                _worldPacket.WriteUInt16(cufProfile.FrameHeight);
                _worldPacket.WriteUInt16(cufProfile.FrameWidth);

                _worldPacket.WriteUInt8(cufProfile.SortBy);
                _worldPacket.WriteUInt8(cufProfile.HealthText);

                _worldPacket.WriteUInt8(cufProfile.TopPoint);
                _worldPacket.WriteUInt8(cufProfile.BottomPoint);
                _worldPacket.WriteUInt8(cufProfile.LeftPoint);

                _worldPacket.WriteUInt16(cufProfile.TopOffset);
                _worldPacket.WriteUInt16(cufProfile.BottomOffset);
                _worldPacket.WriteUInt16(cufProfile.LeftOffset);

                _worldPacket.WriteString(cufProfile.ProfileName);
            }
        }

        public List<CUFProfile> CUFProfiles = new();
    }

    class PlayOneShotAnimKit : ServerPacket
    {
        public PlayOneShotAnimKit() : base(ServerOpcodes.PlayOneShotAnimKit) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Unit);
            _worldPacket.WriteUInt16(AnimKitID);
        }

        public ObjectGuid Unit;
        public ushort AnimKitID;
    }

    class SetAIAnimKit : ServerPacket
    {
        public SetAIAnimKit() : base(ServerOpcodes.SetAiAnimKit, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Unit);
            _worldPacket.WriteUInt16(AnimKitID);
        }

        public ObjectGuid Unit;
        public ushort AnimKitID;
    }

    class SetMeleeAnimKit : ServerPacket
    {
        public SetMeleeAnimKit() : base(ServerOpcodes.SetMeleeAnimKit, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Unit);
            _worldPacket.WriteUInt16(AnimKitID);
        }

        public ObjectGuid Unit;
        public ushort AnimKitID;
    }

    class SetMovementAnimKit : ServerPacket
    {
        public SetMovementAnimKit() : base(ServerOpcodes.SetMovementAnimKit, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Unit);
            _worldPacket.WriteUInt16(AnimKitID);
        }

        public ObjectGuid Unit;
        public ushort AnimKitID;
    }

    class SetPlayHoverAnim : ServerPacket
    {
        public SetPlayHoverAnim() : base(ServerOpcodes.SetPlayHoverAnim, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(UnitGUID);
            _worldPacket.WriteBit(PlayHoverAnim);
            _worldPacket.FlushBits();
        }

        public ObjectGuid UnitGUID;
        public bool PlayHoverAnim;
    }

    class TogglePvP : ClientPacket
    {
        public TogglePvP(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class SetPvP : ClientPacket
    {
        public SetPvP(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            EnablePVP = _worldPacket.HasBit();
        }

        public bool EnablePVP;
    }

    class SetWarMode : ClientPacket
    {
        public SetWarMode(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Enable = _worldPacket.HasBit();
        }

        public bool Enable;
    }

    class AccountHeirloomUpdate : ServerPacket
    {
        public AccountHeirloomUpdate() : base(ServerOpcodes.AccountHeirloomUpdate, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteBit(IsFullUpdate);
            _worldPacket.FlushBits();

            _worldPacket.WriteInt32(ItemCollectionType);

            // both lists have to have the same size
            _worldPacket.WriteInt32(Heirlooms.Count);
            _worldPacket.WriteInt32(Heirlooms.Count);

            foreach (var item in Heirlooms)
                _worldPacket.WriteUInt32(item.Key);

            foreach (var flags in Heirlooms)
                _worldPacket.WriteUInt32((uint)flags.Value.flags);
        }

        public bool IsFullUpdate;
        public Dictionary<uint, HeirloomData> Heirlooms = new();
        public int ItemCollectionType;
    }

    class MountSpecial : ClientPacket
    {
        public MountSpecial(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            SpellVisualKitIDs = new int[_worldPacket.ReadUInt32()];
            SequenceVariation = _worldPacket.ReadInt32();
            for (var i = 0; i < SpellVisualKitIDs.Length; ++i)
                SpellVisualKitIDs[i] = _worldPacket.ReadInt32();
        }

        public int[] SpellVisualKitIDs;
        public int SequenceVariation;
    }

    class SpecialMountAnim : ServerPacket
    {
        public SpecialMountAnim() : base(ServerOpcodes.SpecialMountAnim, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(UnitGUID);
            _worldPacket.WriteInt32(SpellVisualKitIDs.Count);
            _worldPacket.WriteInt32(SequenceVariation);
            foreach (var id in SpellVisualKitIDs)
                _worldPacket.WriteInt32(id);
        }

        public ObjectGuid UnitGUID;
        public List<int> SpellVisualKitIDs = new();
        public int SequenceVariation;
    }

    class CrossedInebriationThreshold : ServerPacket
    {
        public CrossedInebriationThreshold() : base(ServerOpcodes.CrossedInebriationThreshold) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
            _worldPacket.WriteUInt32(Threshold);
            _worldPacket.WriteUInt32(ItemID);
        }

        public ObjectGuid Guid;
        public uint ItemID;
        public uint Threshold;
    }

    class SetTaxiBenchmarkMode : ClientPacket
    {
        public SetTaxiBenchmarkMode(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Enable = _worldPacket.HasBit();
        }

        public bool Enable;
    }

    class OverrideLight : ServerPacket
    {
        public OverrideLight() : base(ServerOpcodes.OverrideLight) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(AreaLightID);
            _worldPacket.WriteUInt32(OverrideLightID);
            _worldPacket.WriteUInt32(TransitionMilliseconds);
        }

        public uint AreaLightID;
        public uint TransitionMilliseconds;
        public uint OverrideLightID;
    }

    public class StartTimer : ServerPacket
    {
        public StartTimer() : base(ServerOpcodes.StartTimer) { }

        public override void Write()
        {
            _worldPacket.WriteInt64(TotalTime);
            _worldPacket.WriteInt32((int)Type);
            _worldPacket.WriteInt64(TimeLeft);
            _worldPacket.WriteBit(PlayerGuid.HasValue);
            _worldPacket.FlushBits();

            if (PlayerGuid.HasValue)
                _worldPacket.WritePackedGuid(PlayerGuid.Value);
        }

        public long TotalTime;
        public long TimeLeft;
        public CountdownTimerType Type;
        public ObjectGuid? PlayerGuid;
    }

    class QueryCountdownTimer : ClientPacket
    {
        public CountdownTimerType TimerType;

        public QueryCountdownTimer(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            TimerType = (CountdownTimerType)_worldPacket.ReadInt32();
        }
    }

    class ConversationLineStarted : ClientPacket
    {
        public ObjectGuid ConversationGUID;
        public uint LineID;

        public ConversationLineStarted(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ConversationGUID = _worldPacket.ReadPackedGuid();
            LineID = _worldPacket.ReadUInt32();
        }
    }

    class RequestLatestSplashScreen : ClientPacket
    {
        public RequestLatestSplashScreen(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class SplashScreenShowLatest : ServerPacket
    {
        public SplashScreenShowLatest() : base(ServerOpcodes.SplashScreenShowLatest, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(UISplashScreenID);
        }

        public uint UISplashScreenID;
    }

    class DisplayToast : ServerPacket
    {
        public ulong Quantity;
        public uint QuestID;
        public DisplayToastMethod DisplayToastMethod;
        public bool Mailed;
        public DisplayToastType Type = DisplayToastType.Money;
        public bool IsSecondaryResult;
        public ItemInstance Item;
        public int LootSpec;
        public Gender Gender = Gender.None;
        public bool BonusRoll;
        public bool ForceToast;    // Ignores ITEM_FLAG3_DO_NOT_TOAST
        public uint CurrencyID;

        public DisplayToast() : base(ServerOpcodes.DisplayToast, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt64(Quantity);
            _worldPacket.WriteUInt8((byte)DisplayToastMethod);
            _worldPacket.WriteUInt32(QuestID);

            _worldPacket.WriteBit(Mailed);
            _worldPacket.WriteBits((byte)Type, 2);
            _worldPacket.WriteBit(IsSecondaryResult);

            switch (Type)
            {
                case DisplayToastType.NewItem:
                    _worldPacket.WriteBit(BonusRoll);
                    _worldPacket.WriteBit(ForceToast);
                    Item.Write(_worldPacket);
                    _worldPacket.WriteInt32(LootSpec);
                    _worldPacket.WriteInt8((sbyte)Gender);
                    break;
                case DisplayToastType.NewCurrency:
                    _worldPacket.WriteUInt32(CurrencyID);
                    break;
                default:
                    break;
            }

            _worldPacket.FlushBits();
        }
    }

    class DisplayGameError : ServerPacket
    {
        public DisplayGameError(GameError error) : base(ServerOpcodes.DisplayGameError)
        {
            Error = error;
        }

        public DisplayGameError(GameError error, int arg) : this(error)
        {
            Arg = arg;
        }
        public DisplayGameError(GameError error, int arg1, int arg2) : this(error)
        {
            Arg = arg1;
            Arg2 = arg2;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32((uint)Error);
            _worldPacket.WriteBit(Arg.HasValue);
            _worldPacket.WriteBit(Arg2.HasValue);
            _worldPacket.FlushBits();

            if (Arg.HasValue)
                _worldPacket.WriteInt32(Arg.Value);

            if (Arg2.HasValue)
                _worldPacket.WriteInt32(Arg2.Value);
        }

        GameError Error;
        int? Arg;
        int? Arg2;
    }

    class AccountMountUpdate : ServerPacket
    {
        public AccountMountUpdate() : base(ServerOpcodes.AccountMountUpdate, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteBit(IsFullUpdate);
            _worldPacket.WriteInt32(Mounts.Count);

            foreach (var (spellId, flags) in Mounts)
            {
                _worldPacket.WriteUInt32(spellId);
                _worldPacket.WriteBits(flags, 4);
            }

            _worldPacket.FlushBits();
        }

        public bool IsFullUpdate = false;
        public Dictionary<uint, MountStatusFlags> Mounts = new();
    }

    class MountSetFavorite : ClientPacket
    {
        public MountSetFavorite(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            MountSpellID = _worldPacket.ReadUInt32();
            IsFavorite = _worldPacket.HasBit();
        }

        public uint MountSpellID;
        public bool IsFavorite;
    }

    class CloseInteraction : ClientPacket
    {
        public CloseInteraction(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            SourceGuid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid SourceGuid;
    }

    class AccountWarbandSceneUpdate : ServerPacket
    {
        public bool IsFullUpdate;
        public Dictionary<uint, WarbandSceneCollectionItem> WarbandScenes;

        public AccountWarbandSceneUpdate() : base(ServerOpcodes.AccountWarbandSceneUpdate) { }

        public override void Write()
        {
            _worldPacket.WriteBit(IsFullUpdate);
            _worldPacket.WriteInt32(WarbandScenes.Count);
            _worldPacket.WriteInt32(WarbandScenes.Count);
            _worldPacket.WriteInt32(WarbandScenes.Count);

            foreach (var (warbandSceneId, _) in WarbandScenes)
                _worldPacket.WriteUInt32(warbandSceneId);

            foreach (var (_, data) in WarbandScenes)
                _worldPacket.WriteBit(data.Flags.HasFlag(WarbandSceneCollectionFlags.Favorite));

            foreach (var (_, data) in WarbandScenes)
                _worldPacket.WriteBit(data.Flags.HasFlag(WarbandSceneCollectionFlags.HasFanfare));

            _worldPacket.FlushBits();
        }
    }

    //Structs
    struct PhaseShiftDataPhase
    {
        public PhaseShiftDataPhase(uint phaseFlags, uint id)
        {
            PhaseFlags = phaseFlags;
            Id = (ushort)id;
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(PhaseFlags);
            data.WriteUInt16(Id);
        }

        public uint PhaseFlags;
        public ushort Id;
    }

    class PhaseShiftData
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(PhaseShiftFlags);
            data.WriteInt32(Phases.Count);
            data.WritePackedGuid(PersonalGUID);
            foreach (PhaseShiftDataPhase phaseShiftDataPhase in Phases)
                phaseShiftDataPhase.Write(data);
        }

        public uint PhaseShiftFlags;
        public List<PhaseShiftDataPhase> Phases = new();
        public ObjectGuid PersonalGUID;
    }
}
