// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using System;
using System.Collections.Generic;
using Framework.Dynamic;

namespace Game.Networking.Packets
{
    public class AllAchievementData : ServerPacket
    {
        public AllAchievementData() : base(ServerOpcodes.AllAchievementData, ConnectionType.Instance) { }

        public override void Write()
        {
            Data.Write(_worldPacket);
        }

        public AllAchievements Data = new();
    }

    class AllAccountCriteria : ServerPacket
    {
        public AllAccountCriteria() : base(ServerOpcodes.AllAccountCriteria, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Progress.Count);
            foreach (var progress in Progress)
                progress.Write(_worldPacket);
        }

        public List<CriteriaProgressPkt> Progress = new();
    }
    
    public class RespondInspectAchievements : ServerPacket
    {
        public RespondInspectAchievements() : base(ServerOpcodes.RespondInspectAchievements, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Player);
            Data.Write(_worldPacket);
        }

        public ObjectGuid Player;
        public AllAchievements Data = new();
    }

    public class CriteriaUpdate : ServerPacket
    {
        public CriteriaUpdate() : base(ServerOpcodes.CriteriaUpdate, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(CriteriaID);
            _worldPacket.WriteUInt64(Quantity);
            _worldPacket.WritePackedGuid(PlayerGUID);
            _worldPacket.WriteUInt32(Flags);
            _worldPacket.WriteUInt32(StateFlags);
            CurrentTime.Write(_worldPacket);
            _worldPacket.WriteInt64(ElapsedTime);
            _worldPacket.WriteInt64(CreationTime);
            _worldPacket.WriteBit(DynamicID.HasValue);
            _worldPacket.FlushBits();

            if (DynamicID.HasValue)
                _worldPacket.WriteUInt64(DynamicID.Value);
        }

        public uint CriteriaID;
        public ulong Quantity;
        public ObjectGuid PlayerGUID;
        public uint StateFlags;
        public uint Flags;
        public WowTime CurrentTime;
        public long ElapsedTime;
        public long CreationTime;
        public ulong? DynamicID;
    }

    class AccountCriteriaUpdate : ServerPacket
    {
        public AccountCriteriaUpdate() : base(ServerOpcodes.AccountCriteriaUpdate) { }

        public override void Write()
        {
            Progress.Write(_worldPacket);
        }

        public CriteriaProgressPkt Progress;
    }
    
    public class CriteriaDeleted : ServerPacket
    {
        public CriteriaDeleted() : base(ServerOpcodes.CriteriaDeleted, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(CriteriaID);
        }

        public uint CriteriaID;
    }

    public class AchievementDeleted : ServerPacket
    {
        public AchievementDeleted() : base(ServerOpcodes.AchievementDeleted, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(AchievementID);
            _worldPacket.WriteUInt32(Immunities);
        }

        public uint AchievementID;
        public uint Immunities; // this is just garbage, not used by client
    }

    public class AchievementEarned : ServerPacket
    {
        public AchievementEarned() : base(ServerOpcodes.AchievementEarned, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Sender);
            _worldPacket.WritePackedGuid(Earner);
            _worldPacket.WriteUInt32(AchievementID);
            Time.Write(_worldPacket);
            _worldPacket.WriteUInt32(EarnerNativeRealm);
            _worldPacket.WriteUInt32(EarnerVirtualRealm);
            _worldPacket.WriteBit(Initial);
            _worldPacket.FlushBits();
        }

        public ObjectGuid Earner;
        public uint EarnerNativeRealm;
        public uint EarnerVirtualRealm;
        public uint AchievementID;
        public WowTime Time;
        public bool Initial;
        public ObjectGuid Sender;
    }

    public class BroadcastAchievement  : ServerPacket
    {
        public BroadcastAchievement() : base(ServerOpcodes.BroadcastAchievement) { }

        public override void Write()
        {
            _worldPacket.WriteBits(Name.GetByteCount(), 7);
            _worldPacket.WriteBit(GuildAchievement);
            _worldPacket.WritePackedGuid(PlayerGUID);
            _worldPacket.WriteUInt32(AchievementID);
            _worldPacket.WriteString(Name);
        }

        public ObjectGuid PlayerGUID;
        public string Name = "";
        public uint AchievementID;
        public bool GuildAchievement;
    }

    public class GuildCriteriaUpdate : ServerPacket
    {
        public GuildCriteriaUpdate() : base(ServerOpcodes.GuildCriteriaUpdate) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Progress.Count);

            foreach (GuildCriteriaProgress progress in Progress)
            {
                _worldPacket.WriteUInt32(progress.CriteriaID);
                _worldPacket.WriteInt64(progress.DateCreated);
                _worldPacket.WriteInt64(progress.DateStarted);
                progress.DateUpdated.Write(_worldPacket);
                _worldPacket.WriteUInt32(0); // this is a hack. this is a packed time written as int64 (progress.DateUpdated)
                _worldPacket.WriteUInt64(progress.Quantity);
                _worldPacket.WritePackedGuid(progress.PlayerGUID);
                _worldPacket.WriteInt32(progress.Flags);
                _worldPacket.WriteInt32(progress.StateFlags);
            }
        }

        public List<GuildCriteriaProgress> Progress = new();
    }

    public class GuildCriteriaDeleted : ServerPacket
    {
        public GuildCriteriaDeleted() : base(ServerOpcodes.GuildCriteriaDeleted) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(GuildGUID);
            _worldPacket.WriteUInt32(CriteriaID);
        }

        public ObjectGuid GuildGUID;
        public uint CriteriaID;
    }

    public class GuildSetFocusedAchievement : ClientPacket
    {
        public GuildSetFocusedAchievement(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            AchievementID = _worldPacket.ReadUInt32();
        }

        public uint AchievementID;
    }

    public class GuildAchievementDeleted : ServerPacket
    {
        public GuildAchievementDeleted() : base(ServerOpcodes.GuildAchievementDeleted) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(GuildGUID);
            _worldPacket.WriteUInt32(AchievementID);
            TimeDeleted.Write(_worldPacket);
        }

        public ObjectGuid GuildGUID;
        public uint AchievementID;
        public WowTime TimeDeleted;
    }

    public class GuildAchievementEarned : ServerPacket
    {
        public GuildAchievementEarned() : base(ServerOpcodes.GuildAchievementEarned) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(GuildGUID);
            _worldPacket.WriteUInt32(AchievementID);
            TimeEarned.Write(_worldPacket);
        }

        public uint AchievementID;
        public ObjectGuid GuildGUID;
        public WowTime TimeEarned;
    }

    public class AllGuildAchievements : ServerPacket
    {
        public AllGuildAchievements() : base(ServerOpcodes.AllGuildAchievements) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Earned.Count);

            foreach (EarnedAchievement earned in Earned)
                earned.Write(_worldPacket);
        }

        public List<EarnedAchievement> Earned = new();
    }

    class GuildGetAchievementMembers : ClientPacket
    {
        public GuildGetAchievementMembers(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PlayerGUID = _worldPacket.ReadPackedGuid();
            GuildGUID = _worldPacket.ReadPackedGuid();
            AchievementID = _worldPacket.ReadUInt32();
        }

        public ObjectGuid PlayerGUID;
        public ObjectGuid GuildGUID;
        public uint AchievementID;
    }

    class GuildAchievementMembers : ServerPacket
    {
        public GuildAchievementMembers() : base(ServerOpcodes.GuildAchievementMembers) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(GuildGUID);
            _worldPacket.WriteUInt32(AchievementID);
            _worldPacket.WriteInt32(Member.Count);
            foreach (ObjectGuid guid in Member)
                _worldPacket.WritePackedGuid(guid);
        }

        public ObjectGuid GuildGUID;
        public uint AchievementID;
        public List<ObjectGuid> Member = new();
    }

    //Structs
    public struct EarnedAchievement
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(Id);
            Date.Write(data);
            data.WritePackedGuid(Owner);
            data.WriteUInt32(VirtualRealmAddress);
            data.WriteUInt32(NativeRealmAddress);
        }

        public uint Id;
        public WowTime Date;
        public ObjectGuid Owner;
        public uint VirtualRealmAddress;
        public uint NativeRealmAddress;
    }

    public struct CriteriaProgressPkt
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(Id);
            data.WriteUInt64(Quantity);
            data.WritePackedGuid(Player);
            data.WriteUInt32(Flags);
            data.WriteUInt32(StateFlags);
            Date.Write(data);
            data.WriteInt64(TimeFromStart);
            data.WriteInt64(TimeFromCreate);
            data.WriteBit(DynamicID.HasValue);
            data.FlushBits();

            if (DynamicID.HasValue)
                data.WriteUInt64(DynamicID.Value);
        }

        public uint Id;
        public ulong Quantity;
        public ObjectGuid Player;
        public uint StateFlags;
        public uint Flags;
        public WowTime Date;
        public long TimeFromStart;
        public long TimeFromCreate;
        public ulong? DynamicID;
    }

    public struct GuildCriteriaProgress
    {
        public uint CriteriaID;
        public long DateCreated;
        public long DateStarted;
        public WowTime DateUpdated;
        public ulong Quantity;
        public ObjectGuid PlayerGUID;
        public int Flags;
        public int StateFlags;
    }

    public class AllAchievements
    {
        public void Write(WorldPacket data)
        {
            data.WriteInt32(Earned.Count);
            data.WriteInt32(Progress.Count);

            foreach (EarnedAchievement earned in Earned)
                earned.Write(data);

            foreach (CriteriaProgressPkt progress in Progress)
                progress.Write(data);
        }

        public List<EarnedAchievement> Earned = new();
        public List<CriteriaProgressPkt> Progress = new();
    }
}
