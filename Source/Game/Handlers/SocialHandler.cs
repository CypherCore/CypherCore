/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using Game.Cache;
using Game.DataStorage;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.Who, Processing = PacketProcessing.ThreadSafe)]
        private void HandleWho(WhoRequestPkt whoRequest)
        {
            var request = whoRequest.Request;

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

            var gmLevelInWhoList = WorldConfig.GetUIntValue(WorldCfg.GmLevelInWhoList);

            var response = new WhoResponsePkt();
            response.RequestID = whoRequest.RequestID;

            var whoList = Global.WhoListStorageMgr.GetWhoList();
            foreach (var target in whoList)
            {
                // player can see member of other team only if CONFIG_ALLOW_TWO_SIDE_WHO_LIST
                if (target.Team != team && !HasPermission(RBACPermissions.TwoSideWhoList))
                    continue;

                // player can see MODERATOR, GAME MASTER, ADMINISTRATOR only if CONFIG_GM_IN_WHO_LIST
                if (target.Security > (AccountTypes)gmLevelInWhoList && !HasPermission(RBACPermissions.WhoSeeAllSecLevels))
                    continue;

                // check if target is globally visible for player
                if (_player.GetGUID() != target.Guid && !target.IsVisible)
                    if (Global.AccountMgr.IsPlayerAccount(_player.GetSession().GetSecurity()) || target.Security > _player.GetSession().GetSecurity())
                        continue;

                // check if target's level is in level range
                var lvl = target.Level;
                if (lvl < request.MinLevel || lvl > request.MaxLevel)
                    continue;

                // check if class matches classmask
                if (!Convert.ToBoolean(request.ClassFilter & (1 << target.Class)))
                    continue;

                // check if race matches racemask
                if (!Convert.ToBoolean(request.RaceFilter & (1 << target.Race)))
                    continue;

                if (!whoRequest.Areas.Empty())
                {
                    if (whoRequest.Areas.Contains((int)target.ZoneId))
                        continue;
                }

                var wTargetName = target.PlayerName.ToLower();
                if (!(request.Name.IsEmpty() || wTargetName.Equals(request.Name)))
                    continue;

                var wTargetGuildName = target.GuildName.ToLower();
                if (!request.Guild.IsEmpty() && !wTargetGuildName.Equals(request.Guild))
                    continue;

                if (!request.Words.Empty())
                {
                    var aname = "";
                    var areaEntry = CliDB.AreaTableStorage.LookupByKey(target.ZoneId);
                    if (areaEntry != null)
                        aname = areaEntry.AreaName[GetSessionDbcLocale()].ToLower();

                    var show = false;
                    for (var i = 0; i < request.Words.Count; ++i)
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

                var whoEntry = new WhoEntry();
                if (!whoEntry.PlayerData.Initialize(target.Guid, null))
                    continue;

                if (!target.GuildGuid.IsEmpty())
                {
                    whoEntry.GuildGUID = target.GuildGuid;
                    whoEntry.GuildVirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
                    whoEntry.GuildName = target.GuildName;
                }

                whoEntry.AreaID = (int)target.ZoneId;
                whoEntry.IsGM = target.IsGamemaster;

                response.Response.Add(whoEntry);

                // 50 is maximum player count sent to client
                if (response.Response.Count >= 50)
                    break;
            }

            SendPacket(response);
        }

        [WorldPacketHandler(ClientOpcodes.WhoIs)]
        private void HandleWhoIs(WhoIsRequest packet)
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

            var stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_WHOIS);
            stmt.AddValue(0, player.GetSession().GetAccountId());

            var result = DB.Login.Query(stmt);
            if (result.IsEmpty())
            {
                SendNotification(CypherStrings.AccountForPlayerNotFound, packet.CharName);
                return;
            }

            var acc = result.Read<string>(0);
            if (string.IsNullOrEmpty(acc))
                acc = "Unknown";

            var email = result.Read<string>(1);
            if (string.IsNullOrEmpty(email))
                email = "Unknown";

            var lastip = result.Read<string>(2);
            if (string.IsNullOrEmpty(lastip))
                lastip = "Unknown";

            var response = new WhoIsResponse();
            response.AccountName = packet.CharName + "'s " + "account is " + acc + ", e-mail: " + email + ", last ip: " + lastip;
            SendPacket(response);
        }

        [WorldPacketHandler(ClientOpcodes.SendContactList)]
        private void HandleContactList(SendContactList packet)
        {
            GetPlayer().GetSocial().SendSocialList(GetPlayer(), packet.Flags);
        }

        [WorldPacketHandler(ClientOpcodes.AddFriend)]
        private void HandleAddFriend(AddFriend packet)
        {
            if (!ObjectManager.NormalizePlayerName(ref packet.Name))
                return;

            FriendsResult friendResult = FriendsResult.NotFound;
            var friendGuid = Global.CharacterCacheStorage.GetCharacterGuidByName(packet.Name);
            if (!friendGuid.IsEmpty())
            {
                var characterInfo = Global.CharacterCacheStorage.GetCharacterCacheByGuid(friendGuid);
                if (characterInfo != null)
                {
                    var team = Player.TeamForRace(characterInfo.RaceId);
                    var friendAccountId = characterInfo.AccountId;

                    if (HasPermission(RBACPermissions.AllowGmFriend) || Global.AccountMgr.IsPlayerAccount(Global.AccountMgr.GetSecurity(friendAccountId, (int)Global.WorldMgr.GetRealm().Id.Index)))
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
                                GetPlayer().GetSocial().SetFriendNote(friendGuid, packet.Notes);
                            else
                                friendResult = FriendsResult.ListFull;
                        }
                    }
                }
            }

            Global.SocialMgr.SendFriendStatus(GetPlayer(), friendResult, friendGuid);
        }

        [WorldPacketHandler(ClientOpcodes.DelFriend)]
        private void HandleDelFriend(DelFriend packet)
        {
            // @todo: handle VirtualRealmAddress
            GetPlayer().GetSocial().RemoveFromSocialList(packet.Player.Guid, SocialFlag.Friend);

            Global.SocialMgr.SendFriendStatus(GetPlayer(), FriendsResult.Removed, packet.Player.Guid);
        }

        [WorldPacketHandler(ClientOpcodes.AddIgnore)]
        private void HandleAddIgnore(AddIgnore packet)
        {
            if (!ObjectManager.NormalizePlayerName(ref packet.Name))
                return;

            var IgnoreGuid = Global.CharacterCacheStorage.GetCharacterGuidByName(packet.Name);
            FriendsResult ignoreResult = FriendsResult.IgnoreNotFound;
            if (IgnoreGuid.IsEmpty())
            {
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

            Global.SocialMgr.SendFriendStatus(GetPlayer(), ignoreResult, IgnoreGuid);
        }

        [WorldPacketHandler(ClientOpcodes.DelIgnore)]
        private void HandleDelIgnore(DelIgnore packet)
        {
            // @todo: handle VirtualRealmAddress
            Log.outDebug(LogFilter.Network, "WorldSession.HandleDelIgnoreOpcode: {0}", packet.Player.Guid.ToString());

            GetPlayer().GetSocial().RemoveFromSocialList(packet.Player.Guid, SocialFlag.Ignored);

            Global.SocialMgr.SendFriendStatus(GetPlayer(), FriendsResult.IgnoreRemoved, packet.Player.Guid);
        }

        [WorldPacketHandler(ClientOpcodes.SetContactNotes)]
        private void HandleSetContactNotes(SetContactNotes packet)
        {
            // @todo: handle VirtualRealmAddress
            Log.outDebug(LogFilter.Network, "WorldSession.HandleSetContactNotesOpcode: Contact: {0}, Notes: {1}", packet.Player.Guid.ToString(), packet.Notes);
            GetPlayer().GetSocial().SetFriendNote(packet.Player.Guid, packet.Notes);
        }
    }
}
