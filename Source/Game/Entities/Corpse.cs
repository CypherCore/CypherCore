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
using Game.Loots;
using Game.Maps;
using System.Collections.Generic;

namespace Game.Entities
{
    public class Corpse : WorldObject
    {
        public Corpse(CorpseType type = CorpseType.Bones) : base(type != CorpseType.Bones)
        {
            m_type = type;
            objectTypeId = TypeId.Corpse;
            objectTypeMask |= TypeMask.Corpse;

            m_updateFlag.Stationary = true;

            valuesCount = (int)CorpseFields.End;

            m_time = Time.UnixTime;
        }

        public override void AddToWorld()
        {
            // Register the corpse for guid lookup
            if (!IsInWorld)
                GetMap().GetObjectsStore().Add(GetGUID(), this);

            base.AddToWorld();
        }

        public override void RemoveFromWorld()
        {
            // Remove the corpse from the accessor
            if (IsInWorld)
                GetMap().GetObjectsStore().Remove(GetGUID());

            base.RemoveFromWorld();
        }

        public bool Create(ulong guidlow, Map map)
        {
            _Create(ObjectGuid.Create(HighGuid.Corpse, map.GetId(), 0, guidlow));
            return true;
        }

        public bool Create(ulong guidlow, Player owner)
        {
            Cypher.Assert(owner != null);

            Relocate(owner.GetPositionX(), owner.GetPositionY(), owner.GetPositionZ(), owner.GetOrientation());

            if (!IsPositionValid())
            {
                Log.outError(LogFilter.Player, "Corpse (guidlow {0}, owner {1}) not created. Suggested coordinates isn't valid (X: {2} Y: {3})",
                    guidlow, owner.GetName(), owner.GetPositionX(), owner.GetPositionY());
                return false;
            }

            _Create(ObjectGuid.Create(HighGuid.Corpse, owner.GetMapId(), 0, guidlow));

            SetObjectScale(1);
            SetGuidValue(CorpseFields.Owner, owner.GetGUID());

            _cellCoord = GridDefines.ComputeCellCoord(GetPositionX(), GetPositionY());

            PhasingHandler.InheritPhaseShift(this, owner);

            return true;
        }

        public void SaveToDB()
        {
            // prevent DB data inconsistence problems and duplicates
            SQLTransaction trans = new SQLTransaction();
            DeleteFromDB(trans);

            byte index = 0;
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CORPSE);
            stmt.AddValue(index++, GetOwnerGUID().GetCounter());                            // guid
            stmt.AddValue(index++, GetPositionX());                                         // posX
            stmt.AddValue(index++, GetPositionY());                                         // posY
            stmt.AddValue(index++, GetPositionZ());                                         // posZ
            stmt.AddValue(index++, GetOrientation());                                       // orientation
            stmt.AddValue(index++, GetMapId());                                             // mapId
            stmt.AddValue(index++, GetUInt32Value(CorpseFields.DisplayId));                // displayId
            stmt.AddValue(index++, _ConcatFields(CorpseFields.Item, EquipmentSlot.End));   // itemCache
            stmt.AddValue(index++, GetUInt32Value(CorpseFields.Bytes1));                   // bytes1
            stmt.AddValue(index++, GetUInt32Value(CorpseFields.Bytes2));                   // bytes2
            stmt.AddValue(index++, GetUInt32Value(CorpseFields.Flags));                     // flags
            stmt.AddValue(index++, GetUInt32Value(CorpseFields.DynamicFlags));             // dynFlags
            stmt.AddValue(index++, (uint)m_time);                                         // time
            stmt.AddValue(index++, (uint)GetCorpseType());                                              // corpseType
            stmt.AddValue(index++, GetInstanceId());                                        // instanceId
            trans.Append(stmt);

