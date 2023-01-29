// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Numerics;
using Game.Collision;

namespace Game.Entities
{
    internal class GameObjectModelOwnerImpl : GameObjectModelOwnerBase
    {
        private readonly GameObject _owner;

        public GameObjectModelOwnerImpl(GameObject owner)
        {
            _owner = owner;
        }

        public override bool IsSpawned()
        {
            return _owner.IsSpawned();
        }

        public override uint GetDisplayId()
        {
            return _owner.GetDisplayId();
        }

        public override byte GetNameSetId()
        {
            return _owner.GetNameSetId();
        }

        public override bool IsInPhase(PhaseShift phaseShift)
        {
            return _owner.GetPhaseShift().CanSee(phaseShift);
        }

        public override Vector3 GetPosition()
        {
            return new Vector3(_owner.GetPositionX(), _owner.GetPositionY(), _owner.GetPositionZ());
        }

        public override Quaternion GetRotation()
        {
            return new Quaternion(_owner.GetLocalRotation().X, _owner.GetLocalRotation().Y, _owner.GetLocalRotation().Z, _owner.GetLocalRotation().W);
        }

        public override float GetScale()
        {
            return _owner.GetObjectScale();
        }
    }

}