// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Movement
{
    public class JumpChargeParams
    {
        public float JumpGravity { get; set; }
        public uint? ParabolicCurveId { get; set; }
        public uint? ProgressCurveId { get; set; }
        public float Speed { get; set; }

        public uint? SpellVisualId { get; set; }

        public bool TreatSpeedAsMoveTimeSeconds { get; set; }
    }
}