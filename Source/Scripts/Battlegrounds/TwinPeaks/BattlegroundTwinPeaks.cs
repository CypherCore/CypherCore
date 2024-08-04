// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.BattleGrounds;
using Game.Maps;
using Game.Scripting;

namespace Scripts.Battlegrounds.TwinPeaks
{
    [Script(nameof(battleground_twin_peaks), 726)]
    class battleground_twin_peaks : BattlegroundScript
    {
        enum PvpStats : uint
        {
            BG_TP_FLAG_CAPTURES = 290,
            BG_TP_FLAG_RETURNS = 291
        }

        public battleground_twin_peaks(BattlegroundMap map) : base(map) { }
    }
}
