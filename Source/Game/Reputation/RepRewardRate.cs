// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game
{
    public class RepRewardRate
    {
        public float CreatureRate { get; set; } // no reputation are given at all for this faction/rate Type.
        public float QuestDailyRate { get; set; }
        public float QuestMonthlyRate { get; set; }
        public float QuestRate { get; set; } // We allow rate = 0.0 in database. For this case, it means that
        public float QuestRepeatableRate { get; set; }
        public float QuestWeeklyRate { get; set; }
        public float SpellRate { get; set; }
    }
}