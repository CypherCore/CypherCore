// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Configuration;
using Framework.Constants;
using Framework.Database;
using Framework.IO;
using System;
using System.Collections.Generic;
using System.IO;

namespace Game.Chat
{
    [CommandGroup("server")]
    class ServerCommands
    {
        [Command("corpses", RBACPermissions.CommandServerCorpses, true)]
        static bool HandleServerCorpsesCommand(CommandHandler handler)
        {
            Global.WorldMgr.RemoveOldCorpses();
            return true;
        }

        [Command("debug", RBACPermissions.CommandServerCorpses, true)]
        static bool HandleServerDebugCommand(CommandHandler handler)
        {
            string dbPortOutput;

            ushort dbPort = 0;
            SQLResult res = DB.Login.Query($"SELECT port FROM realmlist WHERE id = {Global.WorldMgr.GetRealmId().Index}");
            if (!res.IsEmpty())
                dbPort = res.Read<ushort>(0);

            if (dbPort != 0)
                dbPortOutput = $"Realmlist (Realm Id: {Global.WorldMgr.GetRealmId().Index}) configured in port {dbPort}";
            else
                dbPortOutput = $"Realm Id: {Global.WorldMgr.GetRealmId().Index} not found in `realmlist` table. Please check your setup";

            DatabaseTypeFlags updateFlags = ConfigMgr.GetDefaultValue("Updates.EnableDatabases", DatabaseTypeFlags.None);
            if (updateFlags == 0)
                handler.SendSysMessage("Automatic database updates are disabled for all databases!");
            else
                handler.SendSysMessage($"Automatic database updates are enabled for the following databases: {updateFlags}");

            handler.SendSysMessage($"Worldserver listening connections on port {WorldConfig.GetIntValue(WorldCfg.PortWorld)}");
            handler.SendSysMessage(dbPortOutput);

            bool vmapIndoorCheck = WorldConfig.GetBoolValue(WorldCfg.VmapIndoorCheck);
            bool vmapLOSCheck = Global.VMapMgr.IsLineOfSightCalcEnabled();
            bool vmapHeightCheck = Global.VMapMgr.IsHeightCalcEnabled();

            bool mmapEnabled = WorldConfig.GetBoolValue(WorldCfg.EnableMmaps);

            string dataDir = Global.WorldMgr.GetDataPath();
            List<string> subDirs = new();
            subDirs.Add("/maps");
            if (vmapIndoorCheck || vmapLOSCheck || vmapHeightCheck)
            {
                handler.SendSysMessage($"VMAPs status: Enabled. LineOfSight: {vmapLOSCheck}, getHeight: {vmapHeightCheck}, indoorCheck: {vmapIndoorCheck}");
                subDirs.Add("/vmaps");
            }
            else
                handler.SendSysMessage("VMAPs status: Disabled");

            if (mmapEnabled)
            {
                handler.SendSysMessage("MMAPs status: Enabled");
                subDirs.Add("/mmaps");
            }
            else
                handler.SendSysMessage("MMAPs status: Disabled");

            foreach (string subDir in subDirs)
            {
                if (!File.Exists(dataDir + subDir))
                {
                    handler.SendSysMessage($"{subDir} directory doesn't exist!. Using path: {dataDir + subDir}");
                    continue;
                }
            }

            Locale defaultLocale = Global.WorldMgr.GetDefaultDbcLocale();
            uint availableLocalesMask = (1u << (int)defaultLocale);

            for (Locale locale = 0; locale < Locale.Total; ++locale)
            {
                if (locale == defaultLocale)
                    continue;

                if (Global.WorldMgr.GetAvailableDbcLocale(locale) != defaultLocale)
                    availableLocalesMask |= (1u << (int)locale);
            }

            string availableLocales = "";
            for (Locale locale = 0; locale < Locale.Total; ++locale)
            {
                if ((availableLocalesMask & (1 << (int)locale)) == 0)
                    continue;

                availableLocales += locale;
                if (locale != Locale.Total - 1)
                    availableLocales += " ";
            }

            handler.SendSysMessage($"Using {defaultLocale} DBC Locale as default. All available DBC locales: {availableLocales}");

            handler.SendSysMessage($"Using World DB: {Global.WorldMgr.GetDBVersion()}");
            return true;
        }

        [Command("exit", RBACPermissions.CommandServerExit, true)]
        static bool HandleServerExitCommand(CommandHandler handler)
        {
            handler.SendSysMessage(CypherStrings.CommandExit);
            Global.WorldMgr.StopNow(ShutdownExitCode.Shutdown);
            return true;
        }

