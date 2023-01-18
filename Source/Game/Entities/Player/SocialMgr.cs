// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.Networking;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Entities
{
    public class SocialManager : Singleton<SocialManager>
    {
        Dictionary<ObjectGuid, PlayerSocial> _socialMap = new();

        SocialManager() { }

        public const int FriendLimit = 50;
        public const int IgnoreLimit = 50;

        public static void GetFriendInfo(Player player, ObjectGuid friendGUID, FriendInfo friendInfo)
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

            var playerFriendInfo = player.GetSocial().PlayerSocialMap.LookupByKey(friendGUID);
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
                if (target.IsDND())
                    friendInfo.Status = FriendStatus.DND;
                else if (target.IsAFK())
                    friendInfo.Status = FriendStatus.AFK;
                else
                {
                    friendInfo.Status = FriendStatus.Online;
                    if (target.GetSession().GetRecruiterId() == player.GetSession().GetAccountId() || target.GetSession().GetAccountId() == player.GetSession().GetRecruiterId())
                        friendInfo.Status |= FriendStatus.RAF;
                }

                friendInfo.Area = target.GetZoneId();
                friendInfo.Level = target.GetLevel();
                friendInfo.Class = target.GetClass();
            }
        }

        public void SendFriendStatus(Player player, FriendsResult result, ObjectGuid friendGuid, bool broadcast = false)
        {
            FriendInfo fi = new();
            GetFriendInfo(player, friendGuid, fi);

            FriendStatusPkt friendStatus = new();
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
                var info = pair.Value.PlayerSocialMap.LookupByKey(player.GetGUID());
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
            PlayerSocial social = new();
            social.SetPlayerGUID(guid);

            if (!result.IsEmpty())
            {
                do
                {
                    ObjectGuid friendGuid = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(0));
                    ObjectGuid friendAccountGuid = ObjectGuid.Create(HighGuid.WowAccount, result.Read<uint>(1));
                    SocialFlag flags = (SocialFlag)result.Read<byte>(2);

                    social.PlayerSocialMap[friendGuid] = new FriendInfo(friendAccountGuid, flags, result.Read<string>(3));
                    if (flags.HasFlag(SocialFlag.Ignored))
                        social.IgnoredAccounts.Add(friendAccountGuid);
                }
                while (result.NextRow());
            }

            _socialMap[guid] = social;

            return social;
        }

        public void RemovePlayerSocial(ObjectGuid guid) { _socialMap.Remove(guid); }
    }

    public class PlayerSocial
    {
        public Dictionary<ObjectGuid, FriendInfo> PlayerSocialMap = new();
        public List<ObjectGuid> IgnoredAccounts = new();
        ObjectGuid m_playerGUID;

        uint GetNumberOfSocialsWithFlag(SocialFlag flag)
        {
            uint counter = 0;
            foreach (var pair in PlayerSocialMap)
                if (pair.Value.Flags.HasAnyFlag(flag))
                    ++counter;

            return counter;
        }

        public bool AddToSocialList(ObjectGuid friendGuid, ObjectGuid accountGuid, SocialFlag flag)
        {
            // check client limits
            if (GetNumberOfSocialsWithFlag(flag) >= (((flag & SocialFlag.Friend) != 0) ? SocialManager.FriendLimit : SocialManager.IgnoreLimit))
                return false;
            
            var friendInfo = PlayerSocialMap.LookupByKey(friendGuid);
            if (friendInfo != null)
            {
                friendInfo.Flags |= flag;
                friendInfo.WowAccountGuid = accountGuid;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHARACTER_SOCIAL_FLAGS);
                stmt.AddValue(0, (byte)friendInfo.Flags);
                stmt.AddValue(1, GetPlayerGUID().GetCounter());
                stmt.AddValue(2, friendGuid.GetCounter());
                DB.Characters.Execute(stmt);
            }
            else
            {
                FriendInfo fi = new();
                fi.Flags |= flag;
                fi.WowAccountGuid = accountGuid;
                PlayerSocialMap[friendGuid] = fi;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_SOCIAL);
                stmt.AddValue(0, GetPlayerGUID().GetCounter());
                stmt.AddValue(1, friendGuid.GetCounter());
                stmt.AddValue(2, (byte)flag);
                DB.Characters.Execute(stmt);
            }

            if (flag.HasFlag(SocialFlag.Ignored))
                IgnoredAccounts.Add(accountGuid);

            return true;
        }

        public void RemoveFromSocialList(ObjectGuid friendGuid, SocialFlag flag)
        {
            var friendInfo = PlayerSocialMap.LookupByKey(friendGuid);
            if (friendInfo == null)                     // not exist
                return;

            friendInfo.Flags &= ~flag;

            if (friendInfo.Flags == 0)
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_SOCIAL);
                stmt.AddValue(0, GetPlayerGUID().GetCounter());
                stmt.AddValue(1, friendGuid.GetCounter());
                DB.Characters.Execute(stmt);

                ObjectGuid accountGuid = friendInfo.WowAccountGuid;

                PlayerSocialMap.Remove(friendGuid);

                if (flag.HasFlag(SocialFlag.Ignored))
                {
                    var otherIgnoreForAccount = PlayerSocialMap.Any(social => social.Value.Flags.HasFlag(SocialFlag.Ignored) && social.Value.WowAccountGuid == accountGuid);
                    if (!otherIgnoreForAccount)
                        IgnoredAccounts.Remove(accountGuid);
                }
            }
            else
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHARACTER_SOCIAL_FLAGS);
                stmt.AddValue(0, (byte)friendInfo.Flags);
                stmt.AddValue(1, GetPlayerGUID().GetCounter());
                stmt.AddValue(2, friendGuid.GetCounter());
                DB.Characters.Execute(stmt);
            }
        }

        public void SetFriendNote(ObjectGuid friendGuid, string note)
        {
            if (!PlayerSocialMap.ContainsKey(friendGuid))                     // not exist
                return;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHARACTER_SOCIAL_NOTE);
            stmt.AddValue(0, note);
            stmt.AddValue(1, GetPlayerGUID().GetCounter());
            stmt.AddValue(2, friendGuid.GetCounter());
            DB.Characters.Execute(stmt);

            PlayerSocialMap[friendGuid].Note = note;
        }

        public void SendSocialList(Player player, SocialFlag flags)
        {
            if (!player)
                return;

            uint friendsCount = 0;
            uint ignoredCount = 0;

            ContactList contactList = new();
            contactList.Flags = flags;

            foreach (var v in PlayerSocialMap)
            {
                SocialFlag contactFlags = v.Value.Flags;
                if (!contactFlags.HasAnyFlag(flags))
                    continue;

                // Check client limit for friends list
                if (contactFlags.HasFlag(SocialFlag.Friend))
                    if (++friendsCount > SocialManager.FriendLimit)
                        continue;

                // Check client limit for ignore list
                if (contactFlags.HasFlag(SocialFlag.Ignored))
                    if (++ignoredCount > SocialManager.IgnoreLimit)
                        continue;

                SocialManager.GetFriendInfo(player, v.Key, v.Value);

                contactList.Contacts.Add(new ContactInfo(v.Key, v.Value));
            }

            player.SendPacket(contactList);
        }

        bool _HasContact(ObjectGuid guid, SocialFlag flags)
        {
            var friendInfo = PlayerSocialMap.LookupByKey(guid);
            if (friendInfo != null)
                return friendInfo.Flags.HasAnyFlag(flags);

            return false;
        }

        public bool HasFriend(ObjectGuid friendGuid)
        {
            return _HasContact(friendGuid, SocialFlag.Friend);
        }

        public bool HasIgnore(ObjectGuid ignoreGuid, ObjectGuid ignoreAccountGuid)
        {
            return _HasContact(ignoreGuid, SocialFlag.Ignored) || IgnoredAccounts.Contains(ignoreAccountGuid);
        }

        ObjectGuid GetPlayerGUID() { return m_playerGUID; }

        public void SetPlayerGUID(ObjectGuid guid) { m_playerGUID = guid; }
    }

    public class FriendInfo
    {
        public ObjectGuid WowAccountGuid;
        public FriendStatus Status;
        public SocialFlag Flags;
        public uint Area;
        public uint Level;
        public Class Class;
        public string Note;

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
