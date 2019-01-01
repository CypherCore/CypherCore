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
using Framework.GameMath;
using Game.Entities;
using Game.Maps;
using System;
using System.Linq;

namespace Game.Movement
{
    public class Spline
    {
        public int getPointCount() { return points.Length; }
        public Vector3 getPoint(int i) { return points[i]; }
        public Vector3[] getPoints() { return points; }

        public void clear()
        {
           Array.Clear(points, 0, points.Length);
        }
        public int first() { return index_lo; }
        public int last() { return index_hi; }

        public bool isCyclic() { return _cyclic;}
        
        #region Evaluate
        public void Evaluate_Percent(int Idx, float u, out Vector3 c) 
        {            
            switch (m_mode)
            {
                case EvaluationMode.Linear:
                    EvaluateLinear(Idx, u, out c);
                    break;
                case EvaluationMode.Catmullrom:
                    EvaluateCatmullRom(Idx, u, out c);
                    break;
                case EvaluationMode.Bezier3_Unused:
                    EvaluateBezier3(Idx, u, out c);
                    break;
                default:
                    c = new Vector3();
                    break;
            }
        }
        void EvaluateLinear(int index, float u, out Vector3 result)
        {
            result = points[index] + (points[index + 1] - points[index]) * u;
        }
        void EvaluateCatmullRom(int index, float t, out Vector3 result)
        {
            Span<Vector3> span = points;
            C_Evaluate(span.Slice(index - 1), t, s_catmullRomCoeffs, out result);
        }
        void EvaluateBezier3(int index, float t, out Vector3 result)
        {
            index *= (int)3u;
            Span<Vector3> span = points;
            C_Evaluate(span.Slice(index), t, s_Bezier3Coeffs, out result);
        }
        #endregion

        #region Init
        public void init_spline_custom(SplineRawInitializer initializer)
        {
            initializer.Initialize(ref m_mode, ref _cyclic, ref points, ref index_lo, ref index_hi);
        }
        public void init_cyclic_spline(Vector3[] controls, int count, EvaluationMode m, int cyclic_point)
        {
            m_mode = m;
            _cyclic = true;

            Init_Spline(controls, count, m);
        }
        public void Init_Spline(Span<Vector3> controls, int count, EvaluationMode m)
        {
            m_mode = m;
            _cyclic = false;

            switch (m_mode)
            {
                case EvaluationMode.Linear:
                case EvaluationMode.Catmullrom:
                    InitCatmullRom(controls, count, _cyclic, 0);
                    break;
                case EvaluationMode.Bezier3_Unused:
                    InitBezier3(controls, count, _cyclic, 0);
                    break;
                default:
                    break;
            }
        }
        void InitLinear(Vector3[] controls, int count, bool cyclic, int cyclic_point)
        {
            int real_size = count + 1;

            Array.Resize(ref points, real_size);
            Array.Copy(controls, points, count);

            // first and last two indexes are space for special 'virtual points'
            // these points are required for proper C_Evaluate and C_Evaluate_Derivative methtod work
            if (cyclic)
                points[count] = controls[cyclic_point];
            else
                points[count] = controls[count - 1];

            index_lo = 0;
            index_hi = cyclic ? count : (count - 1);
        }
        void InitCatmullRom(Span<Vector3> controls, int count, bool cyclic, int cyclic_point)
        {
            int real_size = count + (cyclic ? (1 + 2) : (1 + 1));

            points = new Vector3[real_size];

            int lo_index = 1;
            int high_index = lo_index + count - 1;

            Array.Copy(controls.ToArray(), 0, points, lo_index, count);

            // first and last two indexes are space for special 'virtual points'
            // these points are required for proper C_Evaluate and C_Evaluate_Derivative methtod work
            if (cyclic)
            {
                if (cyclic_point == 0)
                    points[0] = controls[count - 1];
                else
                    points[0] = controls[0].lerp(controls[1], -1);

                points[high_index + 1] = controls[cyclic_point];
                points[high_index + 2] = controls[cyclic_point + 1];
            }
            else
            {
                points[0] = controls[0].lerp(controls[1], -1);
                points[high_index + 1] = controls[count - 1];
            }

            index_lo = lo_index;
            index_hi = high_index + (cyclic ? 1 : 0);
        }
        void InitBezier3(Span<Vector3> controls, int count, bool cyclic, int cyclic_point)
        {
            int c = (int)(count / 3u * 3u);
            int t = (int)(c / 3u);

            Array.Resize(ref points, c);
            Array.Copy(controls.ToArray(), points, c);

            index_lo = 0;
            index_hi = t - 1;
        }
        #endregion

        #region EvaluateDerivative
        public void Evaluate_Derivative(int Idx, float u, out Vector3 hermite)
        {
            switch (m_mode)
            {
                case EvaluationMode.Linear:
                    EvaluateDerivativeLinear(Idx, u, out hermite);
                    break;
                case EvaluationMode.Catmullrom:
                    EvaluateDerivativeCatmullRom(Idx, u, out hermite);
                    break;
                case EvaluationMode.Bezier3_Unused:
                    EvaluateDerivativeBezier3(Idx, u, out hermite);
                    break;
                default:
                    hermite = new Vector3();
                    break;
            }
        }
        void EvaluateDerivativeLinear(int index, float t, out Vector3 result)
        {
            result = points[index + 1] - points[index];
        }
        void EvaluateDerivativeCatmullRom(int index, float t, out Vector3 result)
        {
            Span<Vector3> span = points;
            C_Evaluate_Derivative(span.Slice(index - 1), t, s_catmullRomCoeffs, out result);
        }
        void EvaluateDerivativeBezier3(int index, float t, out Vector3 result)
        {
            index *= (int)3u;
            Span<Vector3> span = points;
            C_Evaluate_Derivative(span.Slice(index), t, s_Bezier3Coeffs, out result);
        }
        #endregion
        
