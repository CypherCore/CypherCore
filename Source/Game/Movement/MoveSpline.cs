/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Framework.Dynamic;
using Framework.GameMath;
using System;
using System.Collections.Generic;

namespace Game.Movement
{
    public class MoveSpline
    {
        public MoveSpline()
        {
            m_Id = 0;
            time_passed = 0;
            vertical_acceleration = 0.0f;
            initialOrientation = 0.0f;
            effect_start_time = 0;
            point_Idx = 0;
            point_Idx_offset = 0;
            onTransport = false;
            splineIsFacingOnly = false;
            splineflags.Flags = SplineFlag.Done;
        }

        public void Initialize(MoveSplineInitArgs args)
        {
            splineflags = args.flags;
            facing = args.facing;
            m_Id = args.splineId;
            point_Idx_offset = args.path_Idx_offset;
            initialOrientation = args.initialOrientation;

            time_passed = 0;
            vertical_acceleration = 0.0f;
            effect_start_time = 0;
            spell_effect_extra = args.spellEffectExtra;
            splineIsFacingOnly = args.path.Length == 2 && args.facing.type != MonsterMoveType.Normal && ((args.path[1] - args.path[0]).GetLength() < 0.1f);

            // Check if its a stop spline
            if (args.flags.HasFlag(SplineFlag.Done))
            {
                spline.Clear();
                return;
            }

            InitSpline(args);

            // init parabolic / animation
            // spline initialized, duration known and i able to compute parabolic acceleration
            if (args.flags.HasFlag(SplineFlag.Parabolic | SplineFlag.Animation | SplineFlag.FadeObject))
            {
                effect_start_time = (int)(Duration() * args.time_perc);
                if (args.flags.HasFlag(SplineFlag.Parabolic) && effect_start_time < Duration())
                {
                    float f_duration = (float)TimeSpan.FromMilliseconds(Duration() - effect_start_time).TotalSeconds;
                    vertical_acceleration = args.parabolic_amplitude * 8.0f / (f_duration * f_duration);
                }
            }
        }

        void InitSpline(MoveSplineInitArgs args)
        {
            Spline.EvaluationMode[] modes = new Spline.EvaluationMode[2] { Spline.EvaluationMode.Linear, Spline.EvaluationMode.Catmullrom };
            if (args.flags.HasFlag(SplineFlag.Cyclic))
            {
                spline.InitCyclicSpline(args.path, args.path.Length, modes[Convert.ToInt32(args.flags.IsSmooth())], 0);
            }
            else
            {
                spline.InitSpline(args.path, args.path.Length, modes[Convert.ToInt32(args.flags.IsSmooth())]);
            }

            // init spline timestamps
            if (splineflags.HasFlag(SplineFlag.Falling))
            {
                FallInitializer init = new FallInitializer(spline.GetPoint(spline.First()).Z);
                spline.InitLengths(init);
            }
            else
            {
                CommonInitializer init = new CommonInitializer(args.velocity);
                spline.InitLengths(init);
            }

            // TODO: what to do in such cases? problem is in input data (all points are at same coords)
            if (spline.Length() < 1)
            {
                Log.outError(LogFilter.Unit, "MoveSpline.init_spline: zero length spline, wrong input data?");
                spline.Set_length(spline.Last(), spline.IsCyclic() ? 1000 : 1);
            }
            point_Idx = spline.First();
        }

        public int CurrentPathIdx()
        {
            int point = point_Idx_offset + point_Idx - spline.First() + (Finalized() ? 1 : 0);
            if (IsCyclic())
                point = point % (spline.Last() - spline.First());
            return point;
        }

        public Vector3[] GetPath() { return spline.GetPoints(); }
        public int TimePassed() { return time_passed; }

