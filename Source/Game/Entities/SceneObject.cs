// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Maps;
using Game.Networking;
using Game.Networking.Packets;
using Game.Spells;

namespace Game.Entities
{
	public class SceneObject : WorldObject
	{
		private ObjectGuid _createdBySpellCast;
		private SceneObjectData _sceneObjectData;

		private Position _stationaryPosition = new();

		public SceneObject() : base(false)
		{
			ObjectTypeMask |= TypeMask.SceneObject;
			ObjectTypeId   =  TypeId.SceneObject;

			_updateFlag.Stationary  = true;
			_updateFlag.SceneObject = true;

			_sceneObjectData    = new SceneObjectData();
			_stationaryPosition = new Position();
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

		private void Remove()
		{
			if (IsInWorld)
				AddObjectToRemoveList();
		}

		private bool ShouldBeRemoved()
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

		private bool Create(ulong lowGuid, SceneType type, uint sceneId, uint scriptPackageId, Map map, Unit creator, Position pos, ObjectGuid privateObjectOwner)
		{
			SetMap(map);
			Relocate(pos);
			RelocateStationaryPosition(pos);

			SetPrivateObjectOwner(privateObjectOwner);

			_Create(ObjectGuid.Create(HighGuid.SceneObject, GetMapId(), sceneId, lowGuid));
			PhasingHandler.InheritPhaseShift(this, creator);

			SetEntry(scriptPackageId);
			SetObjectScale(1.0f);

			SetUpdateFieldValue(_values.ModifyValue(_sceneObjectData).ModifyValue(_sceneObjectData.ScriptPackageID), (int)scriptPackageId);
			SetUpdateFieldValue(_values.ModifyValue(_sceneObjectData).ModifyValue(_sceneObjectData.RndSeedVal), GameTime.GetGameTimeMS());
			SetUpdateFieldValue(_values.ModifyValue(_sceneObjectData).ModifyValue(_sceneObjectData.CreatedBy), creator.GetGUID());
			SetUpdateFieldValue(_values.ModifyValue(_sceneObjectData).ModifyValue(_sceneObjectData.SceneType), (uint)type);

			if (!GetMap().AddToMap(this))
				return false;

			return true;
		}

		public override void BuildValuesCreate(WorldPacket data, Player target)
		{
			UpdateFieldFlag flags  = GetUpdateFieldFlagsFor(target);
			WorldPacket     buffer = new();

			_objectData.WriteCreate(buffer, flags, this, target);
			_sceneObjectData.WriteCreate(buffer, flags, this, target);

			data.WriteUInt32(buffer.GetSize());
			data.WriteUInt8((byte)flags);
			data.WriteBytes(buffer);
		}

		public override void BuildValuesUpdate(WorldPacket data, Player target)
		{
			UpdateFieldFlag flags  = GetUpdateFieldFlagsFor(target);
			WorldPacket     buffer = new();

			buffer.WriteUInt32(_values.GetChangedObjectTypeMask());

			if (_values.HasChanged(TypeId.Object))
				_objectData.WriteUpdate(buffer, flags, this, target);

			if (_values.HasChanged(TypeId.SceneObject))
				_sceneObjectData.WriteUpdate(buffer, flags, this, target);

			data.WriteUInt32(buffer.GetSize());
			data.WriteBytes(buffer);
		}

		private void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedSceneObjectMask, Player target)
		{
			UpdateMask valuesMask = new((int)TypeId.Max);

			if (requestedObjectMask.IsAnySet())
				valuesMask.Set((int)TypeId.Object);

			if (requestedSceneObjectMask.IsAnySet())
				valuesMask.Set((int)TypeId.SceneObject);

			WorldPacket buffer = new();
			buffer.WriteUInt32(valuesMask.GetBlock(0));

			if (valuesMask[(int)TypeId.Object])
				_objectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

			if (valuesMask[(int)TypeId.SceneObject])
				_sceneObjectData.WriteUpdate(buffer, requestedSceneObjectMask, true, this, target);

			WorldPacket buffer1 = new();
			buffer1.WriteUInt8((byte)UpdateType.Values);
			buffer1.WritePackedGuid(GetGUID());
			buffer1.WriteUInt32(buffer.GetSize());
			buffer1.WriteBytes(buffer.GetData());

			data.AddUpdateBlock(buffer1);
		}

		public override void ClearUpdateMask(bool remove)
		{
			_values.ClearChangesMask(_sceneObjectData);
			base.ClearUpdateMask(remove);
		}

		public override ObjectGuid GetOwnerGUID()
		{
			return _sceneObjectData.CreatedBy;
		}

		public override uint GetFaction()
		{
			return 0;
		}

		public override float GetStationaryX()
		{
			return _stationaryPosition.GetPositionX();
		}

		public override float GetStationaryY()
		{
			return _stationaryPosition.GetPositionY();
		}

		public override float GetStationaryZ()
		{
			return _stationaryPosition.GetPositionZ();
		}

		public override float GetStationaryO()
		{
			return _stationaryPosition.GetOrientation();
		}

		private void RelocateStationaryPosition(Position pos)
		{
			_stationaryPosition.Relocate(pos);
		}

		public void SetCreatedBySpellCast(ObjectGuid castId)
		{
			_createdBySpellCast = castId;
		}

		private class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
		{
			private ObjectFieldData ObjectMask = new();
			private SceneObject Owner;
			private SceneObjectData SceneObjectMask = new();

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