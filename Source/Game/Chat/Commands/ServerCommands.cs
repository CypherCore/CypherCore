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

using Framework.Configuration;
using Framework.Constants;
using Framework.IO;
using System;

namespace Game.Chat
{
    [CommandGroup("server", RBACPermissions.CommandServer, true)]
    class ServerCommands
    {
        [Command("corpses", RBACPermissions.CommandServerCorpses, true)]
        static bool Corpses(StringArguments args, CommandHandler handler)
        {
            Global.WorldMgr.RemoveOldCorpses();
            return true;
        }

        [Command("exit", RBACPermissions.CommandServerExit, true)]
        static bool Exit(StringArguments args, CommandHandler handler)
        {
            handler.SendSysMessage(CypherStrings.CommandExit);
            Global.WorldMgr.StopNow(ShutdownExitCode.Shutdown);
            return true;
        }

        [Command("info", RBACPermissions.CommandServerInfo, true)]
        static bool Info(StringArguments args, CommandHandler handler)
        {
            uint playersNum = Global.WorldMgr.GetPlayerCount();
            uint maxPlayersNum = Global.WorldMgr.GetMaxPlayerCount();
            int activeClientsNum = Global.WorldMgr.GetActiveSessionCount();
            int queuedClientsNum = Global.WorldMgr.GetQueuedSessionCount();
            uint maxActiveClientsNum = Global.WorldMgr.GetMaxActiveSessionCount();
            uint maxQueuedClientsNum = Global.WorldMgr.GetMaxQueuedSessionCount();
            string uptime = Time.secsToTimeString(Global.WorldMgr.GetUptime());
            uint updateTime = Global.WorldMgr.GetUpdateTime();

            handler.SendSysMessage(CypherStrings.ConnectedPlayers, playersNum, maxPlayersNum);
            handler.SendSysMessage(CypherStrings.ConnectedUsers, activeClientsNum, maxActiveClientsNum, queuedClientsNum, maxQueuedClientsNum);
            handler.SendSysMessage(CypherStrings.Uptime, uptime);
            handler.SendSysMessage(CypherStrings.UpdateDiff, updateTime);
            // Can't use Global.WorldMgr.ShutdownMsg here in case of console command
            if (Global.WorldMgr.IsShuttingDown())
                handler.SendSysMessage(CypherStrings.ShutdownTimeleft, Time.secsToTimeString(Global.WorldMgr.GetShutDownTimeLeft()));

            return true;
        }

        [Command("motd", RBACPermissions.CommandServerMotd, true)]
        static bool Motd(StringArguments args, CommandHandler handler)
        {
            string motd = "";
            foreach (var line in Global.WorldMgr.GetMotd())
                motd += line;

            handler.SendSysMessage(CypherStrings.MotdCurrent, motd);
            return true;
        }

        [Command("plimit", RBACPermissions.CommandServerPlimit, true)]
        static bool PLimit(StringArguments args, CommandHandler handler)
        {
            if (!args.Empty())
            {
                string paramStr = args.NextString();
                if (string.IsNullOrEmpty(paramStr))
                    return false;

                switch (paramStr.ToLower())
                {
                    case "player":
                        Global.WorldMgr.SetPlayerSecurityLimit(AccountTypes.Player);
                        break;
                    case "moderator":
                        Global.WorldMgr.SetPlayerSecurityLimit(AccountTypes.Moderator);
                        break;
                    case "gamemaster":
                        Global.WorldMgr.SetPlayerSecurityLimit(AccountTypes.GameMaster);
                        break;
                    case "administrator":
                        Global.WorldMgr.SetPlayerSecurityLimit(AccountTypes.Administrator);
                        break;
                    case "reset":
                        Global.WorldMgr.SetPlayerAmountLimit(ConfigMgr.GetDefaultValue<uint>("PlayerLimit", 100));
                        Global.WorldMgr.LoadDBAllowedSecurityLevel();
                        break;
                    default:
                        if (!int.TryParse(paramStr, out int value))
                            return false;

                        if (value < 0)
                            Global.WorldMgr.SetPlayerSecurityLimit((AccountTypes)(-value));
                        else
                            Global.WorldMgr.SetPlayerAmountLimit((uint)value);
                        break;
                }
            }

            uint playerAmountLimit = Global.WorldMgr.GetPlayerAmountLimit();
            AccountTypes allowedAccountType = Global.WorldMgr.GetPlayerSecurityLimit();
            string secName = "";
            switch (allowedAccountType)
            {
                case AccountTypes.Player:
                    secName = "Player";
                    break;
                case AccountTypes.Moderator:
                    secName = "Moderator";
                    break;
                case AccountTypes.GameMaster:
                    secName = "Gamemaster";
                    break;
                case AccountTypes.Administrator:
                    secName = "Administrator";
                    break;
                default:
                    secName = "<unknown>";
                    break;
            }
            handler.SendSysMessage("Player limits: amount {0}, min. security level {1}.", playerAmountLimit, secName);

            return true;
        }

