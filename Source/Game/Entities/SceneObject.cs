// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Maps;
using Game.Networking;
using Game.Networking.Packets;
using Game.Spells;
using System.Collections.Generic;

namespace Game.Entities
{
    public class SceneObject : WorldObject
    {
        SceneObjectData m_sceneObjectData;

        Position _stationaryPosition = new();
        ObjectGuid _createdBySpellCast;

        public SceneObject() : base(false)
        {
            ObjectTypeMask |= TypeMask.SceneObject;
            ObjectTypeId = TypeId.SceneObject;

            m_updateFlag.Stationary = true;
            m_updateFlag.SceneObject = true;

            m_sceneObjectData = new();
            _stationaryPosition = new();
        }

        public override void AddToWorld()
        {
            if (!IsInWorld)
            {
                GetMap().GetObjectsStore().Add(GetGUID(), this);
                base.AddToWorld();
            }
        }

        public override void RemoveFromWorld()
        {
            if (IsInWorld)
            {
                base.RemoveFromWorld();
                GetMap().GetObjectsStore().Remove(GetGUID());
            }
        }

        public override void Update(uint diff)
        {
            base.Update(diff);

            if (ShouldBeRemoved())
                Remove();
        }

        void Remove()
        {
            if (IsInWorld)
                AddObjectToRemoveList();
        }

        bool ShouldBeRemoved()
        {
            Unit creator = Global.ObjAccessor.GetUnit(this, GetOwnerGUID());
            if (creator == null)
                return true;

            if (!_createdBySpellCast.IsEmpty())
            {
                // search for a dummy aura on creator
                Aura linkedAura = creator.GetAura(_createdBySpellCast.GetEntry(), aura => aura.GetCastId() == _createdBySpellCast);
                if (linkedAura == null)
                    return true;
            }

            return false;
        }

        public static SceneObject CreateSceneObject(uint sceneId, Unit creator, Position pos, ObjectGuid privateObjectOwner)
        {
            SceneTemplate sceneTemplate = Global.ObjectMgr.GetSceneTemplate(sceneId);
            if (sceneTemplate == null)
                return null;

            ulong lowGuid = creator.GetMap().GenerateLowGuid(HighGuid.SceneObject);

            SceneObject sceneObject = new();
            if (!sceneObject.Create(lowGuid, SceneType.Normal, sceneId, sceneTemplate != null ? sceneTemplate.ScenePackageId : 0, creator.GetMap(), creator, pos, privateObjectOwner))
            {
                sceneObject.Dispose();
                return null;
            }

            return sceneObject;
        }

        bool Create(ulong lowGuid, SceneType type, uint sceneId, uint scriptPackageId, Map map, Unit creator, Position pos, ObjectGuid privateObjectOwner)
        {
            SetMap(map);
            Relocate(pos);
            RelocateStationaryPosition(pos);

            SetPrivateObjectOwner(privateObjectOwner);

            _Create(ObjectGuid.Create(HighGuid.SceneObject, GetMapId(), sceneId, lowGuid));
            PhasingHandler.InheritPhaseShift(this, creator);

            SetEntry(scriptPackageId);
            SetObjectScale(1.0f);

            SetUpdateFieldValue(m_values.ModifyValue(m_sceneObjectData).ModifyValue(m_sceneObjectData.ScriptPackageID), (int)scriptPackageId);
            SetUpdateFieldValue(m_values.ModifyValue(m_sceneObjectData).ModifyValue(m_sceneObjectData.RndSeedVal), GameTime.GetGameTimeMS());
            SetUpdateFieldValue(m_values.ModifyValue(m_sceneObjectData).ModifyValue(m_sceneObjectData.CreatedBy), creator.GetGUID());
            SetUpdateFieldValue(m_values.ModifyValue(m_sceneObjectData).ModifyValue(m_sceneObjectData.SceneType), (uint)type);

            if (!GetMap().AddToMap(this))
                return false;

            return true;
        }

        public override void BuildValuesCreate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new();

            m_objectData.WriteCreate(buffer, flags, this, target);
            m_sceneObjectData.WriteCreate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
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

            if (m_values.HasChanged(TypeId.SceneObject))
                m_sceneObjectData.WriteUpdate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedSceneObjectMask, Player target)
        {
            UpdateMask valuesMask = new((int)TypeId.Max);
            if (requestedObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.Object);

            if (requestedSceneObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.SceneObject);

            WorldPacket buffer = new();
            buffer.WriteUInt32(valuesMask.GetBlock(0));

            if (valuesMask[(int)TypeId.Object])
                m_objectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

            if (valuesMask[(int)TypeId.SceneObject])
                m_sceneObjectData.WriteUpdate(buffer, requestedSceneObjectMask, true, this, target);

            WorldPacket buffer1 = new();
            buffer1.WriteUInt8((byte)UpdateType.Values);
            buffer1.WritePackedGuid(GetGUID());
            buffer1.WriteUInt32(buffer.GetSize());
            buffer1.WriteBytes(buffer.GetData());

            data.AddUpdateBlock(buffer1);
        }

        public override void ClearUpdateMask(bool remove)
        {
            m_values.ClearChangesMask(m_sceneObjectData);
            base.ClearUpdateMask(remove);
        }

        public override ObjectGuid GetOwnerGUID() { return m_sceneObjectData.CreatedBy; }
        public override uint GetFaction() { return 0; }

        public override float GetStationaryX() { return _stationaryPosition.GetPositionX(); }
        public override float GetStationaryY() { return _stationaryPosition.GetPositionY(); }
        public override float GetStationaryZ() { return _stationaryPosition.GetPositionZ(); }
        public override float GetStationaryO() { return _stationaryPosition.GetOrientation(); }
        void RelocateStationaryPosition(Position pos) { _stationaryPosition.Relocate(pos); }

        public void SetCreatedBySpellCast(ObjectGuid castId) { _createdBySpellCast = castId; }

        class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
        {
            SceneObject Owner;
            ObjectFieldData ObjectMask = new();
            SceneObjectData SceneObjectMask = new();

            public ValuesUpdateForPlayerWithMaskSender(SceneObject owner)
            {
                Owner = owner;
            }

            public void Invoke(Player player)
            {
                UpdateData udata = new(Owner.GetMapId());

                Owner.BuildValuesUpdateForPlayerWithMask(udata, ObjectMask.GetUpdateMask(), SceneObjectMask.GetUpdateMask(), player);

                udata.BuildPacket(out UpdateObject packet);
                player.SendPacket(packet);
            }
        }
    }
}
