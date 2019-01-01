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

using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using System;
using System.Collections.Generic;

namespace Game
{
    public class DisableManager : Singleton<DisableManager>
    {
        DisableManager() { }

        public class DisableData
        {
            public byte flags;
            public List<uint> param0 = new List<uint>();
            public List<uint> param1 = new List<uint>();
        }

        Dictionary<DisableType, Dictionary<uint, DisableData>> m_DisableMap = new Dictionary<DisableType, Dictionary<uint, DisableData>>();

        public void LoadDisables()
        {
            uint oldMSTime = Time.GetMSTime();

            // reload case
            m_DisableMap.Clear();

            SQLResult result = DB.World.Query("SELECT sourceType, entry, flags, params_0, params_1 FROM disables");
            if (result.IsEmpty())
            {
                Log.outError(LogFilter.ServerLoading, "Loaded 0 disables. DB table `disables` is empty!");
                return;
            }

            uint total_count = 0;
            do
            {
                DisableType type = (DisableType)result.Read<uint>(0);
                if (type >= DisableType.Max)
                {
                    Log.outError(LogFilter.Sql, "Invalid type {0} specified in `disables` table, skipped.", type);
                    continue;
                }

                uint entry = result.Read<uint>(1);
                byte flags = result.Read<byte>(2);
                string params_0 = result.Read<string>(3);
                string params_1 = result.Read<string>(4);

                DisableData data = new DisableData();
                data.flags = flags;

                switch (type)
                {
                    case DisableType.Spell:
                        if (!(Global.SpellMgr.HasSpellInfo(entry) || flags.HasAnyFlag<byte>(DisableFlags.SpellDeprecatedSpell)))
                        {
                            Log.outError(LogFilter.Sql, "Spell entry {0} from `disables` doesn't exist in dbc, skipped.", entry);
                            continue;
                        }

                        if (flags == 0 || flags > DisableFlags.MaxSpell)
                        {
                            Log.outError(LogFilter.Sql, "Disable flags for spell {0} are invalid, skipped.", entry);
                            continue;
                        }

                        if (flags.HasAnyFlag(DisableFlags.SpellMap))
                        {
                            var array = new StringArray(params_0, ',');
                            for (byte i = 0; i < array.Length;)
                            {
                                if (uint.TryParse(array[i++], out uint id))
                                    data.param0.Add(id);
                            }
                        }

                        if (flags.HasAnyFlag(DisableFlags.SpellArea))
                        {
                            var array = new StringArray(params_1, ',');
                            for (byte i = 0; i < array.Length;)
                            {
                                if (uint.TryParse(array[i++], out uint id))
                                    data.param1.Add(id);
                            }
                        }

                        break;
                    // checked later
                    case DisableType.Quest:
                        break;
                    case DisableType.Map:
                    case DisableType.LFGMap:
                        {
                            MapRecord mapEntry = CliDB.MapStorage.LookupByKey(entry);
                            if (mapEntry == null)
                            {
                                Log.outError(LogFilter.Sql, "Map entry {0} from `disables` doesn't exist in dbc, skipped.", entry);
                                continue;
                            }
                            bool isFlagInvalid = false;
                            switch (mapEntry.InstanceType)
                            {
                                case MapTypes.Common:
                                    if (flags != 0)
                                        isFlagInvalid = true;
                                    break;
                                case MapTypes.Instance:
                                case MapTypes.Raid:
                                    if (flags.HasAnyFlag(DisableFlags.DungeonStatusHeroic) && Global.DB2Mgr.GetMapDifficultyData(entry, Difficulty.Heroic) == null)
                                        flags -= DisableFlags.DungeonStatusHeroic;
                                    if (flags.HasAnyFlag(DisableFlags.DungeonStatusHeroic10Man) && Global.DB2Mgr.GetMapDifficultyData(entry, Difficulty.Raid10HC) == null)
                                        flags -= DisableFlags.DungeonStatusHeroic10Man;
                                    if (flags.HasAnyFlag(DisableFlags.DungeonStatusHeroic25Man) && Global.DB2Mgr.GetMapDifficultyData(entry, Difficulty.Raid25HC) == null)
                                        flags -= DisableFlags.DungeonStatusHeroic25Man;
                                    if (flags == 0)
                                        isFlagInvalid = true;
                                    break;
                                case MapTypes.Battleground:
                                case MapTypes.Arena:
                                    Log.outError(LogFilter.Sql, "Battlegroundmap {0} specified to be disabled in map case, skipped.", entry);
                                    continue;
                            }
                            if (isFlagInvalid)
                            {
                                Log.outError(LogFilter.Sql, "Disable flags for map {0} are invalid, skipped.", entry);
                                continue;
                            }
                            break;
                        }
                    case DisableType.Battleground:
                        if (!CliDB.BattlemasterListStorage.ContainsKey(entry))
                        {
                            Log.outError(LogFilter.Sql, "Battlegroundentry {0} from `disables` doesn't exist in dbc, skipped.", entry);
                            continue;
                        }
                        if (flags != 0)
                            Log.outError(LogFilter.Sql, "Disable flags specified for Battleground{0}, useless data.", entry);
                        break;
                    case DisableType.OutdoorPVP:
                        if (entry > (int)OutdoorPvPTypes.Max)
                        {
                            Log.outError(LogFilter.Sql, "OutdoorPvPTypes value {0} from `disables` is invalid, skipped.", entry);
                            continue;
                        }
                        if (flags != 0)
                            Log.outError(LogFilter.Sql, "Disable flags specified for outdoor PvP {0}, useless data.", entry);
                        break;
                    case DisableType.Criteria:
                        if (Global.CriteriaMgr.GetCriteria(entry) == null)
                        {
                            Log.outError(LogFilter.Sql, "Criteria entry {0} from `disables` doesn't exist in dbc, skipped.", entry);
                            continue;
                        }
                        if (flags != 0)
                            Log.outError(LogFilter.Sql, "Disable flags specified for Criteria {0}, useless data.", entry);
                        break;
                    case DisableType.VMAP:
                        {
                            MapRecord mapEntry = CliDB.MapStorage.LookupByKey(entry);
                            if (mapEntry == null)
                            {
                                Log.outError(LogFilter.Sql, "Map entry {0} from `disables` doesn't exist in dbc, skipped.", entry);
                                continue;
                            }
                            switch (mapEntry.InstanceType)
                            {
                                case MapTypes.Common:
                                    if (flags.HasAnyFlag(DisableFlags.VmapAreaFlag))
                                        Log.outInfo(LogFilter.Server, "Areaflag disabled for world map {0}.", entry);
                                    if (flags.HasAnyFlag(DisableFlags.VmapLiquidStatus))
                                        Log.outInfo(LogFilter.Server, "Liquid status disabled for world map {0}.", entry);
                                    break;
                                case MapTypes.Instance:
                                case MapTypes.Raid:
                                    if (flags.HasAnyFlag(DisableFlags.VmapHeight))
                                        Log.outInfo(LogFilter.Server, "Height disabled for instance map {0}.", entry);
                                    if (flags.HasAnyFlag(DisableFlags.VmapLOS))
                                        Log.outInfo(LogFilter.Server, "LoS disabled for instance map {0}.", entry);
                                    break;
                                case MapTypes.Battleground:
                                    if (flags.HasAnyFlag(DisableFlags.VmapHeight))
                                        Log.outInfo(LogFilter.Server, "Height disabled for Battlegroundmap {0}.", entry);
                                    if (flags.HasAnyFlag(DisableFlags.VmapLOS))
                                        Log.outInfo(LogFilter.Server, "LoS disabled for Battlegroundmap {0}.", entry);
                                    break;
                                case MapTypes.Arena:
                                    if (flags.HasAnyFlag(DisableFlags.VmapHeight))
                                        Log.outInfo(LogFilter.Server, "Height disabled for arena map {0}.", entry);
                                    if (flags.HasAnyFlag(DisableFlags.VmapLOS))
                                        Log.outInfo(LogFilter.Server, "LoS disabled for arena map {0}.", entry);
                                    break;
                                default:
                                    break;
                            }
                            break;
                        }
                    case DisableType.MMAP:
                        {
                            MapRecord mapEntry = CliDB.MapStorage.LookupByKey(entry);
                            if (mapEntry == null)
                            {
                                Log.outError(LogFilter.Sql, "Map entry {0} from `disables` doesn't exist in dbc, skipped.", entry);
                                continue;
                            }
                            switch (mapEntry.InstanceType)
                            {
                                case MapTypes.Common:
                                    Log.outInfo(LogFilter.Server, "Pathfinding disabled for world map {0}.", entry);
                                    break;
                                case MapTypes.Instance:
                                case MapTypes.Raid:
                                    Log.outInfo(LogFilter.Server, "Pathfinding disabled for instance map {0}.", entry);
                                    break;
                                case MapTypes.Battleground:
                                    Log.outInfo(LogFilter.Server, "Pathfinding disabled for Battlegroundmap {0}.", entry);
                                    break;
                                case MapTypes.Arena:
                                    Log.outInfo(LogFilter.Server, "Pathfinding disabled for arena map {0}.", entry);
                                    break;
                                default:
                                    break;
                            }
                            break;
                        }
                    default:
                        break;
                }
                if (!m_DisableMap.ContainsKey(type))
                    m_DisableMap[type] = new Dictionary<uint, DisableData>();

                m_DisableMap[type].Add(entry, data);
                ++total_count;
            }
            while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} disables in {1} ms", total_count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void CheckQuestDisables()
        {
            uint oldMSTime = Time.GetMSTime();

            int count = m_DisableMap[DisableType.Quest].Count;
            if (count == 0)
            {
                Log.outInfo(LogFilter.ServerLoading, "Checked 0 quest disables.");
                return;
            }

            // check only quests, rest already done at startup
            foreach (var pair in m_DisableMap[DisableType.Quest])
            {
                uint entry = pair.Key;
                if (Global.ObjectMgr.GetQuestTemplate(entry) == null)
                {
                    Log.outError(LogFilter.Sql, "Quest entry {0} from `disables` doesn't exist, skipped.", entry);
                    m_DisableMap[DisableType.Quest].Remove(entry);
                    continue;
                }
                if (pair.Value.flags != 0)
                    Log.outError(LogFilter.Sql, "Disable flags specified for quest {0}, useless data.", entry);
            }

            Log.outInfo(LogFilter.ServerLoading, "Checked {0} quest disables in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public bool IsDisabledFor(DisableType type, uint entry, Unit unit, byte flags = 0)
        {
            Cypher.Assert(type < DisableType.Max);
            if (!m_DisableMap.ContainsKey(type) || m_DisableMap[type].Empty())
                return false;

            var data = m_DisableMap[type].LookupByKey(entry);
            if (data == null)    // not disabled
                return false;

            switch (type)
            {
                case DisableType.Spell:
                    {
                        byte spellFlags = data.flags;
                        if (unit != null)
                        {
                            if ((spellFlags.HasAnyFlag(DisableFlags.SpellPlayer) && unit.IsTypeId(TypeId.Player)) ||
                                (unit.IsTypeId(TypeId.Unit) && ((unit.IsPet() && spellFlags.HasAnyFlag(DisableFlags.SpellPet)) || spellFlags.HasAnyFlag(DisableFlags.SpellCreature))))
                            {
                                if (spellFlags.HasAnyFlag(DisableFlags.SpellMap))
                                {
                                    List<uint> mapIds = data.param0;
                                    if (mapIds.Contains(unit.GetMapId()))
                                        return true;                                        // Spell is disabled on current map

                                    if (!spellFlags.HasAnyFlag(DisableFlags.SpellArea))
                                        return false;                                       // Spell is disabled on another map, but not this one, return false

                                    // Spell is disabled in an area, but not explicitly our current mapId. Continue processing.
                                }

                                if (spellFlags.HasAnyFlag(DisableFlags.SpellArea))
                                {
                                    var areaIds = data.param1;
                                    if (areaIds.Contains(unit.GetAreaId()))
                                        return true;                                        // Spell is disabled in this area
                                    return false;                                           // Spell is disabled in another area, but not this one, return false
                                }
                                else
                                    return true;                                            // Spell disabled for all maps
                            }

                            return false;
                        }
                        else if (spellFlags.HasAnyFlag(DisableFlags.SpellDeprecatedSpell))    // call not from spellcast
                            return true;
                        else if (flags.HasAnyFlag(DisableFlags.SpellLOS))
                            return spellFlags.HasAnyFlag(DisableFlags.SpellLOS);

                        break;
                    }
                case DisableType.Map:
                case DisableType.LFGMap:
                    Player player = unit.ToPlayer();
                    if (player != null)
                    {
                        MapRecord mapEntry = CliDB.MapStorage.LookupByKey(entry);
                        if (mapEntry.IsDungeon())
                        {
                            byte disabledModes = data.flags;
                            Difficulty targetDifficulty = player.GetDifficultyID(mapEntry);
                            Global.DB2Mgr.GetDownscaledMapDifficultyData(entry, ref targetDifficulty);
                            switch (targetDifficulty)
                            {
                                case Difficulty.Normal:
                                    return disabledModes.HasAnyFlag(DisableFlags.DungeonStatusNormal);
                                case Difficulty.Heroic:
                                    return disabledModes.HasAnyFlag(DisableFlags.DungeonStatusHeroic);
                                case Difficulty.Raid10HC:
                                    return disabledModes.HasAnyFlag(DisableFlags.DungeonStatusHeroic10Man);
                                case Difficulty.Raid25HC:
                                    return disabledModes.HasAnyFlag(DisableFlags.DungeonStatusHeroic25Man);
                                default:
                                    return false;
                            }
                        }
                        else if (mapEntry.InstanceType == MapTypes.Common)
                            return true;
                    }
                    return false;
                case DisableType.Quest:
                    if (unit == null)
                        return true;
                    Player player1 = unit.ToPlayer();
                    if (player1 != null)
                        if (player1.IsGameMaster())
                            return false;
                    return true;
                case DisableType.Battleground:
                case DisableType.OutdoorPVP:
                case DisableType.Criteria:
                case DisableType.MMAP:
                    return true;
                case DisableType.VMAP:
                    return flags.HasAnyFlag(data.flags);
            }

            return false;
        }

        public bool IsVMAPDisabledFor(uint entry, byte flags)
        {
            return IsDisabledFor(DisableType.VMAP, entry, null, flags);
        }

        public bool IsPathfindingEnabled(uint mapId)
        {
            return WorldConfig.GetBoolValue(WorldCfg.EnableMmaps) && !Global.DisableMgr.IsDisabledFor(DisableType.MMAP, mapId, null);
        }
    }

    public enum DisableType
    {
        Spell = 0,
        Quest = 1,
        Map = 2,
        Battleground= 3,
        Criteria = 4,
        OutdoorPVP = 5,
        VMAP = 6,
        MMAP = 7,
        LFGMap = 8,
        Max = 9
    }

    public struct DisableFlags
    {
        public const byte SpellPlayer = 0x1;
        public const byte SpellCreature = 0x2;
        public const byte SpellPet = 0x4;
        public const byte SpellDeprecatedSpell = 0x8;
        public const byte SpellMap = 0x10;
        public const byte SpellArea = 0x20;
        public const byte SpellLOS = 0x40;
        public const byte MaxSpell = (SpellPlayer | SpellCreature | SpellPet | SpellDeprecatedSpell | SpellMap | SpellArea | SpellLOS);

        public const byte VmapAreaFlag = 0x1;
        public const byte VmapHeight = 0x2;
        public const byte VmapLOS = 0x4;
        public const byte VmapLiquidStatus = 0x8;

        public const byte MMapPathFinding = 0x0;

        public const byte DungeonStatusNormal = 0x01;
        public const byte DungeonStatusHeroic = 0x02;

        public const byte DungeonStatusNormal10Man = 0x01;
        public const byte DungeonStatusNormal25Man = 0x02;
        public const byte DungeonStatusHeroic10Man = 0x04;
        public const byte DungeonStatusHeroic25Man = 0x08;

    }
}
