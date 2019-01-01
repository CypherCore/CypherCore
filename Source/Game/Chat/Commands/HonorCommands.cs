/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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
using Game.Entities;

namespace Game.Chat.Commands
{
    [CommandGroup("honor", RBACPermissions.CommandHonor)]
    class HonorCommands
    {
        [Command("update", RBACPermissions.CommandHonorUpdate)]
        static bool HandleHonorUpdateCommand(StringArguments args, CommandHandler handler)
        {
            Player target = handler.getSelectedPlayer();
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

        [CommandGroup("add", RBACPermissions.CommandHonorAdd)]
        class HonorAddCommands
        {
            [Command("", RBACPermissions.CommandHonorAdd)]
            static bool HandleHonorAddCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                Player target = handler.getSelectedPlayer();
                if (!target)
                {
                    handler.SendSysMessage(CypherStrings.PlayerNotFound);
                    return false;
                }

                // check online security
                if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                    return false;

                int amount = args.NextInt32();
                target.RewardHonor(null, 1, amount);
                return true;
            }

            [Command("kill", RBACPermissions.CommandHonorAddKill)]
            static bool HandleHonorAddKillCommand(StringArguments args, CommandHandler handler)
            {
                Unit target = handler.getSelectedUnit();
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
