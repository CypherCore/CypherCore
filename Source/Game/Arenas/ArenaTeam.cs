// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Database;
using Game.Cache;
using Game.Entities;
using Game.Groups;
using Game.Networking;

namespace Game.Arenas
{
    public class ArenaTeam
	{
		private uint _backgroundColor; // ARGB format
		private uint _borderColor;     // ARGB format
		private byte _borderStyle;     // border image Id
		private ObjectGuid _captainGuid;
		private uint _emblemColor; // ARGB format
		private byte _emblemStyle; // icon Id

		private List<ArenaTeamMember> _members = new();
		private ArenaTeamStats _stats;

		private uint _teamId;
		private string _teamName;
		private byte _type;

		public ArenaTeam()
		{
			_stats.Rating = (ushort)WorldConfig.GetIntValue(WorldCfg.ArenaStartRating);
		}

		public bool Create(ObjectGuid captainGuid, byte type, string arenaTeamName, uint backgroundColor, byte emblemStyle, uint emblemColor, byte borderStyle, uint borderColor)
		{
			// Check if captain exists
			if (Global.CharacterCacheStorage.GetCharacterCacheByGuid(captainGuid) == null)
				return false;

			// Check if arena team Name is already taken
			if (Global.ArenaTeamMgr.GetArenaTeamByName(arenaTeamName) != null)
				return false;

			// Generate new arena team Id
			_teamId = Global.ArenaTeamMgr.GenerateArenaTeamId();

			// Assign member variables
			_captainGuid     = captainGuid;
			_type            = type;
			_teamName        = arenaTeamName;
			_backgroundColor = backgroundColor;
			_emblemStyle     = emblemStyle;
			_emblemColor     = emblemColor;
			_borderStyle     = borderStyle;
			_borderColor     = borderColor;
			ulong captainLowGuid = captainGuid.GetCounter();

			// Save arena team to db
			PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ARENA_TEAM);
			stmt.AddValue(0, _teamId);
			stmt.AddValue(1, _teamName);
			stmt.AddValue(2, captainLowGuid);
			stmt.AddValue(3, _type);
			stmt.AddValue(4, _stats.Rating);
			stmt.AddValue(5, _backgroundColor);
			stmt.AddValue(6, _emblemStyle);
			stmt.AddValue(7, _emblemColor);
			stmt.AddValue(8, _borderStyle);
			stmt.AddValue(9, _borderColor);
			DB.Characters.Execute(stmt);

			// Add captain as member
			AddMember(_captainGuid);

			Log.outDebug(LogFilter.Arena, "New ArenaTeam created Id: {0}, Name: {1} Type: {2} Captain low GUID: {3}", GetId(), GetName(), GetArenaType(), captainLowGuid);

			return true;
		}

		public bool AddMember(ObjectGuid playerGuid)
		{
			string playerName;
			Class  playerClass;

			// Check if arena team is full (Can't have more than Type * 2 players)
			if (GetMembersSize() >= GetArenaType() * 2)
				return false;

			// Get player Name and class either from db or character cache
			CharacterCacheEntry characterInfo;
			Player              player = Global.ObjAccessor.FindPlayer(playerGuid);

			if (player)
			{
				playerClass = player.GetClass();
				playerName  = player.GetName();
			}
			else if ((characterInfo = Global.CharacterCacheStorage.GetCharacterCacheByGuid(playerGuid)) != null)
			{
				playerName  = characterInfo.Name;
				playerClass = characterInfo.ClassId;
			}
			else
			{
				return false;
			}

			// Check if player is already in a similar arena team
			if ((player && player.GetArenaTeamId(GetSlot()) != 0) ||
			    Global.CharacterCacheStorage.GetCharacterArenaTeamIdByGuid(playerGuid, GetArenaType()) != 0)
			{
				Log.outDebug(LogFilter.Arena, "Arena: {0} {1} already has an arena team of Type {2}", playerGuid.ToString(), playerName, GetArenaType());

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
			// This will prevent player from joining too many arena teams and corrupt arena team _data integrity
			//Player.RemovePetitionsAndSigns(playerGuid, GetArenaType());

			// Feed _data to the struct
			ArenaTeamMember newMember = new();
			newMember.Name             = playerName;
			newMember.Guid             = playerGuid;
			newMember.Class            = (byte)playerClass;
			newMember.SeasonGames      = 0;
			newMember.WeekGames        = 0;
			newMember.SeasonWins       = 0;
			newMember.WeekWins         = 0;
			newMember.PersonalRating   = (ushort)personalRating;
			newMember.MatchMakerRating = (ushort)matchMakerRating;

			_members.Add(newMember);
			Global.CharacterCacheStorage.UpdateCharacterArenaTeamId(playerGuid, GetSlot(), GetId());

			// Save player's arena team membership to db
			stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ARENA_TEAM_MEMBER);
			stmt.AddValue(0, _teamId);
			stmt.AddValue(1, playerGuid.GetCounter());
			stmt.AddValue(2, (ushort)personalRating);
			DB.Characters.Execute(stmt);

			// Inform player if online
			if (player)
			{
				player.SetInArenaTeam(_teamId, GetSlot(), GetArenaType());
				player.SetArenaTeamIdInvited(0);

				// Hide promote/remove buttons
				if (_captainGuid != playerGuid)
					player.SetArenaTeamInfoField(GetSlot(), ArenaTeamInfoType.Member, 1);
			}

			Log.outDebug(LogFilter.Arena, "Player: {0} [{1}] joined arena team Type: {2} [Id: {3}, Name: {4}].", playerName, playerGuid.ToString(), GetArenaType(), GetId(), GetName());

			return true;
		}

