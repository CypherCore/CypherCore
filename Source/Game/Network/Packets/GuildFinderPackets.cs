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
using Framework.Dynamic;
using Game.Entities;
using System.Collections.Generic;

namespace Game.Network.Packets
{
    class LFGuildAddRecruit : ClientPacket
    {
        public LFGuildAddRecruit(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            GuildGUID = _worldPacket.ReadPackedGuid();
            PlayStyle = _worldPacket.ReadUInt32();
            Availability = _worldPacket.ReadUInt32();
            ClassRoles = _worldPacket.ReadUInt32();
            Comment = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(10));
        }

        public ObjectGuid GuildGUID { get; set; }
        public uint Availability { get; set; }
        public uint ClassRoles { get; set; }
        public uint PlayStyle { get; set; }
        public string Comment { get; set; }
    }

    class LFGuildApplicationsListChanged : ServerPacket
    {
        public LFGuildApplicationsListChanged() : base(ServerOpcodes.LfGuildApplicationsListChanged) { }

        public override void Write() { }
    }

    class LFGuildApplicantListChanged : ServerPacket
    {
        public LFGuildApplicantListChanged() : base(ServerOpcodes.LfGuildApplicantListChanged) { }

        public override void Write() { }
    }

    class LFGuildBrowse : ClientPacket
    {
        public LFGuildBrowse(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PlayStyle = _worldPacket.ReadUInt32();
            Availability = _worldPacket.ReadUInt32();
            ClassRoles = _worldPacket.ReadUInt32();
            CharacterLevel = _worldPacket.ReadUInt32();
        }

        public uint CharacterLevel { get; set; }
        public uint Availability { get; set; }
        public uint ClassRoles { get; set; }
        public uint PlayStyle { get; set; }
    }

    class LFGuildBrowseResult : ServerPacket
    {
        public LFGuildBrowseResult() : base(ServerOpcodes.LfGuildBrowse) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Post.Count);
            foreach (LFGuildBrowseData guildData in Post)
                guildData.Write(_worldPacket);
        }

        public List<LFGuildBrowseData> Post { get; set; } = new List<LFGuildBrowseData>();
    }

    class LFGuildDeclineRecruit : ClientPacket
    {
        public LFGuildDeclineRecruit(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            RecruitGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid RecruitGUID { get; set; }
    }

    class LFGuildGetApplications : ClientPacket
    {
        public LFGuildGetApplications(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class LFGuildApplications : ServerPacket
    {
        public LFGuildApplications() : base(ServerOpcodes.LfGuildApplications) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(NumRemaining);
            _worldPacket.WriteUInt32(Application.Count);
            foreach (LFGuildApplicationData application in Application)
                application.Write(_worldPacket);
        }

        public List<LFGuildApplicationData> Application { get; set; } = new List<LFGuildApplicationData>();
        public int NumRemaining { get; set; }
    }

    class LFGuildGetGuildPost : ClientPacket
    {
        public LFGuildGetGuildPost(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class LFGuildPost : ServerPacket
    {
        public LFGuildPost() : base(ServerOpcodes.LfGuildPost) { }

        public override void Write()
        {
            _worldPacket.WriteBit(Post.HasValue);
            _worldPacket.FlushBits();
            if (Post.HasValue)
                Post.Value.Write(_worldPacket);
        }

        public Optional<GuildPostData> Post;
    }

    class LFGuildGetRecruits : ClientPacket
    {
        public LFGuildGetRecruits(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            LastUpdate = _worldPacket.ReadUInt32();
        }

        public uint LastUpdate { get; set; }
    }

    class LFGuildRecruits : ServerPacket
    {
        public LFGuildRecruits() : base(ServerOpcodes.LfGuildRecruits) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Recruits.Count);
            _worldPacket.WriteUInt32(UpdateTime);
            foreach (LFGuildRecruitData recruit in Recruits)
                recruit.Write(_worldPacket);
        }

        public List<LFGuildRecruitData> Recruits { get; set; } = new List<LFGuildRecruitData>();
        public long UpdateTime { get; set; }
    }

    class LFGuildRemoveRecruit : ClientPacket
    {
        public LFGuildRemoveRecruit(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            GuildGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid GuildGUID { get; set; }
    }

    class LFGuildSetGuildPost : ClientPacket
    {
        public LFGuildSetGuildPost(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PlayStyle = _worldPacket.ReadUInt32();
            Availability = _worldPacket.ReadUInt32();
            ClassRoles = _worldPacket.ReadUInt32();
            LevelRange = _worldPacket.ReadUInt32();
            Active = _worldPacket.HasBit();
            Comment = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(10));
        }

        public uint Availability { get; set; }
        public uint PlayStyle { get; set; }
        public uint ClassRoles { get; set; }
        public uint LevelRange { get; set; }
        public bool Active { get; set; }
        public string Comment { get; set; }
    }

    //Structs

    class LFGuildBrowseData
    {
        public void Write(WorldPacket data)
        {
            data.WriteBits(GuildName.Length, 7);
            data.WriteBits(Comment.Length, 10);
            data.WritePackedGuid(GuildGUID);
            data.WriteUInt32(GuildVirtualRealm);
            data.WriteInt32(GuildMembers);
            data.WriteUInt32(GuildAchievementPoints);
            data.WriteInt32(PlayStyle);
            data.WriteInt32(Availability);
            data.WriteInt32(ClassRoles);
            data.WriteInt32(LevelRange);
            data.WriteUInt32(EmblemStyle);
            data.WriteUInt32(EmblemColor);
            data.WriteUInt32(BorderStyle);
            data.WriteUInt32(BorderColor);
            data.WriteUInt32(Background);
            data.WriteInt8(Cached);
            data.WriteInt8(MembershipRequested);
            data.WriteString(GuildName);
            data.WriteString(Comment);
        }

        public string GuildName { get; set; } = "";
        public ObjectGuid GuildGUID { get; set; }
        public uint GuildVirtualRealm { get; set; }
        public int GuildMembers { get; set; }
        public uint GuildAchievementPoints { get; set; }
        public int PlayStyle { get; set; }
        public int Availability { get; set; }
        public int ClassRoles { get; set; }
        public int LevelRange { get; set; }
        public uint EmblemStyle { get; set; }
        public uint EmblemColor { get; set; }
        public uint BorderStyle { get; set; }
        public uint BorderColor { get; set; }
        public uint Background { get; set; }
        public string Comment { get; set; } = "";
        public sbyte Cached { get; set; }
        public sbyte MembershipRequested { get; set; }
    }

    class LFGuildApplicationData
    {
        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(GuildGUID);
            data.WriteUInt32(GuildVirtualRealm);
            data.WriteInt32(ClassRoles);
            data.WriteInt32(PlayStyle);
            data.WriteInt32(Availability);
            data.WriteUInt32(SecondsSinceCreated);
            data.WriteUInt32(SecondsUntilExpiration);
            data.WriteBits(GuildName.Length, 7);
            data.WriteBits(Comment.Length, 10);
            data.FlushBits();
            data.WriteString(GuildName);
            data.WriteString(Comment);
        }

        public ObjectGuid GuildGUID { get; set; }
        public uint GuildVirtualRealm { get; set; }
        public string GuildName { get; set; } = "";
        public int ClassRoles { get; set; }
        public int PlayStyle { get; set; }
        public int Availability { get; set; }
        public uint SecondsSinceCreated { get; set; }
        public uint SecondsUntilExpiration { get; set; }
        public string Comment { get; set; } = "";
    }

    class GuildPostData
    {
        public void Write(WorldPacket data)
        {
            data.WriteBit(Active);
            data.WriteBits(Comment.Length, 10);
            data.WriteInt32(PlayStyle);
            data.WriteInt32(Availability);
            data.WriteInt32(ClassRoles);
            data.WriteInt32(LevelRange);
            data.WriteUInt32(SecondsRemaining);
            data.WriteString(Comment);
        }

        public bool Active { get; set; }
        public int PlayStyle { get; set; }
        public int Availability { get; set; }
        public int ClassRoles { get; set; }
        public int LevelRange { get; set; }
        public long SecondsRemaining { get; set; }
        public string Comment { get; set; } = "";
    }

    class LFGuildRecruitData
    {
        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(RecruitGUID);
            data.WriteUInt32(RecruitVirtualRealm);
            data.WriteInt32(CharacterClass);
            data.WriteInt32(CharacterGender);
            data.WriteInt32(CharacterLevel);
            data.WriteInt32(ClassRoles);
            data.WriteInt32(PlayStyle);
            data.WriteInt32(Availability);
            data.WriteUInt32(SecondsSinceCreated);
            data.WriteUInt32(SecondsUntilExpiration);
            data.WriteBits(Name.Length, 6);
            data.WriteBits(Comment.Length, 10);
            data.FlushBits();
            data.WriteString(Name);
            data.WriteString(Comment);
        }

        public ObjectGuid RecruitGUID { get; set; }
        public string Name { get; set; } = "";
        public uint RecruitVirtualRealm { get; set; }
        public string Comment { get; set; } = "";
        public int CharacterClass { get; set; }
        public int CharacterGender { get; set; }
        public int CharacterLevel { get; set; }
        public int ClassRoles { get; set; }
        public int PlayStyle { get; set; }
        public int Availability { get; set; }
        public uint SecondsSinceCreated { get; set; }
        public uint SecondsUntilExpiration { get; set; }
    }
}