            foreach (var phaseId in GetPhaseShift().GetPhases().Keys)
            {
                index = 0;
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CORPSE_PHASES);
                stmt.AddValue(index++, GetOwnerGUID().GetCounter());                        // OwnerGuid
                stmt.AddValue(index++, phaseId);                                            // PhaseId
                trans.Append(stmt);
            }
        }

        public void DeleteFromDB(SQLTransaction trans)
        {
            DeleteFromDB(GetOwnerGUID(), trans);
        }

        public static void DeleteFromDB(ObjectGuid ownerGuid, SQLTransaction trans)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CORPSE);
            stmt.AddValue(0, ownerGuid.GetCounter());
            DB.Characters.ExecuteOrAppend(trans, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CORPSE_PHASES);
            stmt.AddValue(0, ownerGuid.GetCounter());
            DB.Characters.ExecuteOrAppend(trans, stmt);
        }

        public bool LoadCorpseFromDB(ulong guid, SQLFields field)
        {
            //        0     1     2     3            4      5          6          7       8       9      10        11    12          13          14
            // SELECT posX, posY, posZ, orientation, mapId, displayId, itemCache, bytes1, bytes2, flags, dynFlags, time, corpseType, instanceId, guid FROM corpse WHERE mapId = ? AND instanceId = ?

            float posX = field.Read<float>(0);
            float posY = field.Read<float>(1);
            float posZ = field.Read<float>(2);
            float o = field.Read<float>(3);
            ushort mapId = field.Read<ushort>(4);

            _Create(ObjectGuid.Create(HighGuid.Corpse, mapId, 0, guid));

            SetObjectScale(1.0f);
            SetUInt32Value(CorpseFields.DisplayId, field.Read<uint>(5));
            _LoadIntoDataField(field.Read<string>(6), (int)CorpseFields.Item, EquipmentSlot.End);
            SetUInt32Value(CorpseFields.Bytes1, field.Read<uint>(7));
            SetUInt32Value(CorpseFields.Bytes2, field.Read<uint>(8));
            SetUInt32Value(CorpseFields.Flags, field.Read<byte>(9));
            SetUInt32Value(CorpseFields.DynamicFlags, field.Read<byte>(10));
            SetGuidValue(CorpseFields.Owner, ObjectGuid.Create(HighGuid.Player, field.Read<ulong>(14)));
            CharacterInfo characterInfo = Global.WorldMgr.GetCharacterInfo(GetGuidValue(CorpseFields.Owner));
            if (characterInfo != null)
                SetUInt32Value(CorpseFields.FactionTemplate, CliDB.ChrRacesStorage.LookupByKey(characterInfo.RaceID).FactionID);

            m_time = field.Read<uint>(11);

            uint instanceId = field.Read<uint>(13);

            // place
            SetLocationInstanceId(instanceId);
            SetMapId(mapId);
            Relocate(posX, posY, posZ, o);

            if (!IsPositionValid())
            {
                Log.outError(LogFilter.Player, "Corpse ({0}, owner: {1}) is not created, given coordinates are not valid (X: {2}, Y: {3}, Z: {4})",
                    GetGUID().ToString(), GetOwnerGUID().ToString(), posX, posY, posZ);
                return false;
            }

            _cellCoord = GridDefines.ComputeCellCoord(GetPositionX(), GetPositionY());
            return true;
        }

        public bool IsExpired(long t)
        {
            // Deleted character
            if (Global.WorldMgr.GetCharacterInfo(GetOwnerGUID()) == null)
                return true;

            if (m_type == CorpseType.Bones)
                return m_time < t - 60 * Time.Minute;
            else
                return m_time < t - 3 * Time.Day;
        }

        public ObjectGuid GetOwnerGUID() { return GetGuidValue(CorpseFields.Owner); }

        public long GetGhostTime() { return m_time; }
        public void ResetGhostTime() { m_time = Time.UnixTime; }
        public CorpseType GetCorpseType() { return m_type; }

        public CellCoord GetCellCoord() { return _cellCoord; }
        public void SetCellCoord(CellCoord cellCoord) { _cellCoord = cellCoord; }

        public Loot loot = new Loot();
        public Player lootRecipient;
        public bool lootForBody;

        CorpseType m_type;
        long m_time;
        CellCoord _cellCoord;                                    // gride for corpse position for fast search
    }
}
