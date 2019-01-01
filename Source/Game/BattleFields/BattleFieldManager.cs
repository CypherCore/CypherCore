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
            BattleField wg = new BattlefieldWG();
            // respawn, init variables
            if (!wg.SetupBattlefield())
            {
                Log.outError(LogFilter.Battlefield, "Battlefield: Wintergrasp init failed.");
            }
            else
            {
                _battlefieldSet.Add(wg);
                Log.outInfo(LogFilter.Battlefield, "Battlefield: Wintergrasp successfully initiated.");
            }

            /*
            For Cataclysm: Tol Barad
            Battlefield* tb = new BattlefieldTB;
            // respawn, init variables
            if (!tb.SetupBattlefield())
            {
                TC_LOG_DEBUG("bg.battlefield", "Battlefield: Tol Barad init failed.");
                delete tb;
            }
            else
            {
                _battlefieldSet.push_back(tb);
                TC_LOG_DEBUG("bg.battlefield", "Battlefield: Tol Barad successfully initiated.");
            }
            */
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
        List<BattleField> _battlefieldSet = new List<BattleField>();
        // maps the zone ids to an battlefield event
        // used in player event handling
        Dictionary<uint, BattleField> _battlefieldMap = new Dictionary<uint, BattleField>();
        // update interval
        uint _updateTimer;
    }
}
