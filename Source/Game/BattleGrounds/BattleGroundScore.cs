// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;
using System.Collections.Generic;

namespace Game.BattleGrounds
{
    public class BattlegroundScore
    {
        public ObjectGuid PlayerGuid;
        public byte TeamId; // PvPTeamId

        // Default score, present in every type
        public uint KillingBlows;
        public uint Deaths;
        public uint HonorableKills;
        public uint BonusHonor;
        public uint DamageDone;
        public uint HealingDone;

        public uint PreMatchRating;
        public uint PreMatchMMR;
        public uint PostMatchRating;
        public uint PostMatchMMR;

        public Dictionary<uint /*pvpStatID*/, uint /*value*/> PvpStats = new();

        List<uint> _validPvpStatIds;

        public BattlegroundScore(ObjectGuid playerGuid, Team team, List<uint> pvpStatIds)
        {
            PlayerGuid = playerGuid;
            TeamId = (byte)(team == Team.Alliance ? PvPTeamId.Alliance : PvPTeamId.Horde);
            _validPvpStatIds = pvpStatIds;

            if (_validPvpStatIds != null)
                foreach (uint pvpStatId in _validPvpStatIds)
                    PvpStats[pvpStatId] = 0;
        }

        public virtual void UpdateScore(ScoreType type, uint value)
        {
            switch (type)
            {
                case ScoreType.KillingBlows:
                    KillingBlows += value;
                    break;
                case ScoreType.Deaths:
                    Deaths += value;
                    break;
                case ScoreType.HonorableKills:
                    HonorableKills += value;
                    break;
                case ScoreType.BonusHonor:
                    BonusHonor += value;
                    break;
                case ScoreType.DamageDone:
                    DamageDone += value;
                    break;
                case ScoreType.HealingDone:
                    HealingDone += value;
                    break;
                default:
                    Cypher.Assert(false, "Not implemented Battleground score type!");
                    break;
            }
        }

        public void UpdatePvpStat(uint pvpStatID, uint value)
        {
            if (_validPvpStatIds == null)
                return;

            if (!_validPvpStatIds.Contains(pvpStatID))
            {
                Log.outWarn(LogFilter.Battleground, $"Tried updating PvpStat {pvpStatID} but this stat is not allowed on this map");
                return;
            }

            PvpStats[pvpStatID] += value;
            Player player = Global.ObjAccessor.FindConnectedPlayer(PlayerGuid);
            if (player != null)
                player.UpdateCriteria(CriteriaType.TrackedWorldStateUIModified, pvpStatID);
        }

        public uint GetAttr(byte index)
        {
            return PvpStats.LookupByKey(index);
        }

        public virtual void BuildPvPLogPlayerDataPacket(out PVPMatchStatistics.PVPMatchPlayerStatistics playerData)
        {
            playerData = new PVPMatchStatistics.PVPMatchPlayerStatistics();
            playerData.PlayerGUID = PlayerGuid;
            playerData.Kills = KillingBlows;
            playerData.Faction = TeamId;
            if (HonorableKills != 0 || Deaths != 0 || BonusHonor != 0)
            {
                PVPMatchStatistics.HonorData playerDataHonor = new();
                playerDataHonor.HonorKills = HonorableKills;
                playerDataHonor.Deaths = Deaths;
                playerDataHonor.ContributionPoints = BonusHonor;
                playerData.Honor = playerDataHonor;
            }

            playerData.DamageDone = DamageDone;
            playerData.HealingDone = HealingDone;

            if (PreMatchRating != 0)
                playerData.PreMatchRating = PreMatchRating;

            if (PostMatchRating != PreMatchRating)
                playerData.RatingChange = (int)(PostMatchRating - PreMatchRating);

            if (PreMatchMMR != 0)
                playerData.PreMatchMMR = PreMatchMMR;

            if (PostMatchMMR != PreMatchMMR)
                playerData.MmrChange = (int)(PostMatchMMR - PreMatchMMR);

            foreach (var (pvpStatID, value) in PvpStats)
                playerData.Stats.Add(new PVPMatchStatistics.PVPMatchPlayerPVPStat((int)pvpStatID, value));
        }

        public override string ToString()
        {
            return $"Damage done: {DamageDone}, Healing done: {HealingDone}, Killing blows: {KillingBlows}, PreMatchRating: {PreMatchRating}, PreMatchMMR: {PreMatchMMR}, PostMatchRating: {PostMatchRating}, PostMatchMMR: {PostMatchMMR}";
        }

        public uint GetKillingBlows() { return KillingBlows; }
        public uint GetDeaths() { return Deaths; }
        public uint GetHonorableKills() { return HonorableKills; }
        public uint GetBonusHonor() { return BonusHonor; }
        public uint GetDamageDone() { return DamageDone; }
        public uint GetHealingDone() { return HealingDone; }
    }
}