        [Command("info", RBACPermissions.CommandServerInfo, true)]
        static bool HandleServerInfoCommand(CommandHandler handler)
        {
            uint playersNum = Global.WorldMgr.GetPlayerCount();
            uint maxPlayersNum = Global.WorldMgr.GetMaxPlayerCount();
            int activeClientsNum = Global.WorldMgr.GetActiveSessionCount();
            int queuedClientsNum = Global.WorldMgr.GetQueuedSessionCount();
            uint maxActiveClientsNum = Global.WorldMgr.GetMaxActiveSessionCount();
            uint maxQueuedClientsNum = Global.WorldMgr.GetMaxQueuedSessionCount();
            string uptime = Time.secsToTimeString(GameTime.GetUptime());
            uint updateTime = Global.WorldMgr.GetWorldUpdateTime().GetLastUpdateTime();

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
        static bool HandleServerMotdCommand(CommandHandler handler)
        {
            string motd = "";
            foreach (var line in Global.WorldMgr.GetMotd())
                motd += line;

            handler.SendSysMessage(CypherStrings.MotdCurrent, motd);
            return true;
        }

        [Command("plimit", RBACPermissions.CommandServerPlimit, true)]
        static bool HandleServerPLimitCommand(CommandHandler handler, StringArguments args)
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
            string secName;
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
            string myAddr = mySession != null ? mySession.GetRemoteAddress() : "";
            var sessions = Global.WorldMgr.GetAllSessions();
            foreach (var session in sessions)
                if (session != null && myAddr != session.GetRemoteAddress())
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

        static bool ShutdownServer(StringArguments args, CommandHandler handler, ShutdownMask shutdownMask, ShutdownExitCode defaultExitCode)
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

        [CommandGroup("idleRestart")]
        class IdleRestartCommands
        {
            [Command("", RBACPermissions.CommandServerIdlerestart, true)]
            static bool HandleServerIdleRestartCommand(CommandHandler handler, StringArguments args)
            {
                return ShutdownServer(args, handler, ShutdownMask.Restart | ShutdownMask.Idle, ShutdownExitCode.Restart);
            }

            [Command("cancel", RBACPermissions.CommandServerIdlerestartCancel, true)]
            static bool HandleServerShutDownCancelCommand(CommandHandler handler)
            {
                uint timer = Global.WorldMgr.ShutdownCancel();
                if (timer != 0)
                    handler.SendSysMessage(CypherStrings.ShutdownCancelled, timer);
                return true;
            }
        }

        [CommandGroup("idleshutdown")]
        class IdleshutdownCommands
        {
            [Command("", RBACPermissions.CommandServerIdleshutdown, true)]
            static bool HandleServerIdleShutDownCommand(CommandHandler handler, StringArguments args)
            {
                return ShutdownServer(args, handler, ShutdownMask.Idle, ShutdownExitCode.Shutdown);
            }

            [Command("cancel", RBACPermissions.CommandServerIdleshutdownCancel, true)]
            static bool HandleServerShutDownCancelCommand(CommandHandler handler)
            {
                uint timer = Global.WorldMgr.ShutdownCancel();
                if (timer != 0)
                    handler.SendSysMessage(CypherStrings.ShutdownCancelled, timer);

                return true;
            }
        }

        [CommandGroup("restart")]
        class RestartCommands
        {
            [Command("", RBACPermissions.CommandServerRestart, true)]
            static bool HandleServerRestartCommand(CommandHandler handler, StringArguments args)
            {
                return ShutdownServer(args, handler, ShutdownMask.Restart, ShutdownExitCode.Restart);
            }

            [Command("cancel", RBACPermissions.CommandServerRestartCancel, true)]
            static bool HandleServerShutDownCancelCommand(CommandHandler handler)
            {
                uint timer = Global.WorldMgr.ShutdownCancel();
                if (timer != 0)
                    handler.SendSysMessage(CypherStrings.ShutdownCancelled, timer);

                return true;
            }

            [Command("force", RBACPermissions.CommandServerRestartCancel, true)]
            static bool HandleServerForceRestartCommand(CommandHandler handler, StringArguments args)
            {
                return ShutdownServer(args, handler, ShutdownMask.Force | ShutdownMask.Restart, ShutdownExitCode.Restart);
            }
        }

        [CommandGroup("shutdown")]
        class ShutdownCommands
        {
            [Command("", RBACPermissions.CommandServerShutdown, true)]
            static bool HandleServerShutDownCommand(CommandHandler handler, StringArguments args)
            {
                return ShutdownServer(args, handler, 0, ShutdownExitCode.Shutdown);
            }

            [Command("cancel", RBACPermissions.CommandServerShutdownCancel, true)]
            static bool HandleServerShutDownCancelCommand(CommandHandler handler)
            {
                uint timer = Global.WorldMgr.ShutdownCancel();
                if (timer != 0)
                    handler.SendSysMessage(CypherStrings.ShutdownCancelled, timer);

                return true;
            }

            [Command("force", RBACPermissions.CommandServerShutdownCancel, true)]
            static bool HandleServerForceShutDownCommand(CommandHandler handler, StringArguments args)
            {
                return ShutdownServer(args, handler, ShutdownMask.Force, ShutdownExitCode.Shutdown);
            }
        }

        [CommandGroup("set")]
        class SetCommands
        {
            [Command("difftime", RBACPermissions.CommandServerSetDifftime, true)]
            static bool HandleServerSetDiffTimeCommand(CommandHandler handler, StringArguments args)
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
            static bool HandleServerSetLogLevelCommand(CommandHandler handler, string type, string name, int level)
            {
                if (name.IsEmpty() || level < 0 || (type != "a" && type != "l"))
                    return false;

                return Log.SetLogLevel(name, level, type == "l");
            }

            [Command("motd", RBACPermissions.CommandServerSetMotd, true)]
            static bool HandleServerSetMotdCommand(CommandHandler handler, StringArguments args)
            {
                Global.WorldMgr.SetMotd(args.NextString(""));
                handler.SendSysMessage(CypherStrings.MotdNew, args.GetString());
                return true;
            }

            [Command("closed", RBACPermissions.CommandServerSetClosed, true)]
            static bool HandleServerSetClosedCommand(CommandHandler handler, StringArguments args)
            {
                string arg1 = args.NextString();
                if (arg1.Equals("on", StringComparison.OrdinalIgnoreCase))
                {
                    handler.SendSysMessage(CypherStrings.WorldClosed);
                    Global.WorldMgr.SetClosed(true);
                    return true;
                }
                else if (arg1.Equals("off", StringComparison.OrdinalIgnoreCase))
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
