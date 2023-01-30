// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game
{
    public struct QuestRewardDisplaySpell
    {
        public uint SpellId;
        public uint PlayerConditionId;

        public QuestRewardDisplaySpell(uint spellId, uint playerConditionId)
        {
            SpellId = spellId;
            PlayerConditionId = playerConditionId;
        }
    }
}