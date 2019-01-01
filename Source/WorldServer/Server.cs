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
using Framework.Database;
using Game;
using Game.Chat;
using Game.Network;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace WorldServer
{
    public class Server
    {
        const uint WorldSleep = 50;

        static void Main()
        {
            //Set Culture
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            Console.CancelKeyPress += (o, e) => Global.WorldMgr.StopNow(ShutdownExitCode.Shutdown);

            if (!ConfigMgr.Load(Process.GetCurrentProcess().ProcessName + ".conf"))
                ExitNow();

            if (!StartDB())
                ExitNow();

            // Server startup begin
            uint startupBegin = Time.GetMSTime();

            // set server offline (not connectable)
            DB.Login.DirectExecute("UPDATE realmlist SET flag = (flag & ~{0}) | {1} WHERE id = '{2}'", (uint)RealmFlags.VersionMismatch, (uint)RealmFlags.Offline, Global.WorldMgr.GetRealm().Id.Realm);

            Global.RealmMgr.Initialize(ConfigMgr.GetDefaultValue("RealmsStateUpdateDelay", 10));

            Global.WorldMgr.SetInitialWorldSettings();

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
            DB.Login.DirectExecute("UPDATE realmlist SET flag = flag & ~{0}, population = 0 WHERE id = '{1}'", (uint)RealmFlags.Offline, Global.WorldMgr.GetRealm().Id.Realm);
            Global.WorldMgr.GetRealm().PopulationLevel = 0.0f;
            Global.WorldMgr.GetRealm().Flags = Global.WorldMgr.GetRealm().Flags & ~RealmFlags.VersionMismatch;

            //- Launch CliRunnable thread
            if (ConfigMgr.GetDefaultValue("Console.Enable", true))
            {
                Thread commandThread = new Thread(CommandManager.InitConsole);
                commandThread.Start();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            uint startupDuration = Time.GetMSTimeDiffToNow(startupBegin);
            Log.outInfo(LogFilter.Server, "World initialized in {0} minutes {1} seconds", (startupDuration / 60000), ((startupDuration % 60000) / 1000));

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
                Global.ScriptMgr.Unload();

                // set server offline
                DB.Login.DirectExecute("UPDATE realmlist SET flag = flag | {0} WHERE id = '{1}'", (uint)RealmFlags.Offline, Global.WorldMgr.GetRealm().Id.Realm);
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
            DatabaseLoader loader = new DatabaseLoader(DatabaseTypeFlags.All);
            loader.AddDatabase(DB.Login, "Login");
            loader.AddDatabase(DB.Characters, "Character");
            loader.AddDatabase(DB.World, "World");
            loader.AddDatabase(DB.Hotfix, "Hotfix");

            if (!loader.Load())
                return false;

            // Get the realm Id from the configuration file
            Global.WorldMgr.GetRealm().Id.Realm = ConfigMgr.GetDefaultValue("RealmID", 0u);
            if (Global.WorldMgr.GetRealm().Id.Realm == 0)
            {
                Log.outError(LogFilter.Server, "Realm ID not defined in configuration file");
                return false;
            }
            Log.outInfo(LogFilter.ServerLoading, "Realm running as realm ID {0} ", Global.WorldMgr.GetRealm().Id.Realm);

            // Clean the database before starting
            ClearOnlineAccounts();

            Log.outInfo(LogFilter.Server, "Using World DB: {0}", Global.WorldMgr.LoadDBVersion());
            return true;
        }

        static void ClearOnlineAccounts()
        {
            // Reset online status for all accounts with characters on the current realm
            DB.Login.DirectExecute("UPDATE account SET online = 0 WHERE online > 0 AND id IN (SELECT acctid FROM realmcharacters WHERE realmid = {0})", Global.WorldMgr.GetRealm().Id.Realm);

            // Reset online status for all characters
            DB.Characters.DirectExecute("UPDATE characters SET online = 0 WHERE online <> 0");

            // Battlegroundinstance ids reset at server restart
            DB.Characters.DirectExecute("UPDATE character_battleground_data SET instanceId = 0");
        }

        static void WorldUpdateLoop()
        {
            uint realPrevTime = Time.GetMSTime();

            while (!Global.WorldMgr.IsStopped)
            {
                var realCurrTime = Time.GetMSTime();

                uint diff = Time.GetMSTimeDiff(realPrevTime, realCurrTime);
                Global.WorldMgr.Update(diff);
                realPrevTime = realCurrTime;

                uint executionTimeDiff = Time.GetMSTimeDiffToNow(realCurrTime);

                // we know exactly how long it took to update the world, if the update took less than WORLD_SLEEP_CONST, sleep for WORLD_SLEEP_CONST - world update time
                if (executionTimeDiff < WorldSleep)               
                    Thread.Sleep((int)(WorldSleep - executionTimeDiff));                
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
            Environment.Exit(-1);
        }
    }
}