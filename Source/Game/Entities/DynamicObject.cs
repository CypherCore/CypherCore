// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Networking;
using Game.Networking.Packets;
using Game.Spells;
using System.Collections.Generic;

namespace Game.Entities
{
    public class DynamicObject : WorldObject
    {
        public DynamicObject(bool isWorldObject) : base(isWorldObject)
        {
            ObjectTypeMask |= TypeMask.DynamicObject;
            ObjectTypeId = TypeId.DynamicObject;

            m_updateFlag.Stationary = true;

            m_entityFragments.Add(EntityFragment.Tag_DynamicObject, false);

            m_dynamicObjectData = new DynamicObjectData();
        }

        public override void Dispose()
        {
            // make sure all references were properly removed
            Cypher.Assert(_aura == null);
            Cypher.Assert(_caster == null);
            Cypher.Assert(!_isViewpoint);
            _removedAura = null;

            base.Dispose();
        }

        public override void AddToWorld()
        {
            // Register the dynamicObject for guid lookup and for caster
            if (!IsInWorld)
            {
                GetMap().GetObjectsStore().Add(GetGUID(), this);
                base.AddToWorld();
                BindToCaster();
            }
        }

        public override void RemoveFromWorld()
        {
            // Remove the dynamicObject from the accessor and from all lists of objects in world
            if (IsInWorld)
            {
                if (_isViewpoint)
                    RemoveCasterViewpoint();

                if (_aura != null)
                    RemoveAura();

                // dynobj could get removed in Aura.RemoveAura
                if (!IsInWorld)
                    return;

                UnbindFromCaster();
                base.RemoveFromWorld();
                GetMap().GetObjectsStore().Remove(GetGUID());
            }
        }

        public bool CreateDynamicObject(ulong guidlow, Unit caster, SpellInfo spell, Position pos, float radius, DynamicObjectType type, SpellCastVisualField spellVisual)
        {
            SetMap(caster.GetMap());
            Relocate(pos);
            if (!IsPositionValid())
            {
                Log.outError(LogFilter.Server, "DynamicObject (spell {0}) not created. Suggested coordinates isn't valid (X: {1} Y: {2})", spell.Id, GetPositionX(), GetPositionY());
                return false;
            }

            _Create(ObjectGuid.Create(HighGuid.DynamicObject, GetMapId(), spell.Id, guidlow));
            PhasingHandler.InheritPhaseShift(this, caster);

            UpdatePositionData();
            SetZoneScript();

            SetEntry(spell.Id);
            SetObjectScale(1f);

            SetUpdateFieldValue(m_values.ModifyValue(m_dynamicObjectData).ModifyValue(m_dynamicObjectData.Caster), caster.GetGUID());
            SetUpdateFieldValue(m_values.ModifyValue(m_dynamicObjectData).ModifyValue(m_dynamicObjectData.Type), (byte)type);

            SpellCastVisualField spellCastVisual = m_values.ModifyValue(m_dynamicObjectData).ModifyValue(m_dynamicObjectData.SpellVisual);
            SetUpdateFieldValue(ref spellCastVisual.SpellXSpellVisualID, spellVisual.SpellXSpellVisualID);
            SetUpdateFieldValue(ref spellCastVisual.ScriptVisualID, spellVisual.ScriptVisualID);

            SetUpdateFieldValue(m_values.ModifyValue(m_dynamicObjectData).ModifyValue(m_dynamicObjectData.SpellID), spell.Id);
            SetUpdateFieldValue(m_values.ModifyValue(m_dynamicObjectData).ModifyValue(m_dynamicObjectData.Radius), radius);
            SetUpdateFieldValue(m_values.ModifyValue(m_dynamicObjectData).ModifyValue(m_dynamicObjectData.CastTime), GameTime.GetGameTimeMS());

            if (IsStoredInWorldObjectGridContainer())
                SetActive(true);    //must before add to map to be put in world container

            ITransport transport = caster.GetTransport();
            if (transport != null)
            {
                float x, y, z, o;
                pos.GetPosition(out x, out y, out z, out o);
                transport.CalculatePassengerOffset(ref x, ref y, ref z, ref o);
                m_movementInfo.transport.pos.Relocate(x, y, z, o);

                // This object must be added to transport before adding to map for the client to properly display it
                transport.AddPassenger(this);
            }

            if (!GetMap().AddToMap(this))
            {
                // Returning false will cause the object to be deleted - remove from transport
                if (transport != null)
                    transport.RemovePassenger(this);
                return false;
            }

            return true;
        }

        public override void Update(uint diff)
        {
            // caster has to be always available and in the same map
            Cypher.Assert(_caster != null);
            Cypher.Assert(_caster.GetMap() == GetMap());

            bool expired = false;

            if (_aura != null)
            {
                if (!_aura.IsRemoved())
                    _aura.UpdateOwner(diff, this);

                // _aura may be set to null in Aura.UpdateOwner call
                if (_aura != null && (_aura.IsRemoved() || _aura.IsExpired()))
                    expired = true;
            }
            else
            {
                if (GetDuration() > diff)
                    _duration -= (int)diff;
                else
                    expired = true;
            }

            if (expired)
                Remove();
            else
                Global.ScriptMgr.OnDynamicObjectUpdate(this, diff);
        }

        public void Remove()
        {
            if (IsInWorld)
                AddObjectToRemoveList();
        }

