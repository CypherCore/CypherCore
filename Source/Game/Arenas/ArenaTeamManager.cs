// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
            foreach (var (_, team) in ArenaTeamStorage)
                if (search == team.GetName().ToLower())
                    return team;

            return null;
        }

        public ArenaTeam GetArenaTeamByCaptain(ObjectGuid guid)
        {
            foreach (var (_, team) in ArenaTeamStorage)
                if (team.GetCaptain() == guid)
                    return team;

            return null;
        }

        public void AddArenaTeam(ArenaTeam arenaTeam)
        {
            var added = ArenaTeamStorage.TryAdd(arenaTeam.GetId(), arenaTeam);
            Cypher.Assert(!added, $"Duplicate arena team with ID {arenaTeam.GetId()}");
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
                "rating, weekGames, weekWins, seasonGames, seasonWins, `rank` FROM arena_team ORDER BY arenaTeamId ASC");
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
                ArenaTeam newArenaTeam = new();

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
        Dictionary<uint, ArenaTeam> ArenaTeamStorage = new();
    }
}
