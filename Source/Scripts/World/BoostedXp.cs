/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Game;
using Game.Entities;
using Game.Scripting;

namespace Scripts.World
{
    class xp_boost_PlayerScript : PlayerScript
    {
        public xp_boost_PlayerScript() : base("xp_boost_PlayerScript") { }

        void OnGiveXP(Player player, ref uint amount, Unit unit)
        {
            if (IsXPBoostActive())
                amount *= (uint)WorldConfig.GetFloatValue(WorldCfg.RateXpBoost);
        }

        bool IsXPBoostActive()
        {
            long time = GameTime.GetGameTime();
            var localTm = Time.UnixTimeToDateTime(time);
            uint weekdayMaskBoosted = WorldConfig.GetUIntValue(WorldCfg.XpBoostDaymask);
            uint weekdayMask = 1u << localTm.Day;
            bool currentDayBoosted = (weekdayMask & weekdayMaskBoosted) != 0;
            return currentDayBoosted;
        }
    }
}
