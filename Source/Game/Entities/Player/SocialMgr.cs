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
using Framework.Database;
using Game.Network;
using Game.Network.Packets;
using System;
using System.Collections.Generic;

namespace Game.Entities
{
    public class SocialManager : Singleton<SocialManager>
    {
        SocialManager() { }

        public const int FriendLimit = 50;
        public const int IgnoreLimit = 50;

        public void GetFriendInfo(Player player, ObjectGuid friendGUID, FriendInfo friendInfo)
        {
            if (!player)
                return;

            friendInfo.Status = FriendStatus.Offline;
            friendInfo.Area = 0;
            friendInfo.Level = 0;
            friendInfo.Class = 0;

            Player target = Global.ObjAccessor.FindPlayer(friendGUID);
            if (!target)
                return;

            var playerFriendInfo = player.GetSocial()._playerSocialMap.LookupByKey(friendGUID);
            if (playerFriendInfo != null)
                friendInfo.Note = playerFriendInfo.Note;

            // PLAYER see his team only and PLAYER can't see MODERATOR, GAME MASTER, ADMINISTRATOR characters
            // MODERATOR, GAME MASTER, ADMINISTRATOR can see all

            if (!player.GetSession().HasPermission(RBACPermissions.WhoSeeAllSecLevels) &&
                target.GetSession().GetSecurity() > (AccountTypes)WorldConfig.GetIntValue(WorldCfg.GmLevelInWhoList))
                return;

            // player can see member of other team only if CONFIG_ALLOW_TWO_SIDE_WHO_LIST
            if (target.GetTeam() != player.GetTeam() && !player.GetSession().HasPermission(RBACPermissions.TwoSideWhoList))
                return;

            if (target.IsVisibleGloballyFor(player))
            {
                if (target.isDND())
                    friendInfo.Status = FriendStatus.DND;
                else if (target.isAFK())
                    friendInfo.Status = FriendStatus.AFK;
                else
                    friendInfo.Status = FriendStatus.Online;

                friendInfo.Area = target.GetZoneId();
                friendInfo.Level = target.getLevel();
                friendInfo.Class = target.GetClass();
            }
        }

        public void SendFriendStatus(Player player, FriendsResult result, ObjectGuid friendGuid, bool broadcast = false)
        {
            FriendInfo fi = new FriendInfo();
            GetFriendInfo(player, friendGuid, fi);

            FriendStatusPkt friendStatus = new FriendStatusPkt();
            friendStatus.Initialize(friendGuid, result, fi);

            if (broadcast)
                BroadcastToFriendListers(player, friendStatus);
            else
                player.SendPacket(friendStatus);
        }

        void BroadcastToFriendListers(Player player, ServerPacket packet)
        {
            if (!player)
                return;

            AccountTypes gmSecLevel = (AccountTypes)WorldConfig.GetIntValue(WorldCfg.GmLevelInWhoList);
            foreach (var pair in _socialMap)
            {
                var info = pair.Value._playerSocialMap.LookupByKey(player.GetGUID());
                if (info != null && info.Flags.HasAnyFlag(SocialFlag.Friend))
                {
                    Player target = Global.ObjAccessor.FindPlayer(pair.Key);
                    if (!target || !target.IsInWorld)
                        continue;

                    WorldSession session = target.GetSession();
                    if (!session.HasPermission(RBACPermissions.WhoSeeAllSecLevels) && player.GetSession().GetSecurity() > gmSecLevel)
                        continue;

                    if (target.GetTeam() != player.GetTeam() && !session.HasPermission(RBACPermissions.TwoSideWhoList))
                        continue;

                    if (player.IsVisibleGloballyFor(target))
                        session.SendPacket(packet);
                }
            }
        }

        public PlayerSocial LoadFromDB(SQLResult result, ObjectGuid guid)
        {
            PlayerSocial social = new PlayerSocial();
            social.SetPlayerGUID(guid);

            if (!result.IsEmpty())
            {
                do
                {
                    ObjectGuid friendGuid = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(0));
                    ObjectGuid friendAccountGuid = ObjectGuid.Create(HighGuid.WowAccount, result.Read<uint>(1));
                    SocialFlag flags = (SocialFlag)result.Read<byte>(2);

                    social._playerSocialMap[friendGuid] = new FriendInfo(friendAccountGuid, flags, result.Read<string>(3));
                }
                while (result.NextRow());
            }

            _socialMap[guid] = social;

            return social;
        }

        public void RemovePlayerSocial(ObjectGuid guid) { _socialMap.Remove(guid); }

