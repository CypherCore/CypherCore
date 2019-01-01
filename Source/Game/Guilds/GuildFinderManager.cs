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
using Game.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Guilds
{
    public class GuildFinderManager : Singleton<GuildFinderManager>
    {
        GuildFinderManager() { }

        public void LoadFromDB()
        {
            LoadGuildSettings();
            LoadMembershipRequests();
        }

        void LoadGuildSettings()
        {
            Log.outInfo(LogFilter.ServerLoading, "Loading guild finder guild-related settings...");
            //                                                           0                1             2                  3             4           5             6         7
            SQLResult result = DB.Characters.Query("SELECT gfgs.guildId, gfgs.availability, gfgs.classRoles, gfgs.interests, gfgs.level, gfgs.listed, gfgs.comment, c.race " +
                "FROM guild_finder_guild_settings gfgs LEFT JOIN guild_member gm ON gm.guildid=gfgs.guildId LEFT JOIN characters c ON c.guid = gm.guid LIMIT 1");

            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 guild finder guild-related settings. Table `guild_finder_guild_settings` is empty.");
                return;
            }

            uint count = 0;
            uint oldMSTime = Time.GetMSTime();
            do
            {
                ObjectGuid guildId = ObjectGuid.Create(HighGuid.Guild, result.Read<ulong>(0));
                byte availability = result.Read<byte>(1);
                byte classRoles = result.Read<byte>(2);
                byte interests = result.Read<byte>(3);
                byte level = result.Read<byte>(4);
                bool listed = (result.Read<byte>(5) != 0);
                string comment = result.Read<string>(6);

                uint guildTeam = TeamId.Alliance;
                ChrRacesRecord raceEntry = CliDB.ChrRacesStorage.LookupByKey(result.Read<byte>(7));
                if (raceEntry != null)
                    if (raceEntry.Alliance == 1)
                        guildTeam = TeamId.Horde;

                LFGuildSettings settings = new LFGuildSettings(listed, guildTeam, guildId, classRoles, availability, interests, level, comment);
                _guildSettings[guildId] = settings;

                ++count;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} guild finder guild-related settings in {1} ms.", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        void LoadMembershipRequests()
        {
            Log.outInfo(LogFilter.ServerLoading, "Loading guild finder membership requests...");
            //                                                      0         1           2            3           4         5         6
            SQLResult result = DB.Characters.Query("SELECT guildId, playerGuid, availability, classRole, interests, comment, submitTime FROM guild_finder_applicant");

            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 guild finder membership requests. Table `guild_finder_applicant` is empty.");
                return;
            }

            uint count = 0;
            uint oldMSTime = Time.GetMSTime();
            do
            {
                ObjectGuid guildId = ObjectGuid.Create(HighGuid.Guild, result.Read<ulong>(0));
                ObjectGuid playerId = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(1));
                byte availability = result.Read<byte>(2);
                byte classRoles = result.Read<byte>(3);
                byte interests = result.Read<byte>(4);
                string comment = result.Read<string>(5);
                uint submitTime = result.Read<uint>(6);

                MembershipRequest request = new MembershipRequest(playerId, guildId, availability, classRoles, interests, comment, submitTime);

                if (!_membershipRequestsByGuild.ContainsKey(guildId))
                    _membershipRequestsByGuild[guildId] = new Dictionary<ObjectGuid, MembershipRequest>();

                _membershipRequestsByGuild[guildId][playerId] = request;

                if (!_membershipRequestsByPlayer.ContainsKey(playerId))
                    _membershipRequestsByPlayer[playerId] = new Dictionary<ObjectGuid, MembershipRequest>();

                _membershipRequestsByPlayer[playerId][guildId] = request;

                ++count;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} guild finder membership requests in {1} ms.", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void AddMembershipRequest(ObjectGuid guildGuid, MembershipRequest request)
        {
            if (!_membershipRequestsByGuild.ContainsKey(guildGuid))
                _membershipRequestsByGuild[guildGuid] = new Dictionary<ObjectGuid, MembershipRequest>();
            _membershipRequestsByGuild[guildGuid][request.GetPlayerGUID()] = request;

            if (!_membershipRequestsByPlayer.ContainsKey(request.GetPlayerGUID()))
                _membershipRequestsByPlayer[request.GetPlayerGUID()] = new Dictionary<ObjectGuid, MembershipRequest>();
            _membershipRequestsByPlayer[request.GetPlayerGUID()][guildGuid] = request;

            SQLTransaction trans = new SQLTransaction();
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_GUILD_FINDER_APPLICANT);
            stmt.AddValue(0, request.GetGuildGuid().GetCounter());
            stmt.AddValue(1, request.GetPlayerGUID().GetCounter());
            stmt.AddValue(2, request.GetAvailability());
            stmt.AddValue(3, request.GetClassRoles());
            stmt.AddValue(4, request.GetInterests());
            stmt.AddValue(5, request.GetComment());
            stmt.AddValue(6, request.GetSubmitTime());
            trans.Append(stmt);
            DB.Characters.CommitTransaction(trans);

            // Notify the applicant his submittion has been added
            Player player = Global.ObjAccessor.FindPlayer(request.GetPlayerGUID());
            if (player)
                SendMembershipRequestListUpdate(player);

            // Notify the guild master and officers the list changed
            Guild guild = Global.GuildMgr.GetGuildById(guildGuid.GetCounter());
            if (guild)
                SendApplicantListUpdate(guild);
        }

        public void RemoveAllMembershipRequestsFromPlayer(ObjectGuid playerId)
        {
            var playerDic = _membershipRequestsByPlayer.LookupByKey(playerId);
            if (playerDic == null)
                return;
            
            SQLTransaction trans = new SQLTransaction();
            foreach (var guid in playerDic.Keys)
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_FINDER_APPLICANT);
                stmt.AddValue(0, guid.GetCounter());
                stmt.AddValue(1, playerId.GetCounter());
                trans.Append(stmt);


                // Notify the guild master and officers the list changed
                Guild guild = Global.GuildMgr.GetGuildByGuid(guid);
                if (guild)
                    SendApplicantListUpdate(guild);

                if (!_membershipRequestsByGuild.ContainsKey(guid))
                    continue;

                var guildDic = _membershipRequestsByGuild[guid];
                guildDic.Remove(playerId);
                if (guildDic.Empty())
                    _membershipRequestsByGuild.Remove(guid);
            }

            DB.Characters.CommitTransaction(trans);
            _membershipRequestsByPlayer.Remove(playerId);
        }

        public void RemoveMembershipRequest(ObjectGuid playerId, ObjectGuid guildId)
        {
            if (_membershipRequestsByGuild.ContainsKey(guildId))
            {
                var guildDic = _membershipRequestsByGuild[guildId];
                guildDic.Remove(playerId);
                if (guildDic.Empty())
                    _membershipRequestsByGuild.Remove(guildId);
            }

            var playerDic = _membershipRequestsByPlayer.LookupByKey(playerId);
            if (playerDic != null)
            {
                playerDic.Remove(guildId);
                if (playerDic.Empty())
                    _membershipRequestsByPlayer.Remove(playerId);
            }

            SQLTransaction trans = new SQLTransaction();
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_FINDER_APPLICANT);
            stmt.AddValue(0, guildId.GetCounter());
            stmt.AddValue(1, playerId.GetCounter());
            trans.Append(stmt);

            DB.Characters.CommitTransaction(trans);

            // Notify the applicant his submittion has been removed
            Player player = Global.ObjAccessor.FindPlayer(playerId);
            if (player)
                SendMembershipRequestListUpdate(player);

            // Notify the guild master and officers the list changed
            Guild guild = Global.GuildMgr.GetGuildByGuid(guildId);
            if (guild)
                SendApplicantListUpdate(guild);
        }

        public List<MembershipRequest> GetAllMembershipRequestsForPlayer(ObjectGuid playerGuid)
        {
            List<MembershipRequest> resultSet = new List<MembershipRequest>();
            var playerDic = _membershipRequestsByPlayer.LookupByKey(playerGuid);
            if (playerDic == null)
                return resultSet;

            foreach (var guildRequestPair in playerDic)
                resultSet.Add(guildRequestPair.Value);

            return resultSet;
        }

        public byte CountRequestsFromPlayer(ObjectGuid playerId)
        {
            return (byte)(_membershipRequestsByPlayer.ContainsKey(playerId) ? _membershipRequestsByPlayer[playerId].Count : 0);
        }

        public List<LFGuildSettings> GetGuildsMatchingSetting(LFGuildPlayer settings, uint faction)
        {
            List<LFGuildSettings> resultSet = new List<LFGuildSettings>();
            foreach (var guildSettings in _guildSettings.Values)
            {
                if (!guildSettings.IsListed())
                    continue;

                if (guildSettings.GetTeam() != faction)
                    continue;

                if (!Convert.ToBoolean(guildSettings.GetAvailability() & settings.GetAvailability()))
                    continue;

                if (!Convert.ToBoolean(guildSettings.GetClassRoles() & settings.GetClassRoles()))
                    continue;

                if (!Convert.ToBoolean(guildSettings.GetInterests() & settings.GetInterests()))
                    continue;

                if (!Convert.ToBoolean(guildSettings.GetLevel() & settings.GetLevel()))
                    continue;

                resultSet.Add(guildSettings);
            }

            return resultSet;
        }

        public bool HasRequest(ObjectGuid playerId, ObjectGuid guildId)
        {
            var guildDic = _membershipRequestsByGuild.LookupByKey(guildId);
            if (guildDic == null)
                return false;

            return guildDic.ContainsKey(playerId);
        }

        public void SetGuildSettings(ObjectGuid guildGuid, LFGuildSettings settings)
        {
            _guildSettings[guildGuid] = settings;

            SQLTransaction trans = new SQLTransaction();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_GUILD_FINDER_GUILD_SETTINGS);
            stmt.AddValue(0, settings.GetGUID().GetCounter());
            stmt.AddValue(1, settings.GetAvailability());
            stmt.AddValue(2, settings.GetClassRoles());
            stmt.AddValue(3, settings.GetInterests());
            stmt.AddValue(4, settings.GetLevel());
            stmt.AddValue(5, settings.IsListed());
            stmt.AddValue(6, settings.GetComment());
            trans.Append(stmt);

            DB.Characters.CommitTransaction(trans);
        }

        public void DeleteGuild(ObjectGuid guildId)
        {
            SQLTransaction trans = new SQLTransaction();
            PreparedStatement stmt;
            var guildDic = _membershipRequestsByGuild.LookupByKey(guildId);
            if (guildDic != null)
            {
                foreach (var guid in guildDic.Keys)
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_FINDER_APPLICANT);
                    stmt.AddValue(0, guildId.GetCounter());
                    stmt.AddValue(1, guid.GetCounter());
                    trans.Append(stmt);

                    if (_membershipRequestsByPlayer.ContainsKey(guid))
                    {
                        var playerDic = _membershipRequestsByPlayer.LookupByKey(guid);
                        playerDic.Remove(guildId);
                        if (playerDic.Empty())
                            _membershipRequestsByPlayer.Remove(guid);
                    }

                    // Notify the applicant his submition has been removed
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    if (player)
                        SendMembershipRequestListUpdate(player);
                }                
            }

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_FINDER_GUILD_SETTINGS);
            stmt.AddValue(0, guildId.GetCounter());
            trans.Append(stmt);

            DB.Characters.CommitTransaction(trans);

            _membershipRequestsByGuild.Remove(guildId);
            _guildSettings.Remove(guildId);

            // Notify the guild master the list changed (even if he's not a GM any more, not sure if needed)
            Guild guild = Global.GuildMgr.GetGuildById(guildId.GetCounter());
            if (guild)
                SendApplicantListUpdate(guild);
        }

        void SendApplicantListUpdate(Guild guild)
        {
            LFGuildApplicantListChanged applicantListChanged = new LFGuildApplicantListChanged();

            guild.BroadcastPacketToRank(applicantListChanged, GuildDefaultRanks.Officer);

            Player player = Global.ObjAccessor.FindPlayer(guild.GetLeaderGUID());
            if (player)
                player.SendPacket(applicantListChanged);
        }

        void SendMembershipRequestListUpdate(Player player)
        {
            player.SendPacket(new LFGuildApplicationsListChanged());
        }

        public Dictionary<ObjectGuid, MembershipRequest> GetAllMembershipRequestsForGuild(ObjectGuid guildGuid) { return _membershipRequestsByGuild.LookupByKey(guildGuid); }

        public LFGuildSettings GetGuildSettings(ObjectGuid guildGuid) { return _guildSettings.LookupByKey(guildGuid); }

        Dictionary<ObjectGuid, LFGuildSettings> _guildSettings = new Dictionary<ObjectGuid, LFGuildSettings>();
        Dictionary<ObjectGuid, Dictionary<ObjectGuid, MembershipRequest>> _membershipRequestsByGuild = new Dictionary<ObjectGuid, Dictionary<ObjectGuid, MembershipRequest>>();
        Dictionary<ObjectGuid, Dictionary<ObjectGuid, MembershipRequest>> _membershipRequestsByPlayer = new Dictionary<ObjectGuid, Dictionary<ObjectGuid, MembershipRequest>>();
    }

    public class MembershipRequest
    {
        public MembershipRequest(MembershipRequest settings)
        {
            _comment = settings.GetComment();

            _availability = settings.GetAvailability();
            _classRoles = settings.GetClassRoles();
            _interests = settings.GetInterests();
            _guildId = settings.GetGuildGuid();
            _playerGUID = settings.GetPlayerGUID();
            _time = settings.GetSubmitTime();
        }

        public MembershipRequest(ObjectGuid playerGUID, ObjectGuid guildId, uint availability, uint classRoles, uint interests, string comment, long submitTime)
        {
            _comment = comment;
            _guildId = guildId;
            _playerGUID = playerGUID;
            _availability = (byte)availability;
            _classRoles = (byte)classRoles;
            _interests = (byte)interests;
            _time = submitTime;
        }

        public MembershipRequest()
        {
            _time = Time.UnixTime;
        }

        public ObjectGuid GetGuildGuid() { return _guildId; }
        public ObjectGuid GetPlayerGUID() { return _playerGUID; }
        public byte GetAvailability() { return _availability; }
        public byte GetClassRoles() { return _classRoles; }
        public byte GetInterests() { return _interests; }
        public long GetSubmitTime() { return _time; }
        public long GetExpiryTime() { return _time + 30 * 24 * 3600; } // Adding 30 days
        public string GetComment() { return _comment; }

        string _comment = "";

        ObjectGuid _guildId;
        ObjectGuid _playerGUID;

        byte _availability;
        byte _classRoles;
        byte _interests;

        long _time;
    }

    public class LFGuildPlayer
    {
        public LFGuildPlayer()
        {
            _guid = ObjectGuid.Empty;
            _roles = 0;
            _availability = 0;
            _interests = 0;
            _level = 0;
        }

        public LFGuildPlayer(ObjectGuid guid, uint role, uint availability, uint interests, uint level)
        {
            _guid = guid;
            _roles = (byte)role;
            _availability = (byte)availability;
            _interests = (byte)interests;
            _level = (byte)level;
        }

        public LFGuildPlayer(ObjectGuid guid, uint role, uint availability, uint interests, uint level, string comment)
        {
            _comment = comment;

            _guid = guid;
            _roles = (byte)role;
            _availability = (byte)availability;
            _interests = (byte)interests;
            _level = (byte)level;
        }

        public LFGuildPlayer(LFGuildPlayer settings)
        {
            _comment = settings.GetComment();
            _guid = settings.GetGUID();
            _roles = settings.GetClassRoles();
            _availability = settings.GetAvailability();
            _interests = settings.GetInterests();
            _level = settings.GetLevel();
        }

        public ObjectGuid GetGUID() { return _guid; }
        public byte GetClassRoles() { return _roles; }
        public byte GetAvailability() { return _availability; }
        public byte GetInterests() { return _interests; }
        public byte GetLevel() { return _level; }
        public string GetComment() { return _comment; }

        string _comment = "";
        ObjectGuid _guid;
        byte _roles;
        byte _availability;
        byte _interests;
        byte _level;
    }

    public class LFGuildSettings : LFGuildPlayer
    {
        public LFGuildSettings()
        {
            _listed = false;
            _team = TeamId.Alliance;
        }

        public LFGuildSettings(bool listed, uint team)
        {
            _listed = listed;
            _team = team;
        }

        public LFGuildSettings(bool listed, uint team, ObjectGuid guid, uint role, uint availability, uint interests, uint level)
            : base(guid, role, availability, interests, level)
        {
            _listed = listed;
            _team = team;
        }

        public LFGuildSettings(bool listed, uint team, ObjectGuid guid, uint role, uint availability, uint interests, uint level, string comment)
            : base(guid, role, availability, interests, level, comment)
        {
            _listed = listed;
            _team = team;
        }

        public LFGuildSettings(LFGuildSettings settings)
            : base(settings)
        {
            _listed = settings.IsListed();
            _team = settings.GetTeam();
        }

        public bool IsListed() { return _listed; }
        void SetListed(bool state) { _listed = state; }

        public uint GetTeam() { return _team; }

        bool _listed;
        uint _team;
    }
}
