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
using Game.Network;

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
            SQLTransaction trans = new SQLTransaction();
            DeleteFromDB(trans);

            StringBuilder items = new StringBuilder();
            for (var i = 0; i < EquipmentSlot.End; ++i)
                items.Append($"{m_corpseData.Items[i]} ");

            uint bytes1 = ((uint)m_corpseData.RaceID << 8) | ((uint)m_corpseData.Sex << 16) | ((uint)m_corpseData.SkinID << 24);
            uint bytes2 = ((uint)m_corpseData.FaceID) | ((uint)m_corpseData.HairStyleID << 8) | ((uint)m_corpseData.HairColorID << 16) | ((uint)m_corpseData.FacialHairStyleID << 24);


            byte index = 0;
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CORPSE);
            stmt.AddValue(index++, GetOwnerGUID().GetCounter());                            // guid
            stmt.AddValue(index++, GetPositionX());                                         // posX
            stmt.AddValue(index++, GetPositionY());                                         // posY
            stmt.AddValue(index++, GetPositionZ());                                         // posZ
            stmt.AddValue(index++, GetOrientation());                                       // orientation
            stmt.AddValue(index++, GetMapId());                                             // mapId
            stmt.AddValue(index++, (uint)m_corpseData.DisplayID);                           // displayId
            stmt.AddValue(index++, items.ToString());                                       // itemCache
            stmt.AddValue(index++, bytes1);                                                 // bytes1
            stmt.AddValue(index++, bytes2);                                                 // bytes2
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
            SetDisplayId(field.Read<uint>(5));
            StringArray items = new StringArray(field.Read<string>(6), ' ');
            for (uint index = 0; index < EquipmentSlot.End; ++index)
                SetItem(index, uint.Parse(items[(int)index]));

            uint bytes1 = field.Read<uint>(7);
            uint bytes2 = field.Read<uint>(8);
            SetRace((Race)((bytes1 >> 8) & 0xFF));
            SetSex((Gender)((bytes1 >> 16) & 0xFF));
            SetSkin((byte)((bytes1 >> 24) & 0xFF));
            SetFace((byte)(bytes2 & 0xFF));
            SetHairStyle((byte)((bytes2 >> 8) & 0xFF));
            SetHairColor((byte)((bytes2 >> 16) & 0xFF));
            SetFacialHairStyle((byte)((bytes2 >> 24) & 0xFF));
            SetFlags((CorpseFlags)field.Read<byte>(9));
            SetCorpseDynamicFlags((CorpseDynFlags)field.Read<byte>(10));
            SetOwnerGUID(ObjectGuid.Create(HighGuid.Player, field.Read<ulong>(14)));
            SetFactionTemplate(CliDB.ChrRacesStorage.LookupByKey(m_corpseData.RaceID).FactionID);

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
            if (!Global.CharacterCacheStorage.HasCharacterCacheEntry(GetOwnerGUID()))
                return true;

            if (m_type == CorpseType.Bones)
                return m_time < t - 60 * Time.Minute;
            else
                return m_time < t - 3 * Time.Day;
        }

        public override void BuildValuesCreate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new WorldPacket();

            m_objectData.WriteCreate(buffer, flags, this, target);
            m_corpseData.WriteCreate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteUInt8((byte)flags);
            data.WriteBytes(buffer);
        }

        public override void BuildValuesUpdate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new WorldPacket();

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
            UpdateMask valuesMask = new UpdateMask((int)TypeId.Max);
            if (requestedObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.Object);

            if (requestedCorpseMask.IsAnySet())
                valuesMask.Set((int)TypeId.Corpse);

            WorldPacket buffer = new WorldPacket();
            buffer.WriteUInt32(valuesMask.GetBlock(0));

            if (valuesMask[(int)TypeId.Object])
                m_objectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

            if (valuesMask[(int)TypeId.Corpse])
                m_corpseData.WriteUpdate(buffer, requestedCorpseMask, true, this, target);

            WorldPacket buffer1 = new WorldPacket();
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
        public void SetRace(Race race) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.RaceID), (byte)race); }
        public void SetSex(Gender sex) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.Sex), (byte)sex); }
        public void SetSkin(byte skin) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.SkinID), skin); }
        public void SetFace(byte face) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.FaceID), face); }
        public void SetHairStyle(byte hairStyle) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.HairStyleID), hairStyle); }
        public void SetHairColor(byte hairColor) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.HairColorID), hairColor); }
        public void SetFacialHairStyle(byte facialHairStyle) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.FacialHairStyleID), facialHairStyle); }
        public void SetFlags(CorpseFlags flags) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.Flags), (uint)flags); }
        public void SetFactionTemplate(int factionTemplate) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.FactionTemplate), factionTemplate); }
        public void SetItem(uint slot, uint item) { SetUpdateFieldValue(ref m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.Items, (int)slot), item); }
        public void SetCustomDisplayOption(uint slot, byte customDisplayOption) { SetUpdateFieldValue(ref m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.CustomDisplayOption, (int)slot), customDisplayOption); }

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
