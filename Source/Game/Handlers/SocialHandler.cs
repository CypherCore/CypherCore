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
using Game.DataStorage;
using Game.Entities;
using Game.Guilds;
using Game.Network;
using Game.Network.Packets;
using System;
using System.Collections.Generic;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.Who)]
        void HandleWho(WhoRequestPkt whoRequest)
        {
            WhoRequest request = whoRequest.Request;

            // zones count, client limit = 10 (2.0.10)
            // can't be received from real client or broken packet
            if (whoRequest.Areas.Count > 10)
                return;

            // user entered strings count, client limit=4 (checked on 2.0.10)
            // can't be received from real client or broken packet
            if (request.Words.Count > 4)
                return;

            // @todo: handle following packet values
            // VirtualRealmNames
            // ShowEnemies
            // ShowArenaPlayers
            // ExactName
            // ServerInfo

            request.Words.ForEach(p => p = p.ToLower());

            request.Name = request.Name.ToLower();
            request.Guild = request.Guild.ToLower();

            // client send in case not set max level value 100 but we support 255 max level,
            // update it to show GMs with characters after 100 level
            if (whoRequest.Request.MaxLevel >= 100)
                whoRequest.Request.MaxLevel = 255;

            var team = GetPlayer().GetTeam();

            uint gmLevelInWhoList = WorldConfig.GetUIntValue(WorldCfg.GmLevelInWhoList);

            WhoResponsePkt response = new WhoResponsePkt();
            foreach (var target in Global.ObjAccessor.GetPlayers())
            {
                // player can see member of other team only if CONFIG_ALLOW_TWO_SIDE_WHO_LIST
                if (target.GetTeam() != team && !HasPermission(RBACPermissions.TwoSideWhoList))
                    continue;

                // player can see MODERATOR, GAME MASTER, ADMINISTRATOR only if CONFIG_GM_IN_WHO_LIST
                if (target.GetSession().GetSecurity() > (AccountTypes)gmLevelInWhoList && !HasPermission(RBACPermissions.WhoSeeAllSecLevels))
                    continue;

                // do not process players which are not in world
                if (!target.IsInWorld)
                    continue;

                // check if target is globally visible for player
                if (!target.IsVisibleGloballyFor(GetPlayer()))
                    continue;

                // check if target's level is in level range
                uint lvl = target.getLevel();
                if (lvl < request.MinLevel || lvl > request.MaxLevel)
                    continue;

                // check if class matches classmask
                int class_ = (byte)target.GetClass();
                if (!Convert.ToBoolean(request.ClassFilter & (1 << class_)))
                    continue;

                // check if race matches racemask
                int race = (int)target.GetRace();
                if (!Convert.ToBoolean(request.RaceFilter & (1 << race)))
                    continue;

                if (!whoRequest.Areas.Empty())
                {
                    if (whoRequest.Areas.Contains((int)target.GetZoneId()))
                        continue;
                }

                string wTargetName = target.GetName().ToLower();
                if (!string.IsNullOrEmpty(request.Name) && !wTargetName.Equals(request.Name))
                    continue;

                string wTargetGuildName = "";
                Guild targetGuild = target.GetGuild();
                if (targetGuild)
                    wTargetGuildName = targetGuild.GetName().ToLower();

                if (!string.IsNullOrEmpty(request.Guild) && !wTargetGuildName.Equals(request.Guild))
                    continue;

                if (!request.Words.Empty())
                {
                    string aname = "";
                    AreaTableRecord areaEntry = CliDB.AreaTableStorage.LookupByKey(target.GetZoneId());
                    if (areaEntry != null)
                        aname = areaEntry.AreaName[GetSessionDbcLocale()].ToLower();

                    bool show = false;
                    for (int i = 0; i < request.Words.Count; ++i)
                    {
                        if (!string.IsNullOrEmpty(request.Words[i]))
                        {
                            if (wTargetName.Equals(request.Words[i]) ||
                                wTargetGuildName.Equals(request.Words[i]) || 
                                aname.Equals(request.Words[i]))
                            {
                                show = true;
                                break;
                            }
                        }
                    }

                    if (!show)
                        continue;
                }

                WhoEntry whoEntry = new WhoEntry();
                if (!whoEntry.PlayerData.Initialize(target.GetGUID(), target))
                    continue;

                if (targetGuild)
                {
                    whoEntry.GuildGUID = targetGuild.GetGUID();
                    whoEntry.GuildVirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
                    whoEntry.GuildName = targetGuild.GetName();
                }

                whoEntry.AreaID = (int)target.GetZoneId();
                whoEntry.IsGM = target.IsGameMaster();

                response.Response.Add(whoEntry);

                // 50 is maximum player count sent to client
                if (response.Response.Count >= 50)
                    break;
            }

            SendPacket(response);
        }

        [WorldPacketHandler(ClientOpcodes.WhoIs)]
        void HandleWhoIs(WhoIsRequest packet)
        {
            if (!HasPermission(RBACPermissions.OpcodeWhois))
            {
                SendNotification(CypherStrings.YouNotHavePermission);
                return;
            }

            if (!ObjectManager.NormalizePlayerName(ref packet.CharName))
            {
                SendNotification(CypherStrings.NeedCharacterName);
                return;
            }

            Player player = Global.ObjAccessor.FindPlayerByName(packet.CharName);
            if (!player)
            {
                SendNotification(CypherStrings.PlayerNotExistOrOffline, packet.CharName);
                return;
            }

            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_WHOIS);
            stmt.AddValue(0, player.GetSession().GetAccountId());

            SQLResult result = DB.Login.Query(stmt);
            if (result.IsEmpty())
            {
                SendNotification(CypherStrings.AccountForPlayerNotFound, packet.CharName);
                return;
            }

            string acc = result.Read<string>(0);
            if (string.IsNullOrEmpty(acc))
                acc = "Unknown";

            string email = result.Read<string>(1);
            if (string.IsNullOrEmpty(email))
                email = "Unknown";

            string lastip = result.Read<string>(2);
            if (string.IsNullOrEmpty(lastip))
                lastip = "Unknown";

            WhoIsResponse response = new WhoIsResponse();
            response.AccountName = packet.CharName + "'s " + "account is " + acc + ", e-mail: " + email + ", last ip: " + lastip;
            SendPacket(response);
        }

        [WorldPacketHandler(ClientOpcodes.SendContactList)]
        void HandleContactList(SendContactList packet)
        {
            GetPlayer().GetSocial().SendSocialList(GetPlayer(), packet.Flags);
        }

        [WorldPacketHandler(ClientOpcodes.AddFriend)]
        void HandleAddFriend(AddFriend packet)
        {
            if (!ObjectManager.NormalizePlayerName(ref packet.Name))
                return;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GUID_RACE_ACC_BY_NAME);
            stmt.AddValue(0, packet.Name);

            _queryProcessor.AddQuery(DB.Characters.AsyncQuery(stmt).WithCallback(HandleAddFriendCallBack, packet.Notes));
        }

        void HandleAddFriendCallBack(string friendNote, SQLResult result)
        {
            if (!GetPlayer())
                return;

            ObjectGuid friendGuid = ObjectGuid.Empty;
            FriendsResult friendResult = FriendsResult.NotFound;

            if (!result.IsEmpty())
            {
                ulong lowGuid = result.Read<ulong>(0);
                if (lowGuid != 0)
                {
                    friendGuid = ObjectGuid.Create(HighGuid.Player, lowGuid);
                    Team team = Player.TeamForRace((Race)result.Read<byte>(1));
                    uint friendAccountId = result.Read<uint>(2);

                    if (HasPermission(RBACPermissions.AllowGmFriend) || Global.AccountMgr.IsPlayerAccount(Global.AccountMgr.GetSecurity(friendAccountId, (int)Global.WorldMgr.GetRealm().Id.Realm)))
                    {
                        if (friendGuid == GetPlayer().GetGUID())
                            friendResult = FriendsResult.Self;
                        else if (GetPlayer().GetTeam() != team && !HasPermission(RBACPermissions.TwoSideAddFriend))
                            friendResult = FriendsResult.Enemy;
                        else if (GetPlayer().GetSocial().HasFriend(friendGuid))
                            friendResult = FriendsResult.Already;
                        else
                        {
                            Player playerFriend = Global.ObjAccessor.FindPlayer(friendGuid);
                            if (playerFriend && playerFriend.IsVisibleGloballyFor(GetPlayer()))
                                friendResult = FriendsResult.AddedOnline;
                            else
                                friendResult = FriendsResult.AddedOffline;

                            if (GetPlayer().GetSocial().AddToSocialList(friendGuid, SocialFlag.Friend))
                                GetPlayer().GetSocial().SetFriendNote(friendGuid, friendNote);
                            else
                                friendResult = FriendsResult.ListFull;
                        }
                    }
                }
            }

            Global.SocialMgr.SendFriendStatus(GetPlayer(), friendResult, friendGuid);
        }

        [WorldPacketHandler(ClientOpcodes.DelFriend)]
        void HandleDelFriend(DelFriend packet)
        {
            // @todo: handle VirtualRealmAddress
            GetPlayer().GetSocial().RemoveFromSocialList(packet.Player.Guid, SocialFlag.Friend);

            Global.SocialMgr.SendFriendStatus(GetPlayer(), FriendsResult.Removed, packet.Player.Guid);
        }

        [WorldPacketHandler(ClientOpcodes.AddIgnore)]
        void HandleAddIgnore(AddIgnore packet)
        {
            if (!ObjectManager.NormalizePlayerName(ref packet.Name))
                return;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GUID_BY_NAME);
            stmt.AddValue(0, packet.Name);

            _queryProcessor.AddQuery(DB.Characters.AsyncQuery(stmt).WithCallback(HandleAddIgnoreCallBack));
        }

        void HandleAddIgnoreCallBack(SQLResult result)
        {
            if (!GetPlayer())
                return;

            ObjectGuid IgnoreGuid = ObjectGuid.Empty;
            FriendsResult ignoreResult = FriendsResult.IgnoreNotFound;

            if (result.IsEmpty())
            {
                ulong lowGuid = result.Read<ulong>(0);
                if (lowGuid != 0)
                {
                    IgnoreGuid = ObjectGuid.Create(HighGuid.Player, lowGuid);

                    if (IgnoreGuid == GetPlayer().GetGUID())              //not add yourself
                        ignoreResult = FriendsResult.IgnoreSelf;
                    else if (GetPlayer().GetSocial().HasIgnore(IgnoreGuid))
                        ignoreResult = FriendsResult.IgnoreAlready;
                    else
                    {
                        ignoreResult = FriendsResult.IgnoreAdded;

                        // ignore list full
                        if (!GetPlayer().GetSocial().AddToSocialList(IgnoreGuid, SocialFlag.Ignored))
                            ignoreResult = FriendsResult.IgnoreFull;
                    }
                }
            }

            Global.SocialMgr.SendFriendStatus(GetPlayer(), ignoreResult, IgnoreGuid);
        }

        [WorldPacketHandler(ClientOpcodes.DelIgnore)]
        void HandleDelIgnore(DelIgnore packet)
        {
            // @todo: handle VirtualRealmAddress
            Log.outDebug(LogFilter.Network, "WorldSession.HandleDelIgnoreOpcode: {0}", packet.Player.Guid.ToString());

            GetPlayer().GetSocial().RemoveFromSocialList(packet.Player.Guid, SocialFlag.Ignored);

            Global.SocialMgr.SendFriendStatus(GetPlayer(), FriendsResult.IgnoreRemoved, packet.Player.Guid);
        }

        [WorldPacketHandler(ClientOpcodes.SetContactNotes)]
        void HandleSetContactNotes(SetContactNotes packet)
        {
            // @todo: handle VirtualRealmAddress
            Log.outDebug(LogFilter.Network, "WorldSession.HandleSetContactNotesOpcode: Contact: {0}, Notes: {1}", packet.Player.Guid.ToString(), packet.Notes);
            GetPlayer().GetSocial().SetFriendNote(packet.Player.Guid, packet.Notes);
        }
    }
}
