/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using System;

namespace Game.Chat
{
    [CommandGroup("instance")]
    class InstanceCommands
    {
        [Command("getbossstate", RBACPermissions.CommandInstanceGetBossState)]
        static bool HandleInstanceGetBossStateCommand(CommandHandler handler, uint encounterId, string playerName)
        {
            var player = PlayerIdentifier.ParseFromString(playerName);

            // Character name must be provided when using this from console.
            if (player == null || handler.GetSession() == null)
            {
                handler.SendSysMessage(CypherStrings.CmdSyntax);
                return false;
            }

            if (player == null)
                player = PlayerIdentifier.FromSelf(handler);

            if (player.IsConnected())
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            InstanceMap map = player.GetConnectedPlayer().GetMap().ToInstanceMap();
            if (map == null)
            {
                handler.SendSysMessage(CypherStrings.NotDungeon);
                return false;
            }

            if (map.GetInstanceScript() == null)
            {
                handler.SendSysMessage(CypherStrings.NoInstanceData);
                return false;
            }

            if (encounterId > map.GetInstanceScript().GetEncounterCount())
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            EncounterState state = map.GetInstanceScript().GetBossState(encounterId);
            handler.SendSysMessage(CypherStrings.CommandInstGetBossState, encounterId, state);
            return true;
        }

        [Command("listbinds", RBACPermissions.CommandInstanceListbinds)]
        static bool HandleInstanceListBindsCommand(CommandHandler handler)
        {
            Player player = handler.GetSelectedPlayer();
            if (!player)
                player = handler.GetSession().GetPlayer();

            string format = "map: {0} inst: {1} perm: {2} diff: {3} canReset: {4} TTR: {5}";

            uint counter = 0;
            foreach (var difficulty in CliDB.DifficultyStorage.Values)
            {
                var binds = player.GetBoundInstances((Difficulty)difficulty.Id);
                foreach (var (mapId, bind) in binds)
                {
                    InstanceSave save = bind.save;
                    string timeleft = Time.GetTimeString(save.GetResetTime() - GameTime.GetGameTime());
                    handler.SendSysMessage(format, mapId, save.GetInstanceId(), bind.perm ? "yes" : "no", save.GetDifficultyID(), save.CanReset() ? "yes" : "no", timeleft);
                    counter++;
                }
            }
            handler.SendSysMessage("player binds: {0}", counter);

            counter = 0;
            Group group = player.GetGroup();
            if (group)
            {
                foreach (var difficulty in CliDB.DifficultyStorage.Values)
                {
                    var binds = group.GetBoundInstances((Difficulty)difficulty.Id);
                    foreach (var (mapId, bind) in binds)
                    {
                        InstanceSave save = bind.save;
                        string timeleft = Time.GetTimeString(save.GetResetTime() - GameTime.GetGameTime());
                        handler.SendSysMessage(format, mapId, save.GetInstanceId(), bind.perm ? "yes" : "no", save.GetDifficultyID(), save.CanReset() ? "yes" : "no", timeleft);
                        counter++;
                    }
                }
            }
            handler.SendSysMessage("group binds: {0}", counter);

            return true;
        }

        [Command("savedata", RBACPermissions.CommandInstanceSavedata)]
        static bool HandleInstanceSaveData(CommandHandler handler, StringArguments args)
        {
            Player player = handler.GetSession().GetPlayer();
            InstanceMap map = player.GetMap().ToInstanceMap();
            if (map == null)
            {
                handler.SendSysMessage("Map is not a dungeon.");
                return false;
            }

            if (map.GetInstanceScript() == null)
            {
                handler.SendSysMessage("Map has no instance data.");
                return false;
            }

            map.GetInstanceScript().SaveToDB();

            return true;
        }

        [Command("setbossstate", RBACPermissions.CommandInstanceSetBossState)]
        static bool HandleInstanceSetBossStateCommand(CommandHandler handler, uint encounterId, EncounterState state, string playerName)
        {
            var player = PlayerIdentifier.ParseFromString(playerName);

            // Character name must be provided when using this from console.
            if (player == null || handler.GetSession() == null)
            {
                handler.SendSysMessage(CypherStrings.CmdSyntax);
                return false;
            }

            if (player == null)
                player = PlayerIdentifier.FromSelf(handler);

            if (!player.IsConnected())
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            InstanceMap map = player.GetConnectedPlayer().GetMap().ToInstanceMap();
            if (map == null)
            {
                handler.SendSysMessage(CypherStrings.NotDungeon);
                return false;
            }

            if (map.GetInstanceScript() == null)
            {
                handler.SendSysMessage(CypherStrings.NoInstanceData);
                return false;
            }

            // Reject improper values.
            if (encounterId > map.GetInstanceScript().GetEncounterCount())
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            map.GetInstanceScript().SetBossState(encounterId, state);
            handler.SendSysMessage(CypherStrings.CommandInstSetBossState, encounterId, state);
            return true;
        }

        [Command("stats", RBACPermissions.CommandInstanceStats, true)]
        static bool HandleInstanceStatsCommand(CommandHandler handler)
        {
            handler.SendSysMessage("instances loaded: {0}", Global.MapMgr.GetNumInstances());
            handler.SendSysMessage("players in instances: {0}", Global.MapMgr.GetNumPlayersInInstances());
            handler.SendSysMessage("instance saves: {0}", Global.InstanceSaveMgr.GetNumInstanceSaves());
            handler.SendSysMessage("players bound: {0}", Global.InstanceSaveMgr.GetNumBoundPlayersTotal());
            handler.SendSysMessage("groups bound: {0}", Global.InstanceSaveMgr.GetNumBoundGroupsTotal());

            return true;
        }

        [Command("unbind", RBACPermissions.CommandInstanceUnbind)]
        static bool HandleInstanceUnbindCommand(CommandHandler handler, string mapArg, byte? difficultyArg)
        {
            Player player = handler.GetSelectedPlayer();
            if (!player)
                player = handler.GetSession().GetPlayer();

            ushort counter = 0;
            uint mapId = 0;

            if (!mapArg.IsEmpty() && mapArg.IsNumber())
                if (!uint.TryParse(mapArg, out mapId) || mapId == 0)
                    return false;

            foreach (var difficulty in CliDB.DifficultyStorage.Values)
            {
                var binds = player.GetBoundInstances((Difficulty)difficulty.Id);
                foreach (var pair in binds)
                {
                    InstanceSave save = pair.Value.save;
                    if (pair.Key != player.GetMapId() && (mapId == 0 || mapId == pair.Key) && (!difficultyArg.HasValue || difficultyArg.Value == (byte)save.GetDifficultyID()))
                    {
                        string timeleft = Time.GetTimeString(save.GetResetTime() - GameTime.GetGameTime());
                        handler.SendSysMessage("unbinding map: {0} inst: {1} perm: {2} diff: {3} canReset: {4} TTR: {5}", pair.Key, save.GetInstanceId(),
                            pair.Value.perm ? "yes" : "no", save.GetDifficultyID(), save.CanReset() ? "yes" : "no", timeleft);
                        player.UnbindInstance(pair.Key, (Difficulty)difficulty.Id);
                        counter++;
                    }
                }
            }
            handler.SendSysMessage("instances unbound: {0}", counter);

            return true;
        }
    }
}
