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

using Framework.Constants;
using Framework.IO;
using Game.Entities;
using Game.Groups;
using Game.Maps;

namespace Game.Chat
{
    [CommandGroup("instance", RBACPermissions.CommandInstance, true)]
    class InstanceCommands
    {
        [Command("listbinds", RBACPermissions.CommandInstanceListbinds)]
        static bool HandleInstanceListBinds(StringArguments args, CommandHandler handler)
        {
            Player player = handler.getSelectedPlayer();
            if (!player)
                player = handler.GetSession().GetPlayer();

            string format = "map: {0} inst: {1} perm: {2} diff: {3} canReset: {4} TTR: {5}";

            uint counter = 0;
            for (byte i = 0; i < (int)Difficulty.Max; ++i)
            {
                var binds = player.GetBoundInstances((Difficulty)i);
                foreach (var pair in binds)
                {
                    InstanceSave save = pair.Value.save;
                    string timeleft = Time.GetTimeString(save.GetResetTime() - Time.UnixTime);
                    handler.SendSysMessage(format, pair.Key, save.GetInstanceId(), pair.Value.perm ? "yes" : "no", save.GetDifficultyID(), save.CanReset() ? "yes" : "no", timeleft);
                    counter++;
                }
            }
            handler.SendSysMessage("player binds: {0}", counter);

            counter = 0;
            Group group = player.GetGroup();
            if (group)
            {
                for (byte i = 0; i < (int)Difficulty.Max; ++i)
                {
                    var binds = group.GetBoundInstances((Difficulty)i);
                    foreach (var pair in binds)
                    {
                        InstanceSave save = pair.Value.save;
                        string timeleft = Time.GetTimeString(save.GetResetTime() - Time.UnixTime);
                        handler.SendSysMessage(format, pair.Key, save.GetInstanceId(), pair.Value.perm ? "yes" : "no", save.GetDifficultyID(), save.CanReset() ? "yes" : "no", timeleft);
                        counter++;
                    }
                }
            }
            handler.SendSysMessage("group binds: {0}", counter);

            return true;
        }

        [Command("unbind", RBACPermissions.CommandInstanceUnbind)]
        static bool HandleInstanceUnbind(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Player player = handler.getSelectedPlayer();
            if (!player)
                player = handler.GetSession().GetPlayer();

            string map = args.NextString();
            if (!sbyte.TryParse(args.NextString(), out sbyte diff))
                diff = -1;

            ushort counter = 0;
            ushort MapId = 0;

            if (map != "all")
            {
                if (!ushort.TryParse(map, out MapId) || MapId == 0)
                    return false;
            }

            for (byte i = 0; i < (int)Difficulty.Max; ++i)
            {
                var binds = player.GetBoundInstances((Difficulty)i);
                foreach (var pair in binds)
                {
                    InstanceSave save = pair.Value.save;
                    if (pair.Key != player.GetMapId() && (MapId == 0 || MapId == pair.Key) && (diff == -1 || diff == (sbyte)save.GetDifficultyID()))
                    {
                        string timeleft = Time.GetTimeString(save.GetResetTime() - Time.UnixTime);
                        handler.SendSysMessage("unbinding map: {0} inst: {1} perm: {2} diff: {3} canReset: {4} TTR: {5}", pair.Key, save.GetInstanceId(),
                            pair.Value.perm ? "yes" : "no", save.GetDifficultyID(), save.CanReset() ? "yes" : "no", timeleft);
                        player.UnbindInstance(pair.Key, (Difficulty)i);
                        counter++;
                    }
                }
            }
            handler.SendSysMessage("instances unbound: {0}", counter);

            return true;
        }

        [Command("stats", RBACPermissions.CommandInstanceStats, true)]
        static bool HandleInstanceStats(StringArguments args, CommandHandler handler)
        {
            handler.SendSysMessage("instances loaded: {0}", Global.MapMgr.GetNumInstances());
            handler.SendSysMessage("players in instances: {0}", Global.MapMgr.GetNumPlayersInInstances());
            handler.SendSysMessage("instance saves: {0}", Global.InstanceSaveMgr.GetNumInstanceSaves());
            handler.SendSysMessage("players bound: {0}", Global.InstanceSaveMgr.GetNumBoundPlayersTotal());
            handler.SendSysMessage("groups bound: {0}", Global.InstanceSaveMgr.GetNumBoundGroupsTotal());

            return true;
        }

        [Command("savedata", RBACPermissions.CommandInstanceSavedata)]
        static bool HandleInstanceSaveData(StringArguments args, CommandHandler handler)
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
        static bool HandleInstanceSetBossState(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            string param1 = args.NextString();
            string param2 = args.NextString();
            string param3 = args.NextString();
            Player player = null;

            // Character name must be provided when using this from console.
            if (string.IsNullOrEmpty(param2) || (string.IsNullOrEmpty(param3) && handler.GetSession() == null))
            {
                handler.SendSysMessage(CypherStrings.CmdSyntax);
                return false;
            }

            if (string.IsNullOrEmpty(param3))
                player = handler.GetSession().GetPlayer();
            else
            {
                if (ObjectManager.NormalizePlayerName(ref param3))
                    player = Global.ObjAccessor.FindPlayerByName(param3);
            }

            if (!player)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            InstanceMap map = player.GetMap().ToInstanceMap();
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

            if (!uint.TryParse(param1, out uint encounterId))
                return false;

            EncounterState state = EncounterState.NotStarted;
            if (int.TryParse(param2, out int param2Value))
                state = (EncounterState)param2Value;

            // Reject improper values.
            if (state > EncounterState.ToBeDecided || encounterId > map.GetInstanceScript().GetEncounterCount())
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            map.GetInstanceScript().SetBossState(encounterId, state);
            handler.SendSysMessage(CypherStrings.CommandInstSetBossState, encounterId, state);
            return true;
        }

        [Command("getbossstate", RBACPermissions.CommandInstanceGetBossState)]
        static bool HandleInstanceGetBossState(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            string param1 = args.NextString();
            string param2 = args.NextString();
            uint encounterId = 0;
            Player player = null;

            // Character name must be provided when using this from console.
            if (string.IsNullOrEmpty(param1) || (string.IsNullOrEmpty(param2) && handler.GetSession() == null))
            {
                handler.SendSysMessage(CypherStrings.CmdSyntax);
                return false;
            }

            if (string.IsNullOrEmpty(param2))
                player = handler.GetSession().GetPlayer();
            else
            {
                if (ObjectManager.NormalizePlayerName(ref param2))
                    player = Global.ObjAccessor.FindPlayerByName(param2);
            }

            if (!player)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            InstanceMap map = player.GetMap().ToInstanceMap();
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

            if (!uint.TryParse(param1, out encounterId))
                return false;

            if (encounterId > map.GetInstanceScript().GetEncounterCount())
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            EncounterState state = map.GetInstanceScript().GetBossState(encounterId);
            handler.SendSysMessage(CypherStrings.CommandInstGetBossState, encounterId, state);
            return true;
        }
    }
}
