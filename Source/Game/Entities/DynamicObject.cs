// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Networking;
using Game.Networking.Packets;
using Game.Scripting.Interfaces.IDynamicObject;
using Game.Spells;

namespace Game.Entities
{
    public class DynamicObject : WorldObject
    {
        private Aura _aura;
        private Unit _caster;
        private int _duration; // for non-aura dynobjects

        private readonly DynamicObjectData _dynamicObjectData;
        private bool _isViewpoint;
        private Aura _removedAura;

        public DynamicObject(bool isWorldObject) : base(isWorldObject)
        {
            ObjectTypeMask |= TypeMask.DynamicObject;
            ObjectTypeId = TypeId.DynamicObject;

            UpdateFlag.Stationary = true;

            _dynamicObjectData = new DynamicObjectData();
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
            // Register the dynamicObject for Guid lookup and for caster
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

            SetUpdateFieldValue(Values.ModifyValue(_dynamicObjectData).ModifyValue(_dynamicObjectData.Caster), caster.GetGUID());
            SetUpdateFieldValue(Values.ModifyValue(_dynamicObjectData).ModifyValue(_dynamicObjectData.Type), (byte)type);

            SpellCastVisualField spellCastVisual = Values.ModifyValue(_dynamicObjectData).ModifyValue(_dynamicObjectData.SpellVisual);
            SetUpdateFieldValue(ref spellCastVisual.SpellXSpellVisualID, spellVisual.SpellXSpellVisualID);
            SetUpdateFieldValue(ref spellCastVisual.ScriptVisualID, spellVisual.ScriptVisualID);

            SetUpdateFieldValue(Values.ModifyValue(_dynamicObjectData).ModifyValue(_dynamicObjectData.SpellID), spell.Id);
            SetUpdateFieldValue(Values.ModifyValue(_dynamicObjectData).ModifyValue(_dynamicObjectData.Radius), radius);
            SetUpdateFieldValue(Values.ModifyValue(_dynamicObjectData).ModifyValue(_dynamicObjectData.CastTime), GameTime.GetGameTimeMS());

            if (IsWorldObject())
                SetActive(true); //must before add to map to be put in world container

            ITransport transport = caster.GetTransport();

            if (transport != null)
            {
                float x, y, z, o;
                pos.GetPosition(out x, out y, out z, out o);
                transport.CalculatePassengerOffset(ref x, ref y, ref z, ref o);
                MovementInfo.Transport.Pos.Relocate(x, y, z, o);

                // This object must be added to Transport before adding to map for the client to properly display it
                transport.AddPassenger(this);
            }

            if (!GetMap().AddToMap(this))
            {
                // Returning false will cause the object to be deleted - remove from Transport
                transport?.RemovePassenger(this);

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
                if (_aura != null &&
                    (_aura.IsRemoved() || _aura.IsExpired()))
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
                Global.ScriptMgr.ForEach<IDynamicObjectOnUpdate>(p => p.OnUpdate(this, diff));
        }

        public void Remove()
        {
            if (IsInWorld)
                AddObjectToRemoveList();
        }

        private int GetDuration()
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

        private void RemoveAura()
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

        private void RemoveCasterViewpoint()
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

        private void BindToCaster()
        {
            Cypher.Assert(_caster == null);
            _caster = Global.ObjAccessor.GetUnit(this, GetCasterGUID());
            Cypher.Assert(_caster != null);
            Cypher.Assert(_caster.GetMap() == GetMap());
            _caster._RegisterDynObject(this);
        }

        private void UnbindFromCaster()
        {
            Cypher.Assert(_caster != null);
            _caster._UnregisterDynObject(this);
            _caster = null;
        }

        public SpellInfo GetSpellInfo()
        {
            return Global.SpellMgr.GetSpellInfo(GetSpellId(), GetMap().GetDifficultyID());
        }

        public override void BuildValuesCreate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new();

            buffer.WriteUInt8((byte)flags);
            ObjectData.WriteCreate(buffer, flags, this, target);
            _dynamicObjectData.WriteCreate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        public override void BuildValuesUpdate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new();

            buffer.WriteUInt32(Values.GetChangedObjectTypeMask());

            if (Values.HasChanged(TypeId.Object))
                ObjectData.WriteUpdate(buffer, flags, this, target);

            if (Values.HasChanged(TypeId.DynamicObject))
                _dynamicObjectData.WriteUpdate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        private void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedDynamicObjectMask, Player target)
        {
            UpdateMask valuesMask = new((int)TypeId.Max);

            if (requestedObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.Object);

            if (requestedDynamicObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.DynamicObject);

            WorldPacket buffer = new();
            buffer.WriteUInt32(valuesMask.GetBlock(0));

            if (valuesMask[(int)TypeId.Object])
                ObjectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

            if (valuesMask[(int)TypeId.DynamicObject])
                _dynamicObjectData.WriteUpdate(buffer, requestedDynamicObjectMask, true, this, target);

            WorldPacket buffer1 = new();
            buffer1.WriteUInt8((byte)UpdateType.Values);
            buffer1.WritePackedGuid(GetGUID());
            buffer1.WriteUInt32(buffer.GetSize());
            buffer1.WriteBytes(buffer.GetData());

            data.AddUpdateBlock(buffer1);
        }

        public override void ClearUpdateMask(bool remove)
        {
            Values.ClearChangesMask(_dynamicObjectData);
            base.ClearUpdateMask(remove);
        }

        public Unit GetCaster()
        {
            return _caster;
        }

        public uint GetSpellId()
        {
            return _dynamicObjectData.SpellID;
        }

        public ObjectGuid GetCasterGUID()
        {
            return _dynamicObjectData.Caster;
        }

        public override ObjectGuid GetOwnerGUID()
        {
            return GetCasterGUID();
        }

        public float GetRadius()
        {
            return _dynamicObjectData.Radius;
        }

        private class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
        {
            private readonly DynamicObjectData DynamicObjectMask = new();
            private readonly ObjectFieldData ObjectMask = new();
            private readonly DynamicObject Owner;

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
        }
    }

    public enum DynamicObjectType
    {
        Portal = 0x0, // unused
        AreaSpell = 0x1,
        FarsightFocus = 0x2
    }
}