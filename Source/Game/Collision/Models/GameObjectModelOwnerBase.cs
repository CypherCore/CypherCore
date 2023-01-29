// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Numerics;

namespace Game.Collision
{
    public abstract class GameObjectModelOwnerBase
    {
        public abstract bool IsSpawned();
        public abstract uint GetDisplayId();
        public abstract byte GetNameSetId();
        public abstract bool IsInPhase(PhaseShift phaseShift);
        public abstract Vector3 GetPosition();
        public abstract Quaternion GetRotation();
        public abstract float GetScale();
    }
}