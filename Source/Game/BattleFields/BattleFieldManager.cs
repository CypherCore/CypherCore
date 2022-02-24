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

using Framework.Database;
using Game.Entities;
using Game.Maps;
using System.Collections.Generic;

namespace Game.BattleFields
{
    public class BattleFieldManager : Singleton<BattleFieldManager>
    {
        BattleFieldManager() { }

        public void InitBattlefield()
        {
            uint oldMSTime = Time.GetMSTime();

            uint count = 0;
            var result = DB.World.Query("SELECT TypeId, ScriptName FROM battlefield_template");
            if (!result.IsEmpty())
            {
                do
                {
                    BattleFieldTypes typeId = (BattleFieldTypes)result.Read<byte>(0);
                    if (typeId >= BattleFieldTypes.Max)
                    {
                        Log.outError(LogFilter.Sql, $"BattlefieldMgr::InitBattlefield: Invalid TypeId value {typeId} in battlefield_template, skipped.");
                        continue;
                    }

                    uint scriptId = Global.ObjectMgr.GetScriptId(result.Read<string>(1));

                    var bf = Global.ScriptMgr.CreateBattlefield(scriptId);
                    if (!bf.SetupBattlefield())
                    {
                        Log.outInfo(LogFilter.Battlefield, $"Setting up battlefield with TypeId {typeId} failed.");
                        continue;
                    }

                    _battlefieldSet.Add(bf);
                    Log.outInfo(LogFilter.Battlefield, $"Setting up battlefield with TypeId {typeId} succeeded.");
                    ++count;

                } while (result.NextRow());
            }

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} battlefields in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }

        public void AddZone(uint zoneId, BattleField bf)
        {
            _battlefieldMap[zoneId] = bf;
        }

        public void HandlePlayerEnterZone(Player player, uint zoneId)
        {
            var bf = _battlefieldMap.LookupByKey(zoneId);
            if (bf == null)
                return;

            if (!bf.IsEnabled() || bf.HasPlayer(player))
                return;

            bf.HandlePlayerEnterZone(player, zoneId);
            Log.outDebug(LogFilter.Battlefield, "Player {0} entered battlefield id {1}", player.GetGUID().ToString(), bf.GetTypeId());
        }

        public void HandlePlayerLeaveZone(Player player, uint zoneId)
        {
            var bf = _battlefieldMap.LookupByKey(zoneId);
            if (bf == null)
                return;

            // teleport: remove once in removefromworld, once in updatezone
            if (!bf.HasPlayer(player))
                return;

            bf.HandlePlayerLeaveZone(player, zoneId);
            Log.outDebug(LogFilter.Battlefield, "Player {0} left battlefield id {1}", player.GetGUID().ToString(), bf.GetTypeId());
        }

        public BattleField GetBattlefieldToZoneId(uint zoneId)
        {
            var bf = _battlefieldMap.LookupByKey(zoneId);
            if (bf == null)
            {
                // no handle for this zone, return
                return null;
            }

            if (!bf.IsEnabled())
                return null;

            return bf;
        }

        public BattleField GetBattlefieldByBattleId(uint battleId)
        {
            foreach (var bf in _battlefieldSet)
            {
                if (bf.GetBattleId() == battleId)
                    return bf;
            }
            return null;
        }

        public BattleField GetBattlefieldByQueueId(ulong queueId)
        {
            foreach (var bf in _battlefieldSet)
                if (bf.GetQueueId() == queueId)
                    return bf;

            return null;
        }

        ZoneScript GetZoneScript(uint zoneId)
        {
            var bf = _battlefieldMap.LookupByKey(zoneId);
            if (bf != null)
                return bf;

            return null;
        }

        public void Update(uint diff)
        {
            _updateTimer += diff;
            if (_updateTimer > 1000)
            {
                foreach (var bf in _battlefieldSet)
                    if (bf.IsEnabled())
                        bf.Update(_updateTimer);
                _updateTimer = 0;
            }
        }

        // contains all initiated battlefield events
        // used when initing / cleaning up
        List<BattleField> _battlefieldSet = new();
        // maps the zone ids to an battlefield event
        // used in player event handling
        Dictionary<uint, BattleField> _battlefieldMap = new();
        // update interval
        uint _updateTimer;
    }
}
