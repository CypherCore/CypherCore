// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Framework.Constants;
using Framework.GameMath;

namespace Game.Collision
{
    public class GameObjectModel : IModel
    {
        private bool _collisionEnabled;
        private AxisAlignedBox _iBound;
        private Matrix4x4 _iInvRot;
        private float _iInvScale;
        private WorldModel _iModel;
        private Vector3 _iPos;
        private float _iScale;
        private bool _isWmo;
        private GameObjectModelOwnerBase _owner;

        private bool Initialize(GameObjectModelOwnerBase modelOwner)
        {
            var modelData = StaticModelList.Models.LookupByKey(modelOwner.GetDisplayId());

            if (modelData == null)
                return false;

            AxisAlignedBox mdl_box = new(modelData.Bound);

            // ignore models with no bounds
            if (mdl_box == AxisAlignedBox.Zero())
            {
                Log.outError(LogFilter.Server, "GameObject model {0} has zero bounds, loading skipped", modelData.Name);

                return false;
            }

            _iModel = Global.VMapMgr.AcquireModelInstance(modelData.Name);

            if (_iModel == null)
                return false;

            _iPos = modelOwner.GetPosition();
            _iScale = modelOwner.GetScale();
            _iInvScale = 1.0f / _iScale;

            Matrix4x4 iRotation = modelOwner.GetRotation().ToMatrix();
            iRotation.Inverse(out _iInvRot);
            // transform bounding box:
            mdl_box = new AxisAlignedBox(mdl_box.Lo * _iScale, mdl_box.Hi * _iScale);
            AxisAlignedBox rotated_bounds = new();

            for (int i = 0; i < 8; ++i)
                rotated_bounds.merge(iRotation.Multiply(mdl_box.corner(i)));

            _iBound = rotated_bounds + _iPos;
            _owner = modelOwner;
            _isWmo = modelData.IsWmo;

            return true;
        }

        public static GameObjectModel Create(GameObjectModelOwnerBase modelOwner)
        {
            GameObjectModel mdl = new();

            if (!mdl.Initialize(modelOwner))
                return null;

            return mdl;
        }

        public override bool IntersectRay(Ray ray, ref float maxDist, bool stopAtFirstHit, PhaseShift phaseShift, ModelIgnoreFlags ignoreFlags)
        {
            if (!IsCollisionEnabled() ||
                !_owner.IsSpawned())
                return false;

            if (!_owner.IsInPhase(phaseShift))
                return false;

            float time = ray.intersectionTime(_iBound);

            if (time == float.PositiveInfinity)
                return false;

            // child bounds are defined in object space:
            Vector3 p = _iInvRot.Multiply(ray.Origin - _iPos) * _iInvScale;
            Ray modRay = new(p, _iInvRot.Multiply(ray.Direction));
            float distance = maxDist * _iInvScale;
            bool hit = _iModel.IntersectRay(modRay, ref distance, stopAtFirstHit, ignoreFlags);

            if (hit)
            {
                distance *= _iScale;
                maxDist = distance;
            }

            return hit;
        }

        public override void IntersectPoint(Vector3 point, AreaInfo info, PhaseShift phaseShift)
        {
            if (!IsCollisionEnabled() ||
                !_owner.IsSpawned() ||
                !IsMapObject())
                return;

            if (!_owner.IsInPhase(phaseShift))
                return;

            if (!_iBound.contains(point))
                return;

            // child bounds are defined in object space:
            Vector3 pModel = _iInvRot.Multiply(point - _iPos) * _iInvScale;
            Vector3 zDirModel = _iInvRot.Multiply(new Vector3(0.0f, 0.0f, -1.0f));
            float zDist;

            if (_iModel.IntersectPoint(pModel, zDirModel, out zDist, info))
            {
                Vector3 modelGround = pModel + zDist * zDirModel;
                float world_Z = (_iInvRot.Multiply(modelGround) * _iScale + _iPos).Z;

                if (info.Ground_Z < world_Z)
                {
                    info.Ground_Z = world_Z;
                    info.AdtId = _owner.GetNameSetId();
                }
            }
        }

