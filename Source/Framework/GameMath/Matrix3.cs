/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * Copyright (C) 2003-2004  Eran Kampf	eran@ekampf.com	http://www.ekampf.com
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Framework.GameMath
{
    /// <summary>
    /// Represents a 3-dimentional single-precision floating point matrix.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public struct Matrix3 : ICloneable
    {
        #region Private Fields
        private float _m11, _m12, _m13;
        private float _m21, _m22, _m23;
        private float _m31, _m32, _m33;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix3"/> structure with the specified values.
        /// </summary>
        public Matrix3(float m11, float m12, float m13, float m21, float m22, float m23, float m31, float m32, float m33)
        {
            _m11 = m11; _m12 = m12; _m13 = m13;
            _m21 = m21; _m22 = m22; _m23 = m23;
            _m31 = m31; _m32 = m32; _m33 = m33;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix3"/> structure with the specified values.
        /// </summary>
        /// <param name="elements">An array containing the matrix values in row-major order.</param>
        public Matrix3(float[] elements)
        {
            Debug.Assert(elements != null);
            Debug.Assert(elements.Length >= 9);

            _m11 = elements[0]; _m12 = elements[1]; _m13 = elements[2];
            _m21 = elements[3]; _m22 = elements[4]; _m23 = elements[5];
            _m31 = elements[6]; _m32 = elements[7]; _m33 = elements[8];
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix4"/> structure with the specified values.
        /// </summary>
        /// <param name="elements">An array containing the matrix values in row-major order.</param>
        public Matrix3(List<float> elements)
        {
            Debug.Assert(elements != null);
            Debug.Assert(elements.Count >= 9);

            _m11 = elements[0]; _m12 = elements[1]; _m13 = elements[2];
            _m21 = elements[3]; _m22 = elements[4]; _m23 = elements[5];
            _m31 = elements[6]; _m32 = elements[7]; _m33 = elements[8];
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix3"/> structure with the specified values.
        /// </summary>
        /// <param name="column1">A <see cref="Vector3"/> instance holding values for the first column.</param>
        /// <param name="column2">A <see cref="Vector3"/> instance holding values for the second column.</param>
        /// <param name="column3">A <see cref="Vector3"/> instance holding values for the third column.</param>
        public Matrix3(Vector3 column1, Vector3 column2, Vector3 column3)
        {
            _m11 = column1.X; _m12 = column2.X; _m13 = column3.X;
            _m21 = column1.Y; _m22 = column2.Y; _m23 = column3.Y;
            _m31 = column1.Z; _m32 = column2.Z; _m33 = column3.Z;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix3"/> class using a given matrix.
        /// </summary>
        public Matrix3(Matrix3 m)
        {
            _m11 = m.M11; _m12 = m.M12; _m13 = m.M13;
            _m21 = m.M21; _m22 = m.M22; _m23 = m.M23;
            _m31 = m.M31; _m32 = m.M32; _m33 = m.M33;
        }
        #endregion

        #region Constants
        /// <summary>
        /// 4-dimentional single-precision floating point zero matrix.
        /// </summary>
        public static readonly Matrix3 Zero = new Matrix3(0, 0, 0, 0, 0, 0, 0, 0, 0);
        /// <summary>
        /// 4-dimentional single-precision floating point identity matrix.
        /// </summary>
        public static readonly Matrix3 Identity = new Matrix3(1, 0, 0, 0, 1, 0, 0, 0, 1);
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets or sets the value of the [1,1] matrix element.
        /// </summary>
        /// <value>A single-precision floating-point number.</value>
        public float M11
        {
            get { return _m11; }
            set { _m11 = value; }
        }
        /// <summary>
        /// Gets or sets the value of the [1,2] matrix element.
        /// </summary>
        /// <value>A single-precision floating-point number.</value>
        public float M12
        {
            get { return _m12; }
            set { _m12 = value; }
        }
        /// <summary>
        /// Gets or sets the value of the [1,3] matrix element.
        /// </summary>
        /// <value>A single-precision floating-point number.</value>
        public float M13
        {
            get { return _m13; }
            set { _m13 = value; }
        }


        /// <summary>
        /// Gets or sets the value of the [2,1] matrix element.
        /// </summary>
        /// <value>A single-precision floating-point number.</value>
        public float M21
        {
            get { return _m21; }
            set { _m21 = value; }
        }
        /// <summary>
        /// Gets or sets the value of the [2,2] matrix element.
        /// </summary>
        /// <value>A single-precision floating-point number.</value>
        public float M22
        {
            get { return _m22; }
            set { _m22 = value; }
        }
        /// <summary>
        /// Gets or sets the value of the [2,3] matrix element.
        /// </summary>
        /// <value>A single-precision floating-point number.</value>
        public float M23
        {
            get { return _m23; }
            set { _m23 = value; }
        }


        /// <summary>
        /// Gets or sets the value of the [3,1] matrix element.
        /// </summary>
        /// <value>A single-precision floating-point number.</value>
        public float M31
        {
            get { return _m31; }
            set { _m31 = value; }
        }
        /// <summary>
        /// Gets or sets the value of the [3,2] matrix element.
        /// </summary>
        /// <value>A single-precision floating-point number.</value>
        public float M32
        {
            get { return _m32; }
            set { _m32 = value; }
        }
        /// <summary>
        /// Gets or sets the value of the [3,3] matrix element.
        /// </summary>
        /// <value>A single-precision floating-point number.</value>
        public float M33
        {
            get { return _m33; }
            set { _m33 = value; }
        }

        /// <summary>
        /// Gets the matrix's trace value.
        /// </summary>
        /// <value>A single-precision floating-point number.</value>
        public float Trace
        {
            get
            {
                return _m11 + _m22 + _m33;
            }
        }
        #endregion

        #region ICloneable Members
        /// <summary>
        /// Creates an exact copy of this <see cref="Matrix3"/> object.
        /// </summary>
        /// <returns>The <see cref="Matrix3"/> object this method creates, cast as an object.</returns>
        object ICloneable.Clone()
        {
            return new Matrix3(this);
        }
        /// <summary>
        /// Creates an exact copy of this <see cref="Matrix3"/> object.
        /// </summary>
        /// <returns>The <see cref="Matrix3"/> object this method creates.</returns>
        public Matrix3 Clone()
        {
            return new Matrix3(this);
        }
        #endregion

        #region Public Static Parse Methods
        private const string regularExp = @"3x3\s*\[(?<m11>.*),(?<m12>.*),(?<m13>.*),(?<m21>.*),(?<m22>.*),(?<m23>.*),(?<m31>.*),(?<m32>.*),(?<m33>.*)\]";
        /// <summary>
        /// Converts the specified string to its <see cref="Matrix3"/> equivalent.
        /// </summary>
        /// <param name="value">A string representation of a <see cref="Matrix3"/>.</param>
        /// <returns>A <see cref="Matrix3"/> that represents the vector specified by the <paramref name="value"/> parameter.</returns>
        /// <remarks>
        /// The string should be in the following form: "3x3..matrix elements..>".<br/>
        /// Exmaple : "3x3[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16]"
        /// </remarks>
        public static Matrix3 Parse(string value)
        {
            Regex r = new Regex(regularExp, RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
            Match m = r.Match(value);
            if (m.Success)
            {
                return new Matrix3(
                    float.Parse(m.Result("${m11}")),
                    float.Parse(m.Result("${m12}")),
                    float.Parse(m.Result("${m13}")),

                    float.Parse(m.Result("${m21}")),
                    float.Parse(m.Result("${m22}")),
                    float.Parse(m.Result("${m23}")),

                    float.Parse(m.Result("${m31}")),
                    float.Parse(m.Result("${m32}")),
                    float.Parse(m.Result("${m33}"))
                    );
            }
            else
            {
                throw new Exception("Unsuccessful Match.");
            }
        }
        /// <summary>
        /// Converts the specified string to its <see cref="Matrix3"/> equivalent.
        /// A return value indicates whether the conversion succeeded or failed.
        /// </summary>
        /// <param name="value">A string representation of a <see cref="Matrix3"/>.</param>
        /// <param name="result">
        /// When this method returns, if the conversion succeeded,
        /// contains a <see cref="Matrix3"/> representing the vector specified by <paramref name="value"/>.
        /// </param>
        /// <returns><see langword="true"/> if value was converted successfully; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// The string should be in the following form: "3x3..matrix elements..>".<br/>
        /// Exmaple : "3x3[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16]"
        /// </remarks>
        public static bool TryParse(string value, out Matrix3 result)
        {
            Regex r = new Regex(regularExp, RegexOptions.Singleline);
            Match m = r.Match(value);
            if (m.Success)
            {
                result = new Matrix3(
                    float.Parse(m.Result("${m11}")),
                    float.Parse(m.Result("${m12}")),
                    float.Parse(m.Result("${m13}")),

                    float.Parse(m.Result("${m21}")),
                    float.Parse(m.Result("${m22}")),
                    float.Parse(m.Result("${m23}")),

                    float.Parse(m.Result("${m31}")),
                    float.Parse(m.Result("${m32}")),
                    float.Parse(m.Result("${m33}"))
                    );

                return true;
            }

            result = Matrix3.Zero;
            return false;
        }
        #endregion

        #region Public Static Matrix Arithmetics
        /// <summary>
        /// Adds two matrices.
        /// </summary>
        /// <param name="left">A <see cref="Matrix3"/> instance.</param>
        /// <param name="right">A <see cref="Matrix3"/> instance.</param>
        /// <returns>A new <see cref="Matrix3"/> instance containing the sum.</returns>
        public static Matrix3 Add(Matrix3 left, Matrix3 right)
        {
            return new Matrix3(
                left.M11 + right.M11, left.M12 + right.M12, left.M13 + right.M13,
                left.M21 + right.M21, left.M22 + right.M22, left.M23 + right.M23,
                left.M31 + right.M31, left.M32 + right.M32, left.M33 + right.M33
                );
        }
        /// <summary>
        /// Adds a matrix and a scalar.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix3"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <returns>A new <see cref="Matrix3"/> instance containing the sum.</returns>
        public static Matrix3 Add(Matrix3 matrix, float scalar)
        {
            return new Matrix3(
                matrix.M11 + scalar, matrix.M12 + scalar, matrix.M13 + scalar,
                matrix.M21 + scalar, matrix.M22 + scalar, matrix.M23 + scalar,
                matrix.M31 + scalar, matrix.M32 + scalar, matrix.M33 + scalar
                );
        }
        /// <summary>
        /// Adds two matrices and put the result in a third matrix.
        /// </summary>
        /// <param name="left">A <see cref="Matrix3"/> instance.</param>
        /// <param name="right">A <see cref="Matrix3"/> instance.</param>
        /// <param name="result">A <see cref="Matrix3"/> instance to hold the result.</param>
        public static void Add(Matrix3 left, Matrix3 right, ref Matrix3 result)
        {
            result.M11 = left.M11 + right.M11;
            result.M12 = left.M12 + right.M12;
            result.M13 = left.M13 + right.M13;

            result.M21 = left.M21 + right.M21;
            result.M22 = left.M22 + right.M22;
            result.M23 = left.M23 + right.M23;

            result.M31 = left.M31 + right.M31;
            result.M32 = left.M32 + right.M32;
            result.M33 = left.M33 + right.M33;
        }
        /// <summary>
        /// Adds a matrix and a scalar and put the result in a third matrix.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix3"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <param name="result">A <see cref="Matrix3"/> instance to hold the result.</param>
        public static void Add(Matrix3 matrix, float scalar, ref Matrix3 result)
        {
            result.M11 = matrix.M11 + scalar;
            result.M12 = matrix.M12 + scalar;
            result.M13 = matrix.M13 + scalar;

            result.M21 = matrix.M21 + scalar;
            result.M22 = matrix.M22 + scalar;
            result.M23 = matrix.M23 + scalar;

            result.M31 = matrix.M31 + scalar;
            result.M32 = matrix.M32 + scalar;
            result.M33 = matrix.M33 + scalar;
        }
        /// <summary>
        /// Subtracts a matrix from a matrix.
        /// </summary>
        /// <param name="left">A <see cref="Matrix3"/> instance to subtract from.</param>
        /// <param name="right">A <see cref="Matrix3"/> instance to subtract.</param>
        /// <returns>A new <see cref="Matrix3"/> instance containing the difference.</returns>
        /// <remarks>result[x][y] = left[x][y] - right[x][y]</remarks>
        public static Matrix3 Subtract(Matrix3 left, Matrix3 right)
        {
            return new Matrix3(
                left.M11 - right.M11, left.M12 - right.M12, left.M13 - right.M13,
                left.M21 - right.M21, left.M22 - right.M22, left.M23 - right.M23,
                left.M31 - right.M31, left.M32 - right.M32, left.M33 - right.M33
                );
        }
        /// <summary>
        /// Subtracts a scalar from a matrix.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix3"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <returns>A new <see cref="Matrix3"/> instance containing the difference.</returns>
        public static Matrix3 Subtract(Matrix3 matrix, float scalar)
        {
            return new Matrix3(
                matrix.M11 - scalar, matrix.M12 - scalar, matrix.M13 - scalar,
                matrix.M21 - scalar, matrix.M22 - scalar, matrix.M23 - scalar,
                matrix.M31 - scalar, matrix.M32 - scalar, matrix.M33 - scalar
                );
        }
        /// <summary>
        /// Subtracts a matrix from a matrix and put the result in a third matrix.
        /// </summary>
        /// <param name="left">A <see cref="Matrix3"/> instance to subtract from.</param>
        /// <param name="right">A <see cref="Matrix3"/> instance to subtract.</param>
        /// <param name="result">A <see cref="Matrix3"/> instance to hold the result.</param>
        /// <remarks>result[x][y] = left[x][y] - right[x][y]</remarks>
        public static void Subtract(Matrix3 left, Matrix3 right, ref Matrix3 result)
        {
            result.M11 = left.M11 - right.M11;
            result.M12 = left.M12 - right.M12;
            result.M13 = left.M13 - right.M13;

            result.M21 = left.M21 - right.M21;
            result.M22 = left.M22 - right.M22;
            result.M23 = left.M23 - right.M23;

            result.M31 = left.M31 - right.M31;
            result.M32 = left.M32 - right.M32;
            result.M33 = left.M33 - right.M33;
        }
        /// <summary>
        /// Subtracts a scalar from a matrix and put the result in a third matrix.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix3"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <param name="result">A <see cref="Matrix3"/> instance to hold the result.</param>
        public static void Subtract(Matrix3 matrix, float scalar, ref Matrix3 result)
        {
            result.M11 = matrix.M11 - scalar;
            result.M12 = matrix.M12 - scalar;
            result.M13 = matrix.M13 - scalar;

            result.M21 = matrix.M21 - scalar;
            result.M22 = matrix.M22 - scalar;
            result.M23 = matrix.M23 - scalar;

            result.M31 = matrix.M31 - scalar;
            result.M32 = matrix.M32 - scalar;
            result.M33 = matrix.M33 - scalar;
        }
        /// <summary>
        /// Multiplies two matrices.
        /// </summary>
        /// <param name="left">A <see cref="Matrix3"/> instance.</param>
        /// <param name="right">A <see cref="Matrix3"/> instance.</param>
        /// <returns>A new <see cref="Matrix3"/> instance containing the result.</returns>
        public static Matrix3 Multiply(Matrix3 left, Matrix3 right)
        {
            return new Matrix3(
                left.M11 * right.M11 + left.M12 * right.M21 + left.M13 * right.M31,
                left.M11 * right.M12 + left.M12 * right.M22 + left.M13 * right.M32,
                left.M11 * right.M13 + left.M12 * right.M23 + left.M13 * right.M33,

                left.M21 * right.M11 + left.M22 * right.M21 + left.M23 * right.M31,
                left.M21 * right.M12 + left.M22 * right.M22 + left.M23 * right.M32,
                left.M21 * right.M13 + left.M22 * right.M23 + left.M23 * right.M33,

                left.M31 * right.M11 + left.M32 * right.M21 + left.M33 * right.M31,
                left.M31 * right.M12 + left.M32 * right.M22 + left.M33 * right.M32,
                left.M31 * right.M13 + left.M32 * right.M23 + left.M33 * right.M33
                );
        }
        /// <summary>
        /// Multiplies two matrices and put the result in a third matrix.
        /// </summary>
        /// <param name="left">A <see cref="Matrix3"/> instance.</param>
        /// <param name="right">A <see cref="Matrix3"/> instance.</param>
        /// <param name="result">A <see cref="Matrix3"/> instance to hold the result.</param>
        public static void Multiply(Matrix3 left, Matrix3 right, ref Matrix3 result)
        {
            result.M11 = left.M11 * right.M11 + left.M12 * right.M21 + left.M13 * right.M31;
            result.M12 = left.M11 * right.M12 + left.M12 * right.M22 + left.M13 * right.M32;
            result.M13 = left.M11 * right.M13 + left.M12 * right.M23 + left.M13 * right.M33;

            result.M21 = left.M21 * right.M11 + left.M22 * right.M21 + left.M23 * right.M31;
            result.M22 = left.M21 * right.M12 + left.M22 * right.M22 + left.M23 * right.M32;
            result.M23 = left.M21 * right.M13 + left.M22 * right.M23 + left.M23 * right.M33;

            result.M31 = left.M31 * right.M11 + left.M32 * right.M21 + left.M33 * right.M31;
            result.M32 = left.M31 * right.M12 + left.M32 * right.M22 + left.M33 * right.M32;
            result.M33 = left.M31 * right.M13 + left.M32 * right.M23 + left.M33 * right.M33;
        }
        /// <summary>
        /// Transforms a given vector by a matrix.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix3"/> instance.</param>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <returns>A new <see cref="Vector3"/> instance containing the result.</returns>
        public static Vector3 Transform(Matrix3 matrix, Vector3 vector)
        {
            return new Vector3(
                (matrix.M11 * vector.X) + (matrix.M12 * vector.Y) + (matrix.M13 * vector.Z),
                (matrix.M21 * vector.X) + (matrix.M22 * vector.Y) + (matrix.M23 * vector.Z),
                (matrix.M31 * vector.X) + (matrix.M32 * vector.Y) + (matrix.M33 * vector.Z));
        }
        /// <summary>
        /// Transforms a given vector by a matrix and put the result in a vector.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix3"/> instance.</param>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <param name="result">A <see cref="Vector3"/> instance to hold the result.</param>
        public static void Transform(Matrix3 matrix, Vector3 vector, ref Vector3 result)
        {
            result.X = (matrix.M11 * vector.X) + (matrix.M12 * vector.Y) + (matrix.M13 * vector.Z);
            result.Y = (matrix.M21 * vector.X) + (matrix.M22 * vector.Y) + (matrix.M23 * vector.Z);
            result.Z = (matrix.M31 * vector.X) + (matrix.M32 * vector.Y) + (matrix.M33 * vector.Z);
        }
        /// <summary>
        /// Transposes a matrix.
        /// </summary>
        /// <param name="m">A <see cref="Matrix3"/> instance.</param>
        /// <returns>A new <see cref="Matrix3"/> instance containing the transposed matrix.</returns>
        public static Matrix3 Transpose(Matrix3 m)
        {
            Matrix3 t = new Matrix3(m);
            t.Transpose();
            return t;
        }

        public static Matrix3 fromEulerAnglesZYX(float fYAngle, float fPAngle, float fRAngle)
        {
            float fCos, fSin;

            fCos = (float)Math.Cos(fYAngle);
            fSin = (float)Math.Sin(fYAngle);
            Matrix3 kZMat = new Matrix3(fCos, -fSin, 0.0f, fSin, fCos, 0.0f, 0.0f, 0.0f, 1.0f);

            fCos = (float)Math.Cos(fPAngle);
            fSin = (float)Math.Sin(fPAngle);
            Matrix3 kYMat = new Matrix3(fCos, 0.0f, fSin, 0.0f, 1.0f, 0.0f, -fSin, 0.0f, fCos);

            fCos = (float)Math.Cos(fRAngle);
            fSin = (float)Math.Sin(fRAngle);
            Matrix3 kXMat = new Matrix3(1.0f, 0.0f, 0.0f, 0.0f, fCos, -fSin, 0.0f, fSin, fCos);

            return (kZMat * (kYMat * kXMat));
        }

        public Matrix3 inverse(float fTolerance = (float)1e-06)
        {
            Matrix3 kInverse = Matrix3.Zero;
            inverse(ref kInverse, fTolerance);
            return kInverse;
        }
        bool inverse(ref Matrix3 rkInverse, float fTolerance)
        {
            // Invert a 3x3 using cofactors.  This is about 8 times faster than
            // the Numerical Recipes code which uses Gaussian elimination.
            rkInverse.M11 = M22 * M33 -
                              M23 * M32;
            rkInverse.M12 = M13 * M32 -
                              M12 * M33;
            rkInverse.M13 = M12 * M23 -
                              M13 * M22;
            rkInverse.M21 = M23 * M31 -
                              M21 * M33;
            rkInverse.M22 = M11 * M33 -
                              M13 * M31;
            rkInverse.M23 = M13 * M21 -
                              M11 * M23;
            rkInverse.M31 = M21 * M32 -
                              M22 * M31;
            rkInverse.M32 = M12 * M31 -
                              M11 * M32;
            rkInverse.M33 = M11 * M22 -
                              M12 * M21;


            float fDet =
                M11 * rkInverse.M11 +
                M12 * rkInverse.M21 +
                M13 * rkInverse.M31;

            if (Math.Abs(fDet) <= fTolerance)
                return false;

            float fInvDet = (float)(1.0 / fDet);

            rkInverse.M11 *= fInvDet;
            rkInverse.M12 *= fInvDet;
            rkInverse.M13 *= fInvDet;
            rkInverse.M21 *= fInvDet;
            rkInverse.M22 *= fInvDet;
            rkInverse.M23 *= fInvDet;
            rkInverse.M31 *= fInvDet;
            rkInverse.M32 *= fInvDet;
            rkInverse.M33 *= fInvDet;

            return true;
        }
        #endregion

        #region System.Object overrides
        /// <summary>
        /// Returns the hashcode for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return
                _m11.GetHashCode() ^ _m12.GetHashCode() ^ _m13.GetHashCode() ^
                _m21.GetHashCode() ^ _m22.GetHashCode() ^ _m23.GetHashCode() ^
                _m31.GetHashCode() ^ _m32.GetHashCode() ^ _m33.GetHashCode();
        }
        /// <summary>
        /// Returns a value indicating whether this instance is equal to
        /// the specified object.
        /// </summary>
        /// <param name="obj">An object to compare to this instance.</param>
        /// <returns><see langword="true"/> if <paramref name="obj"/> is a <see cref="Matrix3"/> and has the same values as this instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object obj)
        {
            if (obj is Matrix3)
            {
                Matrix3 m = (Matrix3)obj;
                return
                    (_m11 == m.M11) && (_m12 == m.M12) && (_m13 == m.M13) &&
                    (_m21 == m.M21) && (_m22 == m.M22) && (_m23 == m.M23) &&
                    (_m31 == m.M31) && (_m32 == m.M32) && (_m33 == m.M33);
            }
            return false;
        }
        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        /// <returns>A string representation of this object.</returns>
        public override string ToString()
        {
            return $"3x3[{_m11}, {_m12}, {_m13}, {_m21}, {_m22}, {_m23}, {_m31}, {_m32}, {_m33}]";
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Calculates the determinant value of the matrix.
        /// </summary>
        /// <returns>The determinant value of the matrix.</returns>
        public float GetDeterminant()
        {
            // rule of Sarrus
            return
                _m11 * _m22 * _m33 + _m12 * _m23 * _m31 + _m13 * _m21 * _m32 -
                _m13 * _m22 * _m31 - _m11 * _m23 * _m32 - _m12 * _m21 * _m33;
        }
        /// <summary>
        /// Transposes this matrix.
        /// </summary>
        public void Transpose()
        {
            MathFunctions.Swap<float>(ref _m12, ref _m21);
            MathFunctions.Swap<float>(ref _m13, ref _m31);
            MathFunctions.Swap<float>(ref _m23, ref _m32);
        }
        #endregion

        #region Comparison Operators
        /// <summary>
        /// Tests whether two specified matrices are equal.
        /// </summary>
        /// <param name="left">A <see cref="Matrix3"/> instance.</param>
        /// <param name="right">A <see cref="Matrix3"/> instance.</param>
        /// <returns><see langword="true"/> if the two matrices are equal; otherwise, <see langword="false"/>.</returns>
        public static bool operator ==(Matrix3 left, Matrix3 right)
        {
            return ValueType.Equals(left, right);
        }
        /// <summary>
        /// Tests whether two specified matrices are not equal.
        /// </summary>
        /// <param name="left">A <see cref="Matrix3"/> instance.</param>
        /// <param name="right">A <see cref="Matrix3"/> instance.</param>
        /// <returns><see langword="true"/> if the two matrices are not equal; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(Matrix3 left, Matrix3 right)
        {
            return !ValueType.Equals(left, right);
        }
        #endregion

        #region Binary Operators
        /// <summary>
        /// Adds two matrices.
        /// </summary>
        /// <param name="left">A <see cref="Matrix3"/> instance.</param>
        /// <param name="right">A <see cref="Matrix3"/> instance.</param>
        /// <returns>A new <see cref="Matrix3"/> instance containing the sum.</returns>
        public static Matrix3 operator +(Matrix3 left, Matrix3 right)
        {
            return Matrix3.Add(left, right);
        }
        /// <summary>
        /// Adds a matrix and a scalar.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix3"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <returns>A new <see cref="Matrix3"/> instance containing the sum.</returns>
        public static Matrix3 operator +(Matrix3 matrix, float scalar)
        {
            return Matrix3.Add(matrix, scalar);
        }
        /// <summary>
        /// Adds a matrix and a scalar.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix3"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <returns>A new <see cref="Matrix3"/> instance containing the sum.</returns>
        public static Matrix3 operator +(float scalar, Matrix3 matrix)
        {
            return Matrix3.Add(matrix, scalar);
        }
        /// <summary>
        /// Subtracts a matrix from a matrix.
        /// </summary>
        /// <param name="left">A <see cref="Matrix3"/> instance.</param>
        /// <param name="right">A <see cref="Matrix3"/> instance.</param>
        /// <returns>A new <see cref="Matrix3"/> instance containing the difference.</returns>
        public static Matrix3 operator -(Matrix3 left, Matrix3 right)
        {
            return Matrix3.Subtract(left, right);
        }
        /// <summary>
        /// Subtracts a scalar from a matrix.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix3"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <returns>A new <see cref="Matrix3"/> instance containing the difference.</returns>
        public static Matrix3 operator -(Matrix3 matrix, float scalar)
        {
            return Matrix3.Subtract(matrix, scalar);
        }
        /// <summary>
        /// Multiplies two matrices.
        /// </summary>
        /// <param name="left">A <see cref="Matrix3"/> instance.</param>
        /// <param name="right">A <see cref="Matrix3"/> instance.</param>
        /// <returns>A new <see cref="Matrix3"/> instance containing the result.</returns>
        public static Matrix3 operator *(Matrix3 left, Matrix3 right)
        {
            return Matrix3.Multiply(left, right);
        }
        /// <summary>
        /// Transforms a given vector by a matrix.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix3"/> instance.</param>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <returns>A new <see cref="Vector3"/> instance containing the result.</returns>
        public static Vector3 operator *(Matrix3 matrix, Vector3 vector)
        {
            return Matrix3.Transform(matrix, vector);
        }
        public static Vector3 operator *(Vector3 rkPoint, Matrix3 rkMatrix)
        {
            return (Matrix3.Transpose(rkMatrix) * rkPoint);
        }
        #endregion

        #region Indexing Operators
        /// <summary>
        /// Indexer allowing to access the matrix elements by an index
        /// where index = 2*row + column.
        /// </summary>
        public unsafe float this[int index]
        {
            get
            {
                if (index < 0 || index >= 9)
                    throw new IndexOutOfRangeException("Invalid matrix index!");

                fixed (float* f = &_m11)
                {
                    return *(f + index);
                }
            }
            set
            {
                if (index < 0 || index >= 9)
                    throw new IndexOutOfRangeException("Invalid matrix index!");

                fixed (float* f = &_m11)
                {
                    *(f + index) = value;
                }
            }
        }
        /// <summary>
        /// Indexer allowing to access the matrix elements by row and column.
        /// </summary>
        public float this[int row, int column]
        {
            get
            {
                return this[row * 3 + column];
            }
            set
            {
                this[row * 3 + column] = value;
            }
        }
        #endregion
    }
}
