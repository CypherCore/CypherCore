// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Numerics;
using Framework.Constants;
using Game.Entities;

namespace Game.Movement
{
    public class FacingInfo
    {
        public float Angle;
        public Vector3 F;
        public ObjectGuid Target;
        public MonsterMoveType type;
    }
}