        Dictionary<ObjectGuid, PlayerSocial> _socialMap = new Dictionary<ObjectGuid, PlayerSocial>();
    }

    public class PlayerSocial
    {
        uint GetNumberOfSocialsWithFlag(SocialFlag flag)
        {
            uint counter = 0;
            foreach (var pair in _playerSocialMap)
                if (pair.Value.Flags.HasAnyFlag(flag))
                    ++counter;

            return counter;
        }

        public bool AddToSocialList(ObjectGuid friendGuid, SocialFlag flag)
        {
            // check client limits
            if (GetNumberOfSocialsWithFlag(flag) >= (((flag & SocialFlag.Friend) != 0) ? SocialManager.FriendLimit : SocialManager.IgnoreLimit))
                return false;
            
            var friendInfo = _playerSocialMap.LookupByKey(friendGuid);
            if (friendInfo != null)
            {
                friendInfo.Flags |= flag;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHARACTER_SOCIAL_FLAGS);
                stmt.AddValue(0, friendInfo.Flags);
                stmt.AddValue(1, GetPlayerGUID().GetCounter());
                stmt.AddValue(2, friendGuid.GetCounter());
                DB.Characters.Execute(stmt);
            }
            else
            {
                FriendInfo fi = new FriendInfo();
                fi.Flags |= flag;
                _playerSocialMap[friendGuid] = fi;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_SOCIAL);
                stmt.AddValue(0, GetPlayerGUID().GetCounter());
                stmt.AddValue(1, friendGuid.GetCounter());
                stmt.AddValue(2, flag);
                DB.Characters.Execute(stmt);
            }
            return true;
        }

        public void RemoveFromSocialList(ObjectGuid friendGuid, SocialFlag flag)
        {
            var friendInfo = _playerSocialMap.LookupByKey(friendGuid);
            if (friendInfo == null)                     // not exist
                return;

            friendInfo.Flags &= ~flag;

            if (friendInfo.Flags == 0)
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_SOCIAL);
                stmt.AddValue(0, GetPlayerGUID().GetCounter());
                stmt.AddValue(1, friendGuid.GetCounter());
                DB.Characters.Execute(stmt);

                _playerSocialMap.Remove(friendGuid);
            }
            else
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHARACTER_SOCIAL_FLAGS);
                stmt.AddValue(0, friendInfo.Flags);
                stmt.AddValue(1, GetPlayerGUID().GetCounter());
                stmt.AddValue(2, friendGuid.GetCounter());
                DB.Characters.Execute(stmt);
            }
        }

        public void SetFriendNote(ObjectGuid friendGuid, string note)
        {
            if (!_playerSocialMap.ContainsKey(friendGuid))                     // not exist
                return;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHARACTER_SOCIAL_NOTE);
            stmt.AddValue(0, note);
            stmt.AddValue(1, GetPlayerGUID().GetCounter());
            stmt.AddValue(2, friendGuid.GetCounter());
            DB.Characters.Execute(stmt);

            _playerSocialMap[friendGuid].Note = note;
        }

        public void SendSocialList(Player player, SocialFlag flags)
        {
            if (!player)
                return;

            ContactList contactList = new ContactList();
            contactList.Flags = flags;

            foreach (var v in _playerSocialMap)
            {
                if (!v.Value.Flags.HasAnyFlag(flags))
                    continue;

                Global.SocialMgr.GetFriendInfo(player, v.Key, v.Value);

                contactList.Contacts.Add(new ContactInfo(v.Key, v.Value));

                // client's friends list and ignore list limit
                if (contactList.Contacts.Count >= (((flags & SocialFlag.Friend) != 0) ? SocialManager.FriendLimit : SocialManager.IgnoreLimit))
                    break;
            }

            player.SendPacket(contactList);
        }

        bool _HasContact(ObjectGuid guid, SocialFlag flags)
        {
            var friendInfo = _playerSocialMap.LookupByKey(guid);
            if (friendInfo != null)
                return friendInfo.Flags.HasAnyFlag(flags);

            return false;
        }

        public bool HasFriend(ObjectGuid friendGuid)
        {
            return _HasContact(friendGuid, SocialFlag.Friend);
        }

        public bool HasIgnore(ObjectGuid ignoreGuid)
        {
            return _HasContact(ignoreGuid, SocialFlag.Ignored);
        }

        ObjectGuid GetPlayerGUID() { return m_playerGUID; }

        public void SetPlayerGUID(ObjectGuid guid) { m_playerGUID = guid; }

        public Dictionary<ObjectGuid, FriendInfo> _playerSocialMap = new Dictionary<ObjectGuid, FriendInfo>();
        ObjectGuid m_playerGUID;
    }

    public class FriendInfo
    {
        public FriendInfo()
        {
            Status = FriendStatus.Offline;
            Note = "";
        }

        public FriendInfo(ObjectGuid accountGuid, SocialFlag flags, string note)
        {
            WowAccountGuid = accountGuid;
            Status = FriendStatus.Offline;
            Flags = flags;
            Note = note;
        }

        public ObjectGuid WowAccountGuid;
        public FriendStatus Status;
        public SocialFlag Flags;
        public uint Area;
        public uint Level;
        public Class Class;
        public string Note;
    }

    public enum FriendStatus
    {
        Offline = 0x00,
        Online = 0x01,
        AFK = 0x02,
        DND = 0x04,
        RAF = 0x08
    }

    public enum SocialFlag
    {
        Friend = 0x01,
        Ignored = 0x02,
        Muted = 0x04,                          // guessed
        Unk = 0x08,                           // Unknown - does not appear to be RaF
        All = Friend | Ignored | Muted
    }

    public enum FriendsResult
    {
        DbError = 0x00,
        ListFull = 0x01,
        Online = 0x02,
        Offline = 0x03,
        NotFound = 0x04,
        Removed = 0x05,
        AddedOnline = 0x06,
        AddedOffline = 0x07,
        Already = 0x08,
        Self = 0x09,
        Enemy = 0x0a,
        IgnoreFull = 0x0b,
        IgnoreSelf = 0x0c,
        IgnoreNotFound = 0x0d,
        IgnoreAlready = 0x0e,
        IgnoreAdded = 0x0f,
        IgnoreRemoved = 0x10,
        IgnoreAmbiguous = 0x11,                         // That Name Is Ambiguous, Type More Of The Player'S Server Name
        MuteFull = 0x12,
        MuteSelf = 0x13,
        MuteNotFound = 0x14,
        MuteAlready = 0x15,
        MuteAdded = 0x16,
        MuteRemoved = 0x17,
        MuteAmbiguous = 0x18,                         // That Name Is Ambiguous, Type More Of The Player'S Server Name
        Unk1 = 0x19,                         // no message at client
        Unk2 = 0x1A,
        Unk3 = 0x1B,
        Unknown = 0x1C                          // Unknown friend response from server
    }
}
