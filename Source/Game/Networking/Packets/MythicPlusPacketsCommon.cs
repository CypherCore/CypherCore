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

using Game.Entities;
using System.Collections.Generic;

namespace Game.Networking.Packets
{
    public struct DungeonScoreMapSummary
    {
        public int ChallengeModeID;
        public float MapScore;
        public int BestRunLevel;
        public int BestRunDurationMS;
        public bool FinishedSuccess;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(ChallengeModeID);
            data.WriteFloat(MapScore);
            data.WriteInt32(BestRunLevel);
            data.WriteInt32(BestRunDurationMS);
            data.WriteBit(FinishedSuccess);
            data.FlushBits();
        }
    }

    public class DungeonScoreSummary
    {
        public float CurrentSeasonScore;
        public float LifetimeBestSeasonScore;
        public List<DungeonScoreMapSummary> Runs = new();

        public void Write(WorldPacket data)
        {
            data.WriteFloat(CurrentSeasonScore);
            data.WriteFloat(LifetimeBestSeasonScore);
            data.WriteInt32(Runs.Count);
            foreach (var dungeonScoreMapSummary in Runs)
                dungeonScoreMapSummary.Write(data);
        }
    }

    public struct MythicPlusMember
    {
        public ObjectGuid BnetAccountGUID;
        public ulong GuildClubMemberID;
        public ObjectGuid GUID;
        public ObjectGuid GuildGUID;
        public uint NativeRealmAddress;
        public uint VirtualRealmAddress;
        public int ChrSpecializationID;
        public short RaceID;
        public int ItemLevel;
        public int CovenantID;
        public int SoulbindID;

        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(BnetAccountGUID);
            data.WriteUInt64(GuildClubMemberID);
            data.WritePackedGuid(GUID);
            data.WritePackedGuid(GuildGUID);
            data.WriteUInt32(NativeRealmAddress);
            data.WriteUInt32(VirtualRealmAddress);
            data.WriteInt32(ChrSpecializationID);
            data.WriteInt16(RaceID);
            data.WriteInt32(ItemLevel);
            data.WriteInt32(CovenantID);
            data.WriteInt32(SoulbindID);
        }
    }

    public class MythicPlusRun
    {
        public int MapChallengeModeID;
        public bool Completed;
        public uint Level;
        public int DurationMs;
        public long StartDate;
        public long CompletionDate;
        public int Season;
        public List<MythicPlusMember> Members = new();
        public float RunScore;
        public int[] KeystoneAffixIDs = new int[4];

        public void Write(WorldPacket data)
        {
            data.WriteInt32(MapChallengeModeID);
            data.WriteUInt32(Level);
            data.WriteInt32(DurationMs);
            data.WriteInt64(StartDate);
            data.WriteInt64(CompletionDate);
            data.WriteInt32(Season);
            foreach (var id in KeystoneAffixIDs)
                data.WriteInt32(id);

            data.WriteInt32(Members.Count);
            data.WriteFloat(RunScore);
            foreach (var member in Members)
                member.Write(data);

            data.WriteBit(Completed);
            data.FlushBits();
        }
    }

    public class DungeonScoreBestRunForAffix
    {
        public int KeystoneAffixID;
        public MythicPlusRun Run = new();
        public float Score;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(KeystoneAffixID);
            data.WriteFloat(Score);
            Run.Write(data);
        }
    }

    public class DungeonScoreMapData
    {
        public int MapChallengeModeID;
        public List<DungeonScoreBestRunForAffix> BestRuns = new();
        public float OverAllScore;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(MapChallengeModeID);
            data.WriteInt32(BestRuns.Count);
            data.WriteFloat(OverAllScore);
            foreach (var bestRun in BestRuns)
                bestRun.Write(data);
        }
    }

    public class DungeonScoreSeasonData
    {
        public int Season;
        public List<DungeonScoreMapData> Maps = new();
        public float SeasonScore;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(Season);
            data.WriteInt32(Maps.Count);
            data.WriteUInt32(0);
            data.WriteFloat(SeasonScore);
            data.WriteFloat(0);

            foreach (var map in Maps)
                map.Write(data);

            foreach (var map in Maps)
                map.Write(data);
        }
    }

    public class DungeonScoreData
    {
        public List<DungeonScoreSeasonData> Seasons = new();
        public int TotalRuns;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(Seasons.Count);
            data.WriteInt32(TotalRuns);
            foreach (var season in Seasons)
                season.Write(data);
        }
    }
}