/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
    /// Represents a 4-dimentional single-precision floating point matrix.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public struct Matrix4 : ICloneable
    {
        #region Private Fields
        private float _m11, _m12, _m13, _m14;
        private float _m21, _m22, _m23, _m24;
        private float _m31, _m32, _m33, _m34;
        private float _m41, _m42, _m43, _m44;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix4"/> structure with the specified values.
        /// </summary>
        public Matrix4(float m11, float m12, float m13, float m14, float m21, float m22, float m23, float m24, float m31, float m32, float m33, float m34, float m41, float m42, float m43, float m44)
        {
            _m11 = m11; _m12 = m12; _m13 = m13; _m14 = m14;
            _m21 = m21; _m22 = m22; _m23 = m23; _m24 = m24;
            _m31 = m31; _m32 = m32; _m33 = m33; _m34 = m34;
            _m41 = m41; _m42 = m42; _m43 = m43; _m44 = m44;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix4"/> structure with the specified values.
        /// </summary>
        /// <param name="elements">An array containing the matrix values in row-major order.</param>
        public Matrix4(float[] elements)
        {
            Debug.Assert(elements != null);
            Debug.Assert(elements.Length >= 16);

            _m11 = elements[0]; _m12 = elements[1]; _m13 = elements[2]; _m14 = elements[3];
            _m21 = elements[4]; _m22 = elements[5]; _m23 = elements[6]; _m24 = elements[7];
            _m31 = elements[8]; _m32 = elements[9]; _m33 = elements[10]; _m34 = elements[11];
            _m41 = elements[12]; _m42 = elements[13]; _m43 = elements[14]; _m44 = elements[15];
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix4"/> structure with the specified values.
        /// </summary>
        /// <param name="elements">An array containing the matrix values in row-major order.</param>
        public Matrix4(List<float> elements)
        {
            Debug.Assert(elements != null);
            Debug.Assert(elements.Count >= 16);

            _m11 = elements[0]; _m12 = elements[1]; _m13 = elements[2]; _m14 = elements[3];
            _m21 = elements[4]; _m22 = elements[5]; _m23 = elements[6]; _m24 = elements[7];
            _m31 = elements[8]; _m32 = elements[9]; _m33 = elements[10]; _m34 = elements[11];
            _m41 = elements[12]; _m42 = elements[13]; _m43 = elements[14]; _m44 = elements[15];
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix4"/> structure with the specified values.
        /// </summary>
        /// <param name="column1">A <see cref="Vector4"/> instance holding values for the first column.</param>
        /// <param name="column2">A <see cref="Vector4"/> instance holding values for the second column.</param>
        /// <param name="column3">A <see cref="Vector4"/> instance holding values for the third column.</param>
        /// <param name="column4">A <see cref="Vector4"/> instance holding values for the fourth column.</param>
        public Matrix4(Vector4 column1, Vector4 column2, Vector4 column3, Vector4 column4)
        {
            _m11 = column1.X; _m12 = column2.X; _m13 = column3.X; _m14 = column4.X;
            _m21 = column1.Y; _m22 = column2.Y; _m23 = column3.Y; _m24 = column4.Y;
            _m31 = column1.Z; _m32 = column2.Z; _m33 = column3.Z; _m34 = column4.Z;
            _m41 = column1.W; _m42 = column2.W; _m43 = column3.W; _m44 = column4.W;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix4"/> class using a given matrix.
        /// </summary>
        public Matrix4(Matrix4 m)
        {
            _m11 = m.M11; _m12 = m.M12; _m13 = m.M13; _m14 = m.M14;
            _m21 = m.M21; _m22 = m.M22; _m23 = m.M23; _m24 = m.M24;
            _m31 = m.M31; _m32 = m.M32; _m33 = m.M33; _m34 = m.M34;
            _m41 = m.M41; _m42 = m.M42; _m43 = m.M43; _m44 = m.M44;
        }
        #endregion

        #region Constants
        /// <summary>
        /// 4-dimentional single-precision floating point zero matrix.
        /// </summary>
        public static readonly Matrix4 Zero = new Matrix4(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        /// <summary>
        /// 4-dimentional single-precision floating point identity matrix.
        /// </summary>
        public static readonly Matrix4 Identity = new Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
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
        /// Gets or sets the value of the [1,4] matrix element.
        /// </summary>
        /// <value>A single-precision floating-point number.</value>
        public float M14
        {
            get { return _m14; }
            set { _m14 = value; }
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
        /// Gets or sets the value of the [2,4] matrix element.
        /// </summary>
        /// <value>A single-precision floating-point number.</value>
        public float M24
        {
            get { return _m24; }
            set { _m24 = value; }
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
        /// Gets or sets the value of the [3,4] matrix element.
        /// </summary>
        /// <value>A single-precision floating-point number.</value>
        public float M34
        {
            get { return _m34; }
            set { _m34 = value; }
        }


        /// <summary>
        /// Gets or sets the value of the [4,1] matrix element.
        /// </summary>
        /// <value>A single-precision floating-point number.</value>
        public float M41
        {
            get { return _m41; }
            set { _m41 = value; }
        }
        /// <summary>
        /// Gets or sets the value of the [4,2] matrix element.
        /// </summary>
        /// <value>A single-precision floating-point number.</value>
        public float M42
        {
            get { return _m42; }
            set { _m42 = value; }
        }
        /// <summary>
        /// Gets or sets the value of the [4,3] matrix element.
        /// </summary>
        /// <value>A single-precision floating-point number.</value>
        public float M43
        {
            get { return _m43; }
            set { _m43 = value; }
        }
        /// <summary>
        /// Gets or sets the value of the [4,4] matrix element.
        /// </summary>
        /// <value>A single-precision floating-point number.</value>
        public float M44
        {
            get { return _m44; }
            set { _m44 = value; }
        }

        /// <summary>
        /// Gets the matrix's trace value.
        /// </summary>
        /// <value>A single-precision floating-point number.</value>
        public float Trace
        {
            get
            {
                return _m11 + _m22 + _m33 + _m44;
            }
        }
        #endregion

        #region ICloneable Members
        /// <summary>
        /// Creates an exact copy of this <see cref="Matrix4"/> object.
        /// </summary>
        /// <returns>The <see cref="Matrix4"/> object this method creates, cast as an object.</returns>
        object ICloneable.Clone()
        {
            return new Matrix4(this);
        }
        /// <summary>
        /// Creates an exact copy of this <see cref="Matrix4"/> object.
        /// </summary>
        /// <returns>The <see cref="Matrix4"/> object this method creates.</returns>
        public Matrix4 Clone()
        {
            return new Matrix4(this);
        }
        #endregion

        #region Public Static Parse Methods
        private const string regularExp = @"4x4\s*\[(?<m11>.*),(?<m12>.*),(?<m13>.*),(?<m14>.*),(?<m21>.*),(?<m22>.*),(?<m23>.*),(?<m24>.*),(?<m31>.*),(?<m32>.*),(?<m33>.*),(?<m34>.*),(?<m41>.*),(?<m42>.*),(?<m43>.*),(?<m44>.*)\]";
        /// <summary>
        /// Converts the specified string to its <see cref="Matrix4"/> equivalent.
        /// </summary>
        /// <param name="value">A string representation of a <see cref="Matrix4"/>.</param>
        /// <returns>A <see cref="Matrix4"/> that represents the vector specified by the <paramref name="value"/> parameter.</returns>
        /// <remarks>
        /// The string should be in the following form: "4x4..matrix elements..>".<br/>
        /// Exmaple : "4x4[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16]"
        /// </remarks>
        public static Matrix4 Parse(string value)
        {
            Regex r = new Regex(regularExp, RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
            Match m = r.Match(value);
            if (m.Success)
            {
                return new Matrix4(
                    float.Parse(m.Result("${m11}")),
                    float.Parse(m.Result("${m12}")),
                    float.Parse(m.Result("${m13}")),
                    float.Parse(m.Result("${m14}")),

                    float.Parse(m.Result("${m21}")),
                    float.Parse(m.Result("${m22}")),
                    float.Parse(m.Result("${m23}")),
                    float.Parse(m.Result("${m24}")),

                    float.Parse(m.Result("${m31}")),
                    float.Parse(m.Result("${m32}")),
                    float.Parse(m.Result("${m33}")),
                    float.Parse(m.Result("${m34}")),

                    float.Parse(m.Result("${m41}")),
                    float.Parse(m.Result("${m42}")),
                    float.Parse(m.Result("${m43}")),
                    float.Parse(m.Result("${m44}"))
                    );
            }
            else
            {
                throw new Exception("Unsuccessful Match.");
            }
        }
        /// <summary>
        /// Converts the specified string to its <see cref="Matrix4"/> equivalent.
        /// A return value indicates whether the conversion succeeded or failed.
        /// </summary>
        /// <param name="value">A string representation of a <see cref="Matrix4"/>.</param>
        /// <param name="result">
        /// When this method returns, if the conversion succeeded,
        /// contains a <see cref="Matrix4"/> representing the vector specified by <paramref name="value"/>.
        /// </param>
        /// <returns><see langword="true"/> if value was converted successfully; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// The string should be in the following form: "4x4..matrix elements..>".<br/>
        /// Exmaple : "4x4[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16]"
        /// </remarks>
        public static bool TryParse(string value, out Matrix4 result)
        {
            Regex r = new Regex(regularExp, RegexOptions.Singleline);
            Match m = r.Match(value);
            if (m.Success)
            {
                result = new Matrix4(
                    float.Parse(m.Result("${m11}")),
                    float.Parse(m.Result("${m12}")),
                    float.Parse(m.Result("${m13}")),
                    float.Parse(m.Result("${m14}")),

                    float.Parse(m.Result("${m21}")),
                    float.Parse(m.Result("${m22}")),
                    float.Parse(m.Result("${m23}")),
                    float.Parse(m.Result("${m24}")),

                    float.Parse(m.Result("${m31}")),
                    float.Parse(m.Result("${m32}")),
                    float.Parse(m.Result("${m33}")),
                    float.Parse(m.Result("${m34}")),

                    float.Parse(m.Result("${m41}")),
                    float.Parse(m.Result("${m42}")),
                    float.Parse(m.Result("${m43}")),
                    float.Parse(m.Result("${m44}"))
                    );

                return true;
            }

            result = Matrix4.Zero;
            return false;
        }
        #endregion

        #region Public Static Matrix Arithmetics
        /// <summary>
        /// Adds two matrices.
        /// </summary>
        /// <param name="left">A <see cref="Matrix4"/> instance.</param>
        /// <param name="right">A <see cref="Matrix4"/> instance.</param>
        /// <returns>A new <see cref="Matrix4"/> instance containing the sum.</returns>
        public static Matrix4 Add(Matrix4 left, Matrix4 right)
        {
            return new Matrix4(
                left.M11 + right.M11, left.M12 + right.M12, left.M13 + right.M13, left.M14 + right.M14,
                left.M21 + right.M21, left.M22 + right.M22, left.M23 + right.M23, left.M24 + right.M24,
                left.M31 + right.M31, left.M32 + right.M32, left.M33 + right.M33, left.M34 + right.M34,
                left.M41 + right.M41, left.M42 + right.M42, left.M43 + right.M43, left.M44 + right.M44
                );
        }
        /// <summary>
        /// Adds a matrix and a scalar.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix4"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <returns>A new <see cref="Matrix4"/> instance containing the sum.</returns>
        public static Matrix4 Add(Matrix4 matrix, float scalar)
        {
            return new Matrix4(
                matrix.M11 + scalar, matrix.M12 + scalar, matrix.M13 + scalar, matrix.M14 + scalar,
                matrix.M21 + scalar, matrix.M22 + scalar, matrix.M23 + scalar, matrix.M24 + scalar,
                matrix.M31 + scalar, matrix.M32 + scalar, matrix.M33 + scalar, matrix.M34 + scalar,
                matrix.M41 + scalar, matrix.M42 + scalar, matrix.M43 + scalar, matrix.M44 + scalar
                );
        }
        /// <summary>
        /// Adds two matrices and put the result in a third matrix.
        /// </summary>
        /// <param name="left">A <see cref="Matrix4"/> instance.</param>
        /// <param name="right">A <see cref="Matrix4"/> instance.</param>
        /// <param name="result">A <see cref="Matrix4"/> instance to hold the result.</param>
        public static void Add(Matrix4 left, Matrix4 right, ref Matrix4 result)
        {
            result.M11 = left.M11 + right.M11;
            result.M12 = left.M12 + right.M12;
            result.M13 = left.M13 + right.M13;
            result.M14 = left.M14 + right.M14;

            result.M21 = left.M21 + right.M21;
            result.M22 = left.M22 + right.M22;
            result.M23 = left.M23 + right.M23;
            result.M24 = left.M24 + right.M24;

            result.M31 = left.M31 + right.M31;
            result.M32 = left.M32 + right.M32;
            result.M33 = left.M33 + right.M33;
            result.M34 = left.M34 + right.M34;

            result.M41 = left.M41 + right.M41;
            result.M42 = left.M42 + right.M42;
            result.M43 = left.M43 + right.M43;
            result.M44 = left.M44 + right.M44;
        }
        /// <summary>
        /// Adds a matrix and a scalar and put the result in a third matrix.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix4"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <param name="result">A <see cref="Matrix4"/> instance to hold the result.</param>
        public static void Add(Matrix4 matrix, float scalar, ref Matrix4 result)
        {
            result.M11 = matrix.M11 + scalar;
            result.M12 = matrix.M12 + scalar;
            result.M13 = matrix.M13 + scalar;
            result.M14 = matrix.M14 + scalar;

            result.M21 = matrix.M21 + scalar;
            result.M22 = matrix.M22 + scalar;
            result.M23 = matrix.M23 + scalar;
            result.M24 = matrix.M24 + scalar;

            result.M31 = matrix.M31 + scalar;
            result.M32 = matrix.M32 + scalar;
            result.M33 = matrix.M33 + scalar;
            result.M34 = matrix.M34 + scalar;

            result.M41 = matrix.M41 + scalar;
            result.M42 = matrix.M42 + scalar;
            result.M43 = matrix.M43 + scalar;
            result.M44 = matrix.M44 + scalar;
        }
        /// <summary>
        /// Subtracts a matrix from a matrix.
        /// </summary>
        /// <param name="left">A <see cref="Matrix4"/> instance to subtract from.</param>
        /// <param name="right">A <see cref="Matrix4"/> instance to subtract.</param>
        /// <returns>A new <see cref="Matrix4"/> instance containing the difference.</returns>
        /// <remarks>result[x][y] = left[x][y] - right[x][y]</remarks>
        public static Matrix4 Subtract(Matrix4 left, Matrix4 right)
        {
            return new Matrix4(
                left.M11 - right.M11, left.M12 - right.M12, left.M13 - right.M13, left.M14 - right.M14,
                left.M21 - right.M21, left.M22 - right.M22, left.M23 - right.M23, left.M24 - right.M24,
                left.M31 - right.M31, left.M32 - right.M32, left.M33 - right.M33, left.M34 - right.M34,
                left.M41 - right.M41, left.M42 - right.M42, left.M43 - right.M43, left.M44 - right.M44
                );
        }
        /// <summary>
        /// Subtracts a scalar from a matrix.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix4"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <returns>A new <see cref="Matrix4"/> instance containing the difference.</returns>
        public static Matrix4 Subtract(Matrix4 matrix, float scalar)
        {
            return new Matrix4(
                matrix.M11 - scalar, matrix.M12 - scalar, matrix.M13 - scalar, matrix.M14 - scalar,
                matrix.M21 - scalar, matrix.M22 - scalar, matrix.M23 - scalar, matrix.M24 - scalar,
                matrix.M31 - scalar, matrix.M32 - scalar, matrix.M33 - scalar, matrix.M34 - scalar,
                matrix.M41 - scalar, matrix.M42 - scalar, matrix.M43 - scalar, matrix.M44 - scalar
                );
        }
        /// <summary>
        /// Subtracts a matrix from a matrix and put the result in a third matrix.
        /// </summary>
        /// <param name="left">A <see cref="Matrix4"/> instance to subtract from.</param>
        /// <param name="right">A <see cref="Matrix4"/> instance to subtract.</param>
        /// <param name="result">A <see cref="Matrix4"/> instance to hold the result.</param>
        /// <remarks>result[x][y] = left[x][y] - right[x][y]</remarks>
        public static void Subtract(Matrix4 left, Matrix4 right, ref Matrix4 result)
        {
            result.M11 = left.M11 - right.M11;
            result.M12 = left.M12 - right.M12;
            result.M13 = left.M13 - right.M13;
            result.M14 = left.M14 - right.M14;

            result.M21 = left.M21 - right.M21;
            result.M22 = left.M22 - right.M22;
            result.M23 = left.M23 - right.M23;
            result.M24 = left.M24 - right.M24;

            result.M31 = left.M31 - right.M31;
            result.M32 = left.M32 - right.M32;
            result.M33 = left.M33 - right.M33;
            result.M34 = left.M34 - right.M34;

            result.M41 = left.M41 - right.M41;
            result.M42 = left.M42 - right.M42;
            result.M43 = left.M43 - right.M43;
            result.M44 = left.M44 - right.M44;
        }
        /// <summary>
        /// Subtracts a scalar from a matrix and put the result in a third matrix.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix4"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <param name="result">A <see cref="Matrix4"/> instance to hold the result.</param>
        public static void Subtract(Matrix4 matrix, float scalar, ref Matrix4 result)
        {
            result.M11 = matrix.M11 - scalar;
            result.M12 = matrix.M12 - scalar;
            result.M13 = matrix.M13 - scalar;
            result.M14 = matrix.M14 - scalar;

            result.M21 = matrix.M21 - scalar;
            result.M22 = matrix.M22 - scalar;
            result.M23 = matrix.M23 - scalar;
            result.M24 = matrix.M24 - scalar;

            result.M31 = matrix.M31 - scalar;
            result.M32 = matrix.M32 - scalar;
            result.M33 = matrix.M33 - scalar;
            result.M34 = matrix.M34 - scalar;

            result.M41 = matrix.M41 - scalar;
            result.M42 = matrix.M42 - scalar;
            result.M43 = matrix.M43 - scalar;
            result.M44 = matrix.M44 - scalar;
        }
        /// <summary>
        /// Multiplies two matrices.
        /// </summary>
        /// <param name="left">A <see cref="Matrix4"/> instance.</param>
        /// <param name="right">A <see cref="Matrix4"/> instance.</param>
        /// <returns>A new <see cref="Matrix4"/> instance containing the result.</returns>
        public static Matrix4 Multiply(Matrix4 left, Matrix4 right)
        {
            return new Matrix4(
                left.M11 * right.M11 + left.M12 * right.M21 + left.M13 * right.M31 + left.M14 * right.M41,
                left.M11 * right.M12 + left.M12 * right.M22 + left.M13 * right.M32 + left.M14 * right.M42,
                left.M11 * right.M13 + left.M12 * right.M23 + left.M13 * right.M33 + left.M14 * right.M43,
                left.M11 * right.M14 + left.M12 * right.M24 + left.M13 * right.M34 + left.M14 * right.M44,

                left.M21 * right.M11 + left.M22 * right.M21 + left.M23 * right.M31 + left.M24 * right.M41,
                left.M21 * right.M12 + left.M22 * right.M22 + left.M23 * right.M32 + left.M24 * right.M42,
                left.M21 * right.M13 + left.M22 * right.M23 + left.M23 * right.M33 + left.M24 * right.M43,
                left.M21 * right.M14 + left.M22 * right.M24 + left.M23 * right.M34 + left.M24 * right.M44,

                left.M31 * right.M11 + left.M32 * right.M21 + left.M33 * right.M31 + left.M34 * right.M41,
                left.M31 * right.M12 + left.M32 * right.M22 + left.M33 * right.M32 + left.M34 * right.M42,
                left.M31 * right.M13 + left.M32 * right.M23 + left.M33 * right.M33 + left.M34 * right.M43,
                left.M31 * right.M14 + left.M32 * right.M24 + left.M33 * right.M34 + left.M34 * right.M44,

                left.M41 * right.M11 + left.M42 * right.M21 + left.M43 * right.M31 + left.M44 * right.M41,
                left.M41 * right.M12 + left.M42 * right.M22 + left.M43 * right.M32 + left.M44 * right.M42,
                left.M41 * right.M13 + left.M42 * right.M23 + left.M43 * right.M33 + left.M44 * right.M43,
                left.M41 * right.M14 + left.M42 * right.M24 + left.M43 * right.M34 + left.M44 * right.M44
                );
        }
        /// <summary>
        /// Multiplies two matrices and put the result in a third matrix.
        /// </summary>
        /// <param name="left">A <see cref="Matrix4"/> instance.</param>
        /// <param name="right">A <see cref="Matrix4"/> instance.</param>
        /// <param name="result">A <see cref="Matrix4"/> instance to hold the result.</param>
        public static void Multiply(Matrix4 left, Matrix4 right, ref Matrix4 result)
        {
            result.M11 = left.M11 * right.M11 + left.M12 * right.M21 + left.M13 * right.M31 + left.M14 * right.M41;
            result.M12 = left.M11 * right.M12 + left.M12 * right.M22 + left.M13 * right.M32 + left.M14 * right.M42;
            result.M13 = left.M11 * right.M13 + left.M12 * right.M23 + left.M13 * right.M33 + left.M14 * right.M43;
            result.M14 = left.M11 * right.M14 + left.M12 * right.M24 + left.M13 * right.M34 + left.M14 * right.M44;

            result.M21 = left.M21 * right.M11 + left.M22 * right.M21 + left.M23 * right.M31 + left.M24 * right.M41;
            result.M22 = left.M21 * right.M12 + left.M22 * right.M22 + left.M23 * right.M32 + left.M24 * right.M42;
            result.M23 = left.M21 * right.M13 + left.M22 * right.M23 + left.M23 * right.M33 + left.M24 * right.M43;
            result.M24 = left.M21 * right.M14 + left.M22 * right.M24 + left.M23 * right.M34 + left.M24 * right.M44;

            result.M31 = left.M31 * right.M11 + left.M32 * right.M21 + left.M33 * right.M31 + left.M34 * right.M41;
            result.M32 = left.M31 * right.M12 + left.M32 * right.M22 + left.M33 * right.M32 + left.M34 * right.M42;
            result.M33 = left.M31 * right.M13 + left.M32 * right.M23 + left.M33 * right.M33 + left.M34 * right.M43;
            result.M34 = left.M31 * right.M14 + left.M32 * right.M24 + left.M33 * right.M34 + left.M34 * right.M44;

            result.M41 = left.M41 * right.M11 + left.M42 * right.M21 + left.M43 * right.M31 + left.M44 * right.M41;
            result.M42 = left.M41 * right.M12 + left.M42 * right.M22 + left.M43 * right.M32 + left.M44 * right.M42;
            result.M43 = left.M41 * right.M13 + left.M42 * right.M23 + left.M43 * right.M33 + left.M44 * right.M43;
            result.M44 = left.M41 * right.M14 + left.M42 * right.M24 + left.M43 * right.M34 + left.M44 * right.M44;
        }
        /// <summary>
        /// Transforms a given vector by a matrix.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix4"/> instance.</param>
        /// <param name="vector">A <see cref="Vector4"/> instance.</param>
        /// <returns>A new <see cref="Vector4"/> instance containing the result.</returns>
        public static Vector4 Transform(Matrix4 matrix, Vector4 vector)
        {
            return new Vector4(
                (matrix.M11 * vector.X) + (matrix.M12 * vector.Y) + (matrix.M13 * vector.Z) + (matrix.M14 * vector.W),
                (matrix.M21 * vector.X) + (matrix.M22 * vector.Y) + (matrix.M23 * vector.Z) + (matrix.M24 * vector.W),
                (matrix.M31 * vector.X) + (matrix.M32 * vector.Y) + (matrix.M33 * vector.Z) + (matrix.M34 * vector.W),
                (matrix.M41 * vector.X) + (matrix.M42 * vector.Y) + (matrix.M43 * vector.Z) + (matrix.M44 * vector.W));
        }
        /// <summary>
        /// Transforms a given vector by a matrix and put the result in a vector.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix4"/> instance.</param>
        /// <param name="vector">A <see cref="Vector4"/> instance.</param>
        /// <param name="result">A <see cref="Vector4"/> instance to hold the result.</param>
        public static void Transform(Matrix4 matrix, Vector4 vector, ref Vector4 result)
        {
            result.X = (matrix.M11 * vector.X) + (matrix.M12 * vector.Y) + (matrix.M13 * vector.Z) + (matrix.M14 * vector.W);
            result.Y = (matrix.M21 * vector.X) + (matrix.M22 * vector.Y) + (matrix.M23 * vector.Z) + (matrix.M24 * vector.W);
            result.Z = (matrix.M31 * vector.X) + (matrix.M32 * vector.Y) + (matrix.M33 * vector.Z) + (matrix.M34 * vector.W);
            result.W = (matrix.M41 * vector.X) + (matrix.M42 * vector.Y) + (matrix.M43 * vector.Z) + (matrix.M44 * vector.W);
        }
        /// <summary>
        /// Transposes a matrix.
        /// </summary>
        /// <param name="m">A <see cref="Matrix4"/> instance.</param>
        /// <returns>A new <see cref="Matrix4"/> instance containing the transposed matrix.</returns>
        public static Matrix4 Transpose(Matrix4 m)
        {
            Matrix4 t = new Matrix4(m);
            t.Transpose();
            return t;
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
                _m11.GetHashCode() ^ _m12.GetHashCode() ^ _m13.GetHashCode() ^ _m14.GetHashCode() ^
                _m21.GetHashCode() ^ _m22.GetHashCode() ^ _m23.GetHashCode() ^ _m24.GetHashCode() ^
                _m31.GetHashCode() ^ _m32.GetHashCode() ^ _m33.GetHashCode() ^ _m34.GetHashCode() ^
                _m41.GetHashCode() ^ _m42.GetHashCode() ^ _m43.GetHashCode() ^ _m44.GetHashCode();
        }
        /// <summary>
        /// Returns a value indicating whether this instance is equal to
        /// the specified object.
        /// </summary>
        /// <param name="obj">An object to compare to this instance.</param>
        /// <returns><see langword="true"/> if <paramref name="obj"/> is a <see cref="Matrix4"/> and has the same values as this instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object obj)
        {
            if (obj is Matrix4)
            {
                Matrix4 m = (Matrix4)obj;
                return
                    (_m11 == m.M11) && (_m12 == m.M12) && (_m13 == m.M13) && (_m14 == m.M14) &&
                    (_m21 == m.M21) && (_m22 == m.M22) && (_m23 == m.M23) && (_m24 == m.M24) &&
                    (_m31 == m.M31) && (_m32 == m.M32) && (_m33 == m.M33) && (_m34 == m.M34) &&
                    (_m41 == m.M41) && (_m42 == m.M42) && (_m43 == m.M43) && (_m44 == m.M44);
            }
            return false;
        }
        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        /// <returns>A string representation of this object.</returns>
        public override string ToString()
        {
            return
                $"4x4[{_m11}, {_m12}, {_m13}, {_m14}, {_m21}, {_m22}, {_m23}, {_m24}, {_m31}, {_m32}, {_m33}, {_m34}, {_m41}, {_m42}, {_m43}, {_m44}]";
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Calculates the determinant value of the matrix.
        /// </summary>
        /// <returns>The determinant value of the matrix.</returns>
        public float GetDeterminant()
        {
            //			float det = 0.0f;
            //			for (int col = 0; col < 4; col++)
            //			{
            //				if ((col % 2) == 0)
            //					det += this[0, col] * Minor(0, col).Determinant();
            //				else
            //					det -= this[0, col] * Minor(0, col).Determinant();
            //			}
            //			return det;
            return
                _m14 * _m23 * _m32 * _m41 - _m13 * _m24 * _m32 * _m41 - _m14 * _m22 * _m33 * _m41 + _m12 * _m24 * _m33 * _m41 +
                _m13 * _m22 * _m34 * _m41 - _m12 * _m23 * _m34 * _m41 - _m14 * _m23 * _m31 * _m42 + _m13 * _m24 * _m31 * _m42 +
                _m14 * _m21 * _m33 * _m42 - _m11 * _m24 * _m33 * _m42 - _m13 * _m21 * _m34 * _m42 + _m11 * _m23 * _m34 * _m42 +
                _m14 * _m22 * _m31 * _m43 - _m12 * _m24 * _m31 * _m43 - _m14 * _m21 * _m32 * _m43 + _m11 * _m24 * _m32 * _m43 +
                _m12 * _m21 * _m34 * _m43 - _m11 * _m22 * _m34 * _m43 - _m13 * _m22 * _m31 * _m44 + _m12 * _m23 * _m31 * _m44 +
                _m13 * _m21 * _m32 * _m44 - _m11 * _m23 * _m32 * _m44 - _m12 * _m21 * _m33 * _m44 + _m11 * _m22 * _m33 * _m44;
        }
        /// <summary>
        /// Transposes this matrix.
        /// </summary>
        public void Transpose()
        {
            MathFunctions.Swap<float>(ref _m12, ref _m21);
            MathFunctions.Swap<float>(ref _m13, ref _m31);
            MathFunctions.Swap<float>(ref _m14, ref _m41);
            MathFunctions.Swap<float>(ref _m23, ref _m32);
            MathFunctions.Swap<float>(ref _m24, ref _m42);
            MathFunctions.Swap<float>(ref _m34, ref _m43);
        }
        #endregion

        #region Comparison Operators
        /// <summary>
        /// Tests whether two specified matrices are equal.
        /// </summary>
        /// <param name="left">A <see cref="Matrix4"/> instance.</param>
        /// <param name="right">A <see cref="Matrix4"/> instance.</param>
        /// <returns><see langword="true"/> if the two matrices are equal; otherwise, <see langword="false"/>.</returns>
        public static bool operator ==(Matrix4 left, Matrix4 right)
        {
            return ValueType.Equals(left, right);
        }
        /// <summary>
        /// Tests whether two specified matrices are not equal.
        /// </summary>
        /// <param name="left">A <see cref="Matrix4"/> instance.</param>
        /// <param name="right">A <see cref="Matrix4"/> instance.</param>
        /// <returns><see langword="true"/> if the two matrices are not equal; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(Matrix4 left, Matrix4 right)
        {
            return !ValueType.Equals(left, right);
        }
        #endregion

        #region Binary Operators
        /// <summary>
        /// Adds two matrices.
        /// </summary>
        /// <param name="left">A <see cref="Matrix4"/> instance.</param>
        /// <param name="right">A <see cref="Matrix4"/> instance.</param>
        /// <returns>A new <see cref="Matrix4"/> instance containing the sum.</returns>
        public static Matrix4 operator +(Matrix4 left, Matrix4 right)
        {
            return Matrix4.Add(left, right);
        }
        /// <summary>
        /// Adds a matrix and a scalar.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix4"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <returns>A new <see cref="Matrix4"/> instance containing the sum.</returns>
        public static Matrix4 operator +(Matrix4 matrix, float scalar)
        {
            return Matrix4.Add(matrix, scalar);
        }
        /// <summary>
        /// Adds a matrix and a scalar.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix4"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <returns>A new <see cref="Matrix4"/> instance containing the sum.</returns>
        public static Matrix4 operator +(float scalar, Matrix4 matrix)
        {
            return Matrix4.Add(matrix, scalar);
        }
        /// <summary>
        /// Subtracts a matrix from a matrix.
        /// </summary>
        /// <param name="left">A <see cref="Matrix4"/> instance.</param>
        /// <param name="right">A <see cref="Matrix4"/> instance.</param>
        /// <returns>A new <see cref="Matrix4"/> instance containing the difference.</returns>
        public static Matrix4 operator -(Matrix4 left, Matrix4 right)
        {
            return Matrix4.Subtract(left, right);
        }
        /// <summary>
        /// Subtracts a scalar from a matrix.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix4"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <returns>A new <see cref="Matrix4"/> instance containing the difference.</returns>
        public static Matrix4 operator -(Matrix4 matrix, float scalar)
        {
            return Matrix4.Subtract(matrix, scalar);
        }
        /// <summary>
        /// Multiplies two matrices.
        /// </summary>
        /// <param name="left">A <see cref="Matrix4"/> instance.</param>
        /// <param name="right">A <see cref="Matrix4"/> instance.</param>
        /// <returns>A new <see cref="Matrix4"/> instance containing the result.</returns>
        public static Matrix4 operator *(Matrix4 left, Matrix4 right)
        {
            return Matrix4.Multiply(left, right);
        }
        /// <summary>
        /// Transforms a given vector by a matrix.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix4"/> instance.</param>
        /// <param name="vector">A <see cref="Vector4"/> instance.</param>
        /// <returns>A new <see cref="Vector4"/> instance containing the result.</returns>
        public static Vector4 operator *(Matrix4 matrix, Vector4 vector)
        {
            Vector4 result = new Vector4();
            for (int r = 0; r < 4; ++r)
            {
                for (int c = 0; c < 4; ++c)
                {
                    result[r] += matrix[r, c] * vector[c];
                }
            }

            return result;
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
                if (index < 0 || index >= 16)
                    throw new IndexOutOfRangeException("Invalid matrix index!");

                fixed (float* f = &_m11)
                {
                    return *(f + index);
                }
            }
            set
            {
                if (index < 0 || index >= 16)
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
                return this[(row) * 4 + (column)];
            }
            set
            {
                this[(row) * 4 + (column)] = value;
            }
        }
        #endregion
    }
}
