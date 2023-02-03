// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Misc;
using Game.Scripting;
using Game.Scripting.Interfaces.IOutdoorPvP;
using System.Collections.Generic;

namespace Game.PvP
{
    public class OutdoorPvPManager : Singleton<OutdoorPvPManager>
    {
        OutdoorPvPManager() { }

        public void InitOutdoorPvP()
        {
            uint oldMSTime = Time.GetMSTime();

            //                                             0       1
            SQLResult result = DB.World.Query("SELECT TypeId, ScriptName FROM outdoorpvp_template");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 outdoor PvP definitions. DB table `outdoorpvp_template` is empty.");
                return;
            }

            uint count = 0;

            do
            {
                OutdoorPvPTypes typeId = (OutdoorPvPTypes)result.Read<byte>(0);

                if (Global.DisableMgr.IsDisabledFor(DisableType.OutdoorPVP, (uint)typeId, null))
                    continue;

                if (typeId >= OutdoorPvPTypes.Max)
                {
                    Log.outError(LogFilter.Sql, "Invalid OutdoorPvPTypes value {0} in outdoorpvp_template; skipped.", typeId);
                    continue;
                }

                m_OutdoorPvPDatas[typeId] = Global.ObjectMgr.GetScriptId(result.Read<string>(1));

                ++count;
            }
            while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} outdoor PvP definitions in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }

        public void CreateOutdoorPvPForMap(Map map)
        {
            for (OutdoorPvPTypes outdoorPvpType = OutdoorPvPTypes.HellfirePeninsula; outdoorPvpType < OutdoorPvPTypes.Max; ++outdoorPvpType)
            {
                if (map.GetId() != m_OutdoorMapIds[(int)outdoorPvpType])
                    continue;

                if (!m_OutdoorPvPDatas.ContainsKey(outdoorPvpType))
                {
                    Log.outError(LogFilter.Sql, "Could not initialize OutdoorPvP object for type ID {0}; no entry in database.", outdoorPvpType);
                    continue;
                }
                
                OutdoorPvP pvp = Global.ScriptMgr.RunScriptRet<IOutdoorPvPGetOutdoorPvP, OutdoorPvP>(p => p.GetOutdoorPvP(map), m_OutdoorPvPDatas[outdoorPvpType], null);
                if (pvp == null)
                {
                    Log.outError(LogFilter.Outdoorpvp, "Could not initialize OutdoorPvP object for type ID {0}; got NULL pointer from script.", outdoorPvpType);
                    continue;
                }

                if (!pvp.SetupOutdoorPvP())
                {
                    Log.outError(LogFilter.Outdoorpvp, "Could not initialize OutdoorPvP object for type ID {0}; SetupOutdoorPvP failed.", outdoorPvpType);
                    continue;
                }

                m_OutdoorPvPByMap.Add(map, pvp);
            }
        }

        public void DestroyOutdoorPvPForMap(Map map)
        {
            m_OutdoorPvPByMap.Remove(map);
        }

        public void AddZone(uint zoneid, OutdoorPvP handle)
        {
            m_OutdoorPvPMap[(handle.GetMap(), zoneid)] = handle;
        }

        public void HandlePlayerEnterZone(Player player, uint zoneid)
        {
            var outdoor = GetOutdoorPvPToZoneId(player.GetMap(), zoneid);
            if (outdoor == null)
                return;

            if (outdoor.HasPlayer(player))
                return;

            outdoor.HandlePlayerEnterZone(player, zoneid);
            Log.outDebug(LogFilter.Outdoorpvp, "Player {0} entered outdoorpvp id {1}", player.GetGUID().ToString(), outdoor.GetTypeId());
        }

        public void HandlePlayerLeaveZone(Player player, uint zoneid)
        {
            var outdoor = GetOutdoorPvPToZoneId(player.GetMap(), zoneid);
            if (outdoor == null)
                return;

            // teleport: remove once in removefromworld, once in updatezone
            if (!outdoor.HasPlayer(player))
                return;

            outdoor.HandlePlayerLeaveZone(player, zoneid);
            Log.outDebug(LogFilter.Outdoorpvp, "Player {0} left outdoorpvp id {1}", player.GetGUID().ToString(), outdoor.GetTypeId());
        }

        public OutdoorPvP GetOutdoorPvPToZoneId(Map map, uint zoneid)
        {
            return m_OutdoorPvPMap.LookupByKey((map, zoneid));
        }

        public void Update(uint diff)
        {
            m_UpdateTimer += diff;
            if (m_UpdateTimer > 1000)
            {
                foreach (var (_, outdoor) in m_OutdoorPvPByMap.KeyValueList)
                    outdoor.Update(m_UpdateTimer);

                m_UpdateTimer = 0;
            }
        }

        public bool HandleCustomSpell(Player player, uint spellId, GameObject go)
        {
            OutdoorPvP pvp = player.GetOutdoorPvP();
            if (pvp != null && pvp.HasPlayer(player))
                return pvp.HandleCustomSpell(player, spellId, go);

            return false;
        }

        public bool HandleOpenGo(Player player, GameObject go)
        {
            OutdoorPvP pvp = player.GetOutdoorPvP();
            if (pvp != null && pvp.HasPlayer(player))
                return pvp.HandleOpenGo(player, go);

            return false;
        }

        public void HandleDropFlag(Player player, uint spellId)
        {
            OutdoorPvP pvp = player.GetOutdoorPvP();
            if (pvp != null && pvp.HasPlayer(player))
                pvp.HandleDropFlag(player, spellId);
        }

        public void HandlePlayerResurrects(Player player, uint zoneid)
        {
            OutdoorPvP pvp = player.GetOutdoorPvP();
            if (pvp != null && pvp.HasPlayer(player))
                pvp.HandlePlayerResurrects(player, zoneid);
        }

        public string GetDefenseMessage(uint zoneId, uint id, Locale locale)
        {
            BroadcastTextRecord bct = CliDB.BroadcastTextStorage.LookupByKey(id);
            if (bct != null)
                return Global.DB2Mgr.GetBroadcastTextValue(bct, locale);

            Log.outError(LogFilter.Outdoorpvp, "Can not find DefenseMessage (Zone: {0}, Id: {1}). BroadcastText (Id: {2}) does not exist.", zoneId, id, id);
            return "";
        }

        // contains all initiated outdoor pvp events
        // used when initing / cleaning up
        MultiMap<Map, OutdoorPvP> m_OutdoorPvPByMap = new();

        // maps the zone ids to an outdoor pvp event
        // used in player event handling
        Dictionary<(Map map, uint zoneId), OutdoorPvP> m_OutdoorPvPMap = new();

        // Holds the outdoor PvP templates
        uint[] m_OutdoorMapIds = { 0, 530, 530, 530, 530, 1 };
        Dictionary<OutdoorPvPTypes, uint> m_OutdoorPvPDatas = new();

        // update interval
        uint m_UpdateTimer;
    }
}
