// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Numerics;
using Framework.Constants;

namespace Game.Entities
{
    public class GameObjectAddon
    {
        public uint AIAnimKitID { get; set; }
        public InvisibilityType InvisibilityType { get; set; }
        public uint InvisibilityValue { get; set; }
        public Quaternion ParentRotation { get; set; }
        public uint WorldEffectID { get; set; }
    }
}