        public int Duration() { return spline.Length(); }
        public int CurrentSplineIdx() { return point_Idx; }
        public uint GetId() { return m_Id; }
        public bool Finalized() { return splineflags.HasFlag(SplineFlag.Done); }
        void _Finalize()
        {
            splineflags.SetUnsetFlag(SplineFlag.Done);
            point_Idx = spline.Last() - 1;
            time_passed = Duration();
        }
        public Vector4 ComputePosition(int time_point, int point_index)
        {            
            float u = 1.0f;
            int seg_time = spline.Length(point_index, point_index + 1);
            if (seg_time > 0)
                u = (time_point - spline.Length(point_index)) / (float)seg_time;

            Vector3 c;
            float orientation = initialOrientation;
            spline.Evaluate_Percent(point_index, u, out c);

            if (splineflags.HasFlag(SplineFlag.Parabolic))
                ComputeParabolicElevation(time_point, ref c.Z);
            else if (splineflags.HasFlag(SplineFlag.Falling))
                ComputeFallElevation(time_point, ref c.Z);

            if (splineflags.HasFlag(SplineFlag.Done) && facing.type != MonsterMoveType.Normal)
            {
                if (facing.type == MonsterMoveType.FacingAngle)
                    orientation = facing.angle;
                else if (facing.type == MonsterMoveType.FacingSpot)
                    orientation = (float)Math.Atan2(facing.f.Y - c.Y, facing.f.X - c.X);
                //nothing to do for MoveSplineFlag.Final_Target flag
            }
            else
            {
                if (!splineflags.HasFlag(SplineFlag.OrientationFixed | SplineFlag.Falling | SplineFlag.Unknown0))
                {
                    Vector3 hermite;
                    spline.Evaluate_Derivative(point_Idx, u, out hermite);
                    orientation = (float)Math.Atan2(hermite.Y, hermite.X);
                }

                if (splineflags.HasFlag(SplineFlag.Backward))
                    orientation = orientation - (float)Math.PI;
            }

            return new Vector4(c.X, c.Y, c.Z, orientation);
        }
        public Vector4 ComputePosition()
        {
            return ComputePosition(time_passed, point_Idx);
        }
        public Vector4 ComputePosition(int time_offset)
        {
            int time_point = time_passed + time_offset;
            if (time_point >= Duration())
                return ComputePosition(Duration(), spline.Last() - 1);
            if (time_point <= 0)
                return ComputePosition(0, spline.First());

            // find point_index where spline.length(point_index) < time_point < spline.length(point_index + 1)
            int point_index = point_Idx;
            while (time_point >= spline.Length(point_index + 1))
                ++point_index;

            while (time_point < spline.Length(point_index))
                --point_index;

            return ComputePosition(time_point, point_index);
        }
        public void ComputeParabolicElevation(int time_point, ref float el)
        {
            if (time_point > effect_start_time)
            {
                float t_passedf = MSToSec((uint)(time_point - effect_start_time));
                float t_durationf = MSToSec((uint)(Duration() - effect_start_time)); //client use not modified duration here
                if (spell_effect_extra.HasValue && spell_effect_extra.Value.ParabolicCurveId != 0)
                    t_passedf *= Global.DB2Mgr.GetCurveValueAt(spell_effect_extra.Value.ParabolicCurveId, (float)time_point / Duration());

                el += (t_durationf - t_passedf) * 0.5f * vertical_acceleration * t_passedf;
            }
        }
        public void ComputeFallElevation(int time_point, ref float el)
        {
            float z_now = spline.GetPoint(spline.First()).Z - ComputeFallElevation(MSToSec((uint)time_point), false);
            float final_z = FinalDestination().Z;
            el = Math.Max(z_now, final_z);
        }
        public static float ComputeFallElevation(float t_passed, bool isSafeFall, float start_velocity = 0.0f)
        {
            float termVel;
            float result;

            if (isSafeFall)
                termVel = SharedConst.terminalSafefallVelocity;
            else
                termVel = SharedConst.terminalVelocity;

            if (start_velocity > termVel)
                start_velocity = termVel;

            float terminal_time = (float)((isSafeFall ? SharedConst.terminal_safeFall_fallTime : SharedConst.terminal_fallTime) - start_velocity / SharedConst.gravity); // the time that needed to reach terminalVelocity

            if (t_passed > terminal_time)
            {
                result = termVel * (t_passed - terminal_time) +
                    start_velocity * terminal_time +
                    (float)SharedConst.gravity * terminal_time * terminal_time * 0.5f;
            }
            else
                result = t_passed * (float)(start_velocity + t_passed * SharedConst.gravity * 0.5f);

            return result;
        }

        float MSToSec(uint ms)
        {
            return ms / 1000.0f;
        }

