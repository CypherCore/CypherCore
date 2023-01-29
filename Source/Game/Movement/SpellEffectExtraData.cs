// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;

namespace Game.Movement
{
    public class SpellEffectExtraData
    {
        public uint ParabolicCurveId { get; set; }
        public uint ProgressCurveId { get; set; }
        public uint SpellVisualId { get; set; }
        public ObjectGuid Target;
    }
}