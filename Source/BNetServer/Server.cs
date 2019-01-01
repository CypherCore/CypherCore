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

using BNetServer.Networking;
using Framework.Configuration;
using Framework.Database;
using Framework.Networking;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Timers;

namespace BNetServer
{
    class Server
    {
        static void Main()
        {
            //Set Culture
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            Console.CancelKeyPress += (o, e) =>
            {
                Global.RealmMgr.Close();

                Log.outInfo(LogFilter.Server, "Halting process...");
                Environment.Exit(-1);
            };

            if (!ConfigMgr.Load(Process.GetCurrentProcess().ProcessName + ".conf"))
                ExitNow();

            // Initialize the database connection
            if (!StartDB())
                ExitNow();

            string bindIp = ConfigMgr.GetDefaultValue("BindIP", "0.0.0.0");

            var restSocketServer = new SocketManager<RestSession>();
            int restPort = ConfigMgr.GetDefaultValue("LoginREST.Port", 8081);
            if (restPort < 0 || restPort > 0xFFFF)
            {
                Log.outError(LogFilter.Network, "Specified login service port ({0}) out of allowed range (1-65535), defaulting to 8081", restPort);
                restPort = 8081;
            }

            if (!restSocketServer.StartNetwork(bindIp, restPort))
            {
                Log.outError(LogFilter.Server, "Failed to initialize Rest Socket Server");
                ExitNow();
            }

            // Get the list of realms for the server
            Global.RealmMgr.Initialize(ConfigMgr.GetDefaultValue("RealmsStateUpdateDelay", 10));
            Global.SessionMgr.Initialize();

            var sessionSocketServer = new SocketManager<Session>();
            // Start the listening port (acceptor) for auth connections
            int bnPort = ConfigMgr.GetDefaultValue("BattlenetPort", 1119);
            if (bnPort < 0 || bnPort > 0xFFFF)
            {
                Log.outError(LogFilter.Server, "Specified battle.net port ({0}) out of allowed range (1-65535)", bnPort);
                ExitNow();
            }

            if (!sessionSocketServer.StartNetwork(bindIp, bnPort))
            {
                Log.outError(LogFilter.Network, "Failed to start BnetServer Network");
                ExitNow();
            }

            Log.outInfo(LogFilter.Server, $"Bnetserver started successfully, listening on {bindIp}:{bnPort}");

            uint _banExpiryCheckInterval = ConfigMgr.GetDefaultValue("BanExpiryCheckInterval", 60u);
            _banExpiryCheckTimer = new Timer(_banExpiryCheckInterval);
            _banExpiryCheckTimer.Elapsed += _banExpiryCheckTimer_Elapsed;
            _banExpiryCheckTimer.Start();
        }

        static bool StartDB()
        {
            DatabaseLoader loader = new DatabaseLoader(DatabaseTypeFlags.None);
            loader.AddDatabase(DB.Login, "Login");

            if (!loader.Load())
                return false;

            Log.SetRealmId(0); // Enables DB appenders when realm is set.
            return true;
        }

        static void ExitNow()
        {
            Log.outInfo(LogFilter.Server, "Halting process...");
            Environment.Exit(-1);
        }

        static void _banExpiryCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            DB.Login.Execute(DB.Login.GetPreparedStatement(LoginStatements.DEL_EXPIRED_IP_BANS));
            DB.Login.Execute(DB.Login.GetPreparedStatement(LoginStatements.UPD_EXPIRED_ACCOUNT_BANS));
            DB.Login.Execute(DB.Login.GetPreparedStatement(LoginStatements.DEL_BNET_EXPIRED_ACCOUNT_BANNED));
        }

        static Timer _banExpiryCheckTimer;
    }
}
