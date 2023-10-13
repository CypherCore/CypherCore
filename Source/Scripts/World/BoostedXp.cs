// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Configuration;
using Framework.Constants;
using Framework.Database;
using Framework.Networking;
using Game.Entities;
using System.Collections.Generic;
using Game.AI;
using Game.Scripting;
using Game.Spells;
using Game;
using System;

namespace Scripts.World.Achievements
{
    [Script]
    class xp_boost_PlayerScript : PlayerScript
    {
        public xp_boost_PlayerScript() : base("xp_boost_PlayerScript") { }

        public override uint OnGiveXP(Player player, uint amount, Unit unit)
        {
            if (IsXPBoostActive())
                amount *= (uint)WorldConfig.GetFloatValue(WorldCfg.RateXpBoost);

            return amount;
        }

        bool IsXPBoostActive()
        {
            long time = GameTime.GetGameTime();
            DateTime localTm = Time.UnixTimeToDateTime(time);
            uint weekdayMaskBoosted = WorldConfig.GetUIntValue(WorldCfg.XpBoostDaymask);
            uint weekdayMask = (1u << (int)localTm.DayOfWeek);
            bool currentDayBoosted = (weekdayMask & weekdayMaskBoosted) != 0;
            return currentDayBoosted;
        }
    }
}

