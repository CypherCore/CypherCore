// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.BattleGrounds
{
	public class BattlegroundScore
	{
		public uint BonusHonor;
		public uint DamageDone;
		public uint Deaths;
		public uint HealingDone;
		public uint HonorableKills;

		// Default score, present in every type
		public uint KillingBlows;

		public ObjectGuid PlayerGuid;
		public int TeamId;

		public BattlegroundScore(ObjectGuid playerGuid, Team team)
		{
			PlayerGuid = playerGuid;
			TeamId     = (int)(team == Team.Alliance ? PvPTeamId.Alliance : PvPTeamId.Horde);
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

		public virtual void BuildPvPLogPlayerDataPacket(out PVPMatchStatistics.PVPMatchPlayerStatistics playerData)
		{
			playerData            = new PVPMatchStatistics.PVPMatchPlayerStatistics();
			playerData.PlayerGUID = PlayerGuid;
			playerData.Kills      = KillingBlows;
			playerData.Faction    = (byte)TeamId;

			if (HonorableKills != 0 ||
			    Deaths != 0 ||
			    BonusHonor != 0)
			{
				PVPMatchStatistics.HonorData playerDataHonor = new();
				playerDataHonor.HonorKills         = HonorableKills;
				playerDataHonor.Deaths             = Deaths;
				playerDataHonor.ContributionPoints = BonusHonor;
				playerData.Honor                   = playerDataHonor;
			}

			playerData.DamageDone  = DamageDone;
			playerData.HealingDone = HealingDone;
		}

		public virtual uint GetAttr1()
		{
			return 0;
		}

		public virtual uint GetAttr2()
		{
			return 0;
		}

		public virtual uint GetAttr3()
		{
			return 0;
		}

		public virtual uint GetAttr4()
		{
			return 0;
		}

		public virtual uint GetAttr5()
		{
			return 0;
		}
	}
}