		public bool LoadArenaTeamFromDB(SQLResult result)
		{
			if (result.IsEmpty())
				return false;

			_teamId            = result.Read<uint>(0);
			_teamName          = result.Read<string>(1);
			_captainGuid       = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(2));
			_type              = result.Read<byte>(3);
			_backgroundColor   = result.Read<uint>(4);
			_emblemStyle       = result.Read<byte>(5);
			_emblemColor       = result.Read<uint>(6);
			_borderStyle       = result.Read<byte>(7);
			_borderColor       = result.Read<uint>(8);
			_stats.Rating      = result.Read<ushort>(9);
			_stats.WeekGames   = result.Read<ushort>(10);
			_stats.WeekWins    = result.Read<ushort>(11);
			_stats.SeasonGames = result.Read<ushort>(12);
			_stats.SeasonWins  = result.Read<ushort>(13);
			_stats.Rank        = result.Read<uint>(14);

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
				if (arenaTeamId > _teamId)
					break;

				ArenaTeamMember newMember = new();
				newMember.Guid             = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(1));
				newMember.WeekGames        = result.Read<ushort>(2);
				newMember.WeekWins         = result.Read<ushort>(3);
				newMember.SeasonGames      = result.Read<ushort>(4);
				newMember.SeasonWins       = result.Read<ushort>(5);
				newMember.Name             = result.Read<string>(6);
				newMember.Class            = result.Read<byte>(7);
				newMember.PersonalRating   = result.Read<ushort>(8);
				newMember.MatchMakerRating = (ushort)(result.Read<ushort>(9) > 0 ? result.Read<ushort>(9) : 1500);

				// Delete member if character information is missing
				if (string.IsNullOrEmpty(newMember.Name))
				{
					Log.outError(LogFilter.Sql, "ArenaTeam {0} has member with empty Name - probably {1} doesn't exist, deleting him from memberlist!", arenaTeamId, newMember.Guid.ToString());
					DelMember(newMember.Guid, true);

					continue;
				}

				// Check if team team has a valid captain
				if (newMember.Guid == GetCaptain())
					captainPresentInTeam = true;

				// Put the player in the team
				_members.Add(newMember);
				Global.CharacterCacheStorage.UpdateCharacterArenaTeamId(newMember.Guid, GetSlot(), GetId());
			} while (result.NextRow());

			if (Empty() ||
			    !captainPresentInTeam)
			{
				// Arena team is empty or captain is not in team, delete from db
				Log.outDebug(LogFilter.Arena, "ArenaTeam {0} does not have any members or its captain is not in team, disbanding it...", _teamId);

				return false;
			}

			return true;
		}

		public bool SetName(string name)
		{
			if (_teamName == name ||
			    string.IsNullOrEmpty(name) ||
			    name.Length > 24 ||
			    Global.ObjectMgr.IsReservedName(name) ||
			    !ObjectManager.IsValidCharterName(name))
				return false;

			_teamName = name;
			PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ARENA_TEAM_NAME);
			stmt.AddValue(0, _teamName);
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
			_captainGuid = guid;

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
					Log.outDebug(LogFilter.Arena,
					             "Player: {0} [GUID: {1}] promoted player: {2} [GUID: {3}] to leader of arena team [Id: {4}, Name: {5}] [Type: {6}].",
					             oldCaptain.GetName(),
					             oldCaptain.GetGUID().ToString(),
					             newCaptain.GetName(),
					             newCaptain.GetGUID().ToString(),
					             GetId(),
					             GetName(),
					             GetArenaType());
			}
		}

		public void DelMember(ObjectGuid guid, bool cleanDb)
		{
			// Remove member from team
			foreach (var member in _members)
				if (member.Guid == guid)
				{
					_members.Remove(member);
					Global.CharacterCacheStorage.UpdateCharacterArenaTeamId(guid, GetSlot(), 0);

					break;
				}

			// Remove arena team info from player _data
			Player player = Global.ObjAccessor.FindPlayer(guid);

			if (player)
			{
				// delete all info regarding this team
				for (uint i = 0; i < (int)ArenaTeamInfoType.End; ++i)
					player.SetArenaTeamInfoField(GetSlot(), (ArenaTeamInfoType)i, 0);

				Log.outDebug(LogFilter.Arena, "Player: {0} [GUID: {1}] left arena team Type: {2} [Id: {3}, Name: {4}].", player.GetName(), player.GetGUID().ToString(), GetArenaType(), GetId(), GetName());
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
					Log.outDebug(LogFilter.Arena, "Player: {0} [GUID: {1}] disbanded arena team Type: {2} [Id: {3}, Name: {4}].", player.GetName(), player.GetGUID().ToString(), GetArenaType(), GetId(), GetName());
			}

			// Remove all members from arena team
			while (!_members.Empty())
				DelMember(_members.FirstOrDefault().Guid, false);

			// Update database
			SQLTransaction trans = new();

			PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ARENA_TEAM);
			stmt.AddValue(0, _teamId);
			trans.Append(stmt);

			stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ARENA_TEAM_MEMBERS);
			stmt.AddValue(0, _teamId);
			trans.Append(stmt);

			DB.Characters.CommitTransaction(trans);

			// Remove arena team from ArenaTeamMgr
			Global.ArenaTeamMgr.RemoveArenaTeam(_teamId);
		}

		public void Disband()
		{
			// Remove all members from arena team
			while (!_members.Empty())
				DelMember(_members.First().Guid, false);

			// Update database
			SQLTransaction trans = new();

			PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ARENA_TEAM);
			stmt.AddValue(0, _teamId);
			trans.Append(stmt);

			stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ARENA_TEAM_MEMBERS);
			stmt.AddValue(0, _teamId);
			trans.Append(stmt);

			DB.Characters.CommitTransaction(trans);

			// Remove arena team from ArenaTeamMgr
			Global.ArenaTeamMgr.RemoveArenaTeam(_teamId);
		}

		public void SendStats(WorldSession session) // @TODO 
		{
			/*WorldPacket _data = new WorldPacket(ServerOpcodes.ArenaTeamStats);
			_data.WriteUInt32(GetId());                                // team Id
			_data.WriteUInt32(Stats.Rating);                           // rating
			_data.WriteUInt32(Stats.WeekGames);                        // games this week
			_data.WriteUInt32(Stats.WeekWins);                         // wins this week
			_data.WriteUInt32(Stats.SeasonGames);                      // played this season
			_data.WriteUInt32(Stats.SeasonWins);                       // wins this season
			_data.WriteUInt32(Stats.Rank);                             // rank
			session.SendPacket(_data);*/
		}

		public void NotifyStatsChanged()
		{
			// This is called after a rated match ended
			// Updates arena team Stats for every member of the team (not only the ones who participated!)
			foreach (var member in _members)
			{
				Player player = Global.ObjAccessor.FindPlayer(member.Guid);

				if (player)
					SendStats(player.GetSession());
			}
		}

		private void BroadcastPacket(ServerPacket packet)
		{
			foreach (var member in _members)
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

			Log.outError(LogFilter.Arena, "FATAL: Unknown arena team Type {0} for some arena team", type);

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

			Log.outError(LogFilter.Arena, "FATAL: Unknown arena team Slot {0} for some arena team", slot);

			return 0xFF;
		}

		public bool IsMember(ObjectGuid guid)
		{
			foreach (var member in _members)
				if (member.Guid == guid)
					return true;

			return false;
		}

		public uint GetAverageMMR(Group group)
		{
			if (!group)
				return 0;

			uint matchMakerRating = 0;
			uint playerDivider    = 0;

			foreach (var member in _members)
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

		private float GetChanceAgainst(uint ownRating, uint opponentRating)
		{
			// Returns the chance to win against a team with the given rating, used in the rating adjustment calculation
			// ELO system
			return (float)(1.0f / (1.0f + Math.Exp(Math.Log(10.0f) * ((float)opponentRating - ownRating) / 650.0f)));
		}

		private int GetMatchmakerRatingMod(uint ownRating, uint opponentRating, bool won)
		{
			// 'Chance' calculation - to beat the opponent
			// This is a simulation. Not much info on how it really works
			float chance  = GetChanceAgainst(ownRating, opponentRating);
			float won_mod = (won) ? 1.0f : 0.0f;
			float mod     = won_mod - chance;

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

		private int GetRatingMod(uint ownRating, uint opponentRating, bool won)
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
				{
					mod = WorldConfig.GetFloatValue(WorldCfg.ArenaWinRatingModifier2) * (1.0f - chance);
				}
			}
			else
			{
				mod = WorldConfig.GetFloatValue(WorldCfg.ArenaLoseRatingModifier) * (-chance);
			}

			return (int)Math.Ceiling(mod);
		}

		public void FinishGame(int mod)
		{
			// Rating can only drop to 0
			if (_stats.Rating + mod < 0)
			{
				_stats.Rating = 0;
			}
			else
			{
				_stats.Rating += (ushort)mod;

				// Check if rating related achivements are met
				foreach (var member in _members)
				{
					Player player = Global.ObjAccessor.FindPlayer(member.Guid);

					if (player)
						player.UpdateCriteria(CriteriaType.EarnTeamArenaRating, _stats.Rating, _type);
				}
			}

			// Update number of games played per season or week
			_stats.WeekGames   += 1;
			_stats.SeasonGames += 1;

			// Update team's rank, start with rank 1 and increase until no team with more rating was found
			_stats.Rank = 1;

			foreach (var (_, team) in Global.ArenaTeamMgr.GetArenaTeamMap())
				if (team.GetArenaType() == _type &&
				    team.GetStats().Rating > _stats.Rating)
					++_stats.Rank;
		}

		public int WonAgainst(uint ownMMRating, uint opponentMMRating, ref int ratingChange)
		{
			// Called when the team has won
			// Change in Matchmaker rating
			int mod = GetMatchmakerRatingMod(ownMMRating, opponentMMRating, true);

			// Change in Team Rating
			ratingChange = GetRatingMod(_stats.Rating, opponentMMRating, true);

			// Modify the team Stats accordingly
			FinishGame(ratingChange);

			// Update number of wins per season and week
			_stats.WeekWins   += 1;
			_stats.SeasonWins += 1;

			// Return the rating change, used to display it on the results screen
			return mod;
		}

		public int LostAgainst(uint ownMMRating, uint opponentMMRating, ref int ratingChange)
		{
			// Called when the team has lost
			// Change in Matchmaker Rating
			int mod = GetMatchmakerRatingMod(ownMMRating, opponentMMRating, false);

			// Change in Team Rating
			ratingChange = GetRatingMod(_stats.Rating, opponentMMRating, false);

			// Modify the team Stats accordingly
			FinishGame(ratingChange);

			// return the rating change, used to display it on the results screen
			return mod;
		}

		public void MemberLost(Player player, uint againstMatchmakerRating, int matchmakerRatingChange = -12)
		{
			// Called for each participant of a match after losing
			foreach (var member in _members)
				if (member.Guid == player.GetGUID())
				{
					// Update personal rating
					int mod = GetRatingMod(member.PersonalRating, againstMatchmakerRating, false);
					member.ModifyPersonalRating(player, mod, GetArenaType());

					// Update matchmaker rating
					member.ModifyMatchmakerRating(matchmakerRatingChange, GetSlot());

					// Update personal played Stats
					member.WeekGames   += 1;
					member.SeasonGames += 1;

					// update the unit fields
					player.SetArenaTeamInfoField(GetSlot(), ArenaTeamInfoType.GamesWeek, member.WeekGames);
					player.SetArenaTeamInfoField(GetSlot(), ArenaTeamInfoType.GamesSeason, member.SeasonGames);

					return;
				}
		}

		public void OfflineMemberLost(ObjectGuid guid, uint againstMatchmakerRating, int matchmakerRatingChange = -12)
		{
			// Called for offline player after ending rated arena match!
			foreach (var member in _members)
				if (member.Guid == guid)
				{
					// update personal rating
					int mod = GetRatingMod(member.PersonalRating, againstMatchmakerRating, false);
					member.ModifyPersonalRating(null, mod, GetArenaType());

					// update matchmaker rating
					member.ModifyMatchmakerRating(matchmakerRatingChange, GetSlot());

					// update personal played Stats
					member.WeekGames   += 1;
					member.SeasonGames += 1;

					return;
				}
		}

		public void MemberWon(Player player, uint againstMatchmakerRating, int matchmakerRatingChange)
		{
			// called for each participant after winning a match
			foreach (var member in _members)
				if (member.Guid == player.GetGUID())
				{
					// update personal rating
					int mod = GetRatingMod(member.PersonalRating, againstMatchmakerRating, true);
					member.ModifyPersonalRating(player, mod, GetArenaType());

					// update matchmaker rating
					member.ModifyMatchmakerRating(matchmakerRatingChange, GetSlot());

					// update personal Stats
					member.WeekGames   += 1;
					member.SeasonGames += 1;
					member.SeasonWins  += 1;
					member.WeekWins    += 1;
					// update unit fields
					player.SetArenaTeamInfoField(GetSlot(), ArenaTeamInfoType.GamesWeek, member.WeekGames);
					player.SetArenaTeamInfoField(GetSlot(), ArenaTeamInfoType.GamesSeason, member.SeasonGames);

					return;
				}
		}

		public void SaveToDB()
		{
			// Save team and member Stats to db
			// Called after a match has ended or when calculating arena_points

			SQLTransaction trans = new();

			PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ARENA_TEAM_STATS);
			stmt.AddValue(0, _stats.Rating);
			stmt.AddValue(1, _stats.WeekGames);
			stmt.AddValue(2, _stats.WeekWins);
			stmt.AddValue(3, _stats.SeasonGames);
			stmt.AddValue(4, _stats.SeasonWins);
			stmt.AddValue(5, _stats.Rank);
			stmt.AddValue(6, GetId());
			trans.Append(stmt);

			foreach (var member in _members)
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
			if (_stats.WeekGames == 0)
				return false;

			// Reset team Stats
			_stats.WeekGames = 0;
			_stats.WeekWins  = 0;

			// Reset member Stats
			foreach (var member in _members)
			{
				member.WeekGames = 0;
				member.WeekWins  = 0;
			}

			return true;
		}

		public bool IsFighting()
		{
			foreach (var member in _members)
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
			foreach (var member in _members)
				if (member.Name == name)
					return member;

			return null;
		}

		public ArenaTeamMember GetMember(ObjectGuid guid)
		{
			foreach (var member in _members)
				if (member.Guid == guid)
					return member;

			return null;
		}

		public uint GetId()
		{
			return _teamId;
		}

		public byte GetArenaType()
		{
			return _type;
		}

		public byte GetSlot()
		{
			return GetSlotByType(GetArenaType());
		}

		public ObjectGuid GetCaptain()
		{
			return _captainGuid;
		}

		public string GetName()
		{
			return _teamName;
		}

		public ArenaTeamStats GetStats()
		{
			return _stats;
		}

		public uint GetRating()
		{
			return _stats.Rating;
		}

		public int GetMembersSize()
		{
			return _members.Count;
		}

		private bool Empty()
		{
			return _members.Empty();
		}

		public List<ArenaTeamMember> GetMembers()
		{
			return _members;
		}
	}
}