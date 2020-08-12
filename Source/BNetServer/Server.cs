// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using BNetServer.Networking;
using Framework.Configuration;
using Framework.Database;
using Framework.Networking;
using Framework.Web.API;
using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Timers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Framework.Cryptography;

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

            FixLegacyAuthHashes();

            string bindIp = ConfigMgr.GetDefaultValue("BindIP", "0.0.0.0");

            var restSocketServer = new SocketManager<RestSession>();
            int restPort = ConfigMgr.GetDefaultValue("LoginREST.Port", 8081);
            if (restPort < 0 || restPort > 0xFFFF)
            {
                Log.outError(LogFilter.Network, "Specified login service port ({restPort}) out of allowed range (1-65535), defaulting to 8081");
                restPort = 8081;
            }

            if (!restSocketServer.StartNetwork(bindIp, restPort))
            {
                Log.outError(LogFilter.Server, "Failed to initialize Rest Socket Server");
                ExitNow();
            }

            // Get the list of realms for the server
            Global.RealmMgr.Initialize(ConfigMgr.GetDefaultValue("RealmsStateUpdateDelay", 10));
            Global.LoginServiceMgr.Initialize();

            var sessionSocketServer = new SocketManager<Session>();
            // Start the listening port (acceptor) for auth connections
            int bnPort = ConfigMgr.GetDefaultValue("BattlenetPort", 1119);
            if (bnPort < 0 || bnPort > 0xFFFF)
            {
                Log.outError(LogFilter.Server, $"Specified battle.net port ({bnPort}) out of allowed range (1-65535)");
                ExitNow();
            }

            if (!sessionSocketServer.StartNetwork(bindIp, bnPort))
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
            DatabaseLoader loader = new DatabaseLoader(DatabaseTypeFlags.None);
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

        static void FixLegacyAuthHashes()
        {
            Log.outInfo(LogFilter.Server, "Updating password hashes...");
            uint start = Time.GetMSTime();
            // the auth update query nulls salt/verifier if they cannot be converted
            // if they are non-null but s/v have been cleared, that means a legacy tool touched our auth DB (otherwise, the core might've done it itself, it used to use those hacks too)
            SQLResult result = DB.Login.Query("SELECT id, sha_pass_hash, IF((salt IS null) AND (verifier IS null), 0, 1) AS shouldWarn FROM account WHERE s != DEFAULT(s) OR v != DEFAULT(v) OR salt IS NULL OR verifier IS NULL");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.Server, $"No password hashes to update - this took us {Time.GetMSTimeDiffToNow(start)} ms to realize");
                return;
            }

            bool hadWarning = false;
            uint count = 0;
            SQLTransaction trans = new SQLTransaction();
            do
            {
                uint id = result.Read<uint>(0);
                (byte[] salt, byte[] verifier) registrationData = SRP6.MakeRegistrationDataFromHash(result.Read<string>(1).ToByteArray());

                if (result.Read<long>(2) != 0 && !hadWarning)
                {
                    hadWarning = true;
                    Log.outWarn(LogFilter.Server, "(!) You appear to be using an outdated external account management tool.\n(!!) This is INSECURE, has been deprecated, and will cease to function entirely in the near future.\n(!) Update your external tool.\n(!!) If no update is available, refer your tool's developer to https://github.com/TrinityCore/TrinityCore/issues/25157.");
                }

                PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_LOGON);
                stmt.AddValue(0, registrationData.salt);
                stmt.AddValue(1, registrationData.verifier);
                stmt.AddValue(2, id);
                trans.Append(stmt);

                ++count;
            } while (result.NextRow());

            DB.Login.CommitTransaction(trans);

            Log.outInfo(LogFilter.Server, $"{count} password hashes updated in {Time.GetMSTimeDiffToNow(start)} ms");
        }

        static void BanExpiryCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            DB.Login.Execute(DB.Login.GetPreparedStatement(LoginStatements.DelExpiredIpBans));
            DB.Login.Execute(DB.Login.GetPreparedStatement(LoginStatements.UpdExpiredAccountBans));
            DB.Login.Execute(DB.Login.GetPreparedStatement(LoginStatements.DelBnetExpiredAccountBanned));
        }

        static Timer _banExpiryCheckTimer;
    }
}