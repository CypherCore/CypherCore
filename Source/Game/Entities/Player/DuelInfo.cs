// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Entities
{
    public class DuelInfo
	{
		public Player Initiator { get; set; }
        public bool IsMounted { get; set; }
        public Player Opponent { get; set; }
        public long OutOfBoundsTime { get; set; }
        public long StartTime { get; set; }
        public DuelState State { get; set; }

        public DuelInfo(Player opponent, Player initiator, bool isMounted)
		{
			Opponent  = opponent;
			Initiator = initiator;
			IsMounted = isMounted;
		}
	}
}