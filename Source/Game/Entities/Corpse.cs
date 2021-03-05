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
using Framework.Database;
using Game.DataStorage;
using Game.Loots;
using Game.Maps;
using System.Collections.Generic;
using System.Text;
using Framework.Collections;
using Game.Networking;

namespace Game.Entities
{
    public class Corpse : WorldObject
    {
        public Corpse(CorpseType type = CorpseType.Bones) : base(type != CorpseType.Bones)
        {
            m_type = type;
            ObjectTypeId = TypeId.Corpse;
            ObjectTypeMask |= TypeMask.Corpse;

            m_updateFlag.Stationary = true;

            m_corpseData = new CorpseData();

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
            SetOwnerGUID(owner.GetGUID());

            _cellCoord = GridDefines.ComputeCellCoord(GetPositionX(), GetPositionY());

            PhasingHandler.InheritPhaseShift(this, owner);

            return true;
        }

        public void SaveToDB()
        {
            // prevent DB data inconsistence problems and duplicates
            var trans = new SQLTransaction();
            DeleteFromDB(trans);

            var items = new StringBuilder();
            for (var i = 0; i < EquipmentSlot.End; ++i)
                items.Append($"{m_corpseData.Items[i]} ");

            byte index = 0;
            var stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CORPSE);
            stmt.AddValue(index++, GetOwnerGUID().GetCounter());                            // guid
            stmt.AddValue(index++, GetPositionX());                                         // posX
            stmt.AddValue(index++, GetPositionY());                                         // posY
            stmt.AddValue(index++, GetPositionZ());                                         // posZ
            stmt.AddValue(index++, GetOrientation());                                       // orientation
            stmt.AddValue(index++, GetMapId());                                             // mapId
            stmt.AddValue(index++, (uint)m_corpseData.DisplayID);                           // displayId
            stmt.AddValue(index++, items.ToString());                                       // itemCache
            stmt.AddValue(index++, (byte)m_corpseData.RaceID);                              // race
            stmt.AddValue(index++, (byte)m_corpseData.Class);                             // class
            stmt.AddValue(index++, (byte)m_corpseData.Sex);                                 // gender
            stmt.AddValue(index++, (uint)m_corpseData.Flags);                               // flags
            stmt.AddValue(index++, (uint)m_corpseData.DynamicFlags);                        // dynFlags
            stmt.AddValue(index++, (uint)m_time);                                           // time
            stmt.AddValue(index++, (uint)GetCorpseType());                                  // corpseType
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

            foreach (var customization in m_corpseData.Customizations)
            {
                index = 0;
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CORPSE_CUSTOMIZATIONS);
                stmt.AddValue(index++, GetOwnerGUID().GetCounter());                        // OwnerGuid
                stmt.AddValue(index++, customization.ChrCustomizationOptionID);
                stmt.AddValue(index++, customization.ChrCustomizationChoiceID);
                trans.Append(stmt);
            }

