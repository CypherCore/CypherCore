// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
        public float OverallScoreCurrentSeason;
        public float LadderScoreCurrentSeason;
        public List<DungeonScoreMapSummary> Runs = new();

        public void Write(WorldPacket data)
        {
            data.WriteFloat(OverallScoreCurrentSeason);
            data.WriteFloat(LadderScoreCurrentSeason);
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
        public List<DungeonScoreMapData> SeasonMaps = new();
        public List<DungeonScoreMapData> LadderMaps = new();
        public float SeasonScore;
        public float LadderScore = 0.0f;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(Season);
            data.WriteInt32(SeasonMaps.Count);
            data.WriteInt32(LadderMaps.Count);
            data.WriteFloat(SeasonScore);
            data.WriteFloat(LadderScore);

            foreach (var map in SeasonMaps)
                map.Write(data);

            foreach (var map in LadderMaps)
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