        #region SegLength
        public float SegLength(int i)
        {
            switch (m_mode)
            {
                case EvaluationMode.Linear:
                    return SegLengthLinear(i);
                case EvaluationMode.Catmullrom:
                    return SegLengthCatmullRom(i);
                case EvaluationMode.Bezier3_Unused:
                    return SegLengthBezier3(i);
                default:
                    return 0;
            }
        }
        float SegLengthLinear(int index)
        {
            return (points[index] - points[index + 1]).GetLength();
        }
        float SegLengthCatmullRom(int index)
        {
            Vector3 nextPos;
            Span<Vector3> p = points.AsSpan(index - 1);
            Vector3 curPos = nextPos = p[1];

            int i = 1;
            double length = 0;
            while (i <= 3)
            {
                C_Evaluate(p, i / (float)3, s_catmullRomCoeffs, out nextPos);
                length += (nextPos - curPos).GetLength();
                curPos = nextPos;
                ++i;
            }
            return (float)length;
        }
        float SegLengthBezier3(int index)
        {
            index *= (int)3u;

            Vector3 nextPos;
            Span<Vector3> p = points.AsSpan(index);

            C_Evaluate(p, 0.0f, s_Bezier3Coeffs, out nextPos);
            Vector3 curPos = nextPos;

            int i = 1;
            double length = 0;
            while (i <= 3)
            {
                C_Evaluate(p, i / (float)3, s_Bezier3Coeffs, out nextPos);
                length += (nextPos - curPos).GetLength();
                curPos = nextPos;
                ++i;
            }
            return (float)length;
        }
        #endregion

        public void computeIndex(float t, ref int index, ref float u)
        {
            //ASSERT(t >= 0.f && t <= 1.f);
            int length_ = (int)(t * length());
            index = computeIndexInBounds(length_);
            //ASSERT(index < index_hi);
            u = (length_ - length(index)) / (float)length(index, index + 1);
        }

        int computeIndexInBounds(int length_)
        {
            // Temporary disabled: causes infinite loop with t = 1.f
            /*
                index_type hi = index_hi;
                index_type lo = index_lo;

                index_type i = lo + (float)(hi - lo) * t;

                while ((lengths[i] > length) || (lengths[i + 1] <= length))
                {
                    if (lengths[i] > length)
                        hi = i - 1; // too big
                    else if (lengths[i + 1] <= length)
                        lo = i + 1; // too small

                    i = (hi + lo) / 2;
                }*/

            int i = index_lo;
            int N = index_hi;
            while (i + 1 < N && lengths[i + 1] < length_)
                ++i;

            return i;
        }
        private static readonly Matrix4 s_catmullRomCoeffs = new Matrix4(-0.5f, 1.5f, -1.5f, 0.5f, 1.0f, -2.5f, 2.0f, -0.5f, -0.5f, 0.0f, 0.5f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f);

        private static readonly Matrix4 s_Bezier3Coeffs = new Matrix4(-1.0f, 3.0f, -3.0f, 1.0f, 3.0f, -6.0f, 3.0f, 0.0f, -3.0f, 3.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f);

        void C_Evaluate(Span<Vector3> vertice, float t, Matrix4 matr, out Vector3 result)
        {
            Vector4 tvec = new Vector4(t * t * t, t * t, t, 1.0f);
            Vector4 weights = (tvec * matr);

            result = vertice[0] * weights[0] + vertice[1] * weights[1]
                   + vertice[2] * weights[2] + vertice[3] * weights[3];
        }
        void C_Evaluate_Derivative(Span<Vector3> vertice, float t, Matrix4 matr, out Vector3 result)
        {
            Vector4 tvec = new Vector4(3.0f * t * t, 2.0f * t, 1.0f, 0.0f);
            Vector4 weights = (tvec * matr);

            result = vertice[0] * weights[0] + vertice[1] * weights[1]
                   + vertice[2] * weights[2] + vertice[3] * weights[3];
        }

        public int length() { return lengths[index_hi];}

        public int length(int first, int last) { return lengths[last] - lengths[first]; }

        public int length(int Idx) { return lengths[Idx]; }

        public void set_length(int i, int length) { lengths[i] = length; }

        public void initLengths(Initializer cacher)
        {
            int i = index_lo;
            Array.Resize(ref lengths, index_hi+1);
            int prev_length = 0, new_length = 0;
            while (i < index_hi)
            {
                new_length = cacher.SetGetTime(this, i);
                if (new_length < 0)
                    new_length = int.MaxValue;
                lengths[++i] = new_length;

                prev_length = new_length;
            }
        }

        public void initLengths()
        {
            int i = index_lo;
            int length = 0;
            Array.Resize(ref lengths, index_hi + 1);
            while (i < index_hi)
            {
                length += (int)SegLength(i);
                lengths[++i] = length;
            }
        }

        public bool empty() { return index_lo == index_hi;}

        int[] lengths = new int[0];
        Vector3[] points = new Vector3[0];
        public EvaluationMode m_mode;
        bool _cyclic;
        int index_lo;
        int index_hi;
        public enum EvaluationMode
        {
            Linear,
            Catmullrom,
            Bezier3_Unused,
            UninitializedMode,
            ModesEnd
        }
    }

    public class FacingInfo
    {
        public Vector3 f;
        public ObjectGuid target;
        public float angle;
        public MonsterMoveType type;
    }
}
