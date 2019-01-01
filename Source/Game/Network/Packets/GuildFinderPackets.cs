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
using Game.Entities;
using System;
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

        public ObjectGuid GuildGUID;
        public uint Availability;
        public uint ClassRoles;
        public uint PlayStyle;
        public string Comment;
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

        public uint CharacterLevel;
        public uint Availability;
        public uint ClassRoles;
        public uint PlayStyle;
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

        public List<LFGuildBrowseData> Post = new List<LFGuildBrowseData>();
    }

    class LFGuildDeclineRecruit : ClientPacket
    {
        public LFGuildDeclineRecruit(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            RecruitGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid RecruitGUID;
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

        public List<LFGuildApplicationData> Application = new List<LFGuildApplicationData>();
        public int NumRemaining;
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

        public uint LastUpdate;
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

        public List<LFGuildRecruitData> Recruits = new List<LFGuildRecruitData>();
        public long UpdateTime;
    }

    class LFGuildRemoveRecruit : ClientPacket
    {
        public LFGuildRemoveRecruit(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            GuildGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid GuildGUID;
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

        public uint Availability;
        public uint PlayStyle;
        public uint ClassRoles;
        public uint LevelRange;
        public bool Active;
        public string Comment;
    }

    //Structs

    class LFGuildBrowseData
    {
        public void Write(WorldPacket data)
        {
            data.WriteBits(GuildName.GetByteCount(), 7);
            data.WriteBits(Comment.GetByteCount(), 10);
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

        public string GuildName = "";
        public ObjectGuid GuildGUID;
        public uint GuildVirtualRealm;
        public int GuildMembers;
        public uint GuildAchievementPoints;
        public int PlayStyle;
        public int Availability;
        public int ClassRoles;
        public int LevelRange;
        public uint EmblemStyle;
        public uint EmblemColor;
        public uint BorderStyle;
        public uint BorderColor;
        public uint Background;
        public string Comment = "";
        public sbyte Cached;
        public sbyte MembershipRequested;
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
            data.WriteBits(GuildName.GetByteCount(), 7);
            data.WriteBits(Comment.GetByteCount(), 10);
            data.FlushBits();
            data.WriteString(GuildName);
            data.WriteString(Comment);
        }

        public ObjectGuid GuildGUID;
        public uint GuildVirtualRealm;
        public string GuildName = "";
        public int ClassRoles;
        public int PlayStyle;
        public int Availability;
        public uint SecondsSinceCreated;
        public uint SecondsUntilExpiration;
        public string Comment = "";
    }

    class GuildPostData
    {
        public void Write(WorldPacket data)
        {
            data.WriteBit(Active);
            data.WriteBits(Comment.GetByteCount(), 10);
            data.WriteInt32(PlayStyle);
            data.WriteInt32(Availability);
            data.WriteInt32(ClassRoles);
            data.WriteInt32(LevelRange);
            data.WriteInt32(SecondsRemaining);
            data.WriteString(Comment);
        }

        public bool Active;
        public int PlayStyle;
        public int Availability;
        public int ClassRoles;
        public int LevelRange;
        public int SecondsRemaining;
        public string Comment = "";
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
            data.WriteBits(Name.GetByteCount(), 6);
            data.WriteBits(Comment.GetByteCount(), 10);
            data.FlushBits();
            data.WriteString(Name);
            data.WriteString(Comment);
        }

        public ObjectGuid RecruitGUID;
        public string Name = "";
        public uint RecruitVirtualRealm;
        public string Comment = "";
        public int CharacterClass;
        public int CharacterGender;
        public int CharacterLevel;
        public int ClassRoles;
        public int PlayStyle;
        public int Availability;
        public uint SecondsSinceCreated;
        public uint SecondsUntilExpiration;
    }
}
