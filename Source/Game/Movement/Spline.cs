// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Numerics;
using Game.Maps;

namespace Game.Movement
{
    public class Spline<T>
    {
        public EvaluationMode _mode;
        private static readonly Matrix4x4 _catmullRomCoeffs = new(-0.5f, 1.5f, -1.5f, 0.5f, 1.0f, -2.5f, 2.0f, -0.5f, -0.5f, 0.0f, 0.5f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f);

        private static readonly Matrix4x4 _Bezier3Coeffs = new(-1.0f, 3.0f, -3.0f, 1.0f, 3.0f, -6.0f, 3.0f, 0.0f, -3.0f, 3.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f);
        private bool _cyclic;
        private int _index_hi;

        private int _index_lo;
        private float _initialOrientation;

        private T[] _lengths = Array.Empty<T>();
        private Vector3[] _points = Array.Empty<Vector3>();

        // could be modified, affects segment length evaluation precision
        // lesser value saves more performance in cost of lover precision
        // minimal value is 1
        // client's value is 20, blizzs use 2-3 steps to compute length
        private int _stepsPerSegment = 3;

        public int GetPointCount()
        {
            return _points.Length;
        }

        public Vector3 GetPoint(int i)
        {
            return _points[i];
        }

        public Vector3[] GetPoints()
        {
            return _points;
        }

        public void Clear()
        {
            Array.Clear(_points, 0, _points.Length);
        }

        public int First()
        {
            return _index_lo;
        }

        public int Last()
        {
            return _index_hi;
        }

        public bool IsCyclic()
        {
            return _cyclic;
        }

        public void set_steps_per_segment(int newStepsPerSegment)
        {
            _stepsPerSegment = newStepsPerSegment;
        }

        public void ComputeIndex(float t, ref int index, ref float u)
        {
            //ASSERT(t >= 0.f && t <= 1.f);
            T length_ = t * (dynamic)Length();
            index = ComputeIndexInBounds(length_);
            //ASSERT(index < index_hi);
            u = (float)(length_ - Length(index)) / (float)Length(index, index + 1);
        }

        public dynamic Length()
        {
            if (_lengths.Length == 0)
                return default;

            return _lengths[_index_hi];
        }

        public dynamic Length(int first, int last)
        {
            return _lengths[last] - (dynamic)_lengths[first];
        }

        public dynamic Length(int Idx)
        {
            return _lengths[Idx];
        }

        public void Set_length(int i, T length)
        {
            _lengths[i] = length;
        }

        public void InitLengths(IInitializer<T> cacher)
        {
            int i = _index_lo;
            Array.Resize(ref _lengths, _index_hi + 1);
            T prev_length;
            T new_length;

            while (i < _index_hi)
            {
                new_length = (dynamic)cacher.Invoke(this, i);

                if ((dynamic)new_length < 0) // todo fix me this is a ulgy hack.
                    new_length = (dynamic)(Type.GetTypeCode(typeof(T)) == TypeCode.Int32 ? int.MaxValue : double.MaxValue);

                _lengths[++i] = new_length;

                prev_length = new_length;
            }
        }

        public void InitLengths()
        {
            int i = _index_lo;
            dynamic length = default(T);
            Array.Resize(ref _lengths, _index_hi + 1);

            while (i < _index_hi)
            {
                length += SegLength(i);
                _lengths[++i] = length;
            }
        }

        public bool Empty()
        {
            return _index_lo == _index_hi;
        }

        private int ComputeIndexInBounds(T length_)
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

            int i = _index_lo;
            int N = _index_hi;

            while (i + 1 < N && (dynamic)_lengths[i + 1] < length_)
                ++i;

            return i;
        }

        private void C_Evaluate(Span<Vector3> vertice, float t, Matrix4x4 matr, out Vector3 result)
        {
            Vector4 tvec = new(t * t * t, t * t, t, 1.0f);
            Vector4 weights = Vector4.Transform(tvec, matr);

            result = vertice[0] * weights.X + vertice[1] * weights.Y + vertice[2] * weights.Z + vertice[3] * weights.W;
        }

        private void C_Evaluate_Derivative(Span<Vector3> vertice, float t, Matrix4x4 matr, out Vector3 result)
        {
            Vector4 tvec = new(3.0f * t * t, 2.0f * t, 1.0f, 0.0f);
            Vector4 weights = Vector4.Transform(tvec, matr);

            result = vertice[0] * weights.X + vertice[1] * weights.Y + vertice[2] * weights.Z + vertice[3] * weights.W;
        }

        #region Evaluate

