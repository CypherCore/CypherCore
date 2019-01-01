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

using Framework.Database;
using Game.Entities;
using System.Collections.Generic;

namespace Game.Arenas
{
    public class ArenaTeamManager : Singleton<ArenaTeamManager>
    {
        ArenaTeamManager()
        {
            NextArenaTeamId = 1;
        }

        public ArenaTeam GetArenaTeamById(uint arenaTeamId)
        {
            return ArenaTeamStorage.LookupByKey(arenaTeamId);
        }

        public ArenaTeam GetArenaTeamByName(string arenaTeamName)
        {
            string search = arenaTeamName.ToLower();
            foreach (var team in ArenaTeamStorage.Values)
            {
                string teamName = team.GetName().ToLower();
                if (search == teamName)
                    return team;
            }
            return null;
        }

        public ArenaTeam GetArenaTeamByCaptain(ObjectGuid guid)
        {
            foreach (var pair in ArenaTeamStorage)
                if (pair.Value.GetCaptain() == guid)
                    return pair.Value;

            return null;
        }

        public void AddArenaTeam(ArenaTeam arenaTeam)
        {
            ArenaTeamStorage[arenaTeam.GetId()] = arenaTeam;
        }

        public void RemoveArenaTeam(uint arenaTeamId)
        {
            ArenaTeamStorage.Remove(arenaTeamId);
        }

        public uint GenerateArenaTeamId()
        {
            if (NextArenaTeamId >= 0xFFFFFFFE)
            {
                Log.outError(LogFilter.Battleground, "Arena team ids overflow!! Can't continue, shutting down server. ");
                Global.WorldMgr.StopNow();
            }
            return NextArenaTeamId++;
        }

        public void LoadArenaTeams()
        {
            uint oldMSTime = Time.GetMSTime();

            // Clean out the trash before loading anything
            DB.Characters.DirectExecute("DELETE FROM arena_team_member WHERE arenaTeamId NOT IN (SELECT arenaTeamId FROM arena_team)");       // One-time query

            //                                                        0        1         2         3          4              5            6            7           8
            SQLResult result = DB.Characters.Query("SELECT arenaTeamId, name, captainGuid, type, backgroundColor, emblemStyle, emblemColor, borderStyle, borderColor, " +
                //      9        10        11         12           13       14
                "rating, weekGames, weekWins, seasonGames, seasonWins, rank FROM arena_team ORDER BY arenaTeamId ASC");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 arena teams. DB table `arena_team` is empty!");
                return;
            }

            SQLResult result2 = DB.Characters.Query(
                //              0              1           2             3              4                 5          6     7          8                  9
                "SELECT arenaTeamId, atm.guid, atm.weekGames, atm.weekWins, atm.seasonGames, atm.seasonWins, c.name, class, personalRating, matchMakerRating FROM arena_team_member atm" +
                " INNER JOIN arena_team ate USING (arenaTeamId) LEFT JOIN characters AS c ON atm.guid = c.guid" +
                " LEFT JOIN character_arena_stats AS cas ON c.guid = cas.guid AND (cas.slot = 0 AND ate.type = 2 OR cas.slot = 1 AND ate.type = 3 OR cas.slot = 2 AND ate.type = 5)" +
                " ORDER BY atm.arenateamid ASC");

            uint count = 0;
            do
            {
                ArenaTeam newArenaTeam = new ArenaTeam();

                if (!newArenaTeam.LoadArenaTeamFromDB(result) || !newArenaTeam.LoadMembersFromDB(result2))
                {
                    newArenaTeam.Disband(null);
                    continue;
                }

                AddArenaTeam(newArenaTeam);

                ++count;
            }
            while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} arena teams in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void SetNextArenaTeamId(uint Id) { NextArenaTeamId = Id; }

        public Dictionary<uint, ArenaTeam> GetArenaTeamMap() { return ArenaTeamStorage; }

        uint NextArenaTeamId;
        Dictionary<uint, ArenaTeam> ArenaTeamStorage = new Dictionary<uint, ArenaTeam>();
    }
}