        static bool IsOnlyUser(WorldSession mySession)
        {
            // check if there is any session connected from a different address
            string myAddr = mySession ? mySession.GetRemoteAddress() : "";
            var sessions = Global.WorldMgr.GetAllSessions();
            foreach (var session in sessions)
                if (session && myAddr != session.GetRemoteAddress())
                    return false;
            return true;
        }

        static bool ParseExitCode(string exitCodeStr, out int exitCode)
        {
            if (!int.TryParse(exitCodeStr, out exitCode))
                return false;

            // Handle atoi() errors
            if (exitCode == 0 && (exitCodeStr[0] != '0' || (exitCodeStr.Length > 1 && exitCodeStr[1] != '\0')))
                return false;

            // Exit code should be in range of 0-125, 126-255 is used
            // in many shells for their own return codes and code > 255
            // is not supported in many others
            if (exitCode < 0 || exitCode > 125)
                return false;

            return true;
        }

        static bool ShutdownServer(StringArguments args,CommandHandler handler, ShutdownMask shutdownMask, ShutdownExitCode defaultExitCode)
        {
            if (args.Empty())
                return false;

            string delayStr = args.NextString();
            if (delayStr.IsEmpty())
                return false;

            int delay;
            if (int.TryParse(delayStr, out delay))
            {
                //  Prevent interpret wrong arg value as 0 secs shutdown time
                if ((delay == 0 && (delayStr[0] != '0' || delayStr.Length > 1 && delayStr[1] != '\0')) || delay < 0)
                    return false;
            }
            else
            {
                delay = (int)Time.TimeStringToSecs(delayStr);

                if (delay == 0)
                    return false;
            }

            string reason = "";
            string exitCodeStr = "";
            string nextToken;
            while (!(nextToken = args.NextString()).IsEmpty())
            {
                if (nextToken.IsNumber())
                    exitCodeStr = nextToken;
                else
                {
                    reason = nextToken;
                    reason += args.NextString("\0");
                    break;
                }
            }

            int exitCode = (int)defaultExitCode;
            if (!exitCodeStr.IsEmpty())
                if (!ParseExitCode(exitCodeStr, out exitCode))
                    return false;

            // Override parameter "delay" with the configuration value if there are still players connected and "force" parameter was not specified
            if (delay < WorldConfig.GetIntValue(WorldCfg.ForceShutdownThreshold) && !shutdownMask.HasAnyFlag(ShutdownMask.Force) && !IsOnlyUser(handler.GetSession()))
            {
                delay = WorldConfig.GetIntValue(WorldCfg.ForceShutdownThreshold);
                handler.SendSysMessage(CypherStrings.ShutdownDelayed, delay);
            }

            Global.WorldMgr.ShutdownServ((uint)delay, shutdownMask, (ShutdownExitCode)exitCode, reason);

            return true;
        }

        [CommandGroup("idleRestart", RBACPermissions.CommandServerIdlerestart, true)]
        class IdleRestartCommands
        {
            [Command("", RBACPermissions.CommandServerIdlerestart, true)]
            static bool HandleServerIdleRestartCommand(StringArguments args, CommandHandler handler)
            {
                return ShutdownServer(args, handler, ShutdownMask.Restart | ShutdownMask.Idle, ShutdownExitCode.Restart);
            }

            [Command("cancel", RBACPermissions.CommandServerIdlerestartCancel, true)]
            static bool Cancel(StringArguments args, CommandHandler handler)
            {
                Global.WorldMgr.ShutdownCancel();
                return true;
            }
        }

        [CommandGroup("idleshutdown", RBACPermissions.CommandServerIdleshutdown, true)]
        class IdleshutdownCommands
        {
            [Command("", RBACPermissions.CommandServerIdleshutdown, true)]
            static bool HandleServerIdleShutDownCommand(StringArguments args, CommandHandler handler)
            {
                return ShutdownServer(args, handler, ShutdownMask.Idle, ShutdownExitCode.Shutdown);
            }

