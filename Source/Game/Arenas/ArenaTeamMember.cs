// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;

namespace Game.Arenas
{
    public class ArenaTeamMember
	{
		public byte Class { get; set; }
        public ObjectGuid Guid;
		public ushort MatchMakerRating { get; set; }
        public string Name { get; set; }
        public ushort PersonalRating { get; set; }
        public ushort SeasonGames { get; set; }
        public ushort SeasonWins { get; set; }
        public ushort WeekGames { get; set; }
        public ushort WeekWins { get; set; }

        public void ModifyPersonalRating(Player player, int mod, uint type)
		{
			if (PersonalRating + mod < 0)
				PersonalRating = 0;
			else
				PersonalRating += (ushort)mod;

			if (player)
			{
				player.SetArenaTeamInfoField(ArenaTeam.GetSlotByType(type), ArenaTeamInfoType.PersonalRating, PersonalRating);
				player.UpdateCriteria(CriteriaType.EarnPersonalArenaRating, PersonalRating, type);
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
}