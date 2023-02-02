// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using System;
using System.Collections.Generic;

namespace Game.Chat
{
    [CommandGroup("instance")]
    class InstanceCommands
    {
        [Command("getbossstate", RBACPermissions.CommandInstanceGetBossState)]
        static bool HandleInstanceGetBossStateCommand(CommandHandler handler, uint encounterId, PlayerIdentifier player)
        {
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
            if (player == null)
                player = handler.GetSession().GetPlayer();

            DateTime now = GameTime.GetDateAndTime();
            var instanceLocks = Global.InstanceLockMgr.GetInstanceLocksForPlayer(player.GetGUID());
            foreach (InstanceLock instanceLock in instanceLocks)
            {
                MapDb2Entries entries = new(instanceLock.GetMapId(), instanceLock.GetDifficultyId());
                string timeleft = !instanceLock.IsExpired() ? Time.secsToTimeString((ulong)(instanceLock.GetEffectiveExpiryTime() - now).TotalSeconds) : "-";
                handler.SendSysMessage(CypherStrings.CommandListBindInfo,
                    entries.Map.Id, entries.Map.MapName[Global.WorldMgr.GetDefaultDbcLocale()],
                    entries.MapDifficulty.DifficultyID, CliDB.DifficultyStorage.LookupByKey(entries.MapDifficulty.DifficultyID).Name,
                    instanceLock.GetInstanceId(),
                    handler.GetCypherString(instanceLock.IsExpired() ? CypherStrings.Yes : CypherStrings.No),
                    handler.GetCypherString(instanceLock.IsExtended() ? CypherStrings.Yes : CypherStrings.No),
                    timeleft);
            }

            handler.SendSysMessage(CypherStrings.CommandListBindPlayerBinds, instanceLocks.Count);
            return true;
        }

        [Command("setbossstate", RBACPermissions.CommandInstanceSetBossState)]
        static bool HandleInstanceSetBossStateCommand(CommandHandler handler, uint encounterId, EncounterState state, PlayerIdentifier player)
        {
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

            InstanceLocksStatistics statistics = Global.InstanceLockMgr.GetStatistics();

            handler.SendSysMessage(CypherStrings.CommandInstStatSaves, statistics.InstanceCount);
            handler.SendSysMessage(CypherStrings.CommandInstStatPlayersbound, statistics.PlayerCount);

            return true;
        }

        [Command("unbind", RBACPermissions.CommandInstanceUnbind)]
        static bool HandleInstanceUnbindCommand(CommandHandler handler, [VariantArg(typeof(uint), typeof(string))] object mapArg, uint? difficultyArg)
        {
            Player player = handler.GetSelectedPlayer();
            if (player == null)
                player = handler.GetSession().GetPlayer();

            uint? mapId = null;
            Difficulty? difficulty = null;

            if (mapArg is uint)
                mapId = (uint)mapArg;

            if (difficultyArg.HasValue && CliDB.DifficultyStorage.ContainsKey(difficultyArg.Value))
                difficulty = (Difficulty)difficultyArg;

            List<InstanceLock> locksReset = new();
            List<InstanceLock> locksNotReset = new();

            Global.InstanceLockMgr.ResetInstanceLocksForPlayer(player.GetGUID(), mapId, difficulty, locksReset, locksNotReset);

            DateTime now = GameTime.GetDateAndTime();
            foreach (InstanceLock instanceLock in locksReset)
            {
                MapDb2Entries entries = new(instanceLock.GetMapId(), instanceLock.GetDifficultyId());
                string timeleft = !instanceLock.IsExpired() ? Time.secsToTimeString((ulong)(instanceLock.GetEffectiveExpiryTime() - now).TotalSeconds) : "-";
                handler.SendSysMessage(CypherStrings.CommandInstUnbindUnbinding,
                    entries.Map.Id, entries.Map.MapName[Global.WorldMgr.GetDefaultDbcLocale()],
                    entries.MapDifficulty.DifficultyID, CliDB.DifficultyStorage.LookupByKey(entries.MapDifficulty.DifficultyID).Name,
                    instanceLock.GetInstanceId(),
                    handler.GetCypherString(instanceLock.IsExpired() ? CypherStrings.Yes : CypherStrings.No),
                    handler.GetCypherString(instanceLock.IsExtended() ? CypherStrings.Yes : CypherStrings.No),
                    timeleft);
            }

            handler.SendSysMessage(CypherStrings.CommandInstUnbindUnbound, locksReset.Count);

            foreach (InstanceLock instanceLock in locksNotReset)
            {
                MapDb2Entries entries = new(instanceLock.GetMapId(), instanceLock.GetDifficultyId());
                string timeleft = !instanceLock.IsExpired() ? Time.secsToTimeString((ulong)(instanceLock.GetEffectiveExpiryTime() - now).TotalSeconds) : "-";
                handler.SendSysMessage(CypherStrings.CommandInstUnbindFailed,
                    entries.Map.Id, entries.Map.MapName[Global.WorldMgr.GetDefaultDbcLocale()],
                    entries.MapDifficulty.DifficultyID, CliDB.DifficultyStorage.LookupByKey(entries.MapDifficulty.DifficultyID).Name,
                    instanceLock.GetInstanceId(),
                    handler.GetCypherString(instanceLock.IsExpired() ? CypherStrings.Yes : CypherStrings.No),
                    handler.GetCypherString(instanceLock.IsExtended() ? CypherStrings.Yes : CypherStrings.No),
                    timeleft);
            }

            player.SendRaidInfo();

            return true;
        }
    }
}
