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

using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Chat
{
    public class Channel
    {
        public Channel(uint channelId, Team team = 0, AreaTableRecord zoneEntry = null)
        {
            _channelFlags = ChannelFlags.General;
            _channelId = channelId;
            _channelTeam = team;
            _zoneEntry = zoneEntry;

            ChatChannelsRecord channelEntry = CliDB.ChatChannelsStorage.LookupByKey(channelId);
            if (channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.Trade))             // for trade channel
                _channelFlags |= ChannelFlags.Trade;

            if (channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.CityOnly2))        // for city only channels
                _channelFlags |= ChannelFlags.City;

            if (channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.Lfg))               // for LFG channel
                _channelFlags |= ChannelFlags.Lfg;
            else                                                // for all other channels
                _channelFlags |= ChannelFlags.NotLfg;
        }

        public Channel(string name, Team team = 0)
        {
            _announceEnabled = true;
            _ownershipEnabled = true;
            _channelFlags = ChannelFlags.Custom;
            _channelTeam = team;
            _channelName = name;

            // If storing custom channels in the db is enabled either load or save the channel
            if (WorldConfig.GetBoolValue(WorldCfg.PreserveCustomChannels))
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHANNEL);
                stmt.AddValue(0, _channelName);
                stmt.AddValue(1, _channelTeam);
                SQLResult result = DB.Characters.Query(stmt);

                if (!result.IsEmpty()) //load
                {
                    _channelName = result.Read<string>(0); // re-get channel name. MySQL table collation is case insensitive
                    _announceEnabled = result.Read<bool>(1);
                    _ownershipEnabled = result.Read<bool>(2);
                    _channelPassword = result.Read<string>(3);
                    string bannedList = result.Read<string>(4);

                    if (string.IsNullOrEmpty(bannedList))
                    {
                        var tokens = new StringArray(bannedList, ' ');
                        for (var i = 0; i < tokens.Length; ++i)
                        {
                            ObjectGuid bannedGuid = new ObjectGuid();
                            if (ulong.TryParse(tokens[i].Substring(0, 16), out ulong highguid) && ulong.TryParse(tokens[i].Substring(16), out ulong lowguid))
                                bannedGuid.SetRawValue(highguid, lowguid);

                            if (!bannedGuid.IsEmpty())
                            {
                                Log.outDebug(LogFilter.ChatSystem, "Channel({0}) loaded bannedStore guid:{1}", _channelName, bannedGuid);
                                _bannedStore.Add(bannedGuid);
                            }
                        }
                    }
                }
                else // save
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHANNEL);
                    stmt.AddValue(0, _channelName);
                    stmt.AddValue(1, _channelTeam);
                    DB.Characters.Execute(stmt);
                    Log.outDebug(LogFilter.ChatSystem, "Channel({0}) saved in database", _channelName);
                }

                _persistentChannel = true;
            }
        }

        public static void GetChannelName(ref string channelName, uint channelId, LocaleConstant locale, AreaTableRecord zoneEntry)
        {
            if (channelId != 0)
            {
                ChatChannelsRecord channelEntry = CliDB.ChatChannelsStorage.LookupByKey(channelId);
                if (!channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.Global))
                {
                    if (channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.CityOnly))
                        channelName = string.Format(channelEntry.Name[locale].ConvertFormatSyntax(), Global.ObjectMgr.GetCypherString(CypherStrings.ChannelCity, locale));
                    else
                        channelName = string.Format(channelEntry.Name[locale].ConvertFormatSyntax(), zoneEntry.AreaName[locale]);
                }
                else
                    channelName = channelEntry.Name[locale];
            }
        }

        public string GetName(LocaleConstant locale = LocaleConstant.enUS)
        {
            string result = _channelName;
            GetChannelName(ref result, _channelId, locale, _zoneEntry);

            return result;
        }

        void UpdateChannelInDB()
        {
            if (_persistentChannel)
            {
                string banlist = "";
                foreach (var iter in _bannedStore)
                    banlist += iter.GetRawValue().ToHexString() + ' ';

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHANNEL);
                stmt.AddValue(0, _announceEnabled);
                stmt.AddValue(1, _ownershipEnabled);
                stmt.AddValue(2, _channelPassword);
                stmt.AddValue(3, banlist);
                stmt.AddValue(4, _channelName);
                stmt.AddValue(5, _channelTeam);
                DB.Characters.Execute(stmt);

                Log.outDebug(LogFilter.ChatSystem, "Channel({0}) updated in database", _channelName);
            }
        }

        void UpdateChannelUseageInDB()
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHANNEL_USAGE);
            stmt.AddValue(0, _channelName);
            stmt.AddValue(1, _channelTeam);
            DB.Characters.Execute(stmt);
        }

        public static void CleanOldChannelsInDB()
        {
            if (WorldConfig.GetIntValue(WorldCfg.PreserveCustomChannelDuration) > 0)
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_OLD_CHANNELS);
                stmt.AddValue(0, WorldConfig.GetIntValue(WorldCfg.PreserveCustomChannelDuration) * Time.Day);
                DB.Characters.Execute(stmt);

                Log.outDebug(LogFilter.ChatSystem, "Cleaned out unused custom chat channels.");
            }
        }

        public void JoinChannel(Player player, string pass)
        {
            ObjectGuid guid = player.GetGUID();
            if (IsOn(guid))
            {
                // Do not send error message for built-in channels
                if (!IsConstant())
                {
                    var builder = new ChannelNameBuilder(this, new PlayerAlreadyMemberAppend(guid));
                    SendToOne(builder, guid);
                }
                return;
            }

            if (IsBanned(guid))
            {
                var builder = new ChannelNameBuilder(this, new BannedAppend());
                SendToOne(builder, guid);
                return;
            }

            if (!string.IsNullOrEmpty(_channelPassword) && pass != _channelPassword)
            {
                var builder = new ChannelNameBuilder(this, new WrongPasswordAppend());
                SendToOne(builder, guid);
                return;
            }

            if (HasFlag(ChannelFlags.Lfg) && WorldConfig.GetBoolValue(WorldCfg.RestrictedLfgChannel) &&
                Global.AccountMgr.IsPlayerAccount(player.GetSession().GetSecurity()) && //FIXME: Move to RBAC
                player.GetGroup())
            {
                var builder = new ChannelNameBuilder(this, new NotInLFGAppend());
                SendToOne(builder, guid);
                return;
            }

            player.JoinedChannel(this);

            if (_announceEnabled && !player.GetSession().HasPermission(RBACPermissions.SilentlyJoinChannel))
            {
                var builder = new ChannelNameBuilder(this, new JoinedAppend(guid));
                SendToAll(builder);
            }

            bool newChannel = _playersStore.Empty();

            PlayerInfo playerInfo = new PlayerInfo();
            playerInfo.SetInvisible(!player.isGMVisible());
            _playersStore[guid] = playerInfo;

            /*
             ChannelNameBuilder<YouJoinedAppend> builder = new ChannelNameBuilder(this, new YouJoinedAppend());
             SendToOne(builder, guid);
            */

            SendToOne(new ChannelNotifyJoinedBuilder(this), guid);

            JoinNotify(player);

            // Custom channel handling
            if (!IsConstant())
            {
                // Update last_used timestamp in db
                if (!_playersStore.Empty())
                    UpdateChannelUseageInDB();

                // If the channel has no owner yet and ownership is allowed, set the new owner.
                // or if the owner was a GM with .gm visible off
                // don't do this if the new player is, too, an invis GM, unless the channel was empty
                if (_ownershipEnabled && (newChannel || !playerInfo.IsInvisible()) && (_ownerGuid.IsEmpty() || _isOwnerInvisible))
                {
                    _isOwnerInvisible = playerInfo.IsInvisible();

                    SetOwner(guid, !newChannel && !_isOwnerInvisible);
                    _playersStore[guid].SetModerator(true);
                }
            }
        }

        public void LeaveChannel(Player player, bool send = true, bool suspend = false)
        {
            ObjectGuid guid = player.GetGUID();
            if (!IsOn(guid))
            {
                if (send)
                {
                    var builder = new ChannelNameBuilder(this, new NotMemberAppend());
                    SendToOne(builder, guid);
                }
                return;
            }

            player.LeftChannel(this);

            if (send)
            {
                /*
                ChannelNameBuilder<YouLeftAppend> builder = new ChannelNameBuilder(this, new YouLeftAppend());
                SendToOne(builder, guid);
                */

                SendToOne(new ChannelNotifyLeftBuilder(this, suspend), guid);
            }

            PlayerInfo info = _playersStore.LookupByKey(guid);
            bool changeowner = info.IsOwner();
            _playersStore.Remove(guid);

            if (_announceEnabled && !player.GetSession().HasPermission(RBACPermissions.SilentlyJoinChannel))
            {
                var builder = new ChannelNameBuilder(this, new LeftAppend(guid));
                SendToAll(builder);
            }

            LeaveNotify(player);

            if (!IsConstant())
            {
                // Update last_used timestamp in db
                UpdateChannelUseageInDB();

                // If the channel owner left and there are still playersStore inside, pick a new owner
                // do not pick invisible gm owner unless there are only invisible gms in that channel (rare)
                if (changeowner && _ownershipEnabled && !_playersStore.Empty())
                {
                    ObjectGuid newowner = ObjectGuid.Empty;
                    foreach (var key in _playersStore.Keys)
                    {
                        if (!_playersStore[key].IsInvisible())
                        {
                            newowner = key;
                            break;
                        }
                    }

                    if (newowner.IsEmpty())
                        newowner = _playersStore.First().Key;

                    _playersStore[newowner].SetModerator(true);

                    SetOwner(newowner);

                    // if the new owner is invisible gm, set flag to automatically choose a new owner
                    if (_playersStore[newowner].IsInvisible())
                        _isOwnerInvisible = true;
                }
            }
        }

        void KickOrBan(Player player, string badname, bool ban)
        {
            ObjectGuid good = player.GetGUID();

            if (!IsOn(good))
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new NotMemberAppend());
                SendToOne(builder, good);
                return;
            }

            PlayerInfo info = _playersStore.LookupByKey(good);
            if (!info.IsModerator() && !player.GetSession().HasPermission(RBACPermissions.ChangeChannelNotModerator))
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new NotModeratorAppend());
                SendToOne(builder, good);
                return;
            }

            Player bad = Global.ObjAccessor.FindPlayerByName(badname);
            ObjectGuid victim = bad ? bad.GetGUID() : ObjectGuid.Empty;
            if (victim.IsEmpty() || !IsOn(victim))
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new PlayerNotFoundAppend(badname));
                SendToOne(builder, good);
                return;
            }

            bool changeowner = _ownerGuid == victim;

            if (!player.GetSession().HasPermission(RBACPermissions.ChangeChannelNotModerator) && changeowner && good != _ownerGuid)
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new NotOwnerAppend());
                SendToOne(builder, good);
                return;
            }

            if (ban && !IsBanned(victim))
            {
                _bannedStore.Add(victim);
                UpdateChannelInDB();

                if (!player.GetSession().HasPermission(RBACPermissions.SilentlyJoinChannel))
                {
                    ChannelNameBuilder builder = new ChannelNameBuilder(this, new PlayerBannedAppend(good, victim));
                    SendToAll(builder);
                }

            }
            else if (!player.GetSession().HasPermission(RBACPermissions.SilentlyJoinChannel))
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new PlayerKickedAppend(good, victim));
                SendToAll(builder);
            }

            _playersStore.Remove(victim);
            bad.LeftChannel(this);

            if (changeowner && _ownershipEnabled && !_playersStore.Empty())
            {
                info.SetModerator(true);
                SetOwner(good);
            }
        }

        public void UnBan(Player player, string badname)
        {
            ObjectGuid good = player.GetGUID();

            if (!IsOn(good))
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new NotMemberAppend());
                SendToOne(builder, good);
                return;
            }

            PlayerInfo info = _playersStore.LookupByKey(good);
            if (!info.IsModerator() && !player.GetSession().HasPermission(RBACPermissions.ChangeChannelNotModerator))
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new NotModeratorAppend());
                SendToOne(builder, good);
                return;
            }

            Player bad = Global.ObjAccessor.FindPlayerByName(badname);
            ObjectGuid victim = bad ? bad.GetGUID() : ObjectGuid.Empty;

            if (victim.IsEmpty() || !IsBanned(victim))
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new PlayerNotFoundAppend(badname));
                SendToOne(builder, good);
                return;
            }

            _bannedStore.Remove(victim);

            ChannelNameBuilder builder1 = new ChannelNameBuilder(this, new PlayerUnbannedAppend(good, victim));
            SendToAll(builder1);

            UpdateChannelInDB();
        }

        public void Password(Player player, string pass)
        {
            ObjectGuid guid = player.GetGUID();

            if (!IsOn(guid))
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new NotMemberAppend());
                SendToOne(builder, guid);
                return;
            }

            PlayerInfo info = _playersStore.LookupByKey(guid);
            if (!info.IsModerator() && !player.GetSession().HasPermission(RBACPermissions.ChangeChannelNotModerator))
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new NotModeratorAppend());
                SendToOne(builder, guid);
                return;
            }

            _channelPassword = pass;

            ChannelNameBuilder builder1 = new ChannelNameBuilder(this, new PasswordChangedAppend(guid));
            SendToAll(builder1);

            UpdateChannelInDB();
        }

        void SetMode(Player player, string p2n, bool mod, bool set)
        {
            ObjectGuid guid = player.GetGUID();

            if (!IsOn(guid))
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new NotMemberAppend());
                SendToOne(builder, guid);
                return;
            }

            PlayerInfo info = _playersStore.LookupByKey(guid);
            if (!info.IsModerator() && !player.GetSession().HasPermission(RBACPermissions.ChangeChannelNotModerator))
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new NotModeratorAppend());
                SendToOne(builder, guid);
                return;
            }

            if (guid == _ownerGuid && p2n == player.GetName() && mod)
                return;

            Player newp = Global.ObjAccessor.FindPlayerByName(p2n);
            ObjectGuid victim = newp ? newp.GetGUID() : ObjectGuid.Empty;

            if (victim.IsEmpty() || !IsOn(victim) ||
                (player.GetTeam() != newp.GetTeam() &&
            (!player.GetSession().HasPermission(RBACPermissions.TwoSideInteractionChannel) ||
            !newp.GetSession().HasPermission(RBACPermissions.TwoSideInteractionChannel))))
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new PlayerNotFoundAppend(p2n));
                SendToOne(builder, guid);
                return;
            }

            if (_ownerGuid == victim && _ownerGuid != guid)
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new NotOwnerAppend());
                SendToOne(builder, guid);
                return;
            }

            if (mod)
                SetModerator(newp.GetGUID(), set);
            else
                SetMute(newp.GetGUID(), set);
        }

        public void SetInvisible(Player player, bool on)
        {
            var playerInfo = _playersStore.LookupByKey(player.GetGUID());
            if (playerInfo == null)
                return;

            playerInfo.SetInvisible(on);

            // we happen to be owner too, update flag
            if (_ownerGuid == player.GetGUID())
                _isOwnerInvisible = on;
        }

        public void SetOwner(Player player, string newname)
        {
            ObjectGuid guid = player.GetGUID();

            if (!IsOn(guid))
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new NotMemberAppend());
                SendToOne(builder, guid);
                return;
            }
            if (!player.GetSession().HasPermission(RBACPermissions.ChangeChannelNotModerator) && guid != _ownerGuid)
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new NotOwnerAppend());
                SendToOne(builder, guid);
                return;
            }

            Player newp = Global.ObjAccessor.FindPlayerByName(newname);
            ObjectGuid victim = newp ? newp.GetGUID() : ObjectGuid.Empty;

            if (victim.IsEmpty() || !IsOn(victim) ||
                (player.GetTeam() != newp.GetTeam() &&
            (!player.GetSession().HasPermission(RBACPermissions.TwoSideInteractionChannel) ||
            !newp.GetSession().HasPermission(RBACPermissions.TwoSideInteractionChannel))))
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new PlayerNotFoundAppend(newname));
                SendToOne(builder, guid);
                return;
            }

            _playersStore[victim].SetModerator(true);
            SetOwner(victim);
        }

        public void SendWhoOwner(Player player)
        {
            ObjectGuid guid = player.GetGUID();
            if (IsOn(guid))
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new ChannelOwnerAppend(this, _ownerGuid));
                SendToOne(builder, guid);
            }
            else
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new NotMemberAppend());
                SendToOne(builder, guid);
            }
        }

        public void List(Player player)
        {
            ObjectGuid guid = player.GetGUID();

            if (!IsOn(guid))
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new NotMemberAppend());
                SendToOne(builder, guid);
                return;
            }

            string channelName = GetName(player.GetSession().GetSessionDbcLocale());
            Log.outDebug(LogFilter.ChatSystem, "SMSG_CHANNEL_LIST {0} Channel: {1}", player.GetSession().GetPlayerInfo(), channelName);

            ChannelListResponse list = new ChannelListResponse();
            list.Display = true; // always true?
            list.Channel = channelName;
            list.ChannelFlags = GetFlags();

            uint gmLevelInWhoList = WorldConfig.GetUIntValue(WorldCfg.GmLevelInWhoList);

            foreach (var pair in _playersStore)
            {
                Player member = Global.ObjAccessor.FindConnectedPlayer(pair.Key);

                // PLAYER can't see MODERATOR, GAME MASTER, ADMINISTRATOR characters
                // MODERATOR, GAME MASTER, ADMINISTRATOR can see all
                if (member && (player.GetSession().HasPermission(RBACPermissions.WhoSeeAllSecLevels) ||
                    member.GetSession().GetSecurity() <= (AccountTypes)gmLevelInWhoList) &&
                    member.IsVisibleGloballyFor(player))
                {
                    list.Members.Add(new ChannelListResponse.ChannelPlayer(pair.Key, Global.WorldMgr.GetVirtualRealmAddress(), pair.Value.GetFlags()));
                }
            }

            player.SendPacket(list);
        }

        public void Announce(Player player)
        {
            ObjectGuid guid = player.GetGUID();

            if (!IsOn(guid))
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new NotMemberAppend());
                SendToOne(builder, guid);
                return;
            }

            PlayerInfo playerInfo = _playersStore.LookupByKey(guid);
            if (!playerInfo.IsModerator() && !player.GetSession().HasPermission(RBACPermissions.ChangeChannelNotModerator))
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new NotModeratorAppend());
                SendToOne(builder, guid);
                return;
            }

            _announceEnabled = !_announceEnabled;

            if (_announceEnabled)
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new AnnouncementsOnAppend(guid));
                SendToAll(builder);
            }
            else
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new AnnouncementsOffAppend(guid));
                SendToAll(builder);
            }

            UpdateChannelInDB();
        }

        public void Say(ObjectGuid guid, string what, Language lang)
        {
            if (string.IsNullOrEmpty(what))
                return;

            // TODO: Add proper RBAC check
            if (WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionChannel))
                lang = Language.Universal;

            if (!IsOn(guid))
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new NotMemberAppend());
                SendToOne(builder, guid);
                return;
            }

            PlayerInfo playerInfo = _playersStore.LookupByKey(guid);
            if (playerInfo.IsMuted())
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new MutedAppend());
                SendToOne(builder, guid);
                return;
            }

            SendToAll(new ChannelSayBuilder(this, lang, what, guid), !playerInfo.IsModerator() ? guid : ObjectGuid.Empty);
        }

        public void AddonSay(ObjectGuid guid, string prefix, string what, bool isLogged)
        {
            if (what.IsEmpty())
                return;

            if (!IsOn(guid))
            {
                NotMemberAppend appender;
                ChannelNameBuilder builder = new ChannelNameBuilder(this, appender);
                SendToOne(builder, guid);
                return;
            }

            PlayerInfo playerInfo = _playersStore.LookupByKey(guid);
            if (playerInfo.IsMuted())
            {
                MutedAppend appender;
                ChannelNameBuilder builder = new ChannelNameBuilder(this, appender);
                SendToOne(builder, guid);
                return;
            }

            SendToAllWithAddon(new ChannelWhisperBuilder(this, isLogged ? Language.AddonLogged : Language.Addon, what, prefix, guid), prefix, !playerInfo.IsModerator() ? guid : ObjectGuid.Empty);
        }

        public void Invite(Player player, string newname)
        {
            ObjectGuid guid = player.GetGUID();

            if (!IsOn(guid))
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new NotMemberAppend());
                SendToOne(builder, guid);
                return;
            }

            Player newp = Global.ObjAccessor.FindPlayerByName(newname);
            if (!newp || !newp.isGMVisible())
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new PlayerNotFoundAppend(newname));
                SendToOne(builder, guid);
                return;
            }

            if (IsBanned(newp.GetGUID()))
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new PlayerInviteBannedAppend(newname));
                SendToOne(builder, guid);
                return;
            }

            if (newp.GetTeam() != player.GetTeam() &&
                (!player.GetSession().HasPermission(RBACPermissions.TwoSideInteractionChannel) ||
                !newp.GetSession().HasPermission(RBACPermissions.TwoSideInteractionChannel)))
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new InviteWrongFactionAppend());
                SendToOne(builder, guid);
                return;
            }

            if (IsOn(newp.GetGUID()))
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new PlayerAlreadyMemberAppend(newp.GetGUID()));
                SendToOne(builder, guid);
                return;
            }

            if (!newp.GetSocial().HasIgnore(guid))
            {
                ChannelNameBuilder builder = new ChannelNameBuilder(this, new InviteAppend(guid));
                SendToOne(builder, newp.GetGUID());
            }

            ChannelNameBuilder builder1 = new ChannelNameBuilder(this, new PlayerInvitedAppend(newp.GetName()));
            SendToOne(builder1, guid);
        }

        public void SetOwner(ObjectGuid guid, bool exclaim = true)
        {
            if (!_ownerGuid.IsEmpty())
            {
                // [] will re-add player after it possible removed
                var playerInfo = _playersStore.LookupByKey(_ownerGuid);
                if (playerInfo != null)
                    playerInfo.SetOwner(false);
            }

            _ownerGuid = guid;
            if (!_ownerGuid.IsEmpty())
            {
                ChannelMemberFlags oldFlag = GetPlayerFlags(_ownerGuid);
                var playerInfo = _playersStore.LookupByKey(_ownerGuid);
                if (playerInfo == null)
                    return;

                playerInfo.SetModerator(true);
                playerInfo.SetOwner(true);

                ChannelNameBuilder builder = new ChannelNameBuilder(this, new ModeChangeAppend(_ownerGuid, oldFlag, GetPlayerFlags(_ownerGuid)));
                SendToAll(builder);

                if (exclaim)
                {
                    ChannelNameBuilder ownerChangedBuilder = new ChannelNameBuilder(this, new OwnerChangedAppend(_ownerGuid));
                    SendToAll(ownerChangedBuilder);
                }

                UpdateChannelInDB();
            }
        }

        public void SilenceAll(Player player, string name) { }

        public void UnsilenceAll(Player player, string name) { }

        public void DeclineInvite(Player player) { }

        void JoinNotify(Player player)
        {
            ObjectGuid guid = player.GetGUID();

            if (IsConstant())
                SendToAllButOne(new ChannelUserlistAddBuilder(this, guid), guid);
            else
                SendToAll(new ChannelUserlistUpdateBuilder(this, guid));
        }

        void LeaveNotify(Player player)
        {
            ObjectGuid guid = player.GetGUID();

            var builder = new ChannelUserlistRemoveBuilder(this, guid);

            if (IsConstant())
                SendToAllButOne(builder, guid);
            else
                SendToAll(builder);
        }

        void SetModerator(ObjectGuid guid, bool set)
        {
            if (!IsOn(guid))
                return;

            PlayerInfo playerInfo = _playersStore.LookupByKey(guid);
            if (playerInfo.IsModerator() != set)
            {
                ChannelMemberFlags oldFlag = _playersStore[guid].GetFlags();
                playerInfo.SetModerator(set);

                ChannelNameBuilder builder = new ChannelNameBuilder(this, new ModeChangeAppend(guid, oldFlag, playerInfo.GetFlags()));
                SendToAll(builder);
            }
        }

        void SetMute(ObjectGuid guid, bool set)
        {
            if (!IsOn(guid))
                return;

            PlayerInfo playerInfo = _playersStore.LookupByKey(guid);
            if (playerInfo.IsMuted() != set)
            {
                ChannelMemberFlags oldFlag = _playersStore[guid].GetFlags();
                playerInfo.SetMuted(set);

                ChannelNameBuilder builder = new ChannelNameBuilder(this, new ModeChangeAppend(guid, oldFlag, playerInfo.GetFlags()));
                SendToAll(builder);
            }
        }

        void SendToAll(MessageBuilder builder, ObjectGuid guid = default(ObjectGuid))
        {
            LocalizedPacketDo localizer = new LocalizedPacketDo(builder);

            foreach (var pair in _playersStore)
            {
                Player player = Global.ObjAccessor.FindConnectedPlayer(pair.Key);
                if (player)
                    if (guid.IsEmpty() || !player.GetSocial().HasIgnore(guid))
                        localizer.Invoke(player);
            }
        }

        void SendToAllButOne(MessageBuilder builder, ObjectGuid who)
        {
            LocalizedPacketDo localizer = new LocalizedPacketDo(builder);

            foreach (var pair in _playersStore)
            {
                if (pair.Key != who)
                {
                    Player player = Global.ObjAccessor.FindConnectedPlayer(pair.Key);
                    if (player)
                        localizer.Invoke(player);
                }
            }
        }

        void SendToOne(MessageBuilder builder, ObjectGuid who)
        {
            LocalizedPacketDo localizer = new LocalizedPacketDo(builder);

            Player player = Global.ObjAccessor.FindConnectedPlayer(who);
            if (player)
                localizer.Invoke(player);
        }

        void SendToAllWithAddon(MessageBuilder builder, string addonPrefix, ObjectGuid guid = default(ObjectGuid))
        {
            LocalizedPacketDo localizer = new LocalizedPacketDo(builder);

            foreach (var pair in _playersStore)
            {
                Player player = Global.ObjAccessor.FindConnectedPlayer(pair.Key);
                if (player)
                    if (player.GetSession().IsAddonRegistered(addonPrefix) && (guid.IsEmpty() || !player.GetSocial().HasIgnore(guid)))
                        localizer.Invoke(player);
            }
        }

        public uint GetChannelId() { return _channelId; }
        public bool IsConstant() { return _channelId != 0; }

        public bool IsLFG() { return GetFlags().HasAnyFlag(ChannelFlags.Lfg); }
        bool IsAnnounce() { return _announceEnabled; }
        void SetAnnounce(bool nannounce) { _announceEnabled = nannounce; }

        string GetPassword() { return _channelPassword; }
        void SetPassword(string npassword) { _channelPassword = npassword; }

        public uint GetNumPlayers() { return (uint)_playersStore.Count; }

        public ChannelFlags GetFlags() { return _channelFlags; }
        bool HasFlag(ChannelFlags flag) { return _channelFlags.HasAnyFlag(flag); }

        public AreaTableRecord GetZoneEntry() { return _zoneEntry; }

        public void Kick(Player player, string badname) { KickOrBan(player, badname, false); }
        public void Ban(Player player, string badname) { KickOrBan(player, badname, true); }

        public void SetModerator(Player player, string newname) { SetMode(player, newname, true, true); }
        public void UnsetModerator(Player player, string newname) { SetMode(player, newname, true, false); }
        public void SetMute(Player player, string newname) { SetMode(player, newname, false, true); }
        public void UnsetMute(Player player, string newname) { SetMode(player, newname, false, false); }

        public void SetOwnership(bool ownership) { _ownershipEnabled = ownership; }

        bool IsOn(ObjectGuid who) { return _playersStore.ContainsKey(who); }
        bool IsBanned(ObjectGuid guid) { return _bannedStore.Contains(guid); }

        public ChannelMemberFlags GetPlayerFlags(ObjectGuid guid)
        {
            var info = _playersStore.LookupByKey(guid);
            return info != null ? info.GetFlags() : 0;
        }

        bool _announceEnabled;
        bool _ownershipEnabled;
        bool _persistentChannel;
        bool _isOwnerInvisible;

        ChannelFlags _channelFlags;
        uint _channelId;
        Team _channelTeam;
        ObjectGuid _ownerGuid;
        string _channelName;
        string _channelPassword;
        Dictionary<ObjectGuid, PlayerInfo> _playersStore = new Dictionary<ObjectGuid, PlayerInfo>();
        List<ObjectGuid> _bannedStore = new List<ObjectGuid>();

        AreaTableRecord _zoneEntry;

        public class PlayerInfo
        {
            public ChannelMemberFlags GetFlags() { return flags; }

            public bool IsInvisible() { return _invisible; }
            public void SetInvisible(bool on) { _invisible = on; }

            public bool HasFlag(ChannelMemberFlags flag) { return flags.HasAnyFlag(flag); }

            public void SetFlag(ChannelMemberFlags flag) { flags |= flag; }

            public void RemoveFlag(ChannelMemberFlags flag) { flags &= ~flag; }

            public bool IsOwner() { return HasFlag(ChannelMemberFlags.Owner); }

            public void SetOwner(bool state)
            {
                if (state)
                    SetFlag(ChannelMemberFlags.Owner);
                else
                    RemoveFlag(ChannelMemberFlags.Owner);
            }

            public bool IsModerator() { return HasFlag(ChannelMemberFlags.Moderator); }

            public void SetModerator(bool state)
            {
                if (state)
                    SetFlag(ChannelMemberFlags.Moderator);
                else
                    RemoveFlag(ChannelMemberFlags.Moderator);
            }

            public bool IsMuted() { return HasFlag(ChannelMemberFlags.Muted); }

            public void SetMuted(bool state)
            {
                if (state)
                    SetFlag(ChannelMemberFlags.Muted);
                else
                    RemoveFlag(ChannelMemberFlags.Muted);
            }

            ChannelMemberFlags flags;
            bool _invisible;
        }
    }
}
