// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Database;
using Game.Entities;

namespace Game.Arenas
{
	public class ArenaTeamManager : Singleton<ArenaTeamManager>
	{
		private readonly Dictionary<uint, ArenaTeam> _arenaTeamStorage = new();

		private uint _nextArenaTeamId;

		private ArenaTeamManager()
		{
			_nextArenaTeamId = 1;
		}

		public ArenaTeam GetArenaTeamById(uint arenaTeamId)
		{
			return _arenaTeamStorage.LookupByKey(arenaTeamId);
		}

		public ArenaTeam GetArenaTeamByName(string arenaTeamName)
		{
			string search = arenaTeamName.ToLower();

			foreach (var (_, team) in _arenaTeamStorage)
				if (search == team.GetName().ToLower())
					return team;

			return null;
		}

		public ArenaTeam GetArenaTeamByCaptain(ObjectGuid guid)
		{
			foreach (var (_, team) in _arenaTeamStorage)
				if (team.GetCaptain() == guid)
					return team;

			return null;
		}

		public void AddArenaTeam(ArenaTeam arenaTeam)
		{
			var added = _arenaTeamStorage.TryAdd(arenaTeam.GetId(), arenaTeam);
			Cypher.Assert(!added, $"Duplicate arena team with ID {arenaTeam.GetId()}");
		}

		public void RemoveArenaTeam(uint arenaTeamId)
		{
			_arenaTeamStorage.Remove(arenaTeamId);
		}

		public uint GenerateArenaTeamId()
		{
			if (_nextArenaTeamId >= 0xFFFFFFFE)
			{
				Log.outError(LogFilter.Battleground, "Arena team ids overflow!! Can't continue, shutting down server. ");
				Global.WorldMgr.StopNow();
			}

			return _nextArenaTeamId++;
		}

		public void LoadArenaTeams()
		{
			uint oldMSTime = Time.GetMSTime();

			// Clean out the trash before loading anything
			DB.Characters.DirectExecute("DELETE FROM arena_team_member WHERE arenaTeamId NOT IN (SELECT arenaTeamId FROM arena_team)"); // One-Time query

			//                                                        0        1         2         3          4              5            6            7           8
			SQLResult result = DB.Characters.Query("SELECT arenaTeamId, Name, captainGuid, Type, backgroundColor, emblemStyle, emblemColor, borderStyle, borderColor, " +
			                                       //      9        10        11         12           13       14
			                                       "rating, weekGames, weekWins, seasonGames, seasonWins, `rank` FROM arena_team ORDER BY arenaTeamId ASC");

			if (result.IsEmpty())
			{
				Log.outInfo(LogFilter.ServerLoading, "Loaded 0 arena teams. DB table `arena_team` is empty!");

				return;
			}

			SQLResult result2 = DB.Characters.Query(
			                                        //              0              1           2             3              4                 5          6     7          8                  9
			                                        "SELECT arenaTeamId, atm.Guid, atm.weekGames, atm.weekWins, atm.seasonGames, atm.seasonWins, c.Name, class, personalRating, matchMakerRating FROM arena_team_member atm" +
			                                        " INNER JOIN arena_team ate USING (arenaTeamId) LEFT JOIN characters AS c ON atm.Guid = c.Guid" +
			                                        " LEFT JOIN character_arena_stats AS cas ON c.Guid = cas.Guid AND (cas.Slot = 0 AND ate.Type = 2 OR cas.Slot = 1 AND ate.Type = 3 OR cas.Slot = 2 AND ate.Type = 5)" +
			                                        " ORDER BY atm.arenateamid ASC");

			uint count = 0;

			do
			{
				ArenaTeam newArenaTeam = new();

				if (!newArenaTeam.LoadArenaTeamFromDB(result) ||
				    !newArenaTeam.LoadMembersFromDB(result2))
				{
					newArenaTeam.Disband(null);

					continue;
				}

				AddArenaTeam(newArenaTeam);

				++count;
			} while (result.NextRow());

			Log.outInfo(LogFilter.ServerLoading, "Loaded {0} arena teams in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
		}

		public void SetNextArenaTeamId(uint Id)
		{
			_nextArenaTeamId = Id;
		}

		public Dictionary<uint, ArenaTeam> GetArenaTeamMap()
		{
			return _arenaTeamStorage;
		}
	}
}