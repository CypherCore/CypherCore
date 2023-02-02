// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Configuration;
using Framework.Constants;
using Framework.Database;
using Framework.Networking;
using Game;
using Game.Chat;
using Game.Networking;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace WorldServer
{
    public class Server
    {
        static void Main()
        {
            //Set Culture
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            Console.CancelKeyPress += (o, e) => Global.WorldMgr.StopNow(ShutdownExitCode.Shutdown);

            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

            if (!ConfigMgr.Load(Process.GetCurrentProcess().ProcessName + ".conf"))
                ExitNow();

            if (!StartDB())
                ExitNow();

            // Server startup begin
            uint startupBegin = Time.GetMSTime();

            // set server offline (not connectable)
            DB.Login.DirectExecute("UPDATE realmlist SET flag = (flag & ~{0}) | {1} WHERE id = '{2}'", (uint)RealmFlags.VersionMismatch, (uint)RealmFlags.Offline, Global.WorldMgr.GetRealm().Id.Index);

            Global.RealmMgr.Initialize(ConfigMgr.GetDefaultValue("RealmsStateUpdateDelay", 10));

            Global.WorldMgr.SetInitialWorldSettings();

            // Start the Remote Access port (acceptor) if enabled
            if (ConfigMgr.GetDefaultValue("Ra.Enable", false))
            {
                int raPort = ConfigMgr.GetDefaultValue("Ra.Port", 3443);
                string raListener = ConfigMgr.GetDefaultValue("Ra.IP", "0.0.0.0");
                AsyncAcceptor raAcceptor = new();
                if (!raAcceptor.Start(raListener, raPort))
                    Log.outError(LogFilter.Server, "Failed to initialize RemoteAccess Socket Server");
                else
                    raAcceptor.AsyncAccept<RASocket>();
            }

            // Launch the worldserver listener socket
            int worldPort = WorldConfig.GetIntValue(WorldCfg.PortWorld);
            string worldListener = ConfigMgr.GetDefaultValue("BindIP", "0.0.0.0");

            int networkThreads = ConfigMgr.GetDefaultValue("Network.Threads", 1);
            if (networkThreads <= 0)
            {
                Log.outError(LogFilter.Server, "Network.Threads must be greater than 0");
                ExitNow();
                return;
            }

            var WorldSocketMgr = new WorldSocketManager();
            if (!WorldSocketMgr.StartNetwork(worldListener, worldPort, networkThreads))
            {
                Log.outError(LogFilter.Network, "Failed to start Realm Network");
                ExitNow();
            }

            // set server online (allow connecting now)
            DB.Login.DirectExecute("UPDATE realmlist SET flag = flag & ~{0}, population = 0 WHERE id = '{1}'", (uint)RealmFlags.Offline, Global.WorldMgr.GetRealm().Id.Index);
            Global.WorldMgr.GetRealm().PopulationLevel = 0.0f;
            Global.WorldMgr.GetRealm().Flags = Global.WorldMgr.GetRealm().Flags & ~RealmFlags.VersionMismatch;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            uint startupDuration = Time.GetMSTimeDiffToNow(startupBegin);
            Log.outInfo(LogFilter.Server, "World initialized in {0} minutes {1} seconds", (startupDuration / 60000), ((startupDuration % 60000) / 1000));

            //- Launch CliRunnable thread
            if (ConfigMgr.GetDefaultValue("Console.Enable", true))
            {
                Thread commandThread = new(CommandManager.InitConsole);
                commandThread.Start();
            }

            WorldUpdateLoop();

            try
            {
                // Shutdown starts here
                Global.WorldMgr.KickAll();                                     // save and kick all players
                Global.WorldMgr.UpdateSessions(1);                             // real players unload required UpdateSessions call

                // unload Battlegroundtemplates before different singletons destroyed
                Global.BattlegroundMgr.DeleteAllBattlegrounds();

                WorldSocketMgr.StopNetwork();

                Global.MapMgr.UnloadAll();                     // unload all grids (including locked in memory)
                Global.TerrainMgr.UnloadAll();
                Global.InstanceLockMgr.Unload();
                Global.ScriptMgr.Unload();

                // set server offline
                DB.Login.DirectExecute("UPDATE realmlist SET flag = flag | {0} WHERE id = '{1}'", (uint)RealmFlags.Offline, Global.WorldMgr.GetRealm().Id.Index);
                Global.RealmMgr.Close();

                ClearOnlineAccounts();

                ExitNow();
            }
            catch (Exception ex)
            {
                Log.outException(ex);
                ExitNow();
            }
        }

        static bool StartDB()
        {
            // Load databases
            DatabaseLoader loader = new(DatabaseTypeFlags.All);
            loader.AddDatabase(DB.Login, "Login");
            loader.AddDatabase(DB.Characters, "Character");
            loader.AddDatabase(DB.World, "World");
            loader.AddDatabase(DB.Hotfix, "Hotfix");

            if (!loader.Load())
                return false;

            // Get the realm Id from the configuration file
            Global.WorldMgr.GetRealm().Id.Index = ConfigMgr.GetDefaultValue("RealmID", 0u);
            if (Global.WorldMgr.GetRealm().Id.Index == 0)
            {
                Log.outError(LogFilter.Server, "Realm ID not defined in configuration file");
                return false;
            }
            Log.outInfo(LogFilter.ServerLoading, "Realm running as realm ID {0} ", Global.WorldMgr.GetRealm().Id.Index);

            // Clean the database before starting
            ClearOnlineAccounts();

            Log.outInfo(LogFilter.Server, "Using World DB: {0}", Global.WorldMgr.LoadDBVersion());
            return true;
        }

        static void ClearOnlineAccounts()
        {
            // Reset online status for all accounts with characters on the current realm
            DB.Login.DirectExecute("UPDATE account SET online = 0 WHERE online > 0 AND id IN (SELECT acctid FROM realmcharacters WHERE realmid = {0})", Global.WorldMgr.GetRealm().Id.Index);

            // Reset online status for all characters
            DB.Characters.DirectExecute("UPDATE characters SET online = 0 WHERE online <> 0");

            // Battlegroundinstance ids reset at server restart
            DB.Characters.DirectExecute("UPDATE character_battleground_data SET instanceId = 0");
        }

        static void WorldUpdateLoop()
        {
            int minUpdateDiff = ConfigMgr.GetDefaultValue("MinWorldUpdateTime", 1);
            uint realPrevTime = Time.GetMSTime();

            uint maxCoreStuckTime = ConfigMgr.GetDefaultValue("MaxCoreStuckTime", 60u) * 1000u;
            uint halfMaxCoreStuckTime = maxCoreStuckTime / 2;
            if (halfMaxCoreStuckTime == 0)
                halfMaxCoreStuckTime = uint.MaxValue;

            while (!Global.WorldMgr.IsStopped)
            {
                var realCurrTime = Time.GetMSTime();

                uint diff = Time.GetMSTimeDiff(realPrevTime, realCurrTime);
                if (diff < minUpdateDiff)
                {
                    uint sleepTime = (uint)(minUpdateDiff - diff);
                    if (sleepTime >= halfMaxCoreStuckTime)
                        Log.outError(LogFilter.Server, $"WorldUpdateLoop() waiting for {sleepTime} ms with MaxCoreStuckTime set to {maxCoreStuckTime} ms");

                    // sleep until enough time passes that we can update all timers
                    Thread.Sleep(TimeSpan.FromMilliseconds(sleepTime));
                    continue;
                }

                Global.WorldMgr.Update(diff);
                realPrevTime = realCurrTime;
            }
        }

        static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            Log.outException(ex);
        }

        static void ExitNow()
        {
            Log.outInfo(LogFilter.Server, "Halting process...");
            Thread.Sleep(5000);
            Environment.Exit(Global.WorldMgr.GetExitCode());
        }
    }
}