// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
            if (!target)
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
                if (!target)
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
                if (!target)
                {
                    handler.SendSysMessage(CypherStrings.PlayerNotFound);
                    return false;
                }

                // check online security
                Player player = target.ToPlayer();
                if (player)
                    if (handler.HasLowerSecurity(player, ObjectGuid.Empty))
                        return false;

                handler.GetPlayer().RewardHonor(target, 1);
                return true;
            }
        }
    }
}
