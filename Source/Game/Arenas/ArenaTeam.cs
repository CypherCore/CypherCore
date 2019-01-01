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
using Game.Entities;
using Game.Groups;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Arenas
{
    public class ArenaTeam
    {
        public ArenaTeam()
        {
            stats.Rating = (ushort)WorldConfig.GetIntValue(WorldCfg.ArenaStartRating);
        }

        public bool Create(ObjectGuid captainGuid, byte _type, string arenaTeamName, uint backgroundColor, byte emblemStyle, uint emblemColor, byte borderStyle, uint borderColor)
        {
            // Check if captain is present
            if (!Global.ObjAccessor.FindPlayer(captainGuid))
                return false;

            // Check if arena team name is already taken
            if (Global.ArenaTeamMgr.GetArenaTeamByName(arenaTeamName) != null)
                return false;

            // Generate new arena team id
            teamId = Global.ArenaTeamMgr.GenerateArenaTeamId();

            // Assign member variables
            CaptainGuid = captainGuid;
            type = _type;
            TeamName = arenaTeamName;
            BackgroundColor = backgroundColor;
            EmblemStyle = emblemStyle;
            EmblemColor = emblemColor;
            BorderStyle = borderStyle;
            BorderColor = borderColor;
            ulong captainLowGuid = captainGuid.GetCounter();

            // Save arena team to db
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ARENA_TEAM);
            stmt.AddValue(0, teamId);
            stmt.AddValue(1, TeamName);
            stmt.AddValue(2, captainLowGuid);
            stmt.AddValue(3, type);
            stmt.AddValue(4, stats.Rating);
            stmt.AddValue(5, BackgroundColor);
            stmt.AddValue(6, EmblemStyle);
            stmt.AddValue(7, EmblemColor);
            stmt.AddValue(8, BorderStyle);
            stmt.AddValue(9, BorderColor);
            DB.Characters.Execute(stmt);

            // Add captain as member
            AddMember(CaptainGuid);

            Log.outDebug(LogFilter.Arena, "New ArenaTeam created Id: {0}, Name: {1} Type: {2} Captain low GUID: {3}", GetId(), GetName(), GetArenaType(), captainLowGuid);
            return true;
        }

        public bool AddMember(ObjectGuid playerGuid)
        {
            string playerName;
            Class playerClass;

            // Check if arena team is full (Can't have more than type * 2 players)
            if (GetMembersSize() >= GetArenaType() * 2)
                return false;

            // Get player name and class either from db or ObjectMgr
            CharacterInfo characterInfo;
            Player player = Global.ObjAccessor.FindPlayer(playerGuid);
            if (player)
            {
                playerClass = player.GetClass();
                playerName = player.GetName();
            }
            else if ((characterInfo = Global.WorldMgr.GetCharacterInfo(playerGuid)) != null)
            {
                playerName = characterInfo.Name;
                playerClass = characterInfo.ClassID;
            }
            else
                return false;

            // Check if player is already in a similar arena team
            if ((player && player.GetArenaTeamId(GetSlot()) != 0) || Player.GetArenaTeamIdFromDB(playerGuid, GetArenaType()) != 0)
            {
                Log.outDebug(LogFilter.Arena, "Arena: {0} {1} already has an arena team of type {2}", playerGuid.ToString(), playerName, GetArenaType());
                return false;
            }

            // Set player's personal rating
            uint personalRating = 0;

            if (WorldConfig.GetIntValue(WorldCfg.ArenaStartPersonalRating) > 0)
                personalRating = WorldConfig.GetUIntValue(WorldCfg.ArenaStartPersonalRating);
            else if (GetRating() >= 1000)
                personalRating = 1000;

            // Try to get player's match maker rating from db and fall back to config setting if not found
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MATCH_MAKER_RATING);
            stmt.AddValue(0, playerGuid.GetCounter());
            stmt.AddValue(1, GetSlot());
            SQLResult result = DB.Characters.Query(stmt);

            uint matchMakerRating;
            if (!result.IsEmpty())
                matchMakerRating = result.Read<ushort>(0);
            else
                matchMakerRating = WorldConfig.GetUIntValue(WorldCfg.ArenaStartMatchmakerRating);

            // Remove all player signatures from other petitions
            // This will prevent player from joining too many arena teams and corrupt arena team data integrity
            //Player.RemovePetitionsAndSigns(playerGuid, GetArenaType());

            // Feed data to the struct
            ArenaTeamMember newMember = new ArenaTeamMember();
            newMember.Name = playerName;
            newMember.Guid = playerGuid;
            newMember.Class = (byte)playerClass;
            newMember.SeasonGames = 0;
            newMember.WeekGames = 0;
            newMember.SeasonWins = 0;
            newMember.WeekWins = 0;
            newMember.PersonalRating = 0;
            newMember.MatchMakerRating = (ushort)matchMakerRating;

            Members.Add(newMember);

            // Save player's arena team membership to db
            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ARENA_TEAM_MEMBER);
            stmt.AddValue(0, teamId);
            stmt.AddValue(1, playerGuid.GetCounter());
            DB.Characters.Execute(stmt);

            // Inform player if online
            if (player)
            {
                player.SetInArenaTeam(teamId, GetSlot(), GetArenaType());
                player.SetArenaTeamIdInvited(0);

                // Hide promote/remove buttons
                if (CaptainGuid != playerGuid)
                    player.SetArenaTeamInfoField(GetSlot(), ArenaTeamInfoType.Member, 1);
            }

            Log.outDebug(LogFilter.Arena, "Player: {0} [{1}] joined arena team type: {2} [Id: {3}, Name: {4}].", playerName, playerGuid.ToString(), GetArenaType(), GetId(), GetName());

            return true;
        }

        public bool LoadArenaTeamFromDB(SQLResult result)
        {
            if (result.IsEmpty())
                return false;

            teamId = result.Read<uint>(0);
            TeamName = result.Read<string>(1);
            CaptainGuid = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(2));
            type = result.Read<byte>(3);
            BackgroundColor = result.Read<uint>(4);
            EmblemStyle = result.Read<byte>(5);
            EmblemColor = result.Read<uint>(6);
            BorderStyle = result.Read<byte>(7);
            BorderColor = result.Read<uint>(8);
            stats.Rating = result.Read<ushort>(9);
            stats.WeekGames = result.Read<ushort>(10);
            stats.WeekWins = result.Read<ushort>(11);
            stats.SeasonGames = result.Read<ushort>(12);
            stats.SeasonWins = result.Read<ushort>(13);
            stats.Rank = result.Read<uint>(14);

            return true;
        }

        public bool LoadMembersFromDB(SQLResult result)
        {
            if (result.IsEmpty())
                return false;

            bool captainPresentInTeam = false;

            do
            {
                uint arenaTeamId = result.Read<uint>(0);

                // We loaded all members for this arena_team already, break cycle
                if (arenaTeamId > teamId)
                    break;

                ArenaTeamMember newMember = new ArenaTeamMember();
                newMember.Guid = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(1));
                newMember.WeekGames = result.Read<ushort>(2);
                newMember.WeekWins = result.Read<ushort>(3);
                newMember.SeasonGames = result.Read<ushort>(4);
                newMember.SeasonWins = result.Read<ushort>(5);
                newMember.Name = result.Read<string>(6);
                newMember.Class = result.Read<byte>(7);
                newMember.PersonalRating = result.Read<ushort>(8);
                newMember.MatchMakerRating = (ushort)(result.Read<ushort>(9) > 0 ? result.Read<ushort>(9) : 1500);

                // Delete member if character information is missing
                if (string.IsNullOrEmpty(newMember.Name))
                {
                    Log.outError(LogFilter.Sql, "ArenaTeam {0} has member with empty name - probably {1} doesn't exist, deleting him from memberlist!", arenaTeamId, newMember.Guid.ToString());
                    DelMember(newMember.Guid, true);
                    continue;
                }

                // Check if team team has a valid captain
                if (newMember.Guid == GetCaptain())
                    captainPresentInTeam = true;

                // Put the player in the team
                Members.Add(newMember);
            }
            while (result.NextRow());

            if (Empty() || !captainPresentInTeam)
            {
                // Arena team is empty or captain is not in team, delete from db
                Log.outDebug(LogFilter.Arena, "ArenaTeam {0} does not have any members or its captain is not in team, disbanding it...", teamId);
                return false;
            }

            return true;
        }

        public bool SetName(string name)
        {
            if (TeamName == name || string.IsNullOrEmpty(name) || name.Length > 24 || Global.ObjectMgr.IsReservedName(name) || !ObjectManager.IsValidCharterName(name))
                return false;

            TeamName = name;
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ARENA_TEAM_NAME);
            stmt.AddValue(0, TeamName);
            stmt.AddValue(1, GetId());
            DB.Characters.Execute(stmt);
            return true;
        }

        public void SetCaptain(ObjectGuid guid)
        {
            // Disable remove/promote buttons
            Player oldCaptain = Global.ObjAccessor.FindPlayer(GetCaptain());
            if (oldCaptain)
                oldCaptain.SetArenaTeamInfoField(GetSlot(), ArenaTeamInfoType.Member, 1);

            // Set new captain
            CaptainGuid = guid;

            // Update database
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ARENA_TEAM_CAPTAIN);
            stmt.AddValue(0, guid.GetCounter());
            stmt.AddValue(1, GetId());
            DB.Characters.Execute(stmt);

            // Enable remove/promote buttons
            Player newCaptain = Global.ObjAccessor.FindPlayer(guid);
            if (newCaptain)
            {
                newCaptain.SetArenaTeamInfoField(GetSlot(), ArenaTeamInfoType.Member, 0);
                if (oldCaptain)
                {
                    Log.outDebug(LogFilter.Arena, "Player: {0} [GUID: {1}] promoted player: {2} [GUID: {3}] to leader of arena team [Id: {4}, Name: {5}] [Type: {6}].",
                        oldCaptain.GetName(), oldCaptain.GetGUID().ToString(), newCaptain.GetName(),
                        newCaptain.GetGUID().ToString(), GetId(), GetName(), GetArenaType());
                }
            }
        }

        public void DelMember(ObjectGuid guid, bool cleanDb)
        {
            // Remove member from team
            foreach (var member in Members)
                if (member.Guid == guid)
                {
                    Members.Remove(member);
                    break;
                }

            // Remove arena team info from player data
            Player player = Global.ObjAccessor.FindPlayer(guid);
            if (player)
            {
                // delete all info regarding this team
                for (uint i = 0; i < (int)ArenaTeamInfoType.End; ++i)
                    player.SetArenaTeamInfoField(GetSlot(), (ArenaTeamInfoType)i, 0);
                Log.outDebug(LogFilter.Arena, "Player: {0} [GUID: {1}] left arena team type: {2} [Id: {3}, Name: {4}].", player.GetName(), player.GetGUID().ToString(), GetArenaType(), GetId(), GetName());
            }

            // Only used for single member deletion, for arena team disband we use a single query for more efficiency
            if (cleanDb)
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ARENA_TEAM_MEMBER);
                stmt.AddValue(0, GetId());
                stmt.AddValue(1, guid.GetCounter());
                DB.Characters.Execute(stmt);
            }
        }

        public void Disband(WorldSession session)
        {
            // Broadcast update
            if (session != null)
            {
                Player player = session.GetPlayer();
                if (player)
                    Log.outDebug(LogFilter.Arena, "Player: {0} [GUID: {1}] disbanded arena team type: {2} [Id: {3}, Name: {4}].", player.GetName(), player.GetGUID().ToString(), GetArenaType(), GetId(), GetName());
            }

            // Remove all members from arena team
            while (!Members.Empty())
                DelMember(Members.FirstOrDefault().Guid, false);

            // Update database
            SQLTransaction trans = new SQLTransaction();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ARENA_TEAM);
            stmt.AddValue(0, teamId);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ARENA_TEAM_MEMBERS);
            stmt.AddValue(0, teamId);
            trans.Append(stmt);

            DB.Characters.CommitTransaction(trans);

            // Remove arena team from ObjectMgr
            Global.ArenaTeamMgr.RemoveArenaTeam(teamId);
        }

        public void Disband()
        {
            // Remove all members from arena team
            while (!Members.Empty())
                DelMember(Members.First().Guid, false);

            // Update database
            SQLTransaction trans = new SQLTransaction();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ARENA_TEAM);
            stmt.AddValue(0, teamId);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ARENA_TEAM_MEMBERS);
            stmt.AddValue(0, teamId);
            trans.Append(stmt);

            DB.Characters.CommitTransaction(trans);

            // Remove arena team from ObjectMgr
            Global.ArenaTeamMgr.RemoveArenaTeam(teamId);
        }

        public void SendStats(WorldSession session)
        {
            /*WorldPacket data = new WorldPacket(ServerOpcodes.ArenaTeamStats);
            data.WriteUInt32(GetId());                                // team id
            data.WriteUInt32(stats.Rating);                           // rating
            data.WriteUInt32(stats.WeekGames);                        // games this week
            data.WriteUInt32(stats.WeekWins);                         // wins this week
            data.WriteUInt32(stats.SeasonGames);                      // played this season
            data.WriteUInt32(stats.SeasonWins);                       // wins this season
            data.WriteUInt32(stats.Rank);                             // rank
            session.SendPacket(data);*/
        }

        public void NotifyStatsChanged()
        {
            // This is called after a rated match ended
            // Updates arena team stats for every member of the team (not only the ones who participated!)
            foreach (var member in Members)
            {
                Player player = Global.ObjAccessor.FindPlayer(member.Guid);
                if (player)
                    SendStats(player.GetSession());
            }
        }

        public void Inspect(WorldSession session, ObjectGuid guid)
        {
            ArenaTeamMember member = GetMember(guid);
            if (member == null)
                return;

            WorldPacket data = new WorldPacket(ServerOpcodes.InspectPvp);
            data.WritePackedGuid(guid);                                   // player guid
            data.WriteUInt8(GetSlot());                               // slot (0...2)
            data.WriteUInt32(GetId());                                // arena team id
            data.WriteUInt32(stats.Rating);                           // rating
            data.WriteUInt32(stats.SeasonGames);                      // season played
            data.WriteUInt32(stats.SeasonWins);                       // season wins
            data.WriteUInt32(member.SeasonGames);                    // played (count of all games, that the inspected member participated...)
            data.WriteUInt32(member.PersonalRating);                 // personal rating
            //session.SendPacket(data);
        }

        void BroadcastPacket(ServerPacket packet)
        {
            foreach (var member in Members)
            {
                Player player = Global.ObjAccessor.FindPlayer(member.Guid);
                if (player)
                    player.SendPacket(packet);
            }
        }

        public static byte GetSlotByType(uint type)
        {
            switch ((ArenaTypes)type)
            {
                case ArenaTypes.Team2v2: return 0;
                case ArenaTypes.Team3v3: return 1;
                case ArenaTypes.Team5v5: return 2;
                default:
                    break;
            }
            Log.outError(LogFilter.Arena, "FATAL: Unknown arena team type {0} for some arena team", type);
            return 0xFF;
        }

        public static byte GetTypeBySlot(byte slot)
        {
            switch (slot)
            {
                case 0: return (byte)ArenaTypes.Team2v2;
                case 1: return (byte)ArenaTypes.Team3v3;
                case 2: return (byte)ArenaTypes.Team5v5;
                default:
                    break;
            }
            Log.outError(LogFilter.Arena, "FATAL: Unknown arena team slot {0} for some arena team", slot);
            return 0xFF;
        }

        public bool IsMember(ObjectGuid guid)
        {
            foreach (var member in Members)
                if (member.Guid == guid)
                    return true;

            return false;
        }

        public uint GetAverageMMR(Group group)
        {
            if (!group)
                return 0;

            uint matchMakerRating = 0;
            uint playerDivider = 0;
            foreach (var member in Members)
            {
                // Skip if player is not online
                if (!Global.ObjAccessor.FindPlayer(member.Guid))
                    continue;

                // Skip if player is not a member of group
                if (!group.IsMember(member.Guid))
                    continue;

                matchMakerRating += member.MatchMakerRating;
                ++playerDivider;
            }

            // x/0 = crash
            if (playerDivider == 0)
                playerDivider = 1;

            matchMakerRating /= playerDivider;

            return matchMakerRating;
        }

        float GetChanceAgainst(uint ownRating, uint opponentRating)
        {
            // Returns the chance to win against a team with the given rating, used in the rating adjustment calculation
            // ELO system
            return (float)(1.0f / (1.0f + Math.Exp(Math.Log(10.0f) * ((float)opponentRating - ownRating) / 650.0f)));
        }

        int GetMatchmakerRatingMod(uint ownRating, uint opponentRating, bool won)
        {
            // 'Chance' calculation - to beat the opponent
            // This is a simulation. Not much info on how it really works
            float chance = GetChanceAgainst(ownRating, opponentRating);
            float won_mod = (won) ? 1.0f : 0.0f;
            float mod = won_mod - chance;

            // Work in progress:
            /*
            // This is a simulation, as there is not much info on how it really works
            float confidence_mod = min(1.0f - fabs(mod), 0.5f);

            // Apply confidence factor to the mod:
            mod *= confidence_factor

            // And only after that update the new confidence factor
            confidence_factor -= ((confidence_factor - 1.0f) * confidence_mod) / confidence_factor;
            */

            // Real rating modification
            mod *= WorldConfig.GetFloatValue(WorldCfg.ArenaMatchmakerRatingModifier);

            return (int)Math.Ceiling(mod);
        }

        int GetRatingMod(uint ownRating, uint opponentRating, bool won)
        {
            // 'Chance' calculation - to beat the opponent
            // This is a simulation. Not much info on how it really works
            float chance = GetChanceAgainst(ownRating, opponentRating);

            // Calculate the rating modification
            float mod;

            // todo Replace this hack with using the confidence factor (limiting the factor to 2.0f)
            if (won)
            {
                if (ownRating < 1300)
                {
                    float win_rating_modifier1 = WorldConfig.GetFloatValue(WorldCfg.ArenaWinRatingModifier1);

                    if (ownRating < 1000)
                        mod = win_rating_modifier1 * (1.0f - chance);
                    else
                        mod = ((win_rating_modifier1 / 2.0f) + ((win_rating_modifier1 / 2.0f) * (1300.0f - ownRating) / 300.0f)) * (1.0f - chance);
                }
                else
                    mod = WorldConfig.GetFloatValue(WorldCfg.ArenaWinRatingModifier2) * (1.0f - chance);
            }
            else
                mod = WorldConfig.GetFloatValue(WorldCfg.ArenaLoseRatingModifier) * (-chance);

            return (int)Math.Ceiling(mod);
        }

        public void FinishGame(int mod)
        {
            // Rating can only drop to 0
            if (stats.Rating + mod < 0)
                stats.Rating = 0;
            else
            {
                stats.Rating += (ushort)mod;

                // Check if rating related achivements are met
                foreach (var member in Members)
                {
                    Player player = Global.ObjAccessor.FindPlayer(member.Guid);
                    if (player)
                        player.UpdateCriteria(CriteriaTypes.HighestTeamRating, stats.Rating, type);
                }
            }

            // Update number of games played per season or week
            stats.WeekGames += 1;
            stats.SeasonGames += 1;

            // Update team's rank, start with rank 1 and increase until no team with more rating was found
            stats.Rank = 1;
            foreach (var i in Global.ArenaTeamMgr.GetArenaTeamMap())
            {
                if (i.Value.GetArenaType() == type && i.Value.GetStats().Rating > stats.Rating)
                    ++stats.Rank;
            }
        }

        public int WonAgainst(uint ownMMRating, uint opponentMMRating, ref int ratingChange)
        {
            // Called when the team has won
            // Change in Matchmaker rating
            int mod = GetMatchmakerRatingMod(ownMMRating, opponentMMRating, true);

            // Change in Team Rating
            ratingChange = GetRatingMod(stats.Rating, opponentMMRating, true);

            // Modify the team stats accordingly
            FinishGame(ratingChange);

            // Update number of wins per season and week
            stats.WeekWins += 1;
            stats.SeasonWins += 1;

            // Return the rating change, used to display it on the results screen
            return mod;
        }

        public int LostAgainst(uint ownMMRating, uint opponentMMRating, ref int ratingChange)
        {
            // Called when the team has lost
            // Change in Matchmaker Rating
            int mod = GetMatchmakerRatingMod(ownMMRating, opponentMMRating, false);

            // Change in Team Rating
            ratingChange = GetRatingMod(stats.Rating, opponentMMRating, false);

            // Modify the team stats accordingly
            FinishGame(ratingChange);

            // return the rating change, used to display it on the results screen
            return mod;
        }

        public void MemberLost(Player player, uint againstMatchmakerRating, int matchmakerRatingChange = -12)
        {
            // Called for each participant of a match after losing
            foreach (var member in Members)
            {
                if (member.Guid == player.GetGUID())
                {
                    // Update personal rating
                    int mod = GetRatingMod(member.PersonalRating, againstMatchmakerRating, false);
                    member.ModifyPersonalRating(player, mod, GetArenaType());

                    // Update matchmaker rating
                    member.ModifyMatchmakerRating(matchmakerRatingChange, GetSlot());

                    // Update personal played stats
                    member.WeekGames += 1;
                    member.SeasonGames += 1;

                    // update the unit fields
                    player.SetArenaTeamInfoField(GetSlot(), ArenaTeamInfoType.GamesWeek, member.WeekGames);
                    player.SetArenaTeamInfoField(GetSlot(), ArenaTeamInfoType.GamesSeason, member.SeasonGames);
                    return;
                }
            }
        }

        public void OfflineMemberLost(ObjectGuid guid, uint againstMatchmakerRating, int matchmakerRatingChange = -12)
        {
            // Called for offline player after ending rated arena match!
            foreach (var member in Members)
            {
                if (member.Guid == guid)
                {
                    // update personal rating
                    int mod = GetRatingMod(member.PersonalRating, againstMatchmakerRating, false);
                    member.ModifyPersonalRating(null, mod, GetArenaType());

                    // update matchmaker rating
                    member.ModifyMatchmakerRating(matchmakerRatingChange, GetSlot());

                    // update personal played stats
                    member.WeekGames += 1;
                    member.SeasonGames += 1;
                    return;
                }
            }
        }

        public void MemberWon(Player player, uint againstMatchmakerRating, int matchmakerRatingChange)
        {
            // called for each participant after winning a match
            foreach (var member in Members)
            {
                if (member.Guid == player.GetGUID())
                {
                    // update personal rating
                    int mod = GetRatingMod(member.PersonalRating, againstMatchmakerRating, true);
                    member.ModifyPersonalRating(player, mod, GetArenaType());

                    // update matchmaker rating
                    member.ModifyMatchmakerRating(matchmakerRatingChange, GetSlot());

                    // update personal stats
                    member.WeekGames += 1;
                    member.SeasonGames += 1;
                    member.SeasonWins += 1;
                    member.WeekWins += 1;
                    // update unit fields
                    player.SetArenaTeamInfoField(GetSlot(), ArenaTeamInfoType.GamesWeek, member.WeekGames);
                    player.SetArenaTeamInfoField(GetSlot(), ArenaTeamInfoType.GamesSeason, member.SeasonGames);
                    return;
                }
            }
        }

        public void SaveToDB()
        {
            // Save team and member stats to db
            // Called after a match has ended or when calculating arena_points

            SQLTransaction trans = new SQLTransaction();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ARENA_TEAM_STATS);
            stmt.AddValue(0, stats.Rating);
            stmt.AddValue(1, stats.WeekGames);
            stmt.AddValue(2, stats.WeekWins);
            stmt.AddValue(3, stats.SeasonGames);
            stmt.AddValue(4, stats.SeasonWins);
            stmt.AddValue(5, stats.Rank);
            stmt.AddValue(6, GetId());
            trans.Append(stmt);

            foreach (var member in Members)
            {
                // Save the effort and go
                if (member.WeekGames == 0)
                    continue;

                stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ARENA_TEAM_MEMBER);
                stmt.AddValue(0, member.PersonalRating);
                stmt.AddValue(1, member.WeekGames);
                stmt.AddValue(2, member.WeekWins);
                stmt.AddValue(3, member.SeasonGames);
                stmt.AddValue(4, member.SeasonWins);
                stmt.AddValue(5, GetId());
                stmt.AddValue(6, member.Guid.GetCounter());
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_CHARACTER_ARENA_STATS);
                stmt.AddValue(0, member.Guid.GetCounter());
                stmt.AddValue(1, GetSlot());
                stmt.AddValue(2, member.MatchMakerRating);
                trans.Append(stmt);
            }

            DB.Characters.CommitTransaction(trans);
        }

        public bool FinishWeek()
        {
            // No need to go further than this
            if (stats.WeekGames == 0)
                return false;

            // Reset team stats
            stats.WeekGames = 0;
            stats.WeekWins = 0;

            // Reset member stats
            foreach (var member in Members)
            {
                member.WeekGames = 0;
                member.WeekWins = 0;
            }

            return true;
        }

        public bool IsFighting()
        {
            foreach (var member in Members)
            {
                Player player = Global.ObjAccessor.FindPlayer(member.Guid);
                if (player)
                    if (player.GetMap().IsBattleArena())
                        return true;
            }

            return false;
        }

        public ArenaTeamMember GetMember(string name)
        {
            foreach (var member in Members)
                if (member.Name == name)
                    return member;

            return null;
        }

        public ArenaTeamMember GetMember(ObjectGuid guid)
        {
            foreach (var member in Members)
                if (member.Guid == guid)
                    return member;

            return null;
        }

        public uint GetId() { return teamId; }
        public byte GetArenaType() { return type; }
        public byte GetSlot() { return GetSlotByType(GetArenaType()); }

        public ObjectGuid GetCaptain() { return CaptainGuid; }
        public string GetName() { return TeamName; }
        public ArenaTeamStats GetStats() { return stats; }
        public uint GetRating() { return stats.Rating; }

        public int GetMembersSize() { return Members.Count; }
        bool Empty() { return Members.Empty(); }
        public List<ArenaTeamMember> GetMembers()
        {
            return Members;
        }

        uint teamId;
        byte type;
        string TeamName;
        ObjectGuid CaptainGuid;

        uint BackgroundColor; // ARGB format
        byte EmblemStyle;     // icon id
        uint EmblemColor;     // ARGB format
        byte BorderStyle;     // border image id
        uint BorderColor;     // ARGB format

        List<ArenaTeamMember> Members = new List<ArenaTeamMember>();
        ArenaTeamStats stats;
    }

    public class ArenaTeamMember
    {
        public ObjectGuid Guid;
        public string Name;
        public byte Class;
        public ushort WeekGames;
        public ushort WeekWins;
        public ushort SeasonGames;
        public ushort SeasonWins;
        public ushort PersonalRating;
        public ushort MatchMakerRating;

        public void ModifyPersonalRating(Player player, int mod, uint type)
        {
            if (PersonalRating + mod < 0)
                PersonalRating = 0;
            else
                PersonalRating += (ushort)mod;

            if (player)
            {
                player.SetArenaTeamInfoField(ArenaTeam.GetSlotByType(type), ArenaTeamInfoType.PersonalRating, PersonalRating);
                player.UpdateCriteria(CriteriaTypes.HighestPersonalRating, PersonalRating, type);
            }
        }

        public void ModifyMatchmakerRating(int mod, uint slot)
        {
            if (MatchMakerRating + mod < 0)
                MatchMakerRating = 0;
            else
                MatchMakerRating += (ushort)mod;
        }
    }

    public struct ArenaTeamStats
    {
        public ushort Rating;
        public ushort WeekGames;
        public ushort WeekWins;
        public ushort SeasonGames;
        public ushort SeasonWins;
        public uint Rank;
    }
}
