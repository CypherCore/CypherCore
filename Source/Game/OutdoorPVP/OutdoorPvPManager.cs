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
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Misc;
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
                Log.outError(LogFilter.ServerLoading, ">> Loaded 0 outdoor PvP definitions. DB table `outdoorpvp_template` is empty.");
                return;
            }

            uint count = 0;
            uint typeId = 0;

            do
            {
                typeId = result.Read<byte>(0);

                if (Global.DisableMgr.IsDisabledFor(DisableType.OutdoorPVP, typeId, null))
                    continue;

                if (typeId >= (int)OutdoorPvPTypes.Max)
                {
                    Log.outError(LogFilter.Sql, "Invalid OutdoorPvPTypes value {0} in outdoorpvp_template; skipped.", typeId);
                    continue;
                }

                OutdoorPvPData data = new OutdoorPvPData();
                OutdoorPvPTypes realTypeId = (OutdoorPvPTypes)typeId;
                data.TypeId = realTypeId;
                data.ScriptId = Global.ObjectMgr.GetScriptId(result.Read<string>(1));
                m_OutdoorPvPDatas[realTypeId] = data;

                ++count;
            }
            while (result.NextRow());

            OutdoorPvP pvp;
            for (byte i = 1; i < (int)OutdoorPvPTypes.Max; ++i)
            {
                var outdoor = m_OutdoorPvPDatas.LookupByKey((OutdoorPvPTypes)i);
                if (outdoor == null)
                {
                    Log.outError(LogFilter.Sql, "Could not initialize OutdoorPvP object for type ID {0}; no entry in database.", i);
                    continue;
                }

                pvp = Global.ScriptMgr.CreateOutdoorPvP(outdoor);
                if (pvp == null)
                {
                    Log.outError(LogFilter.Outdoorpvp, "Could not initialize OutdoorPvP object for type ID {0}; got NULL pointer from script.", i);
                    continue;
                }

                if (!pvp.SetupOutdoorPvP())
                {
                    Log.outError(LogFilter.Outdoorpvp, "Could not initialize OutdoorPvP object for type ID {0}; SetupOutdoorPvP failed.", i);
                    continue;
                }

                m_OutdoorPvPSet.Add(pvp);
            }

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} outdoor PvP definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void AddZone(uint zoneid, OutdoorPvP handle)
        {
            m_OutdoorPvPMap[zoneid] = handle;
        }

        public void HandlePlayerEnterZone(Player player, uint zoneid)
        {
            var outdoor = GetOutdoorPvPToZoneId(zoneid);
            if (outdoor == null)
                return;

            if (outdoor.HasPlayer(player))
                return;

            outdoor.HandlePlayerEnterZone(player, zoneid);
            Log.outDebug(LogFilter.Outdoorpvp, "Player {0} entered outdoorpvp id {1}", player.GetGUID().ToString(), outdoor.GetTypeId());
        }

        public void HandlePlayerLeaveZone(Player player, uint zoneid)
        {
            var outdoor = GetOutdoorPvPToZoneId(zoneid);
            if (outdoor == null)
                return;

            // teleport: remove once in removefromworld, once in updatezone
            if (!outdoor.HasPlayer(player))
                return;

            outdoor.HandlePlayerLeaveZone(player, zoneid);
            Log.outDebug(LogFilter.Outdoorpvp, "Player {0} left outdoorpvp id {1}", player.GetGUID().ToString(), outdoor.GetTypeId());
        }

        public OutdoorPvP GetOutdoorPvPToZoneId(uint zoneid)
        {
            var outdoor = m_OutdoorPvPMap.LookupByKey(zoneid);
            if (outdoor == null)
            {
                // no handle for this zone, return
                return null;
            }
            return outdoor;
        }

        public void Update(uint diff)
        {
            m_UpdateTimer += diff;
            if (m_UpdateTimer > 1000)
            {
                foreach (var outdoor in m_OutdoorPvPSet)
                    outdoor.Update(m_UpdateTimer);
                m_UpdateTimer = 0;
            }
        }

        public bool HandleCustomSpell(Player player, uint spellId, GameObject go)
        {
            foreach (var outdoor in m_OutdoorPvPSet)
            {
                if (outdoor.HandleCustomSpell(player, spellId, go))
                    return true;
            }
            return false;
        }

        public ZoneScript GetZoneScript(uint zoneId)
        {
            var outdoor = GetOutdoorPvPToZoneId(zoneId);
            if (outdoor == null)
                return null;

            return outdoor;
        }

        public bool HandleOpenGo(Player player, GameObject go)
        {
            foreach (var outdoor in m_OutdoorPvPSet)
            {
                if (outdoor.HandleOpenGo(player, go))
                    return true;
            }
            return false;
        }

        public void HandleGossipOption(Player player, Creature creature, uint gossipid)
        {
            foreach (var outdoor in m_OutdoorPvPSet)
            {
                if (outdoor.HandleGossipOption(player, creature, gossipid))
                    return;
            }
        }

        public bool CanTalkTo(Player player, Creature creature, GossipMenuItems gso)
        {
            foreach (var outdoor in m_OutdoorPvPSet)
            {
                if (outdoor.CanTalkTo(player, creature, gso))
                    return true;
            }
            return false;
        }

        public void HandleDropFlag(Player player, uint spellId)
        {
            foreach (var outdoor in m_OutdoorPvPSet)
            {
                if (outdoor.HandleDropFlag(player, spellId))
                    return;
            }
        }

        public void HandlePlayerResurrects(Player player, uint zoneid)
        {
            var outdoor = GetOutdoorPvPToZoneId(zoneid);
            if (outdoor == null)
                return;

            if (outdoor.HasPlayer(player))
                outdoor.HandlePlayerResurrects(player, zoneid);
        }

        public string GetDefenseMessage(uint zoneId, uint id, LocaleConstant locale)
        {
            BroadcastTextRecord bct = CliDB.BroadcastTextStorage.LookupByKey(id);
            if (bct != null)
                return Global.DB2Mgr.GetBroadcastTextValue(bct, locale);

            Log.outError(LogFilter.Outdoorpvp, "Can not find DefenseMessage (Zone: {0}, Id: {1}). BroadcastText (Id: {2}) does not exist.", zoneId, id, id);
            return "";
        }

        // contains all initiated outdoor pvp events
        // used when initing / cleaning up
        List<OutdoorPvP> m_OutdoorPvPSet = new List<OutdoorPvP>();

        // maps the zone ids to an outdoor pvp event
        // used in player event handling
        Dictionary<uint, OutdoorPvP> m_OutdoorPvPMap = new Dictionary<uint, OutdoorPvP>();

        // Holds the outdoor PvP templates
        Dictionary<OutdoorPvPTypes, OutdoorPvPData> m_OutdoorPvPDatas = new Dictionary<OutdoorPvPTypes, OutdoorPvPData>();

        // update interval
        uint m_UpdateTimer;
    }

    public class OutdoorPvPData
    {
        public OutdoorPvPTypes TypeId;
        public uint ScriptId;
    }
}
