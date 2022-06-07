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
using Framework.IO;
using Game.DataStorage;
using Game.Entities;
using System.Collections.Generic;

namespace Game.Chat.Commands
{
    [CommandGroup("achievement", RBACPermissions.CommandAchievement)]
    class AchievementCommand
    {
        [Command("add", RBACPermissions.CommandAchievementAdd)]
        static bool HandleAchievementAddCommand(CommandHandler handler, uint achievemntId)
        {
            AchievementRecord achievementEntry = CliDB.AchievementStorage.LookupByKey(achievemntId);
            if (achievementEntry == null)
                return false;

            Player target = handler.GetSelectedPlayer();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }
            
            target.CompletedAchievement(achievementEntry);

            return true;
        }
    }
}