        public void Evaluate_Percent(int Idx, float u, out Vector3 c)
        {
            switch (_mode)
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

        private void EvaluateLinear(int index, float u, out Vector3 result)
        {
            result = _points[index] + (_points[index + 1] - _points[index]) * u;
        }

        private void EvaluateCatmullRom(int index, float t, out Vector3 result)
        {
            Span<Vector3> span = _points;
            C_Evaluate(span[(index - 1)..], t, _catmullRomCoeffs, out result);
        }

        private void EvaluateBezier3(int index, float t, out Vector3 result)
        {
            index *= (int)3u;
            Span<Vector3> span = _points;
            C_Evaluate(span[index..], t, _Bezier3Coeffs, out result);
        }

        #endregion

        #region Init

        public void InitSplineCustom(SplineRawInitializer initializer)
        {
            initializer.Initialize(ref _mode, ref _cyclic, ref _points, ref _index_lo, ref _index_hi);
        }

        public void InitCyclicSpline(Vector3[] controls, int count, EvaluationMode m, int cyclic_point, float orientation = 0f)
        {
            _mode = m;
            _cyclic = true;

            InitSpline(controls, count, m, orientation);
        }

        public void InitSpline(Span<Vector3> controls, int count, EvaluationMode m, float orientation = 0f)
        {
            _mode = m;
            _cyclic = false;
            _initialOrientation = orientation;

            switch (_mode)
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

        private void InitLinear(Vector3[] controls, int count, bool cyclic, int cyclic_point)
        {
            int real_size = count + 1;

            Array.Resize(ref _points, real_size);
            Array.Copy(controls, _points, count);

            // first and last two indexes are space for special 'virtual points'
            // these points are required for proper C_Evaluate and C_Evaluate_Derivative methtod work
            if (cyclic)
                _points[count] = controls[cyclic_point];
            else
                _points[count] = controls[count - 1];

            _index_lo = 0;
            _index_hi = cyclic ? count : (count - 1);
        }

        private void InitCatmullRom(Span<Vector3> controls, int count, bool cyclic, int cyclic_point)
        {
            int real_size = count + (cyclic ? (1 + 2) : (1 + 1));

            _points = new Vector3[real_size];

            int lo_index = 1;
            int high_index = lo_index + count - 1;

            Array.Copy(controls.ToArray(), 0, _points, lo_index, count);

            // first and last two indexes are space for special 'virtual points'
            // these points are required for proper C_Evaluate and C_Evaluate_Derivative methtod work
            if (cyclic)
            {
                if (cyclic_point == 0)
                    _points[0] = controls[count - 1];
                else
                    _points[0] = controls[0] - new Vector3(MathF.Cos(_initialOrientation), MathF.Sin(_initialOrientation), 0.0f);

                _points[high_index + 1] = controls[cyclic_point];
                _points[high_index + 2] = controls[cyclic_point + 1];
            }
            else
            {
                _points[0] = controls[0] - new Vector3(MathF.Cos(_initialOrientation), MathF.Sin(_initialOrientation), 0.0f);
                _points[high_index + 1] = controls[count - 1];
            }

            _index_lo = lo_index;
            _index_hi = high_index + (cyclic ? 1 : 0);
        }

        private void InitBezier3(Span<Vector3> controls, int count, bool cyclic, int cyclic_point)
        {
            int c = (int)(count / 3u * 3u);
            int t = (int)(c / 3u);

            Array.Resize(ref _points, c);
            Array.Copy(controls.ToArray(), _points, c);

            _index_lo = 0;
            _index_hi = t - 1;
        }

        #endregion

        #region EvaluateDerivative

        public void Evaluate_Derivative(int Idx, float u, out Vector3 hermite)
        {
            switch (_mode)
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

        private void EvaluateDerivativeLinear(int index, float t, out Vector3 result)
        {
            result = _points[index + 1] - _points[index];
        }

        private void EvaluateDerivativeCatmullRom(int index, float t, out Vector3 result)
        {
            Span<Vector3> span = _points;
            C_Evaluate_Derivative(span[(index - 1)..], t, _catmullRomCoeffs, out result);
        }

        private void EvaluateDerivativeBezier3(int index, float t, out Vector3 result)
        {
            index *= (int)3u;
            Span<Vector3> span = _points;
            C_Evaluate_Derivative(span[index..], t, _Bezier3Coeffs, out result);
        }

        #endregion

        #region SegLength

        public float SegLength(int i)
        {
            switch (_mode)
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

        private float SegLengthLinear(int index)
        {
            return (_points[index] - _points[index + 1]).Length();
        }

        private float SegLengthCatmullRom(int index)
        {
            Vector3 nextPos;
            Span<Vector3> p = _points.AsSpan(index - 1);
            Vector3 curPos = p[1];

            int i = 1;
            double length = 0;

            while (i <= _stepsPerSegment)
            {
                C_Evaluate(p, i / (float)_stepsPerSegment, _catmullRomCoeffs, out nextPos);
                length += (nextPos - curPos).Length();
                curPos = nextPos;
                ++i;
            }

            return (float)length;
        }

        private float SegLengthBezier3(int index)
        {
            index *= (int)3u;

            Vector3 nextPos;
            Span<Vector3> p = _points.AsSpan(index);

            C_Evaluate(p, 0.0f, _Bezier3Coeffs, out nextPos);
            Vector3 curPos = nextPos;

            int i = 1;
            double length = 0;

            while (i <= _stepsPerSegment)
            {
                C_Evaluate(p, i / (float)_stepsPerSegment, _Bezier3Coeffs, out nextPos);
                length += (nextPos - curPos).Length();
                curPos = nextPos;
                ++i;
            }

            return (float)length;
        }

        #endregion
    }
}