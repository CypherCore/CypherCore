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
    [CommandGroup("cheat", RBACPermissions.CommandCheat)]
    class CheatCommands
    {
        [Command("god", RBACPermissions.CommandCheatGod)]
        static bool HandleGodModeCheat(StringArguments args, CommandHandler handler)
        {
            if (handler.GetSession() == null || !handler.GetSession().GetPlayer())
                return false;

            string argstr = args.NextString();
            if (args.Empty())
                argstr = (handler.GetSession().GetPlayer().GetCommandStatus(PlayerCommandStates.God)) ? "off" : "on";

            if (argstr == "off")
            {
                handler.GetSession().GetPlayer().SetCommandStatusOff(PlayerCommandStates.God);
                handler.SendSysMessage("Godmode is OFF. You can take damage.");
                return true;
            }
            else if (argstr == "on")
            {
                handler.GetSession().GetPlayer().SetCommandStatusOn(PlayerCommandStates.God);
                handler.SendSysMessage("Godmode is ON. You won't take damage.");
                return true;
            }

            return false;
        }

        [Command("casttime", RBACPermissions.CommandCheatCasttime)]
        static bool HandleCasttimeCheat(StringArguments args, CommandHandler handler)
        {
            if (handler.GetSession() == null || !handler.GetSession().GetPlayer())
                return false;

            string argstr = args.NextString();

            if (args.Empty())
                argstr = (handler.GetSession().GetPlayer().GetCommandStatus(PlayerCommandStates.Casttime)) ? "off" : "on";

            if (argstr == "off")
            {
                handler.GetSession().GetPlayer().SetCommandStatusOff(PlayerCommandStates.Casttime);
                handler.SendSysMessage("CastTime Cheat is OFF. Your spells will have a casttime.");
                return true;
            }
            else if (argstr == "on")
            {
                handler.GetSession().GetPlayer().SetCommandStatusOn(PlayerCommandStates.Casttime);
                handler.SendSysMessage("CastTime Cheat is ON. Your spells won't have a casttime.");
                return true;
            }

            return false;
        }

        [Command("cooldown", RBACPermissions.CommandCheatCooldown)]
        static bool HandleCoolDownCheat(StringArguments args, CommandHandler handler)
        {
            if (handler.GetSession() == null || !handler.GetSession().GetPlayer())
                return false;

            string argstr = args.NextString();

            if (args.Empty())
                argstr = (handler.GetSession().GetPlayer().GetCommandStatus(PlayerCommandStates.Cooldown)) ? "off" : "on";

            if (argstr == "off")
            {
                handler.GetSession().GetPlayer().SetCommandStatusOff(PlayerCommandStates.Cooldown);
                handler.SendSysMessage("Cooldown Cheat is OFF. You are on the global cooldown.");
                return true;
            }
            else if (argstr == "on")
            {
                handler.GetSession().GetPlayer().SetCommandStatusOn(PlayerCommandStates.Cooldown);
                handler.SendSysMessage("Cooldown Cheat is ON. You are not on the global cooldown.");
                return true;
            }

            return false;
        }

        [Command("power", RBACPermissions.CommandCheatPower)]
        static bool HandlePowerCheat(StringArguments args, CommandHandler handler)
        {
            if (handler.GetSession() == null || !handler.GetSession().GetPlayer())
                return false;

            string argstr = args.NextString();

            if (args.Empty())
                argstr = (handler.GetSession().GetPlayer().GetCommandStatus(PlayerCommandStates.Power)) ? "off" : "on";

            if (argstr == "off")
            {
                handler.GetSession().GetPlayer().SetCommandStatusOff(PlayerCommandStates.Power);
                handler.SendSysMessage("Power Cheat is OFF. You need mana/rage/energy to use spells.");
                return true;
            }
            else if (argstr == "on")
            {
                handler.GetSession().GetPlayer().SetCommandStatusOn(PlayerCommandStates.Power);
                handler.SendSysMessage("Power Cheat is ON. You don't need mana/rage/energy to use spells.");
                return true;
            }

            return false;
        }

        [Command("status", RBACPermissions.CommandCheatStatus)]
        static bool HandleCheatStatus(StringArguments args, CommandHandler handler)
        {
            Player player = handler.GetSession().GetPlayer();

            string enabled = "ON";
            string disabled = "OFF";

            handler.SendSysMessage(CypherStrings.CommandCheatStatus);
            handler.SendSysMessage(CypherStrings.CommandCheatGod, player.GetCommandStatus(PlayerCommandStates.God) ? enabled : disabled);
            handler.SendSysMessage(CypherStrings.CommandCheatCd, player.GetCommandStatus(PlayerCommandStates.Cooldown) ? enabled : disabled);
            handler.SendSysMessage(CypherStrings.CommandCheatCt, player.GetCommandStatus(PlayerCommandStates.Casttime) ? enabled : disabled);
            handler.SendSysMessage(CypherStrings.CommandCheatPower, player.GetCommandStatus(PlayerCommandStates.Power) ? enabled : disabled);
            handler.SendSysMessage(CypherStrings.CommandCheatWw, player.GetCommandStatus(PlayerCommandStates.Waterwalk) ? enabled : disabled);
            handler.SendSysMessage(CypherStrings.CommandCheatTaxinodes, player.isTaxiCheater() ? enabled : disabled);
            return true;
        }

        [Command("waterwalk", RBACPermissions.CommandCheatWaterwalk)]
        static bool HandleWaterWalkCheat(StringArguments args, CommandHandler handler)
        {
            if (handler.GetSession() == null || !handler.GetSession().GetPlayer())
                return false;

            string argstr = args.NextString();

            Player target = handler.GetSession().GetPlayer();
            if (args.Empty())
                argstr = (target.GetCommandStatus(PlayerCommandStates.Waterwalk)) ? "off" : "on";

            if (argstr == "off")
            {
                target.SetCommandStatusOff(PlayerCommandStates.Waterwalk);
                target.SetWaterWalking(false);
                handler.SendSysMessage("Waterwalking is OFF. You can't walk on water.");
                return true;
            }
            else if (argstr == "on")
            {
                target.SetCommandStatusOn(PlayerCommandStates.Waterwalk);
                target.SetWaterWalking(true);
                handler.SendSysMessage("Waterwalking is ON. You can walk on water.");
                return true;
            }

            return false;
        }

        [Command("taxi", RBACPermissions.CommandCheatTaxi)]
        static bool HandleTaxiCheatCommand(StringArguments args, CommandHandler handler)
        {
            string argstr = args.NextString();

            Player chr = handler.getSelectedPlayer();
            if (!chr)
                chr = handler.GetSession().GetPlayer();
            else if (handler.HasLowerSecurity(chr, ObjectGuid.Empty)) // check online security
                return false;

            if (args.Empty())
                argstr = (chr.isTaxiCheater()) ? "off" : "on";

            if (argstr == "off")
            {
                chr.SetTaxiCheater(false);
                handler.SendSysMessage(CypherStrings.YouRemoveTaxis, handler.GetNameLink(chr));
                if (handler.needReportToTarget(chr))
                    chr.SendSysMessage(CypherStrings.YoursTaxisRemoved, handler.GetNameLink());

                return true;
            }
            else if (argstr == "on")
            {
                chr.SetTaxiCheater(true);
                handler.SendSysMessage(CypherStrings.YouGiveTaxis, handler.GetNameLink(chr));
                if (handler.needReportToTarget(chr))
                    chr.SendSysMessage(CypherStrings.YoursTaxisAdded, handler.GetNameLink());
                return true;
            }

            handler.SendSysMessage(CypherStrings.UseBol);
            return false;
        }

        [Command("explore", RBACPermissions.CommandCheatExplore)]
        static bool HandleExploreCheat(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            int flag = args.NextInt32();
            Player chr = handler.getSelectedPlayer();
            if (!chr)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            if (flag != 0)
            {
                handler.SendSysMessage(CypherStrings.YouSetExploreAll, handler.GetNameLink(chr));
                if (handler.needReportToTarget(chr))
                    chr.SendSysMessage(CypherStrings.YoursExploreSetAll, handler.GetNameLink());
            }
            else
            {
                handler.SendSysMessage(CypherStrings.YouSetExploreNothing, handler.GetNameLink(chr));
                if (handler.needReportToTarget(chr))
                    chr.SendSysMessage(CypherStrings.YoursExploreSetNothing, handler.GetNameLink());
            }

            for (ushort i = 0; i < PlayerConst.ExploredZonesSize; ++i)
            {
                if (flag != 0)
                    handler.GetSession().GetPlayer().SetFlag(ActivePlayerFields.ExploredZones + i, 0xFFFFFFFF);
                else
                    handler.GetSession().GetPlayer().SetFlag(ActivePlayerFields.ExploredZones + i, 0);
            }

            return true;
        }
    }
}
