// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.IO;
using Game.Entities;

namespace Game.Chat.Commands
{
    [CommandGroup("honor")]
    class HonorCommands
    {
        [Command("update", RBACPermissions.CommandHonorUpdate)]
        static bool HandleHonorUpdateCommand(CommandHandler handler)
        {
            Player target = handler.GetSelectedPlayer();
            if (target == null)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            // check online security
            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            target.UpdateHonorFields();
            return true;
        }

        [CommandGroup("add")]
        class HonorAddCommands
        {
            [Command("", RBACPermissions.CommandHonorAdd)]
            static bool HandleHonorAddCommand(CommandHandler handler, int amount)
            {
                Player target = handler.GetSelectedPlayer();
                if (target == null)
                {
                    handler.SendSysMessage(CypherStrings.PlayerNotFound);
                    return false;
                }

                // check online security
                if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                    return false;

                target.RewardHonor(null, 1, amount);
                return true;
            }

            [Command("kill", RBACPermissions.CommandHonorAddKill)]
            static bool HandleHonorAddKillCommand(CommandHandler handler)
            {
                Unit target = handler.GetSelectedUnit();
                if (target == null)
                {
                    handler.SendSysMessage(CypherStrings.PlayerNotFound);
                    return false;
                }

                // check online security
                Player player = target.ToPlayer();
                if (player != null)
                    if (handler.HasLowerSecurity(player, ObjectGuid.Empty))
                        return false;

                handler.GetPlayer().RewardHonor(target, 1);
                return true;
            }
        }
    }
}
