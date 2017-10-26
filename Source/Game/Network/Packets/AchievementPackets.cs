/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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

        public AllAchievements Data { get; set; } = new AllAchievements();
    }

    public class RespondInspectAchievements : ServerPacket
    {
        public RespondInspectAchievements() : base(ServerOpcodes.RespondInspectAchievements, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Player);
            Data.Write(_worldPacket);
        }

        public ObjectGuid Player { get; set; }
        public AllAchievements Data { get; set; } = new AllAchievements();
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

        public uint CriteriaID { get; set; }
        public ulong Quantity { get; set; }
        public ObjectGuid PlayerGUID { get; set; }
        public uint Flags { get; set; }
        public long CurrentTime { get; set; }
        public uint ElapsedTime { get; set; }
        public uint CreationTime { get; set; }
    }

    public class CriteriaDeleted : ServerPacket
    {
        public CriteriaDeleted() : base(ServerOpcodes.CriteriaDeleted, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(CriteriaID);
        }

        public uint CriteriaID { get; set; }
    }

    public class AchievementDeleted : ServerPacket
    {
        public AchievementDeleted() : base(ServerOpcodes.AchievementDeleted, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(AchievementID);
            _worldPacket.WriteUInt32(Immunities);
        }

        public uint AchievementID { get; set; }
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

        public ObjectGuid Earner { get; set; }
        public uint EarnerNativeRealm { get; set; }
        public uint EarnerVirtualRealm { get; set; }
        public uint AchievementID { get; set; }
        public long Time { get; set; }
        public bool Initial { get; set; }
        public ObjectGuid Sender { get; set; }
    }

    public class ServerFirstAchievement : ServerPacket
    {
        public ServerFirstAchievement() : base(ServerOpcodes.ServerFirstAchievement) { }

        public override void Write()
        {
            _worldPacket.WriteBits(Name.Length, 7);
            _worldPacket.WriteBit(GuildAchievement);
            _worldPacket.WritePackedGuid(PlayerGUID);
            _worldPacket.WriteUInt32(AchievementID);
            _worldPacket.WriteString(Name);
        }

        public ObjectGuid PlayerGUID { get; set; }
        public string Name { get; set; } = "";
        public uint AchievementID { get; set; }
        public bool GuildAchievement { get; set; }
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

        public List<GuildCriteriaProgress> Progress { get; set; } = new List<GuildCriteriaProgress>();
    }

    public class GuildCriteriaDeleted : ServerPacket
    {
        public GuildCriteriaDeleted() : base(ServerOpcodes.GuildCriteriaDeleted) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(GuildGUID);
            _worldPacket.WriteUInt32(CriteriaID);
        }

        public ObjectGuid GuildGUID { get; set; }
        public uint CriteriaID { get; set; }
    }

    public class GuildSetFocusedAchievement : ClientPacket
    {
        public GuildSetFocusedAchievement(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            AchievementID = _worldPacket.ReadUInt32();
        }

        public uint AchievementID { get; set; }
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

        public ObjectGuid GuildGUID { get; set; }
        public uint AchievementID { get; set; }
        public long TimeDeleted { get; set; }
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

        public uint AchievementID { get; set; }
        public ObjectGuid GuildGUID { get; set; }
        public long TimeEarned { get; set; }
    }

    public class AllGuildAchievements : ServerPacket
    {
        public AllGuildAchievements() : base(ServerOpcodes.AllGuildAchievements) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Earned.Count());

            foreach (EarnedAchievement earned in Earned)
                earned.Write(_worldPacket);
        }

        public List<EarnedAchievement> Earned { get; set; } = new List<EarnedAchievement>();
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

        public ObjectGuid PlayerGUID { get; set; }
        public ObjectGuid GuildGUID { get; set; }
        public uint AchievementID { get; set; }
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

        public ObjectGuid GuildGUID { get; set; }
        public uint AchievementID { get; set; }
        public List<ObjectGuid> Member { get; set; } = new List<ObjectGuid>();
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

        public uint Id { get; set; }
        public long Date { get; set; }
        public ObjectGuid Owner { get; set; }
        public uint VirtualRealmAddress { get; set; }
        public uint NativeRealmAddress { get; set; }
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

        public uint Id { get; set; }
        public ulong Quantity { get; set; }
        public ObjectGuid Player { get; set; }
        public uint Flags { get; set; }
        public long Date { get; set; }
        public uint TimeFromStart { get; set; }
        public uint TimeFromCreate { get; set; }
    }

    public struct GuildCriteriaProgress
    {
        public uint CriteriaID { get; set; }
        public uint DateCreated { get; set; }
        public uint DateStarted { get; set; }
        public long DateUpdated { get; set; }
        public ulong Quantity { get; set; }
        public ObjectGuid PlayerGUID { get; set; }
        public int Flags { get; set; }
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

        public List<EarnedAchievement> Earned { get; set; } = new List<EarnedAchievement>();
        public List<CriteriaProgressPkt> Progress { get; set; } = new List<CriteriaProgressPkt>();
    }
}
