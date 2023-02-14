// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