            [Command("cancel", RBACPermissions.CommandServerIdleshutdownCancel, true)]
            static bool Cancel(StringArguments args, CommandHandler handler)
            {
                Global.WorldMgr.ShutdownCancel();

                return true;
            }
        }

        [CommandGroup("restart", RBACPermissions.CommandServerInfo, true)]
        class RestartCommands
        {
            [Command("", RBACPermissions.CommandServerRestart, true)]
            static bool HandleServerRestartCommand(StringArguments args, CommandHandler handler)
            {
                return ShutdownServer(args, handler, ShutdownMask.Restart, ShutdownExitCode.Restart);
            }

            [Command("cancel", RBACPermissions.CommandServerRestartCancel, true)]
            static bool HandleServerRestartCancelCommand(StringArguments args, CommandHandler handler)
            {
                Global.WorldMgr.ShutdownCancel();

                return true;
            }

            [Command("force", RBACPermissions.CommandServerRestartCancel, true)]
            static bool HandleServerForceRestartCommand(StringArguments args, CommandHandler handler)
            {
                return ShutdownServer(args, handler, ShutdownMask.Force | ShutdownMask.Restart, ShutdownExitCode.Restart);
            }
        }

        [CommandGroup("shutdown", RBACPermissions.CommandServerMotd, true)]
        class ShutdownCommands
        {
            [Command("", RBACPermissions.CommandServerShutdown, true)]
            static bool HandleServerShutDownCommand(StringArguments args, CommandHandler handler)
            {
                return ShutdownServer(args, handler, 0, ShutdownExitCode.Shutdown);
            }

            [Command("cancel", RBACPermissions.CommandServerShutdownCancel, true)]
            static bool HandleServerShutDownCancelCommand(StringArguments args, CommandHandler handler)
            {
                uint timer = Global.WorldMgr.ShutdownCancel();
                if (timer != 0)
                    handler.SendSysMessage(CypherStrings.ShutdownCancelled, timer);

                return true;
            }

            [Command("force", RBACPermissions.CommandServerShutdownCancel, true)]
            static bool HandleServerForceShutDownCommand(StringArguments args, CommandHandler handler)
            {
                return ShutdownServer(args, handler, ShutdownMask.Force, ShutdownExitCode.Shutdown);
            }
        }

        [CommandGroup("set", RBACPermissions.CommandServerSet, true)]
        class SetCommands
        {
            [Command("difftime", RBACPermissions.CommandServerSetDifftime, true)]
            static bool HandleServerSetDiffTimeCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                string newTimeStr = args.NextString();
                if (newTimeStr.IsEmpty())
                    return false;

                if (!int.TryParse(newTimeStr, out int newTime) || newTime < 0)
                    return false;

                //Global.WorldMgr.SetRecordDiffInterval(newTime);
                //printf("Record diff every %i ms\n", newTime);

                return true;
            }

            [Command("loglevel", RBACPermissions.CommandServerSetLoglevel, true)]
            static bool HandleServerSetLogLevelCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                string type = args.NextString();
                string name = args.NextString();
                string level = args.NextString();

                if (type.IsEmpty() || name.IsEmpty() || level.IsEmpty() || (type[0] != 'a' && type[0] != 'l'))
                    return false;

                return Log.SetLogLevel(name, level, type[0] == 'l');
            }

            [Command("motd", RBACPermissions.CommandServerSetMotd, true)]
            static bool SetMotd(StringArguments args, CommandHandler handler)
            {
                Global.WorldMgr.SetMotd(args.NextString(""));
                handler.SendSysMessage(CypherStrings.MotdNew, args.GetString());
                return true;
            }

            [Command("closed", RBACPermissions.CommandServerSetClosed, true)]
            static bool SetClosed(StringArguments args, CommandHandler handler)
            {
                string arg1 = args.NextString();
                if (arg1.Equals("on"))
                {
                    handler.SendSysMessage(CypherStrings.WorldClosed);
                    Global.WorldMgr.SetClosed(true);
                    return true;
                }
                else if (arg1.Equals("off"))
                {
                    handler.SendSysMessage(CypherStrings.WorldOpened);
                    Global.WorldMgr.SetClosed(false);
                    return true;
                }

                handler.SendSysMessage(CypherStrings.UseBol);
                return false;
            }
        }
    }
}