        int GetDuration()
        {
            if (_aura == null)
                return _duration;
            else
                return _aura.GetDuration();
        }

        public void SetDuration(int newDuration)
        {
            if (_aura == null)
                _duration = newDuration;
            else
                _aura.SetDuration(newDuration);
        }

        public void Delay(int delaytime)
        {
            SetDuration(GetDuration() - delaytime);
        }

        public void SetAura(Aura aura)
        {
            Cypher.Assert(_aura == null && aura != null);
            _aura = aura;
        }

        void RemoveAura()
        {
            Cypher.Assert(_aura != null && _removedAura == null);
            _removedAura = _aura;
            _aura = null;
            if (!_removedAura.IsRemoved())
                _removedAura._Remove(AuraRemoveMode.Default);
        }

        public void SetCasterViewpoint()
        {
            Player caster = _caster.ToPlayer();
            if (caster != null)
            {
                caster.SetViewpoint(this, true);
                _isViewpoint = true;
            }
        }

        void RemoveCasterViewpoint()
        {
            Player caster = _caster.ToPlayer();
            if (caster != null)
            {
                caster.SetViewpoint(this, false);
                _isViewpoint = false;
            }
        }

        public override uint GetFaction()
        {
            Cypher.Assert(_caster != null);
            return _caster.GetFaction();
        }

        void BindToCaster()
        {
            Cypher.Assert(_caster == null);
            _caster = Global.ObjAccessor.GetUnit(this, GetCasterGUID());
            Cypher.Assert(_caster != null);
            Cypher.Assert(_caster.GetMap() == GetMap());
            _caster._RegisterDynObject(this);
        }

        void UnbindFromCaster()
        {
            Cypher.Assert(_caster != null);
            _caster._UnregisterDynObject(this);
            _caster = null;
        }

        public SpellInfo GetSpellInfo()
        {
            return Global.SpellMgr.GetSpellInfo(GetSpellId(), GetMap().GetDifficultyID());
        }

        public override void BuildValuesCreate(WorldPacket data, UpdateFieldFlag flags, Player target)
        {
            m_objectData.WriteCreate(data, flags, this, target);
            m_dynamicObjectData.WriteCreate(data, flags, this, target);
        }

        public override void BuildValuesUpdate(WorldPacket data, UpdateFieldFlag flags, Player target)
        {
            data.WriteUInt32(m_values.GetChangedObjectTypeMask());
            if (m_values.HasChanged(TypeId.Object))
                m_objectData.WriteUpdate(data, flags, this, target);

            if (m_values.HasChanged(TypeId.DynamicObject))
                m_dynamicObjectData.WriteUpdate(data, flags, this, target);
        }

        void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedDynamicObjectMask, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            UpdateMask valuesMask = new((int)TypeId.Max);
            if (requestedObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.Object);

            if (requestedDynamicObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.DynamicObject);

            WorldPacket buffer = new();
            BuildEntityFragmentsForValuesUpdateForPlayerWithMask(buffer, flags);
            buffer.WriteUInt32(valuesMask.GetBlock(0));

            if (valuesMask[(int)TypeId.Object])
                m_objectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

            if (valuesMask[(int)TypeId.DynamicObject])
                m_dynamicObjectData.WriteUpdate(buffer, requestedDynamicObjectMask, true, this, target);

            WorldPacket buffer1 = new();
            buffer1.WriteUInt8((byte)UpdateType.Values);
            buffer1.WritePackedGuid(GetGUID());
            buffer1.WriteUInt32(buffer.GetSize());
            buffer1.WriteBytes(buffer.GetData());

            data.AddUpdateBlock(buffer1);
        }

        public override void ClearUpdateMask(bool remove)
        {
            m_values.ClearChangesMask(m_dynamicObjectData);
            base.ClearUpdateMask(remove);
        }

        public Unit GetCaster() { return _caster; }
        public uint GetSpellId() { return m_dynamicObjectData.SpellID; }
        public ObjectGuid GetCasterGUID() { return m_dynamicObjectData.Caster; }
        public override ObjectGuid GetCreatorGUID() { return GetCasterGUID(); }
        public override ObjectGuid GetOwnerGUID() { return GetCasterGUID(); }
        public float GetRadius() { return m_dynamicObjectData.Radius; }

        DynamicObjectData m_dynamicObjectData;
        Aura _aura;
        Aura _removedAura;
        Unit _caster;
        int _duration; // for non-aura dynobjects
        bool _isViewpoint;

        class ValuesUpdateForPlayerWithMaskSender
        {
            DynamicObject Owner;
            ObjectFieldData ObjectMask = new();
            DynamicObjectData DynamicObjectMask = new();

            public ValuesUpdateForPlayerWithMaskSender(DynamicObject owner)
            {
                Owner = owner;
            }

            public void Invoke(Player player)
            {
                UpdateData udata = new(Owner.GetMapId());

                Owner.BuildValuesUpdateForPlayerWithMask(udata, ObjectMask.GetUpdateMask(), DynamicObjectMask.GetUpdateMask(), player);

                udata.BuildPacket(out UpdateObject packet);
                player.SendPacket(packet);
            }

            public static implicit operator IDoWork<Player>(ValuesUpdateForPlayerWithMaskSender obj) => obj.Invoke;
        }
    }

    public enum DynamicObjectType
    {
        Portal = 0x0,      // unused
        AreaSpell = 0x1,
        FarsightFocus = 0x2
    }
}
