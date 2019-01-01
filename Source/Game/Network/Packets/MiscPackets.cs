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
using Framework.Dynamic;
using Framework.GameMath;
using Game.Entities;
using System;
using System.Collections.Generic;

namespace Game.Network.Packets
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

    public class BinderConfirm : ServerPacket
    {
        public BinderConfirm(ObjectGuid unit) : base(ServerOpcodes.BinderConfirm)
        {
            Unit = unit;
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Unit);
        }

        ObjectGuid Unit;
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
            _worldPacket.WritePackedTime(ServerTime);
            _worldPacket.WritePackedTime(GameTime);
            _worldPacket.WriteFloat(NewSpeed);
            _worldPacket.WriteUInt32(ServerTimeHolidayOffset);
            _worldPacket.WriteUInt32(GameTimeHolidayOffset);
        }

        public float NewSpeed;
        public int ServerTimeHolidayOffset;
        public uint GameTime;
        public uint ServerTime;
        public int GameTimeHolidayOffset;
    }

    public class SetCurrency : ServerPacket
    {
        public SetCurrency() : base(ServerOpcodes.SetCurrency, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Type);
            _worldPacket.WriteInt32(Quantity);
            _worldPacket.WriteUInt32(Flags);
            _worldPacket.WriteBit(WeeklyQuantity.HasValue);
            _worldPacket.WriteBit(TrackedQuantity.HasValue);
            _worldPacket.WriteBit(MaxQuantity.HasValue);
            _worldPacket.WriteBit(SuppressChatLog);
            _worldPacket.FlushBits();

            if (WeeklyQuantity.HasValue)
                _worldPacket.WriteInt32(WeeklyQuantity.Value);

            if (TrackedQuantity.HasValue)
                _worldPacket.WriteInt32(TrackedQuantity.Value);

            if (MaxQuantity.HasValue)
                _worldPacket.WriteInt32(MaxQuantity.Value);
        }

        public uint Type;
        public int Quantity;
        public uint Flags;
        public Optional<int> WeeklyQuantity;
        public Optional<int> TrackedQuantity;
        public Optional<int> MaxQuantity;
        public bool SuppressChatLog;
    }

    public class SetMaxWeeklyQuantity : ServerPacket
    {
        public SetMaxWeeklyQuantity() : base(ServerOpcodes.SetMaxWeeklyQuantity, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32("Type");
            _worldPacket.WriteUInt32("MaxWeeklyQuantity");
        }

        public uint MaxWeeklyQuantity;
        public uint Type;
    }

    public class ResetWeeklyCurrency : ServerPacket
    {
        public ResetWeeklyCurrency() : base(ServerOpcodes.ResetWeeklyCurrency, ConnectionType.Instance) { }

        public override void Write() { }
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

    public class SetupCurrency : ServerPacket
    {
        public SetupCurrency() : base(ServerOpcodes.SetupCurrency, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Data.Count);

            foreach (Record data in Data)
            {
                _worldPacket.WriteUInt32(data.Type);
                _worldPacket.WriteUInt32(data.Quantity);

                _worldPacket.WriteBit(data.WeeklyQuantity.HasValue);
                _worldPacket.WriteBit(data.MaxWeeklyQuantity.HasValue);
                _worldPacket.WriteBit(data.TrackedQuantity.HasValue);
                _worldPacket.WriteBit(data.MaxQuantity.HasValue);
                _worldPacket.WriteBits(data.Flags, 5);
                _worldPacket.FlushBits();

                if (data.WeeklyQuantity.HasValue)
                    _worldPacket.WriteUInt32(data.WeeklyQuantity.Value);
                if (data.MaxWeeklyQuantity.HasValue)
                    _worldPacket.WriteUInt32(data.MaxWeeklyQuantity.Value);
                if (data.TrackedQuantity.HasValue)
                    _worldPacket.WriteUInt32(data.TrackedQuantity.Value);
                if (data.MaxQuantity.HasValue)
                    _worldPacket.WriteUInt32(data.MaxQuantity.Value);
            }
        }

        public List<Record> Data = new List<Record>();

        public struct Record
        {
            public uint Type;
            public uint Quantity;
            public Optional<uint> WeeklyQuantity;       // Currency count obtained this Week.  
            public Optional<uint> MaxWeeklyQuantity;    // Weekly Currency cap.
            public Optional<uint> TrackedQuantity;
            public Optional<int> MaxQuantity;
            public byte Flags;                      // 0 = none, 
        }
    }

    public class ViolenceLevel : ClientPacket
    {
        public ViolenceLevel(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            violenceLevel = _worldPacket.ReadInt8();
        }

        sbyte violenceLevel = -1; // 0 - no combat effects, 1 - display some combat effects, 2 - blood, 3 - bloody, 4 - bloodier, 5 - bloodiest
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

        public uint ClientTime; // Client ticks in ms
        public uint SequenceIndex; // Same index as in request
    }

    public class TriggerCinematic : ServerPacket
    {
        public TriggerCinematic() : base(ServerOpcodes.TriggerCinematic) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(CinematicID);
        }

        public uint CinematicID;
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

    public class UITimeRequest : ClientPacket
    {
        public UITimeRequest(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class UITime : ServerPacket
    {
        public UITime() : base(ServerOpcodes.UiTime) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Time);
        }

        public uint Time;
    }

    public class TutorialFlags : ServerPacket
    {
        public TutorialFlags() : base(ServerOpcodes.TutorialFlags) { }

        public override void Write()
        {
            for (byte i = 0; i < (int)Tutorials.Max; ++i)
                _worldPacket.WriteUInt32(TutorialData[i]);
        }

        public uint[] TutorialData = new uint[(int)Tutorials.Max];
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
            InstanceGroupSize = new Optional<uint>();

            RestrictedAccountMaxLevel = new Optional<uint>();
            RestrictedAccountMaxMoney = new Optional<uint>();
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(DifficultyID);
            _worldPacket.WriteUInt8(IsTournamentRealm);
            _worldPacket.WriteBit(XRealmPvpAlert);
            _worldPacket.WriteBit(RestrictedAccountMaxLevel.HasValue);
            _worldPacket.WriteBit(RestrictedAccountMaxMoney.HasValue);
            _worldPacket.WriteBit(InstanceGroupSize.HasValue);
            _worldPacket.FlushBits();

            if (RestrictedAccountMaxLevel.HasValue)
                _worldPacket.WriteUInt32(RestrictedAccountMaxLevel.Value);

            if (RestrictedAccountMaxMoney.HasValue)
                _worldPacket.WriteUInt32(RestrictedAccountMaxMoney.Value);

            if (InstanceGroupSize.HasValue)
                _worldPacket.WriteUInt32(InstanceGroupSize.Value);
        }

        public uint DifficultyID;
        public byte IsTournamentRealm;
        public bool XRealmPvpAlert;
        public Optional<uint> RestrictedAccountMaxLevel;
        public Optional<uint> RestrictedAccountMaxMoney;
        public Optional<uint> InstanceGroupSize;
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
        public SetRaidDifficulty(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            DifficultyID = _worldPacket.ReadInt32();
            Legacy = _worldPacket.ReadUInt8();
        }

        public int DifficultyID;
        public byte Legacy;
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
        public RaidDifficultySet() : base(ServerOpcodes.RaidDifficultySet) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(DifficultyID);
            _worldPacket.WriteUInt8(Legacy);
        }

        public int DifficultyID;
        public bool Legacy;
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
            _worldPacket.WriteVector3(Loc);
        }

        public int MapID;
        public Vector3 Loc;
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

            _worldPacket.WriteUInt32(CemeteryID.Count);
            foreach (uint cemetery in CemeteryID)
                _worldPacket.WriteUInt32(cemetery);
        }

        public bool IsGossipTriggered;
        public List<uint> CemeteryID = new List<uint>();
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
            _worldPacket.WriteUInt32(WeatherID);
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
        public StandStateChange(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            StandState = (UnitStandStateType)_worldPacket.ReadUInt32();
        }

        public UnitStandStateType StandState;
    }

    public class StandStateUpdate : ServerPacket
    {
        public StandStateUpdate(UnitStandStateType state, uint animKitId) : base(ServerOpcodes.StandStateUpdate)
        {
            State = state;
            AnimKitID = animKitId;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(AnimKitID);
            _worldPacket.WriteUInt8(State);
        }

        uint AnimKitID;
        UnitStandStateType State;
    }

    public class StartMirrorTimer : ServerPacket
    {
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
            _worldPacket.WriteInt32(Timer);
            _worldPacket.WriteInt32(Value);
            _worldPacket.WriteInt32(MaxValue);
            _worldPacket.WriteInt32(Scale);
            _worldPacket.WriteInt32(SpellID);
            _worldPacket.WriteBit(Paused);
            _worldPacket.FlushBits();
        }

        public int Scale;
        public int MaxValue;
        public MirrorTimerType Timer;
        public int SpellID;
        public int Value;
        public bool Paused;
    }

    public class PauseMirrorTimer : ServerPacket
    {
        public PauseMirrorTimer(MirrorTimerType timer, bool paused) : base(ServerOpcodes.PauseMirrorTimer)
        {
            Timer = timer;
            Paused = paused;
        }

        public override void Write()
        {
            _worldPacket.WriteInt32(Timer);
            _worldPacket.WriteBit(Paused);
            _worldPacket.FlushBits();
        }

        public bool Paused = true;
        public MirrorTimerType Timer;
    }

    public class StopMirrorTimer : ServerPacket
    {
        public StopMirrorTimer(MirrorTimerType timer) : base(ServerOpcodes.StopMirrorTimer)
        {
            Timer = timer;
        }

        public override void Write()
        {
            _worldPacket.WriteInt32(Timer);
        }

        public MirrorTimerType Timer;
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
        public int[] PowerDelta = new int[6];
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
            Min = _worldPacket.ReadUInt32();
            Max = _worldPacket.ReadUInt32();
            PartyIndex = _worldPacket.ReadUInt8();
        }

        public uint Min;
        public uint Max;
        public byte PartyIndex;
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
        public EnableBarberShop() : base(ServerOpcodes.EnableBarberShop) { }

        public override void Write() { }
    }


    class PhaseShiftChange : ServerPacket
    {
        public PhaseShiftChange() : base(ServerOpcodes.PhaseShiftChange) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Client);
            Phaseshift.Write(_worldPacket);
            _worldPacket.WriteUInt32(VisibleMapIDs.Count * 2);           // size in bytes
            foreach (ushort visibleMapId in VisibleMapIDs)
                _worldPacket.WriteUInt16(visibleMapId);                   // Active terrain swap map id

            _worldPacket.WriteUInt32(PreloadMapIDs.Count * 2);           // size in bytes
            foreach (ushort preloadMapId in PreloadMapIDs)
                _worldPacket.WriteUInt16(preloadMapId);                            // Inactive terrain swap map id

            _worldPacket.WriteUInt32(UiMapPhaseIDs.Count * 2);   // size in bytes
            foreach (ushort uiMapPhaseId in UiMapPhaseIDs)
                _worldPacket.WriteUInt16(uiMapPhaseId);          // UI map id, WorldMapArea.db2, controls map display
        }

        public ObjectGuid Client;
        public PhaseShiftData Phaseshift = new PhaseShiftData();
        public List<ushort> PreloadMapIDs = new List<ushort>();
        public List<ushort> UiMapPhaseIDs = new List<ushort>();
        public List<ushort> VisibleMapIDs = new List<ushort>();
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

        public override void Write()
        {
            _worldPacket.WriteUInt32(SoundKitID);
            _worldPacket.WritePackedGuid(SourceObjectGUID);
            _worldPacket.WritePackedGuid(TargetObjectGUID);
            _worldPacket.WriteVector3(Position);
        }

        ObjectGuid TargetObjectGUID;
        ObjectGuid SourceObjectGUID;
        uint SoundKitID;
        Vector3 Position;
    }

    class PlaySound : ServerPacket
    {
        public PlaySound(ObjectGuid sourceObjectGuid, uint soundKitID) : base(ServerOpcodes.PlaySound)
        {
            SourceObjectGuid = sourceObjectGuid;
            SoundKitID = soundKitID;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SoundKitID);
            _worldPacket.WritePackedGuid(SourceObjectGuid);
        }

        ObjectGuid SourceObjectGuid;
        uint SoundKitID;
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

    class Dismount : ServerPacket
    {
        public Dismount() : base(ServerOpcodes.Dismount) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
        }

        public ObjectGuid Guid;
    }

    class SaveCUFProfiles : ClientPacket
    {
        public SaveCUFProfiles(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint count = _worldPacket.ReadUInt32();
            for (byte i = 0; i < count && i < PlayerConst.MaxCUFProfiles; i++)
            {
                CUFProfile cufProfile = new CUFProfile();

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

        public List<CUFProfile> CUFProfiles = new List<CUFProfile>();
    }

    class LoadCUFProfiles : ServerPacket
    {
        public LoadCUFProfiles() : base(ServerOpcodes.LoadCufProfiles, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(CUFProfiles.Count);

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

        public List<CUFProfile> CUFProfiles = new List<CUFProfile>();
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

    class AccountHeirloomUpdate : ServerPacket
    {
        public AccountHeirloomUpdate() : base(ServerOpcodes.AccountHeirloomUpdate, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteBit(IsFullUpdate);
            _worldPacket.FlushBits();

            _worldPacket.WriteInt32(Unk);

            // both lists have to have the same size
            _worldPacket.WriteInt32(Heirlooms.Count);
            _worldPacket.WriteInt32(Heirlooms.Count);

            foreach (var item in Heirlooms)
                _worldPacket.WriteUInt32(item.Key);

            foreach (var flags in Heirlooms)
                _worldPacket.WriteUInt32(flags.Value.flags);
        }

        public bool IsFullUpdate;
        public Dictionary<uint, HeirloomData> Heirlooms = new Dictionary<uint, HeirloomData>();
        int Unk;
    }

    class MountSpecial : ClientPacket
    {
        public MountSpecial(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class SpecialMountAnim : ServerPacket
    {
        public SpecialMountAnim() : base(ServerOpcodes.SpecialMountAnim, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(UnitGUID);
        }

        public ObjectGuid UnitGUID;
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
            _worldPacket.WriteUInt32(Type);
            _worldPacket.WriteUInt32(TimeRemaining);
            _worldPacket.WriteUInt32(TotalTime);
        }

        public uint TimeRemaining;
        public uint TotalTime;
        public TimerType Type;
    }

    class DisplayGameError : ServerPacket
    {
        public DisplayGameError(GameError error) : base(ServerOpcodes.DisplayGameError)
        {
            Error = error;
        }

        public DisplayGameError(GameError error, int arg) : this(error)
        {
            Arg.Set(arg);
        }
        public DisplayGameError(GameError error, int arg1, int arg2) : this(error)
        {
            Arg.Set(arg1);
            Arg2.Set(arg2);
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Error);
            _worldPacket.WriteBit(Arg.HasValue);
            _worldPacket.WriteBit(Arg2.HasValue);
            _worldPacket.FlushBits();

            if (Arg.HasValue)
                _worldPacket.WriteInt32(Arg.Value);

            if (Arg2.HasValue)
                _worldPacket.WriteInt32(Arg2.Value);
        }

        GameError Error;
        Optional<int> Arg;
        Optional<int> Arg2;
    }

    class AccountMountUpdate : ServerPacket
    {
        public AccountMountUpdate() : base(ServerOpcodes.AccountMountUpdate, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteBit(IsFullUpdate);
            _worldPacket.WriteUInt32(Mounts.Count);

            foreach (var spell in Mounts)
            {
                _worldPacket.WriteUInt32(spell.Key);
                _worldPacket.WriteBits(spell.Value, 2);
            }

            _worldPacket.FlushBits();
        }

        public bool IsFullUpdate = false;
        public Dictionary<uint, MountStatusFlags> Mounts = new Dictionary<uint, MountStatusFlags>();
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

    //Structs
    struct PhaseShiftDataPhase
    {
        public PhaseShiftDataPhase(uint phaseFlags, uint id)
        {
            PhaseFlags = (ushort)phaseFlags;
            Id = (ushort)id;
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt16(PhaseFlags);
            data.WriteUInt16(Id);
        }

        public ushort PhaseFlags;
        public ushort Id;
    }

    class PhaseShiftData
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(PhaseShiftFlags);
            data.WriteUInt32(Phases.Count);
            data.WritePackedGuid(PersonalGUID);
            foreach (PhaseShiftDataPhase phaseShiftDataPhase in Phases)
                phaseShiftDataPhase.Write(data);
        }

        public uint PhaseShiftFlags;
        public List<PhaseShiftDataPhase> Phases = new List<PhaseShiftDataPhase>();
        public ObjectGuid PersonalGUID;
    }
}
