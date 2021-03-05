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
using Game.BattleFields;

namespace Game.Chat
{
    [CommandGroup("bf", RBACPermissions.CommandBf)]
    internal class BattleFieldCommands
    {
        [Command("enable", RBACPermissions.CommandBfEnable)]
        private static bool HandleBattlefieldEnable(StringArguments args, CommandHandler handler)
        {
            var battleid = args.NextUInt32();
            var bf = Global.BattleFieldMgr.GetBattlefieldByBattleId(battleid);

            if (bf == null)
                return false;

            if (bf.IsEnabled())
            {
                bf.ToggleBattlefield(false);
                if (battleid == 1)
                    handler.SendGlobalGMSysMessage("Wintergrasp is disabled");
            }
            else
            {
                bf.ToggleBattlefield(true);
                if (battleid == 1)
                    handler.SendGlobalGMSysMessage("Wintergrasp is enabled");
            }

            return true;
        }

        [Command("start", RBACPermissions.CommandBfStart)]
        private static bool HandleBattlefieldStart(StringArguments args, CommandHandler handler)
        {
            var battleid = args.NextUInt32();
            var bf = Global.BattleFieldMgr.GetBattlefieldByBattleId(battleid);

            if (bf == null)
                return false;

            bf.StartBattle();

            if (battleid == 1)
                handler.SendGlobalGMSysMessage("Wintergrasp (Command start used)");

            return true;
        }

        [Command("stop", RBACPermissions.CommandBfStop)]
        private static bool HandleBattlefieldEnd(StringArguments args, CommandHandler handler)
        {
            var battleid = args.NextUInt32();
            var bf = Global.BattleFieldMgr.GetBattlefieldByBattleId(battleid);

            if (bf == null)
                return false;

            bf.EndBattle(true);

            if (battleid == 1)
                handler.SendGlobalGMSysMessage("Wintergrasp (Command stop used)");

            return true;
        }

        [Command("switch", RBACPermissions.CommandBfSwitch)]
        private static bool HandleBattlefieldSwitch(StringArguments args, CommandHandler handler)
        {
            var battleid = args.NextUInt32();
            var bf = Global.BattleFieldMgr.GetBattlefieldByBattleId(battleid);

            if (bf == null)
                return false;

            bf.EndBattle(false);
            if (battleid == 1)
                handler.SendGlobalGMSysMessage("Wintergrasp (Command switch used)");

            return true;
        }

        [Command("timer", RBACPermissions.CommandBfTimer)]
        private static bool HandleBattlefieldTimer(StringArguments args, CommandHandler handler)
        {
            var battleid = args.NextUInt32();
            var bf = Global.BattleFieldMgr.GetBattlefieldByBattleId(battleid);

            if (bf == null)
                return false;

            var time = args.NextUInt32();

            bf.SetTimer(time * Time.InMilliseconds);
            bf.SendInitWorldStatesToAll();
            if (battleid == 1)
                handler.SendGlobalGMSysMessage("Wintergrasp (Command timer used)");

            return true;
        }
    }
}
