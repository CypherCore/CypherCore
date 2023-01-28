// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Entities
{
    internal struct QuestObjectiveStatusData
	{
		public (uint QuestID, QuestStatusData Status) QuestStatusPair { get; set; }
        public QuestObjective Objective { get; set; }
    }
}