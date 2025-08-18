// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Game.Movement
{
    public class MoveSplineInitArgs
    {
        public MoveSplineInitArgs(int path_capacity = 16)
        {
            path_Idx_offset = 0;
            velocity = 0.0f;
            parabolic_amplitude = 0.0f;
            effect_start_time_percent = 0.0f;
            splineId = 0;
            initialOrientation = 0.0f;
            HasVelocity = false;
            TransformForTransport = true;
        }

        public List<Vector3> path = new();
        public FacingInfo facing = new();
        public MoveSplineFlag flags = new();
        public int path_Idx_offset;
        public float velocity;
        public float parabolic_amplitude;
        public float vertical_acceleration;
        public float effect_start_time_percent; // fraction of total spline duration
        public TimeSpan effect_start_time;  // absolute value
        public uint splineId;
        public float initialOrientation;
        public SpellEffectExtraData spellEffectExtra;
        public TurnData turnData;
        public AnimTierTransition animTier;
        public bool walk;
        public bool HasVelocity;
        public bool TransformForTransport;

        // Returns true to show that the arguments were configured correctly and MoveSpline initialization will succeed.
        public bool Validate(Unit unit)
        {
            bool CHECK(bool exp, string verbose)
            {
                if (!exp)
                {
                    if (unit != null)
                        Log.outError(LogFilter.Movement, $"MoveSplineInitArgs::Validate: expression '{exp}' failed for {verbose}");
                    else
                        Log.outError(LogFilter.Movement, $"MoveSplineInitArgs::Validate: expression '{exp}' failed for cyclic spline continuation");
                    return false;
                }
                return true;
            }

            if (!CHECK(path.Count > 1, unit.GetDebugInfo()))
                return false;
            if (!CHECK(velocity >= 0.01f, unit.GetDebugInfo()))
                return false;
            if (!CHECK(effect_start_time_percent >= 0.0f && effect_start_time_percent <= 1.0f, unit.GetDebugInfo()))
                return false;
            if (!CHECK(_checkPathLengths(), unit.GetGUID().ToString()))
                return false;
            if (spellEffectExtra != null)
            {
                if (!CHECK(spellEffectExtra.ProgressCurveId == 0 || CliDB.CurveStorage.ContainsKey(spellEffectExtra.ProgressCurveId), unit.GetDebugInfo()))
                    return false;
                if (!CHECK(spellEffectExtra.ParabolicCurveId == 0 || CliDB.CurveStorage.ContainsKey(spellEffectExtra.ParabolicCurveId), unit.GetDebugInfo()))
                    return false;
            }
            return true;
        }

        bool _checkPathLengths()
        {
            float MIN_XY_OFFSET = -(1 << 11) / 4.0f;
            float MIN_Z_OFFSET = -(1 << 10) / 4.0f;

            // positive values have 1 less bit limit (if the highest bit was set, value would be sign extended into negative when decompressing)
            float MAX_XY_OFFSET = (1 << 10) / 4.0f;
            float MAX_Z_OFFSET = (1 << 9) / 4.0f;

            var isValidPackedXYOffset = (float coord) => coord > MIN_XY_OFFSET && coord < MAX_XY_OFFSET;
            var isValidPackedZOffset = (float coord) => coord > MIN_Z_OFFSET && coord < MAX_Z_OFFSET;

            if (path.Count > 2)
            {
                Vector3 middle = (path.First() + path.Last()) / 2;
                for (int i = 1; i < path.Count - 1; ++i)
                {
                    if ((path[i + 1] - path[i]).Length() < 0.1f)
                        return false;

                    // when compression is enabled, each point coord is packed into 11 bits (10 for Z)
                    if (!flags.HasFlag(MoveSplineFlagEnum.UncompressedPath))
                        if (!isValidPackedXYOffset(middle.X - path[i].X)
                            || !isValidPackedXYOffset(middle.Y - path[i].Y)
                            || !isValidPackedZOffset(middle.Z - path[i].Z))
                            flags.SetUnsetFlag(MoveSplineFlagEnum.UncompressedPath, true);
                }
            }
            return true;
        }
    }

    public class SpellEffectExtraData
    {
        public ObjectGuid Target;
        public uint SpellVisualId;
        public uint ProgressCurveId;
        public uint ParabolicCurveId;
    }

    public class TurnData
    {
        public float StartFacing;
        public float TotalTurnRads;
        public float RadsPerSec;
    }


    public class AnimTierTransition
    {
        public uint TierTransitionId;
        public byte AnimTier;
    }
}
