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


namespace Scripts.Northrend.Naxxramas
{
    struct TextIds
    {
        public const uint SayAggro = 0;
        public const uint SayGreet = 1;
        public const uint SaySlay = 2;

        public const uint EmoteLocust = 3;

        public const uint EmoteFrenzy = 0;
        public const uint EmoteSpawn = 1;
        public const uint EmoteScarab = 2;
    }

    struct EventIds
    {
        public const uint Impale = 1;        // Cast Impale On A Random Target
        public const uint Locust = 2;                               // Begin Channeling Locust Swarm
        public const uint LocustEnds = 3;                          // Locust Swarm Dissipates
        public const uint SpawnGuard = 4;                          // 10-Man Only - Crypt Guard Has Delayed Spawn; Also Used For The Locust Swarm Crypt Guard In Both Modes
        public const uint Scarabs = 5;                              // Spawn Corpse Scarabs
        public const uint Berserk = 6;                               // Berserk
    }

    struct SpellIds
    {
        public const uint Impale = 28783;    // 25-Man: 56090
        public const uint LocustSwarm = 28785;    // 25-Man: 54021
        public const uint SummonCorpseScarabsPlr = 29105;    // This Spawns 5 Corpse Scarabs On Top Of Player
        public const uint SummonCorpseScarabsMob = 28864;   // This Spawns 10 Corpse Scarabs On Top Of Dead Guards
        public const uint Berserk = 27680;
    }

    struct Misc
    {
        public const uint achievTimedStartEvent = 9891;

        public const uint PhaseNormal = 1;
        public const uint PhaseSwarm = 2;

        public const uint SpawnGroupsInitial25M = 1;
        public const uint SpawnGroupsSingleSpawn = 2;
    }
}
