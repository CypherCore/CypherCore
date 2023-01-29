// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;

namespace Game.Movement
{
    public class MoveSplineInitArgs
    {
        public AnimTierTransition AnimTier { get; set; }
        public FacingInfo Facing { get; set; } = new();
        public MoveSplineFlag Flags { get; set; } = new();
        public bool HasVelocity { get; set; }
        public float InitialOrientation { get; set; }
        public float ParabolicAmplitude { get; set; }

        public List<Vector3> Path { get; set; } = new();
        public int PathIdxOffset { get; set; }
        public SpellEffectExtraData SpellEffectExtra;
        public uint SplineId { get; set; }
        public float TimePerc { get; set; }
        public bool TransformForTransport { get; set; }
        public float Velocity { get; set; }
        public float VerticalAcceleration { get; set; }
        public bool Walk { get; set; }

        public MoveSplineInitArgs(int path_capacity = 16)
        {
            PathIdxOffset = 0;
            Velocity = 0.0f;
            ParabolicAmplitude = 0.0f;
            TimePerc = 0.0f;
            SplineId = 0;
            InitialOrientation = 0.0f;
            HasVelocity = false;
            TransformForTransport = true;
        }

        // Returns true to show that the arguments were configured correctly and MoveSpline initialization will succeed.
        public bool Validate(Unit unit)
        {
            bool CHECK(bool exp, bool verbose)
            {
                if (!exp)
                {
                    if (unit)
                        Log.outError(LogFilter.Movement, $"MoveSplineInitArgs::Validate: expression '{exp}' failed for {(verbose ? unit.GetDebugInfo() : unit.GetGUID().ToString())}");
                    else
                        Log.outError(LogFilter.Movement, $"MoveSplineInitArgs::Validate: expression '{exp}' failed for cyclic spline continuation");

                    return false;
                }

                return true;
            }

            if (!CHECK(Path.Count > 1, true))
                return false;

            if (!CHECK(Velocity >= 0.01f, true))
                return false;

            if (!CHECK(TimePerc >= 0.0f && TimePerc <= 1.0f, true))
                return false;

            if (!CHECK(_checkPathLengths(), false))
                return false;

            if (SpellEffectExtra != null)
            {
                if (!CHECK(SpellEffectExtra.ProgressCurveId == 0 || CliDB.CurveStorage.ContainsKey(SpellEffectExtra.ProgressCurveId), false))
                    return false;

                if (!CHECK(SpellEffectExtra.ParabolicCurveId == 0 || CliDB.CurveStorage.ContainsKey(SpellEffectExtra.ParabolicCurveId), false))
                    return false;

                if (!CHECK(SpellEffectExtra.ProgressCurveId == 0 || CliDB.CurveStorage.ContainsKey(SpellEffectExtra.ProgressCurveId), true))
                    return false;

                if (!CHECK(SpellEffectExtra.ParabolicCurveId == 0 || CliDB.CurveStorage.ContainsKey(SpellEffectExtra.ParabolicCurveId), true))
                    return false;
            }

            return true;
        }

        private bool _checkPathLengths()
        {
            if (Path.Count > 2 ||
                Facing.type == MonsterMoveType.Normal)
                for (int i = 0; i < Path.Count - 1; ++i)
                    if ((Path[i + 1] - Path[i]).Length() < 0.1f)
                        return false;

            return true;
        }
    }
}