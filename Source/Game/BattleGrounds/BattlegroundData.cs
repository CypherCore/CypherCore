// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.BattleGrounds
{
    public class BattlegroundData
    {
        public BattlegroundData()
        {
            for (var i = 0; i < (int)BattlegroundBracketId.Max; ++i)
                ClientBattlegroundIds[i] = new List<uint>();
        }

        public Dictionary<uint, Battleground> Battlegrounds { get; set; } = new();
        public List<uint>[] ClientBattlegroundIds { get; set; } = new List<uint>[(int)BattlegroundBracketId.Max];
        public Battleground Template { get; set; }
    }
}