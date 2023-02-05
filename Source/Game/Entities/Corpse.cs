// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Loots;
using Game.Maps;
using Game.Networking;
using Game.Networking.Packets;
using System.Collections.Generic;
using System.Text;

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

            m_corpseData = new();

            m_time = GameTime.GetGameTime();
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

        public override void Update(uint diff)
        {
            base.Update(diff);
            
            loot?.Update();
        }

        public void SaveToDB()
        {
            // prevent DB data inconsistence problems and duplicates
            SQLTransaction trans = new();
            DeleteFromDB(trans);

            StringBuilder items = new();
            for (var i = 0; i < m_corpseData.Items.GetSize(); ++i)
                items.Append($"{m_corpseData.Items[i]} ");

            byte index = 0;
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CORPSE);
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
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CORPSE_PHASES);
                stmt.AddValue(index++, GetOwnerGUID().GetCounter());                        // OwnerGuid
                stmt.AddValue(index++, phaseId);                                            // PhaseId
                trans.Append(stmt);
            }

            foreach (var customization in m_corpseData.Customizations)
            {
                index = 0;
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CORPSE_CUSTOMIZATIONS);
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
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CORPSE);
            stmt.AddValue(0, ownerGuid.GetCounter());
            DB.Characters.ExecuteOrAppend(trans, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CORPSE_PHASES);
            stmt.AddValue(0, ownerGuid.GetCounter());
            DB.Characters.ExecuteOrAppend(trans, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CORPSE_CUSTOMIZATIONS);
            stmt.AddValue(0, ownerGuid.GetCounter());
            DB.Characters.ExecuteOrAppend(trans, stmt);
        }

        public bool LoadCorpseFromDB(ulong guid, SQLFields field)
        {
            //        0     1     2     3            4      5          6          7     8      9       10     11        12    13          14          15
            // SELECT posX, posY, posZ, orientation, mapId, displayId, itemCache, race, class, gender, flags, dynFlags, time, corpseType, instanceId, guid FROM corpse WHERE mapId = ? AND instanceId = ?

            float posX = field.Read<float>(0);
            float posY = field.Read<float>(1);
            float posZ = field.Read<float>(2);
            float o = field.Read<float>(3);
            ushort mapId = field.Read<ushort>(4);

            _Create(ObjectGuid.Create(HighGuid.Corpse, mapId, 0, guid));

            SetObjectScale(1.0f);
            SetDisplayId(field.Read<uint>(5));
            StringArray items = new(field.Read<string>(6), ' ');
            if (items.Length == m_corpseData.Items.GetSize())
                for (uint index = 0; index < m_corpseData.Items.GetSize(); ++index)
                    SetItem(index, uint.Parse(items[(int)index]));

            SetRace(field.Read<byte>(7));
            SetClass(field.Read<byte>(8));
            SetSex(field.Read<byte>(9));
            ReplaceAllFlags((CorpseFlags)field.Read<byte>(10));
            ReplaceAllCorpseDynamicFlags((CorpseDynFlags)field.Read<byte>(11));
            SetOwnerGUID(ObjectGuid.Create(HighGuid.Player, field.Read<ulong>(15)));
            SetFactionTemplate(CliDB.ChrRacesStorage.LookupByKey(m_corpseData.RaceID).FactionID);

            m_time = field.Read<uint>(12);

            uint instanceId = field.Read<uint>(14);

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
            WorldPacket buffer = new();

            m_objectData.WriteCreate(buffer, flags, this, target);
            m_corpseData.WriteCreate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize() + 1);
            data.WriteUInt8((byte)flags);
            data.WriteBytes(buffer);
        }

        public override void BuildValuesUpdate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new();

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
            UpdateMask valuesMask = new((int)TypeId.Max);
            if (requestedObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.Object);

            if (requestedCorpseMask.IsAnySet())
                valuesMask.Set((int)TypeId.Corpse);

            WorldPacket buffer = new();
            buffer.WriteUInt32(valuesMask.GetBlock(0));

            if (valuesMask[(int)TypeId.Object])
                m_objectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

            if (valuesMask[(int)TypeId.Corpse])
                m_corpseData.WriteUpdate(buffer, requestedCorpseMask, true, this, target);

            WorldPacket buffer1 = new();
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

        public CorpseDynFlags GetCorpseDynamicFlags() { return (CorpseDynFlags)(uint)m_corpseData.DynamicFlags; }
        public bool HasCorpseDynamicFlag(CorpseDynFlags flag) { return (m_corpseData.DynamicFlags & (uint)flag) != 0; }
        public void SetCorpseDynamicFlag(CorpseDynFlags dynamicFlags) { SetUpdateFieldFlagValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.DynamicFlags), (uint)dynamicFlags); }
        public void RemoveCorpseDynamicFlag(CorpseDynFlags dynamicFlags) { RemoveUpdateFieldFlagValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.DynamicFlags), (uint)dynamicFlags); }
        public void ReplaceAllCorpseDynamicFlags(CorpseDynFlags dynamicFlags) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.DynamicFlags), (uint)dynamicFlags); }
        public override ObjectGuid GetOwnerGUID() { return m_corpseData.Owner; }
        public void SetOwnerGUID(ObjectGuid owner) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.Owner), owner); }
        public void SetPartyGUID(ObjectGuid partyGuid) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.PartyGUID), partyGuid); }
        public void SetGuildGUID(ObjectGuid guildGuid) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.GuildGUID), guildGuid); }
        public void SetDisplayId(uint displayId) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.DisplayID), displayId); }
        public void SetRace(byte race) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.RaceID), race); }
        public void SetClass(byte classId) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.Class), classId); }
        public void SetSex(byte sex) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.Sex), sex); }
        public void ReplaceAllFlags(CorpseFlags flags) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.Flags), (uint)flags); }
        public void SetFactionTemplate(int factionTemplate) { SetUpdateFieldValue(m_values.ModifyValue(m_corpseData).ModifyValue(m_corpseData.FactionTemplate), factionTemplate); }
        public override uint GetFaction() { return (uint)(int)m_corpseData.FactionTemplate; }
        public override void SetFaction(uint faction) { SetFactionTemplate((int)faction); }
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
        public void ResetGhostTime() { m_time = GameTime.GetGameTime(); }
        public CorpseType GetCorpseType() { return m_type; }

        public CellCoord GetCellCoord() { return _cellCoord; }
        public void SetCellCoord(CellCoord cellCoord) { _cellCoord = cellCoord; }

        public override Loot GetLootForPlayer(Player player)  { return loot; }
        
        public CorpseData m_corpseData;

        public Loot loot;
        public Player lootRecipient;

        CorpseType m_type;
        long m_time;
        CellCoord _cellCoord;                                    // gride for corpse position for fast search

        class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
        {
            Corpse Owner;
            ObjectFieldData ObjectMask = new();
            CorpseData CorpseMask = new();

            public ValuesUpdateForPlayerWithMaskSender(Corpse owner)
            {
                Owner = owner;
            }

            public void Invoke(Player player)
            {
                UpdateData udata = new(Owner.GetMapId());

                Owner.BuildValuesUpdateForPlayerWithMask(udata, ObjectMask.GetUpdateMask(), CorpseMask.GetUpdateMask(), player);

                udata.BuildPacket(out UpdateObject packet);
                player.SendPacket(packet);
            }
        }
    }
}
