// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;

namespace Game.Movement
{
    public struct MovementGeneratorInformation
    {
        public MovementGeneratorType Type;
        public ObjectGuid TargetGUID;
        public string TargetName;

        public MovementGeneratorInformation(MovementGeneratorType type, ObjectGuid targetGUID, string targetName = "")
        {
            Type = type;
            TargetGUID = targetGUID;
            TargetName = targetName;
        }
    }
}