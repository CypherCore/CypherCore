/*
 * Copyright (C) 2012-2018 CypherCore <http://github.com/CypherCore>
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
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Game.Discord;


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

            if (!ConfigMgr.Load(System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".conf"))
                ExitNow();

            if (!StartDB())
                ExitNow();

            // set server offline (not connectable)
            DB.Login.Execute("UPDATE realmlist SET flag = (flag & ~{0}) | {1} WHERE id = '{2}'", (uint)RealmFlags.VersionMismatch, (uint)RealmFlags.Offline, Global.WorldMgr.GetRealm().Id.Realm);

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
            DB.Login.Execute("UPDATE realmlist SET flag = flag & ~{0}, population = 0 WHERE id = '{1}'", (uint)RealmFlags.Offline, Global.WorldMgr.GetRealm().Id.Realm);
            Global.WorldMgr.GetRealm().PopulationLevel = 0.0f;
            Global.WorldMgr.GetRealm().Flags = Global.WorldMgr.GetRealm().Flags & ~RealmFlags.VersionMismatch;

            if (ConfigMgr.GetDefaultValue("Discord.Enable", false))
            {
                Thread threadDiscord = new Thread( StartDiscordBot );
                threadDiscord.Start();

                Log.outInfo( LogFilter.ChatSystem, "Starting up world to discord thread...");
            }

            //- Launch CliRunnable thread
            if (ConfigMgr.GetDefaultValue("Console.Enable", true))
            {
                Thread commandThread = new Thread(CommandManager.InitConsole);
                commandThread.Start();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

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
                DB.Login.Execute("UPDATE realmlist SET flag = flag | {0} WHERE id = '{1}'", (uint)RealmFlags.Offline, Global.WorldMgr.GetRealm().Id.Realm);
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
            DB.Login.Execute("UPDATE account SET online = 0 WHERE online > 0 AND id IN (SELECT acctid FROM realmcharacters WHERE realmid = {0})", Global.WorldMgr.GetRealm().Id.Realm);

            // Reset online status for all characters
            DB.Characters.Execute("UPDATE characters SET online = 0 WHERE online <> 0");

            // Battlegroundinstance ids reset at server restart
            DB.Characters.Execute("UPDATE character_battleground_data SET instanceId = 0");
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

        static async void StartDiscordBot()
        {
            DiscordSocketClient client = new DiscordSocketClient();

            client.Log += LogDiscord;
            client.MessageReceived += SendToDiscord;

            string token = ConfigMgr.GetDefaultValue("Discord.BotToken", "");

            if (token != "")
            {
                await client.LoginAsync(TokenType.Bot, token);
                await client.StartAsync();

                while (true)
                {
                    if (!DiscordMessageQueue.Empty())
                    {
                        foreach (DiscordMessage message in DiscordMessageQueue.DiscordMessages)
                        {
                            string formatedMessage = "";
                            switch (message.Channel)
                            {
                                case DiscordMessageChannel.Discord_World_A:
                                case DiscordMessageChannel.Discord_World_H:
                                    formatedMessage = GetFormatedMessage( message );
                                    break;
                                default:
                                    formatedMessage = message.Message;
                                    break;
                            }

                            ulong serverID = ConfigMgr.GetDefaultValue("Discord.BotServerID", default(ulong));
                            ulong channelID = ConfigMgr.GetDefaultValue("Discord.BotChannelID", default(ulong));
                        
                            if( serverID != default(ulong) && channelID != default(ulong))
                                await client.GetGuild(serverID).GetTextChannel(channelID).SendMessageAsync(formatedMessage);
                        }
                    }
                    await Task.Delay(-1);
                }
            }
        }

        private static string GetFormatedMessage(DiscordMessage message)
        {
            string blizzIcon = message.IsGm ? "<:blizz:407908963007594496>" : "";
            return blizzIcon + "[" + message.CharacterName + "]: " + message.Message;
        }

        private static async Task SendToDiscord(SocketMessage message)
        {
            //Todo: we can implement some logic for custom commands from discord here!
            if (message.Content == "!ping")
            {
                await message.Channel.SendMessageAsync("Pong!");
            }
        }

        private static Task LogDiscord(LogMessage msg)
        {
            Log.outInfo(LogFilter.DiscordBot, msg.ToString());
            return Task.CompletedTask;
        }
    }
}