        public void Interrupt() { splineflags.SetUnsetFlag(SplineFlag.Done); }
        public void UpdateState(int difftime)
        {
            do
            {
                UpdateState(ref difftime);
            } while (difftime > 0);
        }
        UpdateResult UpdateState(ref int ms_time_diff)
        {
            if (Finalized())
            {
                ms_time_diff = 0;
                return UpdateResult.Arrived;
            }

            UpdateResult result = UpdateResult.None;
            int minimal_diff = Math.Min(ms_time_diff, SegmentTimeElapsed());
            time_passed += minimal_diff;
            ms_time_diff -= minimal_diff;

            if (time_passed >= NextTimestamp())
            {
                ++point_Idx;
                if (point_Idx < spline.Last())
                {
                    result = UpdateResult.NextSegment;
                }
                else
                {
                    if (spline.IsCyclic())
                    {
                        point_Idx = spline.First();
                        time_passed = time_passed % Duration();
                        result = UpdateResult.NextCycle;
                    }
                    else
                    {
                        _Finalize();
                        ms_time_diff = 0;
                        result = UpdateResult.Arrived;
                    }
                }
            }

            return result;
        }
        int NextTimestamp() { return spline.Length(point_Idx + 1); }
        int SegmentTimeElapsed() { return NextTimestamp() - time_passed; }
        public bool IsCyclic() { return splineflags.HasFlag(SplineFlag.Cyclic); }
        public bool IsFalling() { return splineflags.HasFlag(SplineFlag.Falling); }
        public bool Initialized() { return !spline.Empty(); }
        public Vector3 FinalDestination() { return Initialized() ? spline.GetPoint(spline.Last()) : new Vector3(); }

        #region Fields
        public MoveSplineInitArgs InitArgs;
        public Spline spline = new Spline();
        public FacingInfo facing;
        public MoveSplineFlag splineflags = new MoveSplineFlag();
        public bool onTransport;
        public bool splineIsFacingOnly;
        public uint m_Id;
        public int time_passed;
        public float vertical_acceleration;
        public float initialOrientation;
        public int effect_start_time;
        public int point_Idx;
        public int point_Idx_offset;
        public Optional<SpellEffectExtraData> spell_effect_extra;
        #endregion

        public class CommonInitializer : IInitializer
        {
            public CommonInitializer(float _velocity)
            {
                velocityInv = 1000f / _velocity;
                time = 1;
            }
            public float velocityInv;
            public int time;
            public int SetGetTime(Spline s, int i)
            {
                time += (int)(s.SegLength(i) * velocityInv);
                return time;
            }
        }
        public class FallInitializer : IInitializer
        {
            public FallInitializer(float startelevation)
            {
                startElevation = startelevation;
            }
            float startElevation;
            public int SetGetTime(Spline s, int i)
            {
                return (int)(ComputeFallTime(startElevation - s.GetPoint(i + 1).Z, false) * 1000.0f);
            }

            float ComputeFallTime(float path_length, bool isSafeFall)
            {
                if (path_length < 0.0f)
                    return 0.0f;

                float time;
                if (isSafeFall)
                {
                    if (path_length >= SharedConst.terminal_safeFall_length)
                        time = (path_length - SharedConst.terminal_safeFall_length) / SharedConst.terminalSafefallVelocity + SharedConst.terminal_safeFall_fallTime;
                    else
                        time = (float)Math.Sqrt(2.0f * path_length / SharedConst.gravity);
                }
                else
                {
                    if (path_length >= SharedConst.terminal_length)
                        time = (path_length - SharedConst.terminal_length) / SharedConst.terminalVelocity + SharedConst.terminal_fallTime;
                    else
                        time = (float)Math.Sqrt(2.0f * path_length / SharedConst.gravity);
                }

                return time;
            }
        }
        public enum UpdateResult
        {
            None = 0x01,
            Arrived = 0x02,
            NextCycle = 0x04,
            NextSegment = 0x08
        }
    }
    public interface IInitializer
    {
        int SetGetTime(Spline s, int i);
    }

    public class SplineChainLink
    {
        public SplineChainLink(Vector3[] points, uint expectedDuration, uint msToNext)
        {
            Points.AddRange(points);
            ExpectedDuration = expectedDuration;
            TimeToNext = msToNext;
        }

        public SplineChainLink(uint expectedDuration, uint msToNext)
        {
            ExpectedDuration = expectedDuration;
            TimeToNext = msToNext;
        }

        public List<Vector3> Points = new List<Vector3>();
        public uint ExpectedDuration;
        public uint TimeToNext;
    }

    public class SplineChainResumeInfo
    {
        public SplineChainResumeInfo() { }
        public SplineChainResumeInfo(uint id, List<SplineChainLink> chain, bool walk, byte splineIndex, byte wpIndex, uint msToNext)
        {
            PointID = id;
            Chain = chain;
            IsWalkMode = walk;
            SplineIndex = splineIndex;
            PointIndex = wpIndex;
            TimeToNext = msToNext;
        }

        public bool Empty() { return Chain.Empty(); }
        public void Clear() { Chain.Clear(); }

        public uint PointID;
        public List<SplineChainLink> Chain = new List<SplineChainLink>();
        public bool IsWalkMode;
        public byte SplineIndex;
        public byte PointIndex;
        public uint TimeToNext;
    }
}
