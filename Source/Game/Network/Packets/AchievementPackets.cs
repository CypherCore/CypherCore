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
using Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Network.Packets
{
    public class AllAchievementData : ServerPacket
    {
        public AllAchievementData() : base(ServerOpcodes.AllAchievementData, ConnectionType.Instance) { }

        public override void Write()
        {
            Data.Write(_worldPacket);
        }

        public AllAchievements Data = new AllAchievements();
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
        public AllAchievements Data = new AllAchievements();
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
            _worldPacket.WritePackedTime(CurrentTime);
            _worldPacket.WriteUInt32(ElapsedTime);
            _worldPacket.WriteUInt32(CreationTime);
        }

        public uint CriteriaID;
        public ulong Quantity;
        public ObjectGuid PlayerGUID;
        public uint Flags;
        public long CurrentTime;
        public uint ElapsedTime;
        public uint CreationTime;
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
            _worldPacket.WritePackedTime(Time);
            _worldPacket.WriteUInt32(EarnerNativeRealm);
            _worldPacket.WriteUInt32(EarnerVirtualRealm);
            _worldPacket.WriteBit(Initial);
            _worldPacket.FlushBits();
        }

        public ObjectGuid Earner;
        public uint EarnerNativeRealm;
        public uint EarnerVirtualRealm;
        public uint AchievementID;
        public long Time;
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
            _worldPacket.WriteUInt32(Progress.Count);

            foreach (GuildCriteriaProgress progress in Progress)
            {
                _worldPacket.WriteInt32(progress.CriteriaID);
                _worldPacket.WriteUInt32(progress.DateCreated);
                _worldPacket.WriteUInt32(progress.DateStarted);
                _worldPacket.WritePackedTime(progress.DateUpdated);
                _worldPacket.WriteUInt64(progress.Quantity);
                _worldPacket.WritePackedGuid(progress.PlayerGUID);
                _worldPacket.WriteInt32(progress.Flags);
            }
        }

        public List<GuildCriteriaProgress> Progress = new List<GuildCriteriaProgress>();
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
            _worldPacket.WritePackedTime(TimeDeleted);
        }

        public ObjectGuid GuildGUID;
        public uint AchievementID;
        public long TimeDeleted;
    }

    public class GuildAchievementEarned : ServerPacket
    {
        public GuildAchievementEarned() : base(ServerOpcodes.GuildAchievementEarned) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(GuildGUID);
            _worldPacket.WriteUInt32(AchievementID);
            _worldPacket.WritePackedTime(TimeEarned);
        }

        public uint AchievementID;
        public ObjectGuid GuildGUID;
        public long TimeEarned;
    }

    public class AllGuildAchievements : ServerPacket
    {
        public AllGuildAchievements() : base(ServerOpcodes.AllGuildAchievements) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Earned.Count);

            foreach (EarnedAchievement earned in Earned)
                earned.Write(_worldPacket);
        }

        public List<EarnedAchievement> Earned = new List<EarnedAchievement>();
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
            _worldPacket.WriteUInt32(Member.Count);
            foreach (ObjectGuid guid in Member)
                _worldPacket.WritePackedGuid(guid);
        }

        public ObjectGuid GuildGUID;
        public uint AchievementID;
        public List<ObjectGuid> Member = new List<ObjectGuid>();
    }

    //Structs
    public struct EarnedAchievement
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(Id);
            data.WritePackedTime(Date);
            data.WritePackedGuid(Owner);
            data.WriteUInt32(VirtualRealmAddress);
            data.WriteUInt32(NativeRealmAddress);
        }

        public uint Id;
        public long Date;
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
            data.WritePackedTime(Date);
            data.WriteUInt32(TimeFromStart);
            data.WriteUInt32(TimeFromCreate);
            data.WriteBits(Flags, 4);
            data.FlushBits();
        }

        public uint Id;
        public ulong Quantity;
        public ObjectGuid Player;
        public uint Flags;
        public long Date;
        public uint TimeFromStart;
        public uint TimeFromCreate;
    }

    public struct GuildCriteriaProgress
    {
        public uint CriteriaID;
        public uint DateCreated;
        public uint DateStarted;
        public long DateUpdated;
        public ulong Quantity;
        public ObjectGuid PlayerGUID;
        public int Flags;
    }

    public class AllAchievements
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(Earned.Count);
            data.WriteUInt32(Progress.Count);

            foreach (EarnedAchievement earned in Earned)
                earned.Write(data);

            foreach (CriteriaProgressPkt progress in Progress)
                progress.Write(data);
        }

        public List<EarnedAchievement> Earned = new List<EarnedAchievement>();
        public List<CriteriaProgressPkt> Progress = new List<CriteriaProgressPkt>();
    }
}
