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
using Game.Spells;
using Game.Network;

namespace Game.Entities
{
    public class DynamicObject : WorldObject
    {
        public DynamicObject(bool isWorldObject) : base(isWorldObject)
        {
            ObjectTypeMask |= TypeMask.DynamicObject;
            ObjectTypeId = TypeId.DynamicObject;

            m_updateFlag.Stationary = true;

            m_dynamicObjectData = new DynamicObjectData();
        }

        public override void Dispose()
        {
            // make sure all references were properly removed
            Cypher.Assert(_aura == null);
            Cypher.Assert(!_caster);
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

        public bool CreateDynamicObject(ulong guidlow, Unit caster, SpellInfo spell, Position pos, float radius, DynamicObjectType type, uint spellXSpellVisualId)
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

            SetEntry(spell.Id);
            SetObjectScale(1f);

            SetUpdateFieldValue(m_values.ModifyValue(m_dynamicObjectData).ModifyValue(m_dynamicObjectData.Caster), caster.GetGUID());
            SetUpdateFieldValue(m_values.ModifyValue(m_dynamicObjectData).ModifyValue(m_dynamicObjectData.Type), (byte)type);
            SetUpdateFieldValue(m_values.ModifyValue(m_dynamicObjectData).ModifyValue(m_dynamicObjectData.SpellXSpellVisualID), spellXSpellVisualId);
            SetUpdateFieldValue(m_values.ModifyValue(m_dynamicObjectData).ModifyValue(m_dynamicObjectData.SpellID), spell.Id);
            SetUpdateFieldValue(m_values.ModifyValue(m_dynamicObjectData).ModifyValue(m_dynamicObjectData.Radius), radius);
            SetUpdateFieldValue(m_values.ModifyValue(m_dynamicObjectData).ModifyValue(m_dynamicObjectData.CastTime), GameTime.GetGameTimeMS());

            if (IsWorldObject())
                SetActive(true);    //must before add to map to be put in world container

            Transport transport = caster.GetTransport();
            if (transport)
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
                if (transport)
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
            {
                RemoveFromWorld();
                AddObjectToRemoveList();
            }
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
            return Global.SpellMgr.GetSpellInfo(GetSpellId());
        }

        public override void BuildValuesCreate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new WorldPacket();

            buffer.WriteUInt8((byte)flags);
            m_objectData.WriteCreate(buffer, flags, this, target);
            m_dynamicObjectData.WriteCreate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        public override void BuildValuesUpdate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new WorldPacket();

            buffer.WriteUInt32(m_values.GetChangedObjectTypeMask());
            if (m_values.HasChanged(TypeId.Object))
                m_objectData.WriteUpdate(buffer, flags, this, target);

            if (m_values.HasChanged(TypeId.DynamicObject))
                m_dynamicObjectData.WriteUpdate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        public override void ClearUpdateMask(bool remove)
        {
            m_values.ClearChangesMask(m_dynamicObjectData);
            base.ClearUpdateMask(remove);
        }

        public Unit GetCaster() { return _caster; }
        public uint GetSpellId() { return m_dynamicObjectData.SpellID; }
        public ObjectGuid GetCasterGUID() { return m_dynamicObjectData.Caster; }
        public float GetRadius() { return m_dynamicObjectData.Radius; }

        DynamicObjectData m_dynamicObjectData;
        Aura _aura;
        Aura _removedAura;
        Unit _caster;
        int _duration; // for non-aura dynobjects
        bool _isViewpoint;
    }

    public enum DynamicObjectType
    {
        Portal = 0x0,      // unused
        AreaSpell = 0x1,
        FarsightFocus = 0x2
    }
}
