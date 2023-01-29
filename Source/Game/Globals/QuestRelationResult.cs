// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Game
{
    public class QuestRelationResult : List<uint>
    {
        private readonly bool _onlyActive;

        public QuestRelationResult()
        {
        }

        public QuestRelationResult(List<uint> range, bool onlyActive) : base(range)
        {
            _onlyActive = onlyActive;
        }

        public bool HasQuest(uint questId)
        {
            return Contains(questId) && (!_onlyActive || Quest.IsTakingQuestEnabled(questId));
        }
    }
}