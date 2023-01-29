// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.Spells
{
    public class SpellCastTargets
    {
        public SpellCastTargets()
        {
            _strTarget = "";

            _src = new SpellDestination();
            _dst = new SpellDestination();
        }

        public SpellCastTargets(Unit caster, SpellCastRequest spellCastRequest)
        {
            _targetMask = spellCastRequest.Target.Flags;
            _objectTargetGUID = spellCastRequest.Target.Unit;
            _itemTargetGUID = spellCastRequest.Target.Item;
            _strTarget = spellCastRequest.Target.Name;

            _src = new SpellDestination();
            _dst = new SpellDestination();

            if (spellCastRequest.Target.SrcLocation != null)
            {
                _src.TransportGUID = spellCastRequest.Target.SrcLocation.Transport;
                Position pos;

                if (!_src.TransportGUID.IsEmpty())
                    pos = _src.TransportOffset;
                else
                    pos = _src.Position;

                pos.Relocate(spellCastRequest.Target.SrcLocation.Location);

                if (spellCastRequest.Target.Orientation.HasValue)
                    pos.SetOrientation(spellCastRequest.Target.Orientation.Value);
            }

            if (spellCastRequest.Target.DstLocation != null)
            {
                _dst.TransportGUID = spellCastRequest.Target.DstLocation.Transport;
                Position pos;

                if (!_dst.TransportGUID.IsEmpty())
                    pos = _dst.TransportOffset;
                else
                    pos = _dst.Position;

                pos.Relocate(spellCastRequest.Target.DstLocation.Location);

                if (spellCastRequest.Target.Orientation.HasValue)
                    pos.SetOrientation(spellCastRequest.Target.Orientation.Value);
            }

            SetPitch(spellCastRequest.MissileTrajectory.Pitch);
            SetSpeed(spellCastRequest.MissileTrajectory.Speed);

            Update(caster);
        }

        public void Write(SpellTargetData data)
        {
            data.Flags = _targetMask;

            if (_targetMask.HasAnyFlag(SpellCastTargetFlags.Unit | SpellCastTargetFlags.CorpseAlly | SpellCastTargetFlags.Gameobject | SpellCastTargetFlags.CorpseEnemy | SpellCastTargetFlags.UnitMinipet))
                data.Unit = _objectTargetGUID;

            if (_targetMask.HasAnyFlag(SpellCastTargetFlags.Item | SpellCastTargetFlags.TradeItem) && _itemTarget)
                data.Item = _itemTarget.GetGUID();

            if (_targetMask.HasAnyFlag(SpellCastTargetFlags.SourceLocation))
            {
                TargetLocation target = new();
                target.Transport = _src.TransportGUID; // relative position Guid here - Transport for example

                if (!_src.TransportGUID.IsEmpty())
                    target.Location = _src.TransportOffset;
                else
                    target.Location = _src.Position;

                data.SrcLocation = target;
            }

            if (Convert.ToBoolean(_targetMask & SpellCastTargetFlags.DestLocation))
            {
                TargetLocation target = new();
                target.Transport = _dst.TransportGUID; // relative position Guid here - Transport for example

                if (!_dst.TransportGUID.IsEmpty())
                    target.Location = _dst.TransportOffset;
                else
                    target.Location = _dst.Position;

                data.DstLocation = target;
            }

            if (Convert.ToBoolean(_targetMask & SpellCastTargetFlags.String))
                data.Name = _strTarget;
        }

        public ObjectGuid GetUnitTargetGUID()
        {
            if (_objectTargetGUID.IsUnit())
                return _objectTargetGUID;

            return ObjectGuid.Empty;
        }

        public Unit GetUnitTarget()
        {
            if (_objectTarget)
                return _objectTarget.ToUnit();

            return null;
        }

        public void SetUnitTarget(Unit target)
        {
            if (target == null)
                return;

            _objectTarget = target;
            _objectTargetGUID = target.GetGUID();
            _targetMask |= SpellCastTargetFlags.Unit;
        }

        private ObjectGuid GetGOTargetGUID()
        {
            if (_objectTargetGUID.IsAnyTypeGameObject())
                return _objectTargetGUID;

            return ObjectGuid.Empty;
        }

        public GameObject GetGOTarget()
        {
            if (_objectTarget != null)
                return _objectTarget.ToGameObject();

            return null;
        }

        public void SetGOTarget(GameObject target)
        {
            if (target == null)
                return;

            _objectTarget = target;
            _objectTargetGUID = target.GetGUID();
            _targetMask |= SpellCastTargetFlags.Gameobject;
        }

        public ObjectGuid GetCorpseTargetGUID()
        {
            if (_objectTargetGUID.IsCorpse())
                return _objectTargetGUID;

            return ObjectGuid.Empty;
        }

        public Corpse GetCorpseTarget()
        {
            if (_objectTarget != null)
                return _objectTarget.ToCorpse();

            return null;
        }

        public WorldObject GetObjectTarget()
        {
            return _objectTarget;
        }

        public ObjectGuid GetObjectTargetGUID()
        {
            return _objectTargetGUID;
        }

        public void RemoveObjectTarget()
        {
            _objectTarget = null;
            _objectTargetGUID.Clear();
            _targetMask &= ~(SpellCastTargetFlags.UnitMask | SpellCastTargetFlags.CorpseMask | SpellCastTargetFlags.GameobjectMask);
        }

        public void SetItemTarget(Item item)
        {
            if (item == null)
                return;

            _itemTarget = item;
            _itemTargetGUID = item.GetGUID();
            _itemTargetEntry = item.GetEntry();
            _targetMask |= SpellCastTargetFlags.Item;
        }

        public void SetTradeItemTarget(Player caster)
        {
            _itemTargetGUID = ObjectGuid.TradeItem;
            _itemTargetEntry = 0;
            _targetMask |= SpellCastTargetFlags.TradeItem;

            Update(caster);
        }

        public void UpdateTradeSlotItem()
        {
            if (_itemTarget != null &&
                Convert.ToBoolean(_targetMask & SpellCastTargetFlags.TradeItem))
            {
                _itemTargetGUID = _itemTarget.GetGUID();
                _itemTargetEntry = _itemTarget.GetEntry();
            }
        }

        public SpellDestination GetSrc()
        {
            return _src;
        }

        public Position GetSrcPos()
        {
            return _src.Position;
        }

        private void SetSrc(float x, float y, float z)
        {
            _src = new SpellDestination(x, y, z);
            _targetMask |= SpellCastTargetFlags.SourceLocation;
        }

        private void SetSrc(Position pos)
        {
            _src = new SpellDestination(pos);
            _targetMask |= SpellCastTargetFlags.SourceLocation;
        }

        public void SetSrc(WorldObject wObj)
        {
            _src = new SpellDestination(wObj);
            _targetMask |= SpellCastTargetFlags.SourceLocation;
        }

        public void ModSrc(Position pos)
        {
            Cypher.Assert(_targetMask.HasAnyFlag(SpellCastTargetFlags.SourceLocation));
            _src.Relocate(pos);
        }

        public void RemoveSrc()
        {
            _targetMask &= ~SpellCastTargetFlags.SourceLocation;
        }

        public SpellDestination GetDst()
        {
            return _dst;
        }

        public WorldLocation GetDstPos()
        {
            return _dst.Position;
        }

        public void SetDst(float x, float y, float z, float orientation, uint mapId = 0xFFFFFFFF)
        {
            _dst = new SpellDestination(x, y, z, orientation, mapId);
            _targetMask |= SpellCastTargetFlags.DestLocation;
        }

        public void SetDst(Position pos)
        {
            _dst = new SpellDestination(pos);
            _targetMask |= SpellCastTargetFlags.DestLocation;
        }

        public void SetDst(WorldObject wObj)
        {
            _dst = new SpellDestination(wObj);
            _targetMask |= SpellCastTargetFlags.DestLocation;
        }

        public void SetDst(SpellDestination spellDest)
        {
            _dst = spellDest;
            _targetMask |= SpellCastTargetFlags.DestLocation;
        }

        public void SetDst(SpellCastTargets spellTargets)
        {
            _dst = spellTargets._dst;
            _targetMask |= SpellCastTargetFlags.DestLocation;
        }

        public void ModDst(Position pos)
        {
            Cypher.Assert(_targetMask.HasAnyFlag(SpellCastTargetFlags.DestLocation));
            _dst.Relocate(pos);
        }

        public void ModDst(SpellDestination spellDest)
        {
            Cypher.Assert(_targetMask.HasAnyFlag(SpellCastTargetFlags.DestLocation));
            _dst = spellDest;
        }

        public void RemoveDst()
        {
            _targetMask &= ~SpellCastTargetFlags.DestLocation;
        }

        public void Update(WorldObject caster)
        {
            _objectTarget = (_objectTargetGUID == caster.GetGUID()) ? caster : Global.ObjAccessor.GetWorldObject(caster, _objectTargetGUID);

            _itemTarget = null;

            if (caster is Player)
            {
                Player player = caster.ToPlayer();

                if (_targetMask.HasAnyFlag(SpellCastTargetFlags.Item))
                    _itemTarget = player.GetItemByGuid(_itemTargetGUID);
                else if (_targetMask.HasAnyFlag(SpellCastTargetFlags.TradeItem))
                    if (_itemTargetGUID == ObjectGuid.TradeItem) // here it is not Guid but Slot. Also prevents hacking slots
                    {
                        TradeData pTrade = player.GetTradeData();

                        if (pTrade != null)
                            _itemTarget = pTrade.GetTraderData().GetItem(TradeSlots.NonTraded);
                    }

                if (_itemTarget != null)
                    _itemTargetEntry = _itemTarget.GetEntry();
            }

            // update positions by Transport move
            if (HasSrc() &&
                !_src.TransportGUID.IsEmpty())
            {
                WorldObject transport = Global.ObjAccessor.GetWorldObject(caster, _src.TransportGUID);

                if (transport != null)
                {
                    _src.Position.Relocate(transport.GetPosition());
                    _src.Position.RelocateOffset(_src.TransportOffset);
                }
            }

            if (HasDst() &&
                !_dst.TransportGUID.IsEmpty())
            {
                WorldObject transport = Global.ObjAccessor.GetWorldObject(caster, _dst.TransportGUID);

                if (transport != null)
                {
                    _dst.Position.Relocate(transport.GetPosition());
                    _dst.Position.RelocateOffset(_dst.TransportOffset);
                }
            }
        }

        public SpellCastTargetFlags GetTargetMask()
        {
            return _targetMask;
        }

        public void SetTargetMask(SpellCastTargetFlags newMask)
        {
            _targetMask = newMask;
        }

        public void SetTargetFlag(SpellCastTargetFlags flag)
        {
            _targetMask |= flag;
        }

        public ObjectGuid GetItemTargetGUID()
        {
            return _itemTargetGUID;
        }

        public Item GetItemTarget()
        {
            return _itemTarget;
        }

        public uint GetItemTargetEntry()
        {
            return _itemTargetEntry;
        }

        public bool HasSrc()
        {
            return Convert.ToBoolean(_targetMask & SpellCastTargetFlags.SourceLocation);
        }

        public bool HasDst()
        {
            return Convert.ToBoolean(_targetMask & SpellCastTargetFlags.DestLocation);
        }

        public bool HasTraj()
        {
            return _speed != 0;
        }

        public float GetPitch()
        {
            return _pitch;
        }

        public void SetPitch(float pitch)
        {
            _pitch = pitch;
        }

        private float GetSpeed()
        {
            return _speed;
        }

        public void SetSpeed(float speed)
        {
            _speed = speed;
        }

        public float GetDist2d()
        {
            return _src.Position.GetExactDist2d(_dst.Position);
        }

        public float GetSpeedXY()
        {
            return (float)(_speed * Math.Cos(_pitch));
        }

        public float GetSpeedZ()
        {
            return (float)(_speed * Math.Sin(_pitch));
        }

        public string GetTargetString()
        {
            return _strTarget;
        }

        #region Fields

        private SpellCastTargetFlags _targetMask;

        // objects (can be used at spell creating and after Update at casting)
        private WorldObject _objectTarget;
        private Item _itemTarget;

        // object GUID/etc, can be used always
        private ObjectGuid _objectTargetGUID;
        private ObjectGuid _itemTargetGUID;
        private uint _itemTargetEntry;

        private SpellDestination _src;
        private SpellDestination _dst;

        private float _pitch;
        private float _speed;
        private readonly string _strTarget;

        #endregion
    }
}