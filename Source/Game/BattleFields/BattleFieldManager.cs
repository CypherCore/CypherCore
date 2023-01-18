// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Database;
using Game.Entities;
using Game.Maps;
using System.Collections.Generic;
using System.Linq;

namespace Game.BattleFields
{
    public class BattleFieldManager : Singleton<BattleFieldManager>
    {
        static uint[] BattlefieldIdToMapId = { 0, 571, 732 };
        static uint[] BattlefieldIdToZoneId = { 0, 4197, 5095 }; // imitate World_PVP_Area.db2
        static uint[] BattlefieldIdToScriptId = { 0, 0, 0 };

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

                    BattlefieldIdToScriptId[(int)typeId] = Global.ObjectMgr.GetScriptId(result.Read<string>(1));
                    ++count;

                } while (result.NextRow());
            }

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} battlefields in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }

        public void CreateBattlefieldsForMap(Map map)
        {
            for (uint i = 0; i < (int)BattleFieldTypes.Max; ++i)
            {
                if (BattlefieldIdToScriptId[i] == 0)
                    continue;

                if (BattlefieldIdToMapId[i] != map.GetId())
                    continue;

                BattleField bf = Global.ScriptMgr.CreateBattlefield(BattlefieldIdToScriptId[i], map);
                if (bf == null)
                    continue;

                if (!bf.SetupBattlefield())
                {
                    Log.outInfo(LogFilter.Battlefield, $"Setting up battlefield with TypeId {(BattleFieldTypes)i} on map {map.GetId()} instance id {map.GetInstanceId()} failed.");
                    continue;
                }

                _battlefieldsByMap.Add(map, bf);
                Log.outInfo(LogFilter.Battlefield, $"Setting up battlefield with TypeId {(BattleFieldTypes)i} on map {map.GetId()} instance id {map.GetInstanceId()} succeeded.");
            }
        }

        public void DestroyBattlefieldsForMap(Map map)
        {
            _battlefieldsByMap.Remove(map);
        }

        public void AddZone(uint zoneId, BattleField bf)
        {
            _battlefieldsByZone[(bf.GetMap(), zoneId)] = bf;
        }

        public void HandlePlayerEnterZone(Player player, uint zoneId)
        {
            var bf = _battlefieldsByZone.LookupByKey((player.GetMap(), zoneId));
            if (bf == null)
                return;

            if (!bf.IsEnabled() || bf.HasPlayer(player))
                return;

            bf.HandlePlayerEnterZone(player, zoneId);
            Log.outDebug(LogFilter.Battlefield, "Player {0} entered battlefield id {1}", player.GetGUID().ToString(), bf.GetTypeId());
        }

        public void HandlePlayerLeaveZone(Player player, uint zoneId)
        {
            var bf = _battlefieldsByZone.LookupByKey((player.GetMap(), zoneId));
            if (bf == null)
                return;

            // teleport: remove once in removefromworld, once in updatezone
            if (!bf.HasPlayer(player))
                return;

            bf.HandlePlayerLeaveZone(player, zoneId);
            Log.outDebug(LogFilter.Battlefield, "Player {0} left battlefield id {1}", player.GetGUID().ToString(), bf.GetTypeId());
        }

        public bool IsWorldPvpArea(uint zoneId)
        {
            return BattlefieldIdToZoneId.Contains(zoneId);
        }

        public BattleField GetBattlefieldToZoneId(Map map, uint zoneId)
        {
            var bf = _battlefieldsByZone.LookupByKey((map, zoneId));
            if (bf == null)
            {
                // no handle for this zone, return
                return null;
            }

            if (!bf.IsEnabled())
                return null;

            return bf;
        }

        public BattleField GetBattlefieldByBattleId(Map map, uint battleId)
        {
            var battlefields = _battlefieldsByMap.LookupByKey(map);
            foreach (var battlefield in battlefields)
                if (battlefield.GetBattleId() == battleId)
                    return battlefield;

            return null;
        }

        public void Update(uint diff)
        {
            _updateTimer += diff;
            if (_updateTimer > 1000)
            {
                foreach (var (map, battlefield) in _battlefieldsByMap)
                    if (battlefield.IsEnabled())
                        battlefield.Update(_updateTimer);

                _updateTimer = 0;
            }
        }

        // contains all initiated battlefield events
        // used when initing / cleaning up
        MultiMap<Map, BattleField>  _battlefieldsByMap = new();
        // maps the zone ids to an battlefield event
        // used in player event handling
        Dictionary<(Map map, uint zoneId), BattleField>  _battlefieldsByZone = new();
        // update interval
        uint _updateTimer;
    }
}