        public bool GetLocationInfo(Vector3 point, LocationInfo info, PhaseShift phaseShift)
        {
            if (!IsCollisionEnabled() ||
                !_owner.IsSpawned() ||
                !IsMapObject())
                return false;

            if (!_owner.IsInPhase(phaseShift))
                return false;

            if (!_iBound.contains(point))
                return false;

            // child bounds are defined in object space:
            Vector3 pModel = _iInvRot.Multiply(point - _iPos) * _iInvScale;
            Vector3 zDirModel = _iInvRot.Multiply(new Vector3(0.0f, 0.0f, -1.0f));
            float zDist;

            GroupLocationInfo groupInfo = new();

            if (_iModel.GetLocationInfo(pModel, zDirModel, out zDist, groupInfo))
            {
                Vector3 modelGround = pModel + zDist * zDirModel;
                float world_Z = (_iInvRot.Multiply(modelGround) * _iScale + _iPos).Z;

                if (info.Ground_Z < world_Z)
                {
                    info.Ground_Z = world_Z;

                    return true;
                }
            }

            return false;
        }

        public bool GetLiquidLevel(Vector3 point, LocationInfo info, ref float liqHeight)
        {
            // child bounds are defined in object space:
            Vector3 pModel = _iInvRot.Multiply(point - _iPos) * _iInvScale;
            //Vector3 zDirModel = iInvRot * Vector3(0.f, 0.f, -1.f);
            float zDist;

            if (info.HitModel.GetLiquidLevel(pModel, out zDist))
            {
                // calculate world height (zDist in model coords):
                // assume WMO not tilted (wouldn't make much sense anyway)
                liqHeight = zDist * _iScale + _iPos.Z;

                return true;
            }

            return false;
        }

        public bool UpdatePosition()
        {
            if (_iModel == null)
                return false;

            var it = StaticModelList.Models.LookupByKey(_owner.GetDisplayId());

            if (it == null)
                return false;

            AxisAlignedBox mdl_box = new(it.Bound);

            // ignore models with no bounds
            if (mdl_box == AxisAlignedBox.Zero())
            {
                Log.outError(LogFilter.Server, "GameObject model {0} has zero bounds, loading skipped", it.Name);

                return false;
            }

            _iPos = _owner.GetPosition();

            Matrix4x4 iRotation = _owner.GetRotation().ToMatrix();
            iRotation.Inverse(out _iInvRot);
            // transform bounding box:
            mdl_box = new AxisAlignedBox(mdl_box.Lo * _iScale, mdl_box.Hi * _iScale);
            AxisAlignedBox rotated_bounds = new();

            for (int i = 0; i < 8; ++i)
                rotated_bounds.merge(iRotation.Multiply(mdl_box.corner(i)));

            _iBound = rotated_bounds + _iPos;

            return true;
        }

        public override Vector3 GetPosition()
        {
            return _iPos;
        }

        public override AxisAlignedBox GetBounds()
        {
            return _iBound;
        }

        public void EnableCollision(bool enable)
        {
            _collisionEnabled = enable;
        }

        private bool IsCollisionEnabled()
        {
            return _collisionEnabled;
        }

        public bool IsMapObject()
        {
            return _isWmo;
        }

        public byte GetNameSetId()
        {
            return _owner.GetNameSetId();
        }

        public static bool LoadGameObjectModelList()
        {
            uint oldMSTime = Time.GetMSTime();
            var filename = Global.WorldMgr.GetDataPath() + "/vmaps/GameObjectModels.dtree";

            if (!File.Exists(filename))
            {
                Log.outWarn(LogFilter.Server, "Unable to open '{0}' file.", filename);

                return false;
            }

            try
            {
                using BinaryReader reader = new(new FileStream(filename, FileMode.Open, FileAccess.Read));
                string magic = reader.ReadStringFromChars(8);

                if (magic != MapConst.VMapMagic)
                {
                    Log.outError(LogFilter.Misc, $"File '{filename}' has wrong header, expected {MapConst.VMapMagic}.");

                    return false;
                }

                long length = reader.BaseStream.Length;

                while (true)
                {
                    if (reader.BaseStream.Position >= length)
                        break;

                    uint displayId = reader.ReadUInt32();
                    bool isWmo = reader.ReadBoolean();
                    int name_length = reader.ReadInt32();
                    string name = reader.ReadString(name_length);
                    Vector3 v1 = reader.Read<Vector3>();
                    Vector3 v2 = reader.Read<Vector3>();

                    StaticModelList.Models.Add(displayId, new GameobjectModelData(name, v1, v2, isWmo));
                }
            }
            catch (EndOfStreamException ex)
            {
                Log.outException(ex);
            }

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} GameObject models in {1} ms", StaticModelList.Models.Count, Time.GetMSTimeDiffToNow(oldMSTime));

            return true;
        }
    }
}