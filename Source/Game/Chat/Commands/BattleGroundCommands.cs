// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.BattleGrounds;

namespace Game.Chat.Commands
{
    [CommandGroup("bg")]
    class BattleGroundCommands
    {
        [Command("start", RBACPermissions.CommandBgStart)]
        static bool HandleBgStartCommand(CommandHandler handler)
        {
            Battleground bg = handler.GetPlayer().GetBattleground();
            if (bg != null)
            {
                handler.SendSysMessage(CypherStrings.CommandNoBattlegroundFound);
                return false;
            }

            bg.SetStartDelayTime(0);

            return true;
        }

        [Command("stop", RBACPermissions.CommandBgStop)]
        static bool HandleBgStopCommand(CommandHandler handler)
        {
            Battleground bg = handler.GetPlayer().GetBattleground();
            if (bg == null)
            {
                handler.SendSysMessage(CypherStrings.CommandNoBattlegroundFound);
                return false;
            }

            bg.EndBattleground(Team.Other);

            return true;
        }
    }
}
