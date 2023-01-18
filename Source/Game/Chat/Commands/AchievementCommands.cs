// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.IO;
using Game.DataStorage;
using Game.Entities;
using System.Collections.Generic;

namespace Game.Chat.Commands
{
    [CommandGroup("achievement")]
    class AchievementCommand
    {
        [Command("add", CypherStrings.CommandAchievementAddHelp, RBACPermissions.CommandAchievementAdd)]
        static bool HandleAchievementAddCommand(CommandHandler handler, AchievementRecord achievementEntry)
        {
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