            DB.Characters.CommitTransaction(trans);
        }

        public void DeleteFromDB(SQLTransaction trans)
        {
            DeleteFromDB(GetOwnerGUID(), trans);
        }

        public static void DeleteFromDB(ObjectGuid ownerGuid, SQLTransaction trans)
        {
            var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CORPSE);
            stmt.AddValue(0, ownerGuid.GetCounter());
            DB.Characters.ExecuteOrAppend(trans, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CORPSE_PHASES);
            stmt.AddValue(0, ownerGuid.GetCounter());
            DB.Characters.ExecuteOrAppend(trans, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CORPSE_CUSTOMIZATIONS);
            stmt.AddValue(0, ownerGuid.GetCounter());
            DB.Characters.ExecuteOrAppend(trans, stmt);
        }

        public bool LoadCorpseFromDB(ulong guid, SQLFields field)
        {
            //        0     1     2     3            4      5          6          7     8      9       10     11        12    13          14          15
            // SELECT posX, posY, posZ, orientation, mapId, displayId, itemCache, race, class, gender, flags, dynFlags, time, corpseType, instanceId, guid FROM corpse WHERE mapId = ? AND instanceId = ?

                        var posX = field.Read<float>(0);
            var posY = field.Read<float>(1);
            var posZ = field.Read<float>(2);
            var o = field.Read<float>(3);
            var mapId = field.Read<ushort>(4);

            _Create(ObjectGuid.Create(HighGuid.Corpse, mapId, 0, guid));

            SetObjectScale(1.0f);
            SetDisplayId(field.Read<uint>(5));
            var items = new StringArray(field.Read<string>(6), ' ');
            for (uint index = 0; index < EquipmentSlot.End; ++index)
                SetItem(index, uint.Parse(items[(int)index]));

            SetRace(field.Read<byte>(7));
            SetClass(field.Read<byte>(8));
            SetSex(field.Read<byte>(9));
            SetFlags((CorpseFlags)field.Read<byte>(10));
            SetCorpseDynamicFlags((CorpseDynFlags)field.Read<byte>(11));
            SetOwnerGUID(ObjectGuid.Create(HighGuid.Player, field.Read<ulong>(15)));
            SetFactionTemplate(CliDB.ChrRacesStorage.LookupByKey(m_corpseData.RaceID).FactionID);

            m_time = field.Read<uint>(12);

            var instanceId = field.Read<uint>(14);

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
            if (!Global.CharacterCacheStorage.HasCharacterCacheEntry(GetOwnerGUID()))
                return true;

            if (m_type == CorpseType.Bones)
                return m_time < t - 60 * Time.Minute;
            else
                return m_time < t - 3 * Time.Day;
        }

        public override void BuildValuesCreate(WorldPacket data, Player target)
        {
            var flags = GetUpdateFieldFlagsFor(target);
            var buffer = new WorldPacket();

            m_objectData.WriteCreate(buffer, flags, this, target);
            m_corpseData.WriteCreate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize() + 1);
            data.WriteUInt8((byte)flags);
            data.WriteBytes(buffer);
        }

        public override void BuildValuesUpdate(WorldPacket data, Player target)
        {
            var flags = GetUpdateFieldFlagsFor(target);
            var buffer = new WorldPacket();

            buffer.WriteUInt32(m_values.GetChangedObjectTypeMask());
            if (m_values.HasChanged(TypeId.Object))
                m_objectData.WriteUpdate(buffer, flags, this, target);

            if (m_values.HasChanged(TypeId.Corpse))
                m_corpseData.WriteUpdate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedCorpseMask, Player target)
        {
            var valuesMask = new UpdateMask((int)TypeId.Max);
            if (requestedObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.Object);

            if (requestedCorpseMask.IsAnySet())
                valuesMask.Set((int)TypeId.Corpse);

            var buffer = new WorldPacket();
            buffer.WriteUInt32(valuesMask.GetBlock(0));

            if (valuesMask[(int)TypeId.Object])
                m_objectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

            if (valuesMask[(int)TypeId.Corpse])
                m_corpseData.WriteUpdate(buffer, requestedCorpseMask, true, this, target);

            var buffer1 = new WorldPacket();
            buffer1.WriteUInt8((byte)UpdateType.Values);
            buffer1.WritePackedGuid(GetGUID());
            buffer1.WriteUInt32(buffer.GetSize());
            buffer1.WriteBytes(buffer.GetData());

            data.AddUpdateBlock(buffer1);
        }

        public override void ClearUpdateMask(bool remove)
        {
            m_values.ClearChangesMask(m_corpseData);
            base.ClearUpdateMask(remove);
        }

        public void AddCorpseDynamicFlag(CorpseDynFlags dynamicFlags) { SetUpdateFieldFlagValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.DynamicFlags), (uint)dynamicFlags); }
        public void RemoveCorpseDynamicFlag(CorpseDynFlags dynamicFlags) { RemoveUpdateFieldFlagValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.DynamicFlags), (uint)dynamicFlags); }
        public void SetCorpseDynamicFlags(CorpseDynFlags dynamicFlags) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.DynamicFlags), (uint)dynamicFlags); }
        public ObjectGuid GetOwnerGUID() { return m_corpseData.Owner; }
        public void SetOwnerGUID(ObjectGuid owner) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.Owner), owner); }
        public void SetPartyGUID(ObjectGuid partyGuid) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.PartyGUID), partyGuid); }
        public void SetGuildGUID(ObjectGuid guildGuid) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.GuildGUID), guildGuid); }
        public void SetDisplayId(uint displayId) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.DisplayID), displayId); }
        public void SetRace(byte race) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.RaceID), race); }
        public void SetClass(byte classId) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.Class), classId); }
        public void SetSex(byte sex) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.Sex), sex); }
        public void SetFlags(CorpseFlags flags) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.Flags), (uint)flags); }
        public void SetFactionTemplate(int factionTemplate) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.FactionTemplate), factionTemplate); }
        public void SetItem(uint slot, uint item) { SetUpdateFieldValue(ref m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.Items, (int)slot), item); }

        public void SetCustomizations(List<ChrCustomizationChoice> customizations)
        {
            ClearDynamicUpdateFieldValues(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.Customizations));
            foreach (var customization in customizations)
            {
                var newChoice = new ChrCustomizationChoice();
                newChoice.ChrCustomizationOptionID = customization.ChrCustomizationOptionID;
                newChoice.ChrCustomizationChoiceID = customization.ChrCustomizationChoiceID;
                AddDynamicUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.Customizations), newChoice);
            }
        }

        public long GetGhostTime() { return m_time; }
        public void ResetGhostTime() { m_time = Time.UnixTime; }
        public CorpseType GetCorpseType() { return m_type; }

        public CellCoord GetCellCoord() { return _cellCoord; }
        public void SetCellCoord(CellCoord cellCoord) { _cellCoord = cellCoord; }

        public CorpseData m_corpseData;

        public Loot loot = new Loot();
        public Player lootRecipient;

        CorpseType m_type;
        long m_time;
        CellCoord _cellCoord;                                    // gride for corpse position for fast search
    }
}
