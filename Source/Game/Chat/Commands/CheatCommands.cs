// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.IO;
using Game.Entities;

namespace Game.Chat.Commands
{
    [CommandGroup("cheat")]
    class CheatCommands
    {
        [Command("casttime", RBACPermissions.CommandCheatCasttime)]
        static bool HandleCasttimeCheatCommand(CommandHandler handler, bool? enableArg)
        {
            bool enable = !handler.GetSession().GetPlayer().GetCommandStatus(PlayerCommandStates.Casttime);
            if (enableArg.HasValue)
                enable = enableArg.Value;

            if (enable)
            {
                handler.GetSession().GetPlayer().SetCommandStatusOn(PlayerCommandStates.Casttime);
                handler.SendSysMessage("CastTime Cheat is ON. Your spells won't have a casttime.");
            }
            else
            {
                handler.GetSession().GetPlayer().SetCommandStatusOff(PlayerCommandStates.Casttime);
                handler.SendSysMessage("CastTime Cheat is OFF. Your spells will have a casttime.");
            }

            return true;
        }

        [Command("cooldown", RBACPermissions.CommandCheatCooldown)]
        static bool HandleCoolDownCheatCommand(CommandHandler handler, bool? enableArg)
        {
            bool enable = !handler.GetSession().GetPlayer().GetCommandStatus(PlayerCommandStates.Cooldown);
            if (enableArg.HasValue)
                enable = enableArg.Value;

            if (enable)
            {
                handler.GetSession().GetPlayer().SetCommandStatusOn(PlayerCommandStates.Cooldown);
                handler.SendSysMessage("Cooldown Cheat is ON. You are not on the global cooldown.");
            }
            else
            {
                handler.GetSession().GetPlayer().SetCommandStatusOff(PlayerCommandStates.Cooldown);
                handler.SendSysMessage("Cooldown Cheat is OFF. You are on the global cooldown.");
            }

            return true;
        }

        [Command("explore", RBACPermissions.CommandCheatExplore)]
        static bool HandleExploreCheatCommand(CommandHandler handler, bool reveal)
        {
            Player chr = handler.GetSelectedPlayer();
            if (chr == null)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            if (reveal)
            {
                handler.SendSysMessage(CypherStrings.YouSetExploreAll, handler.GetNameLink(chr));
                if (handler.NeedReportToTarget(chr))
                    chr.SendSysMessage(CypherStrings.YoursExploreSetAll, handler.GetNameLink());
            }
            else
            {
                handler.SendSysMessage(CypherStrings.YouSetExploreNothing, handler.GetNameLink(chr));
                if (handler.NeedReportToTarget(chr))
                    chr.SendSysMessage(CypherStrings.YoursExploreSetNothing, handler.GetNameLink());
            }

            for (ushort i = 0; i < PlayerConst.ExploredZonesSize; ++i)
            {
                if (reveal)
                    handler.GetSession().GetPlayer().AddExploredZones(i, 0xFFFFFFFFFFFFFFFF);
                else
                    handler.GetSession().GetPlayer().RemoveExploredZones(i, 0xFFFFFFFFFFFFFFFF);
            }

            return true;
        }

        [Command("god", RBACPermissions.CommandCheatGod)]
        static bool HandleGodModeCheatCommand(CommandHandler handler, bool? enableArg)
        {
            bool enable = !handler.GetSession().GetPlayer().GetCommandStatus(PlayerCommandStates.God);
            if (enableArg.HasValue)
                enable = enableArg.Value;

            if (enable)
            {
                handler.GetSession().GetPlayer().SetCommandStatusOn(PlayerCommandStates.God);
                handler.SendSysMessage("Godmode is ON. You won't take damage.");
            }
            else
            {
                handler.GetSession().GetPlayer().SetCommandStatusOff(PlayerCommandStates.God);
                handler.SendSysMessage("Godmode is OFF. You can take damage.");
            }

            return true;
        }

        [Command("power", RBACPermissions.CommandCheatPower)]
        static bool HandlePowerCheatCommand(CommandHandler handler, bool? enableArg)
        {
            bool enable = !handler.GetSession().GetPlayer().GetCommandStatus(PlayerCommandStates.Power);
            if (enableArg.HasValue)
                enable = enableArg.Value;

            if (enable)
            {
                Player player = handler.GetSession().GetPlayer();
                // Set max power to all powers
                for (PowerType powerType = 0; powerType < PowerType.Max; ++powerType)
                    player.SetPower(powerType, player.GetMaxPower(powerType));

                player.SetCommandStatusOn(PlayerCommandStates.Power);
                handler.SendSysMessage("Power Cheat is ON. You don't need mana/rage/energy to use spells.");
            }
            else
            {
                handler.GetSession().GetPlayer().SetCommandStatusOff(PlayerCommandStates.Power);
                handler.SendSysMessage("Power Cheat is OFF. You need mana/rage/energy to use spells.");
            }

            return true;
        }

        [Command("status", RBACPermissions.CommandCheatStatus)]
        static bool HandleCheatStatusCommand(CommandHandler handler)
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
            handler.SendSysMessage(CypherStrings.CommandCheatTaxinodes, player.IsTaxiCheater() ? enabled : disabled);
            return true;
        }

        [Command("taxi", RBACPermissions.CommandCheatTaxi)]
        static bool HandleTaxiCheatCommand(CommandHandler handler, bool? enableArg)
        {
            Player chr = handler.GetSelectedPlayer();
            if (chr == null)
                chr = handler.GetSession().GetPlayer();
            else if (handler.HasLowerSecurity(chr, ObjectGuid.Empty)) // check online security
                return false;

            bool enable = !chr.IsTaxiCheater();
            if (enableArg.HasValue)
                enable = enableArg.Value;

            if (enable)
            {
                chr.SetTaxiCheater(true);
                handler.SendSysMessage(CypherStrings.YouGiveTaxis, handler.GetNameLink(chr));
                if (handler.NeedReportToTarget(chr))
                    chr.SendSysMessage(CypherStrings.YoursTaxisAdded, handler.GetNameLink());
            }
            else
            {
                chr.SetTaxiCheater(false);
                handler.SendSysMessage(CypherStrings.YouRemoveTaxis, handler.GetNameLink(chr));
                if (handler.NeedReportToTarget(chr))
                    chr.SendSysMessage(CypherStrings.YoursTaxisRemoved, handler.GetNameLink());
            }

            return true;
        }

        [Command("waterwalk", RBACPermissions.CommandCheatWaterwalk)]
        static bool HandleWaterWalkCheatCommand(CommandHandler handler, bool? enableArg)
        {
            bool enable = !handler.GetSession().GetPlayer().GetCommandStatus(PlayerCommandStates.Waterwalk);
            if (enableArg.HasValue)
                enable = enableArg.Value;

            if (enable)
            {
                handler.GetSession().GetPlayer().SetCommandStatusOn(PlayerCommandStates.Waterwalk);
                handler.GetSession().GetPlayer().SetWaterWalking(true);
                handler.SendSysMessage("Waterwalking is ON. You can walk on water.");
            }
            else
            {
                handler.GetSession().GetPlayer().SetCommandStatusOff(PlayerCommandStates.Waterwalk);
                handler.GetSession().GetPlayer().SetWaterWalking(false);
                handler.SendSysMessage("Waterwalking is OFF. You can't walk on water.");
            }

            return true;
        }
    }
}
