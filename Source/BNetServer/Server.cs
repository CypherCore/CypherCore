// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Configuration;
using Framework.Database;
using System;
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

            if (!ConfigMgr.Load("BNetServer.conf"))
                ExitNow();

            // Initialize the database
            if (!StartDB())
                ExitNow();

            string httpBindIp = ConfigMgr.GetDefaultValue("BindIP", "0.0.0.0");
            int httpPort = ConfigMgr.GetDefaultValue("LoginREST.Port", 8081);
            if (httpPort <= 0 || httpPort > 0xFFFF)
            {
                Log.outError(LogFilter.Server, $"Specified login service port ({httpPort}) out of allowed range (1-65535)");
                ExitNow();
            }

            if (!Global.LoginService.StartNetwork(httpBindIp, httpPort))
            {
                Log.outError(LogFilter.Server, "Failed to initialize login service");
                ExitNow();
            }

            // Start the listening port (acceptor) for auth connections
            int bnPort = ConfigMgr.GetDefaultValue("BattlenetPort", 1119);
            if (bnPort <= 0 || bnPort > 0xFFFF)
            {
                Log.outError(LogFilter.Server, $"Specified battle.net port ({bnPort}) out of allowed range (1-65535)");
                ExitNow();
            }

            // Get the list of realms for the server
            Global.RealmMgr.Initialize(ConfigMgr.GetDefaultValue("RealmsStateUpdateDelay", 10));

            Global.LoginServiceMgr.Initialize();

            string bindIp = ConfigMgr.GetDefaultValue("BindIP", "0.0.0.0");

            if (!Global.SessionMgr.StartNetwork(bindIp, bnPort))
            {
                Log.outError(LogFilter.Network, "Failed to start BnetServer Network");
                ExitNow();
            }

            uint _banExpiryCheckInterval = ConfigMgr.GetDefaultValue("BanExpiryCheckInterval", 60u);
            _banExpiryCheckTimer = new Timer(_banExpiryCheckInterval);
            _banExpiryCheckTimer.Elapsed += BanExpiryCheckTimer_Elapsed;
            _banExpiryCheckTimer.Start();
        }

        static bool StartDB()
        {
            DatabaseLoader loader = new(DatabaseTypeFlags.None);
            loader.AddDatabase(DB.Login, "Login");

            if (!loader.Load())
                return false;

            Log.SetRealmId(0); // Enables DB appenders when realm is set.
            return true;
        }

        static void ExitNow()
        {
            Console.WriteLine("Halting process...");
            System.Threading.Thread.Sleep(10000);
            Environment.Exit(-1);
        }

        static void BanExpiryCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            DB.Login.Execute(LoginDatabase.GetPreparedStatement(LoginStatements.DEL_EXPIRED_IP_BANS));
            DB.Login.Execute(LoginDatabase.GetPreparedStatement(LoginStatements.UPD_EXPIRED_ACCOUNT_BANS));
            DB.Login.Execute(LoginDatabase.GetPreparedStatement(LoginStatements.DEL_BNET_EXPIRED_ACCOUNT_BANNED));
        }

        static Timer _banExpiryCheckTimer;
    }
}