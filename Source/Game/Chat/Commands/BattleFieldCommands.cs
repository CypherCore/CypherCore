// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.IO;
using Game.BattleFields;

namespace Game.Chat
{
    [CommandGroup("bf")]
    class BattleFieldCommands
    {
        [Command("enable", RBACPermissions.CommandBfEnable)]
        static bool HandleBattlefieldEnable(CommandHandler handler, uint battleId)
        {
            BattleField bf = Global.BattleFieldMgr.GetBattlefieldByBattleId(handler.GetPlayer().GetMap(), battleId);
            if (bf == null)
                return false;

            if (bf.IsEnabled())
            {
                bf.ToggleBattlefield(false);
                if (battleId == 1)
                    handler.SendGlobalGMSysMessage("Wintergrasp is disabled");
            }
            else
            {
                bf.ToggleBattlefield(true);
                if (battleId == 1)
                    handler.SendGlobalGMSysMessage("Wintergrasp is enabled");
            }

            return true;
        }

        [Command("start", RBACPermissions.CommandBfStart)]
        static bool HandleBattlefieldStart(CommandHandler handler, uint battleId)
        {
            BattleField bf = Global.BattleFieldMgr.GetBattlefieldByBattleId(handler.GetPlayer().GetMap(), battleId);
            if (bf == null)
                return false;

            bf.StartBattle();

            if (battleId == 1)
                handler.SendGlobalGMSysMessage("Wintergrasp (Command start used)");

            return true;
        }

        [Command("stop", RBACPermissions.CommandBfStop)]
        static bool HandleBattlefieldEnd(CommandHandler handler, uint battleId)
        {
            BattleField bf = Global.BattleFieldMgr.GetBattlefieldByBattleId(handler.GetPlayer().GetMap(), battleId);
            if (bf == null)
                return false;

            bf.EndBattle(true);

            if (battleId == 1)
                handler.SendGlobalGMSysMessage("Wintergrasp (Command stop used)");

            return true;
        }

        [Command("switch", RBACPermissions.CommandBfSwitch)]
        static bool HandleBattlefieldSwitch(CommandHandler handler, uint battleId)
        {
            BattleField bf = Global.BattleFieldMgr.GetBattlefieldByBattleId(handler.GetPlayer().GetMap(), battleId);
            if (bf == null)
                return false;

            bf.EndBattle(false);
            if (battleId == 1)
                handler.SendGlobalGMSysMessage("Wintergrasp (Command switch used)");

            return true;
        }

        [Command("timer", RBACPermissions.CommandBfTimer)]
        static bool HandleBattlefieldTimer(CommandHandler handler, uint battleId, uint time)
        {
            BattleField bf = Global.BattleFieldMgr.GetBattlefieldByBattleId(handler.GetPlayer().GetMap(), battleId);
            if (bf == null)
                return false;

            bf.SetTimer(time * Time.InMilliseconds);
            if (battleId == 1)
                handler.SendGlobalGMSysMessage("Wintergrasp (Command timer used)");

            return true;
        }
    }
}
