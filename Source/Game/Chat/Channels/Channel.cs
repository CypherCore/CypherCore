// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Chat
{
    public class Channel
    {
        public Channel(ObjectGuid guid, uint channelId, Team team = 0, AreaTableRecord zoneEntry = null)
        {
            _channelFlags = ChannelFlags.General;
            _channelId = channelId;
            _channelTeam = team;
            _channelGuid = guid;
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

        public Channel(ObjectGuid guid, string name, Team team = 0, string banList = "")
        {
            _announceEnabled = true;
            _ownershipEnabled = true;
            _channelFlags = ChannelFlags.Custom;
            _channelTeam = team;
            _channelGuid = guid;
            _channelName = name;

            StringArray tokens = new(banList, ' ');
            foreach (string token in tokens)
            {
                // legacy db content might not have 0x prefix, account for that
                string bannedGuidStr = token.Contains("0x") ? token.Substring(2) : token;
                ObjectGuid banned = new();
                banned.SetRawValue(ulong.Parse(bannedGuidStr.Substring(0, 16)), ulong.Parse(bannedGuidStr.Substring(16)));
                if (banned.IsEmpty())
                    continue;

                Log.outDebug(LogFilter.ChatSystem, $"Channel({name}) loaded player {banned} into bannedStore");
                _bannedStore.Add(banned);
            }
        }

        public static void GetChannelName(ref string channelName, uint channelId, Locale locale, AreaTableRecord zoneEntry)
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

        public string GetName(Locale locale = Locale.enUS)
        {
            string result = _channelName;
            GetChannelName(ref result, _channelId, locale, _zoneEntry);

            return result;
        }

        public void UpdateChannelInDB()
        {
            long now = GameTime.GetGameTime();
            if (_isDirty)
            {
                string banlist = "";
                foreach (var iter in _bannedStore)
                    banlist += iter.GetRawValue().ToHexString() + ' ';

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHANNEL);
                stmt.AddValue(0, _channelName);
                stmt.AddValue(1, (uint)_channelTeam);
                stmt.AddValue(2, _announceEnabled);
                stmt.AddValue(3, _ownershipEnabled);
                stmt.AddValue(4, _channelPassword);
                stmt.AddValue(5, banlist);
                DB.Characters.Execute(stmt);
            }
            else if (_nextActivityUpdateTime <= now)
            {
                if (!_playersStore.Empty())
                {
                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHANNEL_USAGE);
                    stmt.AddValue(0, _channelName);
                    stmt.AddValue(1, (uint)_channelTeam);
                    DB.Characters.Execute(stmt);
                }
            }
            else
                return;

            _isDirty = false;
            _nextActivityUpdateTime = now + RandomHelper.URand(1 * Time.Minute, 6 * Time.Minute) * Math.Max(1u, WorldConfig.GetUIntValue(WorldCfg.PreserveCustomChannelInterval));
        }

        public void JoinChannel(Player player, string pass = "")
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

            if (!CheckPassword(pass))
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
            if (newChannel)
                _nextActivityUpdateTime = 0; // force activity update on next channel tick

            PlayerInfo playerInfo = new();
            playerInfo.SetInvisible(!player.IsGMVisible());
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
                ChannelNameBuilder builder = new(this, new NotMemberAppend());
                SendToOne(builder, good);
                return;
            }

            PlayerInfo info = _playersStore.LookupByKey(good);
            if (!info.IsModerator() && !player.GetSession().HasPermission(RBACPermissions.ChangeChannelNotModerator))
            {
                ChannelNameBuilder builder = new(this, new NotModeratorAppend());
                SendToOne(builder, good);
                return;
            }

            Player bad = Global.ObjAccessor.FindPlayerByName(badname);
            ObjectGuid victim = bad ? bad.GetGUID() : ObjectGuid.Empty;
            if (bad == null || victim.IsEmpty() || !IsOn(victim))
            {
                ChannelNameBuilder builder = new(this, new PlayerNotFoundAppend(badname));
                SendToOne(builder, good);
                return;
            }

            bool changeowner = _ownerGuid == victim;

            if (!player.GetSession().HasPermission(RBACPermissions.ChangeChannelNotModerator) && changeowner && good != _ownerGuid)
            {
                ChannelNameBuilder builder = new(this, new NotOwnerAppend());
                SendToOne(builder, good);
                return;
            }

            if (ban && !IsBanned(victim))
            {
                _bannedStore.Add(victim);
                _isDirty = true;

                if (!player.GetSession().HasPermission(RBACPermissions.SilentlyJoinChannel))
                {
                    ChannelNameBuilder builder = new(this, new PlayerBannedAppend(good, victim));
                    SendToAll(builder);
                }

            }
            else if (!player.GetSession().HasPermission(RBACPermissions.SilentlyJoinChannel))
            {
                ChannelNameBuilder builder = new(this, new PlayerKickedAppend(good, victim));
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
                ChannelNameBuilder builder = new(this, new NotMemberAppend());
                SendToOne(builder, good);
                return;
            }

            PlayerInfo info = _playersStore.LookupByKey(good);
            if (!info.IsModerator() && !player.GetSession().HasPermission(RBACPermissions.ChangeChannelNotModerator))
            {
                ChannelNameBuilder builder = new(this, new NotModeratorAppend());
                SendToOne(builder, good);
                return;
            }

            Player bad = Global.ObjAccessor.FindPlayerByName(badname);
            ObjectGuid victim = bad ? bad.GetGUID() : ObjectGuid.Empty;

            if (victim.IsEmpty() || !IsBanned(victim))
            {
                ChannelNameBuilder builder = new(this, new PlayerNotFoundAppend(badname));
                SendToOne(builder, good);
                return;
            }

            _bannedStore.Remove(victim);

            ChannelNameBuilder builder1 = new(this, new PlayerUnbannedAppend(good, victim));
            SendToAll(builder1);

            _isDirty = true;
        }

        public void Password(Player player, string pass)
        {
            ObjectGuid guid = player.GetGUID();

            if (!IsOn(guid))
            {
                ChannelNameBuilder builder = new(this, new NotMemberAppend());
                SendToOne(builder, guid);
                return;
            }

            PlayerInfo info = _playersStore.LookupByKey(guid);
            if (!info.IsModerator() && !player.GetSession().HasPermission(RBACPermissions.ChangeChannelNotModerator))
            {
                ChannelNameBuilder builder = new(this, new NotModeratorAppend());
                SendToOne(builder, guid);
                return;
            }

            _channelPassword = pass;

            ChannelNameBuilder builder1 = new(this, new PasswordChangedAppend(guid));
            SendToAll(builder1);

            _isDirty = true;
        }

        void SetMode(Player player, string p2n, bool mod, bool set)
        {
            ObjectGuid guid = player.GetGUID();

            if (!IsOn(guid))
            {
                ChannelNameBuilder builder = new(this, new NotMemberAppend());
                SendToOne(builder, guid);
                return;
            }

            PlayerInfo info = _playersStore.LookupByKey(guid);
            if (!info.IsModerator() && !player.GetSession().HasPermission(RBACPermissions.ChangeChannelNotModerator))
            {
                ChannelNameBuilder builder = new(this, new NotModeratorAppend());
                SendToOne(builder, guid);
                return;
            }

            if (guid == _ownerGuid && p2n == player.GetName() && mod)
                return;

            Player newp = Global.ObjAccessor.FindPlayerByName(p2n);
            ObjectGuid victim = newp ? newp.GetGUID() : ObjectGuid.Empty;

            if (newp == null || victim.IsEmpty() || !IsOn(victim) ||
                (player.GetTeam() != newp.GetTeam() &&
            (!player.GetSession().HasPermission(RBACPermissions.TwoSideInteractionChannel) ||
            !newp.GetSession().HasPermission(RBACPermissions.TwoSideInteractionChannel))))
            {
                ChannelNameBuilder builder = new(this, new PlayerNotFoundAppend(p2n));
                SendToOne(builder, guid);
                return;
            }

            if (_ownerGuid == victim && _ownerGuid != guid)
            {
                ChannelNameBuilder builder = new(this, new NotOwnerAppend());
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
                ChannelNameBuilder builder = new(this, new NotMemberAppend());
                SendToOne(builder, guid);
                return;
            }
            if (!player.GetSession().HasPermission(RBACPermissions.ChangeChannelNotModerator) && guid != _ownerGuid)
            {
                ChannelNameBuilder builder = new(this, new NotOwnerAppend());
                SendToOne(builder, guid);
                return;
            }

            Player newp = Global.ObjAccessor.FindPlayerByName(newname);
            ObjectGuid victim = newp ? newp.GetGUID() : ObjectGuid.Empty;

            if (newp == null || victim.IsEmpty() || !IsOn(victim) ||
                (player.GetTeam() != newp.GetTeam() &&
            (!player.GetSession().HasPermission(RBACPermissions.TwoSideInteractionChannel) ||
            !newp.GetSession().HasPermission(RBACPermissions.TwoSideInteractionChannel))))
            {
                ChannelNameBuilder builder = new(this, new PlayerNotFoundAppend(newname));
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
                ChannelNameBuilder builder = new(this, new ChannelOwnerAppend(this, _ownerGuid));
                SendToOne(builder, guid);
            }
            else
            {
                ChannelNameBuilder builder = new(this, new NotMemberAppend());
                SendToOne(builder, guid);
            }
        }

        public void List(Player player)
        {
            ObjectGuid guid = player.GetGUID();

            if (!IsOn(guid))
            {
                ChannelNameBuilder builder = new(this, new NotMemberAppend());
                SendToOne(builder, guid);
                return;
            }

            string channelName = GetName(player.GetSession().GetSessionDbcLocale());
            Log.outDebug(LogFilter.ChatSystem, "SMSG_CHANNEL_LIST {0} Channel: {1}", player.GetSession().GetPlayerInfo(), channelName);

            ChannelListResponse list = new();
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
                ChannelNameBuilder builder = new(this, new NotMemberAppend());
                SendToOne(builder, guid);
                return;
            }

            PlayerInfo playerInfo = _playersStore.LookupByKey(guid);
            if (!playerInfo.IsModerator() && !player.GetSession().HasPermission(RBACPermissions.ChangeChannelNotModerator))
            {
                ChannelNameBuilder builder = new(this, new NotModeratorAppend());
                SendToOne(builder, guid);
                return;
            }

            _announceEnabled = !_announceEnabled;

            if (_announceEnabled)
            {
                ChannelNameBuilder builder = new(this, new AnnouncementsOnAppend(guid));
                SendToAll(builder);
            }
            else
            {
                ChannelNameBuilder builder = new(this, new AnnouncementsOffAppend(guid));
                SendToAll(builder);
            }

            _isDirty = true;
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
                ChannelNameBuilder builder = new(this, new NotMemberAppend());
                SendToOne(builder, guid);
                return;
            }

            PlayerInfo playerInfo = _playersStore.LookupByKey(guid);
            if (playerInfo.IsMuted())
            {
                ChannelNameBuilder builder = new(this, new MutedAppend());
                SendToOne(builder, guid);
                return;
            }

            Player player = Global.ObjAccessor.FindConnectedPlayer(guid);
            SendToAll(new ChannelSayBuilder(this, lang, what, guid, _channelGuid), !playerInfo.IsModerator() ? guid : ObjectGuid.Empty, !playerInfo.IsModerator() && player ? player.GetSession().GetAccountGUID() : ObjectGuid.Empty);
        }

        public void AddonSay(ObjectGuid guid, string prefix, string what, bool isLogged)
        {
            if (what.IsEmpty())
                return;

            if (!IsOn(guid))
            {
                NotMemberAppend appender;
                ChannelNameBuilder builder = new(this, appender);
                SendToOne(builder, guid);
                return;
            }

            PlayerInfo playerInfo = _playersStore.LookupByKey(guid);
            if (playerInfo.IsMuted())
            {
                MutedAppend appender;
                ChannelNameBuilder builder = new(this, appender);
                SendToOne(builder, guid);
                return;
            }

            Player player = Global.ObjAccessor.FindConnectedPlayer(guid);
            SendToAllWithAddon(new ChannelWhisperBuilder(this, isLogged ? Language.AddonLogged : Language.Addon, what, prefix, guid), prefix, !playerInfo.IsModerator() ? guid : ObjectGuid.Empty,
                !playerInfo.IsModerator() && player ? player.GetSession().GetAccountGUID() : ObjectGuid.Empty);
        }

        public void Invite(Player player, string newname)
        {
            ObjectGuid guid = player.GetGUID();

            if (!IsOn(guid))
            {
                ChannelNameBuilder builder = new(this, new NotMemberAppend());
                SendToOne(builder, guid);
                return;
            }

            Player newp = Global.ObjAccessor.FindPlayerByName(newname);
            if (!newp || !newp.IsGMVisible())
            {
                ChannelNameBuilder builder = new(this, new PlayerNotFoundAppend(newname));
                SendToOne(builder, guid);
                return;
            }

            if (IsBanned(newp.GetGUID()))
            {
                ChannelNameBuilder builder = new(this, new PlayerInviteBannedAppend(newname));
                SendToOne(builder, guid);
                return;
            }

            if (newp.GetTeam() != player.GetTeam() &&
                (!player.GetSession().HasPermission(RBACPermissions.TwoSideInteractionChannel) ||
                !newp.GetSession().HasPermission(RBACPermissions.TwoSideInteractionChannel)))
            {
                ChannelNameBuilder builder = new(this, new InviteWrongFactionAppend());
                SendToOne(builder, guid);
                return;
            }

            if (IsOn(newp.GetGUID()))
            {
                ChannelNameBuilder builder = new(this, new PlayerAlreadyMemberAppend(newp.GetGUID()));
                SendToOne(builder, guid);
                return;
            }

            if (!newp.GetSocial().HasIgnore(guid, player.GetSession().GetAccountGUID()))
            {
                ChannelNameBuilder builder = new(this, new InviteAppend(guid));
                SendToOne(builder, newp.GetGUID());
            }

            ChannelNameBuilder builder1 = new(this, new PlayerInvitedAppend(newp.GetName()));
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

                ChannelNameBuilder builder = new(this, new ModeChangeAppend(_ownerGuid, oldFlag, GetPlayerFlags(_ownerGuid)));
                SendToAll(builder);

                if (exclaim)
                {
                    ChannelNameBuilder ownerBuilder = new(this, new OwnerChangedAppend(_ownerGuid));
                    SendToAll(ownerBuilder);
                }

                _isDirty = true;
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

                ChannelNameBuilder builder = new(this, new ModeChangeAppend(guid, oldFlag, playerInfo.GetFlags()));
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

                ChannelNameBuilder builder = new(this, new ModeChangeAppend(guid, oldFlag, playerInfo.GetFlags()));
                SendToAll(builder);
            }
        }

        void SendToAll(MessageBuilder builder, ObjectGuid guid = default, ObjectGuid accountGuid = default)
        {
            LocalizedDo localizer = new(builder);

            foreach (var pair in _playersStore)
            {
                Player player = Global.ObjAccessor.FindConnectedPlayer(pair.Key);
                if (player)
                    if (guid.IsEmpty() || !player.GetSocial().HasIgnore(guid, accountGuid))
                        localizer.Invoke(player);
            }
        }

        void SendToAllButOne(MessageBuilder builder, ObjectGuid who)
        {
            LocalizedDo localizer = new(builder);

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
            LocalizedDo localizer = new(builder);

            Player player = Global.ObjAccessor.FindConnectedPlayer(who);
            if (player)
                localizer.Invoke(player);
        }

        void SendToAllWithAddon(MessageBuilder builder, string addonPrefix, ObjectGuid guid = default, ObjectGuid accountGuid = default)
        {
            LocalizedDo localizer = new(builder);

            foreach (var pair in _playersStore)
            {
                Player player = Global.ObjAccessor.FindConnectedPlayer(pair.Key);
                if (player)
                    if (player.GetSession().IsAddonRegistered(addonPrefix) && (guid.IsEmpty() || !player.GetSocial().HasIgnore(guid, accountGuid)))
                        localizer.Invoke(player);
            }
        }

        public uint GetChannelId() { return _channelId; }
        public bool IsConstant() { return _channelId != 0; }

        public ObjectGuid GetGUID() { return _channelGuid; }
        
        public bool IsLFG() { return GetFlags().HasAnyFlag(ChannelFlags.Lfg); }
        bool IsAnnounce() { return _announceEnabled; }
        public void SetAnnounce(bool announce) { _announceEnabled = announce; }

        // will be saved to DB on next channel save interval
        public void SetDirty() { _isDirty = true; }

        public void SetPassword(string npassword) { _channelPassword = npassword; }
        public bool CheckPassword(string password) { return _channelPassword.IsEmpty() || (_channelPassword == password); }
        
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

        bool _isDirty; // whether the channel needs to be saved to DB
        long _nextActivityUpdateTime;

        bool _announceEnabled;
        bool _ownershipEnabled;
        bool _isOwnerInvisible;

        ChannelFlags _channelFlags;
        uint _channelId;
        Team _channelTeam;
        ObjectGuid _channelGuid;
        ObjectGuid _ownerGuid;
        string _channelName;
        string _channelPassword;
        Dictionary<ObjectGuid, PlayerInfo> _playersStore = new();
        List<ObjectGuid> _bannedStore = new();

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
