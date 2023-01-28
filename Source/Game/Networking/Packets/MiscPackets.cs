// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets
{
	public class BindPointUpdate : ServerPacket
	{
		public uint BindAreaID;

		public uint BindMapID = 0xFFFFFFFF;
		public Vector3 BindPosition;

		public BindPointUpdate() : base(ServerOpcodes.BindPointUpdate, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteVector3(BindPosition);
			_worldPacket.WriteUInt32(BindMapID);
			_worldPacket.WriteUInt32(BindAreaID);
		}
	}

	public class PlayerBound : ServerPacket
	{
		private uint AreaID;

		private ObjectGuid BinderID;

		public PlayerBound(ObjectGuid binderId, uint areaId) : base(ServerOpcodes.PlayerBound)
		{
			BinderID = binderId;
			AreaID   = areaId;
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(BinderID);
			_worldPacket.WriteUInt32(AreaID);
		}
	}

	public class InvalidatePlayer : ServerPacket
	{
		public ObjectGuid Guid;

		public InvalidatePlayer() : base(ServerOpcodes.InvalidatePlayer)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(Guid);
		}
	}

	public class LoginSetTimeSpeed : ServerPacket
	{
		public uint GameTime;
		public int GameTimeHolidayOffset;

		public float NewSpeed;
		public uint ServerTime;
		public int ServerTimeHolidayOffset;

		public LoginSetTimeSpeed() : base(ServerOpcodes.LoginSetTimeSpeed, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedTime(ServerTime);
			_worldPacket.WritePackedTime(GameTime);
			_worldPacket.WriteFloat(NewSpeed);
			_worldPacket.WriteInt32(ServerTimeHolidayOffset);
			_worldPacket.WriteInt32(GameTimeHolidayOffset);
		}
	}

	public class ResetWeeklyCurrency : ServerPacket
	{
		public ResetWeeklyCurrency() : base(ServerOpcodes.ResetWeeklyCurrency, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
		}
	}

	public class SetCurrency : ServerPacket
	{
		public uint? FirstCraftOperationID;
		public uint Flags;
		public long? LastSpendTime;
		public int? MaxQuantity;
		public int Quantity;
		public int? QuantityChange;
		public int? QuantityGainSource;
		public int? QuantityLostSource;
		public bool SuppressChatLog;
		public List<UiEventToast> Toasts = new();
		public int? TotalEarned;
		public int? TrackedQuantity;

		public uint Type;
		public int? WeeklyQuantity;

		public SetCurrency() : base(ServerOpcodes.SetCurrency, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(Type);
			_worldPacket.WriteInt32(Quantity);
			_worldPacket.WriteUInt32(Flags);
			_worldPacket.WriteBit(WeeklyQuantity.HasValue);
			_worldPacket.WriteBit(TrackedQuantity.HasValue);
			_worldPacket.WriteBit(MaxQuantity.HasValue);
			_worldPacket.WriteBit(TotalEarned.HasValue);
			_worldPacket.WriteBit(SuppressChatLog);
			_worldPacket.WriteBit(QuantityChange.HasValue);
			_worldPacket.WriteBit(QuantityLostSource.HasValue);
			_worldPacket.WriteBit(QuantityGainSource.HasValue);
			_worldPacket.WriteBit(FirstCraftOperationID.HasValue);
			_worldPacket.WriteBit(LastSpendTime.HasValue);
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

			if (QuantityLostSource.HasValue)
				_worldPacket.WriteInt32(QuantityLostSource.Value);

			if (QuantityGainSource.HasValue)
				_worldPacket.WriteInt32(QuantityGainSource.Value);

			if (FirstCraftOperationID.HasValue)
				_worldPacket.WriteUInt32(FirstCraftOperationID.Value);

			if (LastSpendTime.HasValue)
				_worldPacket.WriteInt64(LastSpendTime.Value);
		}
	}

	public class SetMaxWeeklyQuantity : ServerPacket
	{
		public uint MaxWeeklyQuantity;
		public uint Type;

		public SetMaxWeeklyQuantity() : base(ServerOpcodes.SetMaxWeeklyQuantity, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(Type);
			_worldPacket.WriteUInt32(MaxWeeklyQuantity);
		}
	}

	public class SetSelection : ClientPacket
	{
		public ObjectGuid Selection; // Target

		public SetSelection(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			Selection = _worldPacket.ReadPackedGuid();
		}
	}

	public class SetupCurrency : ServerPacket
	{
		public List<Record> Data = new();

		public SetupCurrency() : base(ServerOpcodes.SetupCurrency, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteInt32(Data.Count);

			foreach (Record data in Data)
			{
				_worldPacket.WriteUInt32(data.Type);
				_worldPacket.WriteUInt32(data.Quantity);

				_worldPacket.WriteBit(data.WeeklyQuantity.HasValue);
				_worldPacket.WriteBit(data.MaxWeeklyQuantity.HasValue);
				_worldPacket.WriteBit(data.TrackedQuantity.HasValue);
				_worldPacket.WriteBit(data.MaxQuantity.HasValue);
				_worldPacket.WriteBit(data.TotalEarned.HasValue);
				_worldPacket.WriteBit(data.LastSpendTime.HasValue);
				_worldPacket.WriteBits(data.Flags, 5);
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

				if (data.LastSpendTime.HasValue)
					_worldPacket.WriteInt64(data.LastSpendTime.Value);
			}
		}

		public struct Record
		{
			public uint Type;
			public uint Quantity;
			public uint? WeeklyQuantity;    // Currency Count obtained this Week.  
			public uint? MaxWeeklyQuantity; // Weekly Currency cap.
			public uint? TrackedQuantity;
			public int? MaxQuantity;
			public int? TotalEarned;
			public long? LastSpendTime;
			public byte Flags; // 0 = none, 
		}
	}

	public class ViolenceLevel : ClientPacket
	{
		public sbyte violenceLevel; // 0 - no combat effects, 1 - display some combat effects, 2 - blood, 3 - bloody, 4 - bloodier, 5 - bloodiest

		public ViolenceLevel(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			violenceLevel = _worldPacket.ReadInt8();
		}
	}

	public class TimeSyncRequest : ServerPacket
	{
		public uint SequenceIndex;

		public TimeSyncRequest() : base(ServerOpcodes.TimeSyncRequest, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(SequenceIndex);
		}
	}

	public class TimeSyncResponse : ClientPacket
	{
		public uint ClientTime;    // Client ticks in ms
		public uint SequenceIndex; // Same index as in request

		public TimeSyncResponse(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			SequenceIndex = _worldPacket.ReadUInt32();
			ClientTime    = _worldPacket.ReadUInt32();
		}

		public DateTime GetReceivedTime()
		{
			return _worldPacket.GetReceivedTime();
		}
	}

	public class TriggerCinematic : ServerPacket
	{
		public uint CinematicID;
		public ObjectGuid ConversationGuid;

		public TriggerCinematic() : base(ServerOpcodes.TriggerCinematic)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(CinematicID);
			_worldPacket.WritePackedGuid(ConversationGuid);
		}
	}

	public class TriggerMovie : ServerPacket
	{
		public uint MovieID;

		public TriggerMovie() : base(ServerOpcodes.TriggerMovie)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(MovieID);
		}
	}

	public class ServerTimeOffsetRequest : ClientPacket
	{
		public ServerTimeOffsetRequest(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
		}
	}

	public class ServerTimeOffset : ServerPacket
	{
		public long Time;

		public ServerTimeOffset() : base(ServerOpcodes.ServerTimeOffset)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteInt64(Time);
		}
	}

	public class TutorialFlags : ServerPacket
	{
		public uint[] TutorialData = new uint[SharedConst.MaxAccountTutorialValues];

		public TutorialFlags() : base(ServerOpcodes.TutorialFlags)
		{
		}

		public override void Write()
		{
			for (byte i = 0; i < (int)Tutorials.Max; ++i)
				_worldPacket.WriteUInt32(TutorialData[i]);
		}
	}

	public class TutorialSetFlag : ClientPacket
	{
		public TutorialAction Action;
		public uint TutorialBit;

		public TutorialSetFlag(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			Action = (TutorialAction)_worldPacket.ReadBits<byte>(2);

			if (Action == TutorialAction.Update)
				TutorialBit = _worldPacket.ReadUInt32();
		}
	}

	public class WorldServerInfo : ServerPacket
	{
		public bool BlockExitingLoadingScreen; // when set to true, sending SMSG_UPDATE_OBJECT with CreateObject Self bit = true will not hide loading screen

		public uint DifficultyID;
		public uint? InstanceGroupSize;

		public bool IsTournamentRealm;

		// instead it will be done after this packet is sent again with false in this bit and SMSG_UPDATE_OBJECT Values for player
		public uint? RestrictedAccountMaxLevel;
		public ulong? RestrictedAccountMaxMoney;
		public bool XRealmPvpAlert;

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
	}

	public class SetDungeonDifficulty : ClientPacket
	{
		public uint DifficultyID;

		public SetDungeonDifficulty(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			DifficultyID = _worldPacket.ReadUInt32();
		}
	}

	public class SetRaidDifficulty : ClientPacket
	{
		public int DifficultyID;
		public byte Legacy;

		public SetRaidDifficulty(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			DifficultyID = _worldPacket.ReadInt32();
			Legacy       = _worldPacket.ReadUInt8();
		}
	}

	public class DungeonDifficultySet : ServerPacket
	{
		public int DifficultyID;

		public DungeonDifficultySet() : base(ServerOpcodes.SetDungeonDifficulty)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteInt32(DifficultyID);
		}
	}

	public class RaidDifficultySet : ServerPacket
	{
		public int DifficultyID;
		public bool Legacy;

		public RaidDifficultySet() : base(ServerOpcodes.RaidDifficultySet)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteInt32(DifficultyID);
			_worldPacket.WriteUInt8((byte)(Legacy ? 1 : 0));
		}
	}

	public class CorpseReclaimDelay : ServerPacket
	{
		public uint Remaining;

		public CorpseReclaimDelay() : base(ServerOpcodes.CorpseReclaimDelay, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(Remaining);
		}
	}

	public class DeathReleaseLoc : ServerPacket
	{
		public WorldLocation Loc;

		public int MapID;

		public DeathReleaseLoc() : base(ServerOpcodes.DeathReleaseLoc)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteInt32(MapID);
			_worldPacket.WriteXYZ(Loc);
		}
	}

	public class PortGraveyard : ClientPacket
	{
		public PortGraveyard(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
		}
	}

	public class PreRessurect : ServerPacket
	{
		public ObjectGuid PlayerGUID;

		public PreRessurect() : base(ServerOpcodes.PreRessurect)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(PlayerGUID);
		}
	}

	public class ReclaimCorpse : ClientPacket
	{
		public ObjectGuid CorpseGUID;

		public ReclaimCorpse(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			CorpseGUID = _worldPacket.ReadPackedGuid();
		}
	}

	public class RepopRequest : ClientPacket
	{
		public bool CheckInstance;

		public RepopRequest(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			CheckInstance = _worldPacket.HasBit();
		}
	}

	public class RequestCemeteryList : ClientPacket
	{
		public RequestCemeteryList(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
		}
	}

	public class RequestCemeteryListResponse : ServerPacket
	{
		public List<uint> CemeteryID = new();

		public bool IsGossipTriggered;

		public RequestCemeteryListResponse() : base(ServerOpcodes.RequestCemeteryListResponse, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteBit(IsGossipTriggered);
			_worldPacket.FlushBits();

			_worldPacket.WriteInt32(CemeteryID.Count);

			foreach (uint cemetery in CemeteryID)
				_worldPacket.WriteUInt32(cemetery);
		}
	}

	public class ResurrectResponse : ClientPacket
	{
		public uint Response;

		public ObjectGuid Resurrecter;

		public ResurrectResponse(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			Resurrecter = _worldPacket.ReadPackedGuid();
			Response    = _worldPacket.ReadUInt32();
		}
	}

	public class WeatherPkt : ServerPacket
	{
		private bool Abrupt;
		private float Intensity;
		private WeatherState WeatherID;

		public WeatherPkt(WeatherState weatherID = 0, float intensity = 0.0f, bool abrupt = false) : base(ServerOpcodes.Weather, ConnectionType.Instance)
		{
			WeatherID = weatherID;
			Intensity = intensity;
			Abrupt    = abrupt;
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32((uint)WeatherID);
			_worldPacket.WriteFloat(Intensity);
			_worldPacket.WriteBit(Abrupt);

			_worldPacket.FlushBits();
		}
	}

	public class StandStateChange : ClientPacket
	{
		public UnitStandStateType StandState;

		public StandStateChange(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			StandState = (UnitStandStateType)_worldPacket.ReadUInt32();
		}
	}

	public class StandStateUpdate : ServerPacket
	{
		private uint AnimKitID;
		private UnitStandStateType State;

		public StandStateUpdate(UnitStandStateType state, uint animKitId) : base(ServerOpcodes.StandStateUpdate)
		{
			State     = state;
			AnimKitID = animKitId;
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(AnimKitID);
			_worldPacket.WriteUInt8((byte)State);
		}
	}

	public class SetAnimTier : ServerPacket
	{
		public int Tier;

		public ObjectGuid Unit;

		public SetAnimTier() : base(ServerOpcodes.SetAnimTier, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(Unit);
			_worldPacket.WriteBits(Tier, 3);
			_worldPacket.FlushBits();
		}
	}

	public class StartMirrorTimer : ServerPacket
	{
		public int MaxValue;
		public bool Paused;

		public int Scale;
		public int SpellID;
		public MirrorTimerType Timer;
		public int Value;

		public StartMirrorTimer(MirrorTimerType timer, int value, int maxValue, int scale, int spellID, bool paused) : base(ServerOpcodes.StartMirrorTimer)
		{
			Timer    = timer;
			Value    = value;
			MaxValue = maxValue;
			Scale    = scale;
			SpellID  = spellID;
			Paused   = paused;
		}

		public override void Write()
		{
			_worldPacket.WriteInt32((int)Timer);
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
		public bool Paused = true;
		public MirrorTimerType Timer;

		public PauseMirrorTimer(MirrorTimerType timer, bool paused) : base(ServerOpcodes.PauseMirrorTimer)
		{
			Timer  = timer;
			Paused = paused;
		}

		public override void Write()
		{
			_worldPacket.WriteInt32((int)Timer);
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
			_worldPacket.WriteInt32((int)Timer);
		}
	}

	public class ExplorationExperience : ServerPacket
	{
		public uint AreaID;

		public uint Experience;

		public ExplorationExperience(uint experience, uint areaID) : base(ServerOpcodes.ExplorationExperience)
		{
			Experience = experience;
			AreaID     = areaID;
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(AreaID);
			_worldPacket.WriteUInt32(Experience);
		}
	}

	public class LevelUpInfo : ServerPacket
	{
		public uint HealthDelta = 0;

		public uint Level = 0;
		public int NumNewPvpTalentSlots;
		public int NumNewTalents;
		public int[] PowerDelta = new int[(int)PowerType.MaxPerClass];
		public int[] StatDelta = new int[(int)Stats.Max];

		public LevelUpInfo() : base(ServerOpcodes.LevelUpInfo)
		{
		}

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
	}

	public class PlayMusic : ServerPacket
	{
		private uint SoundKitID;

		public PlayMusic(uint soundKitID) : base(ServerOpcodes.PlayMusic)
		{
			SoundKitID = soundKitID;
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(SoundKitID);
		}
	}

	public class RandomRollClient : ClientPacket
	{
		public uint Max;

		public uint Min;
		public byte PartyIndex;

		public RandomRollClient(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			Min        = _worldPacket.ReadUInt32();
			Max        = _worldPacket.ReadUInt32();
			PartyIndex = _worldPacket.ReadUInt8();
		}
	}

	public class RandomRoll : ServerPacket
	{
		public int Max;
		public int Min;
		public int Result;
		public ObjectGuid Roller;
		public ObjectGuid RollerWowAccount;

		public RandomRoll() : base(ServerOpcodes.RandomRoll)
		{
		}

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
		public EnableBarberShop() : base(ServerOpcodes.EnableBarberShop)
		{
		}

		public override void Write()
		{
		}
	}

	internal class PhaseShiftChange : ServerPacket
	{
		public ObjectGuid Client;
		public PhaseShiftData Phaseshift = new();
		public List<ushort> PreloadMapIDs = new();
		public List<ushort> UiMapPhaseIDs = new();
		public List<ushort> VisibleMapIDs = new();

		public PhaseShiftChange() : base(ServerOpcodes.PhaseShiftChange)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(Client);
			Phaseshift.Write(_worldPacket);
			_worldPacket.WriteInt32(VisibleMapIDs.Count * 2); // size in bytes

			foreach (ushort visibleMapId in VisibleMapIDs)
				_worldPacket.WriteUInt16(visibleMapId); // Active terrain swap map Id

			_worldPacket.WriteInt32(PreloadMapIDs.Count * 2); // size in bytes

			foreach (ushort preloadMapId in PreloadMapIDs)
				_worldPacket.WriteUInt16(preloadMapId); // Inactive terrain swap map Id

			_worldPacket.WriteInt32(UiMapPhaseIDs.Count * 2); // size in bytes

			foreach (ushort uiMapPhaseId in UiMapPhaseIDs)
				_worldPacket.WriteUInt16(uiMapPhaseId); // UI map Id, WorldMapArea.db2, controls map display
		}
	}

	public class ZoneUnderAttack : ServerPacket
	{
		public int AreaID;

		public ZoneUnderAttack() : base(ServerOpcodes.ZoneUnderAttack, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteInt32(AreaID);
		}
	}

	internal class DurabilityDamageDeath : ServerPacket
	{
		public uint Percent;

		public DurabilityDamageDeath() : base(ServerOpcodes.DurabilityDamageDeath)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(Percent);
		}
	}

	internal class ObjectUpdateFailed : ClientPacket
	{
		public ObjectGuid ObjectGUID;

		public ObjectUpdateFailed(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			ObjectGUID = _worldPacket.ReadPackedGuid();
		}
	}

	internal class ObjectUpdateRescued : ClientPacket
	{
		public ObjectGuid ObjectGUID;

		public ObjectUpdateRescued(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			ObjectGUID = _worldPacket.ReadPackedGuid();
		}
	}

	internal class PlayObjectSound : ServerPacket
	{
		public int BroadcastTextID;
		public Vector3 Position;
		public uint SoundKitID;
		public ObjectGuid SourceObjectGUID;

		public ObjectGuid TargetObjectGUID;

		public PlayObjectSound() : base(ServerOpcodes.PlayObjectSound)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(SoundKitID);
			_worldPacket.WritePackedGuid(SourceObjectGUID);
			_worldPacket.WritePackedGuid(TargetObjectGUID);
			_worldPacket.WriteVector3(Position);
			_worldPacket.WriteInt32(BroadcastTextID);
		}
	}

	internal class PlaySound : ServerPacket
	{
		public uint BroadcastTextID;
		public uint SoundKitID;

		public ObjectGuid SourceObjectGuid;

		public PlaySound(ObjectGuid sourceObjectGuid, uint soundKitID, uint broadcastTextId) : base(ServerOpcodes.PlaySound)
		{
			SourceObjectGuid = sourceObjectGuid;
			SoundKitID       = soundKitID;
			BroadcastTextID  = broadcastTextId;
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(SoundKitID);
			_worldPacket.WritePackedGuid(SourceObjectGuid);
			_worldPacket.WriteUInt32(BroadcastTextID);
		}
	}

	internal class PlaySpeakerBoxSound : ServerPacket
	{
		public uint SoundKitID;

		public ObjectGuid SourceObjectGUID;

		public PlaySpeakerBoxSound(ObjectGuid sourceObjectGuid, uint soundKitID) : base(ServerOpcodes.PlaySpeakerbotSound)
		{
			SourceObjectGUID = sourceObjectGuid;
			SoundKitID       = soundKitID;
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(SourceObjectGUID);
			_worldPacket.WriteUInt32(SoundKitID);
		}
	}

	internal class OpeningCinematic : ClientPacket
	{
		public OpeningCinematic(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
		}
	}

	internal class CompleteCinematic : ClientPacket
	{
		public CompleteCinematic(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
		}
	}

	internal class NextCinematicCamera : ClientPacket
	{
		public NextCinematicCamera(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
		}
	}

	internal class CompleteMovie : ClientPacket
	{
		public CompleteMovie(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
		}
	}

	internal class FarSight : ClientPacket
	{
		public bool Enable;

		public FarSight(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			Enable = _worldPacket.HasBit();
		}
	}

	internal class SaveCUFProfiles : ClientPacket
	{
		public List<CUFProfile> CUFProfiles = new();

		public SaveCUFProfiles(WorldPacket packet) : base(packet)
		{
		}

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
				cufProfile.FrameWidth  = _worldPacket.ReadUInt16();

				cufProfile.SortBy     = _worldPacket.ReadUInt8();
				cufProfile.HealthText = _worldPacket.ReadUInt8();

				cufProfile.TopPoint    = _worldPacket.ReadUInt8();
				cufProfile.BottomPoint = _worldPacket.ReadUInt8();
				cufProfile.LeftPoint   = _worldPacket.ReadUInt8();

				cufProfile.TopOffset    = _worldPacket.ReadUInt16();
				cufProfile.BottomOffset = _worldPacket.ReadUInt16();
				cufProfile.LeftOffset   = _worldPacket.ReadUInt16();

				cufProfile.ProfileName = _worldPacket.ReadString(strLen);

				CUFProfiles.Add(cufProfile);
			}
		}
	}

	internal class LoadCUFProfiles : ServerPacket
	{
		public List<CUFProfile> CUFProfiles = new();

		public LoadCUFProfiles() : base(ServerOpcodes.LoadCufProfiles, ConnectionType.Instance)
		{
		}

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
	}

	internal class PlayOneShotAnimKit : ServerPacket
	{
		public ushort AnimKitID;

		public ObjectGuid Unit;

		public PlayOneShotAnimKit() : base(ServerOpcodes.PlayOneShotAnimKit)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(Unit);
			_worldPacket.WriteUInt16(AnimKitID);
		}
	}

	internal class SetAIAnimKit : ServerPacket
	{
		public ushort AnimKitID;

		public ObjectGuid Unit;

		public SetAIAnimKit() : base(ServerOpcodes.SetAiAnimKit, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(Unit);
			_worldPacket.WriteUInt16(AnimKitID);
		}
	}

	internal class SetMeleeAnimKit : ServerPacket
	{
		public ushort AnimKitID;

		public ObjectGuid Unit;

		public SetMeleeAnimKit() : base(ServerOpcodes.SetMeleeAnimKit, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(Unit);
			_worldPacket.WriteUInt16(AnimKitID);
		}
	}

	internal class SetMovementAnimKit : ServerPacket
	{
		public ushort AnimKitID;

		public ObjectGuid Unit;

		public SetMovementAnimKit() : base(ServerOpcodes.SetMovementAnimKit, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(Unit);
			_worldPacket.WriteUInt16(AnimKitID);
		}
	}

	internal class SetPlayHoverAnim : ServerPacket
	{
		public bool PlayHoverAnim;

		public ObjectGuid UnitGUID;

		public SetPlayHoverAnim() : base(ServerOpcodes.SetPlayHoverAnim, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(UnitGUID);
			_worldPacket.WriteBit(PlayHoverAnim);
			_worldPacket.FlushBits();
		}
	}

	internal class TogglePvP : ClientPacket
	{
		public TogglePvP(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
		}
	}

	internal class SetPvP : ClientPacket
	{
		public bool EnablePVP;

		public SetPvP(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			EnablePVP = _worldPacket.HasBit();
		}
	}

	internal class SetWarMode : ClientPacket
	{
		public bool Enable;

		public SetWarMode(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			Enable = _worldPacket.HasBit();
		}
	}

	internal class AccountHeirloomUpdate : ServerPacket
	{
		public Dictionary<uint, HeirloomData> Heirlooms = new();

		public bool IsFullUpdate;
		public int Unk;

		public AccountHeirloomUpdate() : base(ServerOpcodes.AccountHeirloomUpdate, ConnectionType.Instance)
		{
		}

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
				_worldPacket.WriteUInt32((uint)flags.Value.flags);
		}
	}

	internal class MountSpecial : ClientPacket
	{
		public int SequenceVariation;

		public int[] SpellVisualKitIDs;

		public MountSpecial(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			SpellVisualKitIDs = new int[_worldPacket.ReadUInt32()];
			SequenceVariation = _worldPacket.ReadInt32();

			for (var i = 0; i < SpellVisualKitIDs.Length; ++i)
				SpellVisualKitIDs[i] = _worldPacket.ReadInt32();
		}
	}

	internal class SpecialMountAnim : ServerPacket
	{
		public int SequenceVariation;
		public List<int> SpellVisualKitIDs = new();

		public ObjectGuid UnitGUID;

		public SpecialMountAnim() : base(ServerOpcodes.SpecialMountAnim, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(UnitGUID);
			_worldPacket.WriteInt32(SpellVisualKitIDs.Count);
			_worldPacket.WriteInt32(SequenceVariation);

			foreach (var id in SpellVisualKitIDs)
				_worldPacket.WriteInt32(id);
		}
	}

	internal class CrossedInebriationThreshold : ServerPacket
	{
		public ObjectGuid Guid;
		public uint ItemID;
		public uint Threshold;

		public CrossedInebriationThreshold() : base(ServerOpcodes.CrossedInebriationThreshold)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(Guid);
			_worldPacket.WriteUInt32(Threshold);
			_worldPacket.WriteUInt32(ItemID);
		}
	}

	internal class SetTaxiBenchmarkMode : ClientPacket
	{
		public bool Enable;

		public SetTaxiBenchmarkMode(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			Enable = _worldPacket.HasBit();
		}
	}

	internal class OverrideLight : ServerPacket
	{
		public uint AreaLightID;
		public uint OverrideLightID;
		public uint TransitionMilliseconds;

		public OverrideLight() : base(ServerOpcodes.OverrideLight)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(AreaLightID);
			_worldPacket.WriteUInt32(OverrideLightID);
			_worldPacket.WriteUInt32(TransitionMilliseconds);
		}
	}

	public class StartTimer : ServerPacket
	{
		public uint TimeLeft;

		public uint TotalTime;
		public TimerType Type;

		public StartTimer() : base(ServerOpcodes.StartTimer)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(TotalTime);
			_worldPacket.WriteUInt32(TimeLeft);
			_worldPacket.WriteInt32((int)Type);
		}
	}

	internal class ConversationLineStarted : ClientPacket
	{
		public ObjectGuid ConversationGUID;
		public uint LineID;

		public ConversationLineStarted(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			ConversationGUID = _worldPacket.ReadPackedGuid();
			LineID           = _worldPacket.ReadUInt32();
		}
	}

	internal class RequestLatestSplashScreen : ClientPacket
	{
		public RequestLatestSplashScreen(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
		}
	}

	internal class SplashScreenShowLatest : ServerPacket
	{
		public uint UISplashScreenID;

		public SplashScreenShowLatest() : base(ServerOpcodes.SplashScreenShowLatest, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(UISplashScreenID);
		}
	}

	internal class DisplayToast : ServerPacket
	{
		public bool BonusRoll;
		public uint CurrencyID;
		public DisplayToastMethod DisplayToastMethod;
		public Gender Gender = Gender.None;
		public bool IsSecondaryResult;
		public ItemInstance Item;
		public int LootSpec;
		public bool Mailed;
		public ulong Quantity;
		public uint QuestID;
		public DisplayToastType Type = DisplayToastType.Money;

		public DisplayToast() : base(ServerOpcodes.DisplayToast, ConnectionType.Instance)
		{
		}

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
					Item.Write(_worldPacket);
					_worldPacket.WriteInt32(LootSpec);
					_worldPacket.WriteInt32((int)Gender);

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

	internal class DisplayGameError : ServerPacket
	{
		private int? Arg;
		private int? Arg2;

		private GameError Error;

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
			Arg  = arg1;
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
	}

	internal class AccountMountUpdate : ServerPacket
	{
		public bool IsFullUpdate = false;
		public Dictionary<uint, MountStatusFlags> Mounts = new();

		public AccountMountUpdate() : base(ServerOpcodes.AccountMountUpdate, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteBit(IsFullUpdate);
			_worldPacket.WriteInt32(Mounts.Count);

			foreach (var spell in Mounts)
			{
				_worldPacket.WriteUInt32(spell.Key);
				_worldPacket.WriteBits(spell.Value, 2);
			}

			_worldPacket.FlushBits();
		}
	}

	internal class MountSetFavorite : ClientPacket
	{
		public bool IsFavorite;

		public uint MountSpellID;

		public MountSetFavorite(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			MountSpellID = _worldPacket.ReadUInt32();
			IsFavorite   = _worldPacket.HasBit();
		}
	}

	internal class CloseInteraction : ClientPacket
	{
		public ObjectGuid SourceGuid;

		public CloseInteraction(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			SourceGuid = _worldPacket.ReadPackedGuid();
		}
	}

	//Structs
	internal struct PhaseShiftDataPhase
	{
		public PhaseShiftDataPhase(uint phaseFlags, uint id)
		{
			PhaseFlags = (ushort)phaseFlags;
			Id         = (ushort)id;
		}

		public void Write(WorldPacket data)
		{
			data.WriteUInt16(PhaseFlags);
			data.WriteUInt16(Id);
		}

		public ushort PhaseFlags;
		public ushort Id;
	}

	internal class PhaseShiftData
	{
		public ObjectGuid PersonalGUID;
		public List<PhaseShiftDataPhase> Phases = new();

		public uint PhaseShiftFlags;

		public void Write(WorldPacket data)
		{
			data.WriteUInt32(PhaseShiftFlags);
			data.WriteInt32(Phases.Count);
			data.WritePackedGuid(PersonalGUID);

			foreach (PhaseShiftDataPhase phaseShiftDataPhase in Phases)
				phaseShiftDataPhase.Write(data);
		}
	}
}