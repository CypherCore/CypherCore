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
    /// Represents a 2-dimentional single-precision floating point matrix.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public struct Matrix2 : ICloneable
    {
        #region Private Fields
        private float _m11, _m12;
        private float _m21, _m22;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix2"/> structure with the specified values.
        /// </summary>
        public Matrix2(float m11, float m12, float m21, float m22)
        {
            _m11 = m11; _m12 = m12;
            _m21 = m21; _m22 = m22;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix2"/> structure with the specified values.
        /// </summary>
        /// <param name="elements">An array containing the matrix values in row-major order.</param>
        public Matrix2(float[] elements)
        {
            Debug.Assert(elements != null);
            Debug.Assert(elements.Length >= 4);

            _m11 = elements[0]; _m12 = elements[1];
            _m21 = elements[2]; _m22 = elements[3];
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix4"/> structure with the specified values.
        /// </summary>
        /// <param name="elements">An array containing the matrix values in row-major order.</param>
        public Matrix2(List<float> elements)
        {
            Debug.Assert(elements != null);
            Debug.Assert(elements.Count >= 4);

            _m11 = elements[0]; _m12 = elements[1];
            _m21 = elements[2]; _m22 = elements[3];
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix2"/> structure with the specified values.
        /// </summary>
        /// <param name="column1">A <see cref="Vector2"/> instance holding values for the first column.</param>
        /// <param name="column2">A <see cref="Vector2"/> instance holding values for the second column.</param>
        public Matrix2(Vector2 column1, Vector2 column2)
        {
            _m11 = column1.X; _m12 = column2.X;
            _m21 = column1.Y; _m22 = column2.Y;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix2"/> class using a given matrix.
        /// </summary>
        public Matrix2(Matrix2 m)
        {
            _m11 = m.M11; _m12 = m.M12;
            _m21 = m.M21; _m22 = m.M22;
        }
        #endregion

        #region Constants
        /// <summary>
        /// 4-dimentional single-precision floating point zero matrix.
        /// </summary>
        public static readonly Matrix2 Zero = new Matrix2(0, 0, 0, 0);
        /// <summary>
        /// 4-dimentional single-precision floating point identity matrix.
        /// </summary>
        public static readonly Matrix2 Identity = new Matrix2(1, 0, 0, 1);
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
        /// Gets the matrix's trace value.
        /// </summary>
        /// <value>A single-precision floating-point number.</value>
        public float Trace
        {
            get
            {
                return _m11 + _m22;
            }
        }
        #endregion

        #region ICloneable Members
        /// <summary>
        /// Creates an exact copy of this <see cref="Matrix2"/> object.
        /// </summary>
        /// <returns>The <see cref="Matrix2"/> object this method creates, cast as an object.</returns>
        object ICloneable.Clone()
        {
            return new Matrix2(this);
        }
        /// <summary>
        /// Creates an exact copy of this <see cref="Matrix2"/> object.
        /// </summary>
        /// <returns>The <see cref="Matrix2"/> object this method creates.</returns>
        public Matrix2 Clone()
        {
            return new Matrix2(this);
        }
        #endregion

        #region Public Static Parse Methods
        private const string regularExp = @"2x2\s*\[(?<m11>.*),(?<m12>.*),(?<m21>.*),(?<m22>.*)\]";
        /// <summary>
        /// Converts the specified string to its <see cref="Matrix2"/> equivalent.
        /// </summary>
        /// <param name="value">A string representation of a <see cref="Matrix2"/>.</param>
        /// <returns>A <see cref="Matrix2"/> that represents the vector specified by the <paramref name="value"/> parameter.</returns>
        /// <remarks>
        /// The string should be in the following form: "2x2..matrix elements..>".<br/>
        /// Exmaple : "2x2[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16]"
        /// </remarks>
        public static Matrix2 Parse(string value)
        {
            Regex r = new Regex(regularExp, RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
            Match m = r.Match(value);
            if (m.Success)
            {
                return new Matrix2(
                    float.Parse(m.Result("${m11}")),
                    float.Parse(m.Result("${m12}")),

                    float.Parse(m.Result("${m21}")),
                    float.Parse(m.Result("${m22}"))
                    );
            }
            else
            {
                throw new Exception("Unsuccessful Match.");
            }
        }
        /// <summary>
        /// Converts the specified string to its <see cref="Matrix2"/> equivalent.
        /// A return value indicates whether the conversion succeeded or failed.
        /// </summary>
        /// <param name="value">A string representation of a <see cref="Matrix2"/>.</param>
        /// <param name="result">
        /// When this method returns, if the conversion succeeded,
        /// contains a <see cref="Matrix2"/> representing the vector specified by <paramref name="value"/>.
        /// </param>
        /// <returns><see langword="true"/> if value was converted successfully; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// The string should be in the following form: "2x2..matrix elements..>".<br/>
        /// Exmaple : "2x2[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16]"
        /// </remarks>
        public static bool TryParse(string value, out Matrix2 result)
        {
            Regex r = new Regex(regularExp, RegexOptions.Singleline);
            Match m = r.Match(value);
            if (m.Success)
            {
                result = new Matrix2(
                    float.Parse(m.Result("${m11}")),
                    float.Parse(m.Result("${m12}")),

                    float.Parse(m.Result("${m21}")),
                    float.Parse(m.Result("${m22}"))
                    );

                return true;
            }

            result = Matrix2.Zero;
            return false;
        }
        #endregion

        #region Public Static Matrix Arithmetics
        /// <summary>
        /// Adds two matrices.
        /// </summary>
        /// <param name="left">A <see cref="Matrix2"/> instance.</param>
        /// <param name="right">A <see cref="Matrix2"/> instance.</param>
        /// <returns>A new <see cref="Matrix2"/> instance containing the sum.</returns>
        public static Matrix2 Add(Matrix2 left, Matrix2 right)
        {
            return new Matrix2(
                left.M11 + right.M11, left.M12 + right.M12,
                left.M21 + right.M21, left.M22 + right.M22
                );
        }
        /// <summary>
        /// Adds a matrix and a scalar.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix2"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <returns>A new <see cref="Matrix2"/> instance containing the sum.</returns>
        public static Matrix2 Add(Matrix2 matrix, float scalar)
        {
            return new Matrix2(
                matrix.M11 + scalar, matrix.M12 + scalar,
                matrix.M21 + scalar, matrix.M22 + scalar
                );
        }
        /// <summary>
        /// Adds two matrices and put the result in a third matrix.
        /// </summary>
        /// <param name="left">A <see cref="Matrix2"/> instance.</param>
        /// <param name="right">A <see cref="Matrix2"/> instance.</param>
        /// <param name="result">A <see cref="Matrix2"/> instance to hold the result.</param>
        public static void Add(Matrix2 left, Matrix2 right, ref Matrix2 result)
        {
            result.M11 = left.M11 + right.M11;
            result.M12 = left.M12 + right.M12;

            result.M21 = left.M21 + right.M21;
            result.M22 = left.M22 + right.M22;
        }
        /// <summary>
        /// Adds a matrix and a scalar and put the result in a third matrix.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix2"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <param name="result">A <see cref="Matrix2"/> instance to hold the result.</param>
        public static void Add(Matrix2 matrix, float scalar, ref Matrix2 result)
        {
            result.M11 = matrix.M11 + scalar;
            result.M12 = matrix.M12 + scalar;

            result.M21 = matrix.M21 + scalar;
            result.M22 = matrix.M22 + scalar;
        }
        /// <summary>
        /// Subtracts a matrix from a matrix.
        /// </summary>
        /// <param name="left">A <see cref="Matrix2"/> instance to subtract from.</param>
        /// <param name="right">A <see cref="Matrix2"/> instance to subtract.</param>
        /// <returns>A new <see cref="Matrix2"/> instance containing the difference.</returns>
        /// <remarks>result[x][y] = left[x][y] - right[x][y]</remarks>
        public static Matrix2 Subtract(Matrix2 left, Matrix2 right)
        {
            return new Matrix2(
                left.M11 - right.M11, left.M12 - right.M12,
                left.M21 - right.M21, left.M22 - right.M22
                );
        }
        /// <summary>
        /// Subtracts a scalar from a matrix.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix2"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <returns>A new <see cref="Matrix2"/> instance containing the difference.</returns>
        public static Matrix2 Subtract(Matrix2 matrix, float scalar)
        {
            return new Matrix2(
                matrix.M11 - scalar, matrix.M12 - scalar,
                matrix.M21 - scalar, matrix.M22 - scalar
                );
        }
        /// <summary>
        /// Subtracts a matrix from a matrix and put the result in a third matrix.
        /// </summary>
        /// <param name="left">A <see cref="Matrix2"/> instance to subtract from.</param>
        /// <param name="right">A <see cref="Matrix2"/> instance to subtract.</param>
        /// <param name="result">A <see cref="Matrix2"/> instance to hold the result.</param>
        /// <remarks>result[x][y] = left[x][y] - right[x][y]</remarks>
        public static void Subtract(Matrix2 left, Matrix2 right, ref Matrix2 result)
        {
            result.M11 = left.M11 - right.M11;
            result.M12 = left.M12 - right.M12;

            result.M21 = left.M21 - right.M21;
            result.M22 = left.M22 - right.M22;
        }
        /// <summary>
        /// Subtracts a scalar from a matrix and put the result in a third matrix.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix2"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <param name="result">A <see cref="Matrix2"/> instance to hold the result.</param>
        public static void Subtract(Matrix2 matrix, float scalar, ref Matrix2 result)
        {
            result.M11 = matrix.M11 - scalar;
            result.M12 = matrix.M12 - scalar;

            result.M21 = matrix.M21 - scalar;
            result.M22 = matrix.M22 - scalar;
        }
        /// <summary>
        /// Multiplies two matrices.
        /// </summary>
        /// <param name="left">A <see cref="Matrix2"/> instance.</param>
        /// <param name="right">A <see cref="Matrix2"/> instance.</param>
        /// <returns>A new <see cref="Matrix2"/> instance containing the result.</returns>
        public static Matrix2 Multiply(Matrix2 left, Matrix2 right)
        {
            return new Matrix2(
                left.M11 * right.M11 + left.M12 * right.M21,
                left.M11 * right.M12 + left.M12 * right.M22,
                left.M21 * right.M11 + left.M22 * right.M21,
                left.M21 * right.M12 + left.M22 * right.M22
                );
        }
        /// <summary>
        /// Multiplies two matrices and put the result in a third matrix.
        /// </summary>
        /// <param name="left">A <see cref="Matrix2"/> instance.</param>
        /// <param name="right">A <see cref="Matrix2"/> instance.</param>
        /// <param name="result">A <see cref="Matrix2"/> instance to hold the result.</param>
        public static void Multiply(Matrix2 left, Matrix2 right, ref Matrix2 result)
        {
            result.M11 = left.M11 * right.M11 + left.M12 * right.M21;
            result.M12 = left.M11 * right.M12 + left.M12 * right.M22;

            result.M21 = left.M21 * right.M11 + left.M22 * right.M21;
            result.M22 = left.M21 * right.M12 + left.M22 * right.M22;
        }
        /// <summary>
        /// Transforms a given vector by a matrix.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix2"/> instance.</param>
        /// <param name="vector">A <see cref="Vector2"/> instance.</param>
        /// <returns>A new <see cref="Vector2"/> instance containing the result.</returns>
        public static Vector2 Transform(Matrix2 matrix, Vector2 vector)
        {
            return new Vector2(
                (matrix.M11 * vector.X) + (matrix.M12 * vector.Y),
                (matrix.M21 * vector.X) + (matrix.M22 * vector.Y));
        }
        /// <summary>
        /// Transforms a given vector by a matrix and put the result in a vector.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix2"/> instance.</param>
        /// <param name="vector">A <see cref="Vector2"/> instance.</param>
        /// <param name="result">A <see cref="Vector2"/> instance to hold the result.</param>
        public static void Transform(Matrix2 matrix, Vector2 vector, ref Vector2 result)
        {
            result.X = (matrix.M11 * vector.X) + (matrix.M12 * vector.Y);
            result.Y = (matrix.M21 * vector.X) + (matrix.M22 * vector.Y);
        }
        /// <summary>
        /// Transposes a matrix.
        /// </summary>
        /// <param name="m">A <see cref="Matrix2"/> instance.</param>
        /// <returns>A new <see cref="Matrix2"/> instance containing the transposed matrix.</returns>
        public static Matrix2 Transpose(Matrix2 m)
        {
            Matrix2 t = new Matrix2(m);
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
                _m11.GetHashCode() ^ _m12.GetHashCode() ^
                _m21.GetHashCode() ^ _m22.GetHashCode();
        }
        /// <summary>
        /// Returns a value indicating whether this instance is equal to
        /// the specified object.
        /// </summary>
        /// <param name="obj">An object to compare to this instance.</param>
        /// <returns><see langword="true"/> if <paramref name="obj"/> is a <see cref="Matrix2"/> and has the same values as this instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object obj)
        {
            if (obj is Matrix2)
            {
                Matrix2 m = (Matrix2)obj;
                return
                    (_m11 == m.M11) && (_m12 == m.M12) &&
                    (_m21 == m.M21) && (_m22 == m.M22);
            }
            return false;
        }
        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        /// <returns>A string representation of this object.</returns>
        public override string ToString()
        {
            return $"2x2[{_m11}, {_m12}, {_m21}, {_m22}]";
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Calculates the determinant value of the matrix.
        /// </summary>
        /// <returns>The determinant value of the matrix.</returns>
        public float GetDeterminant()
        {
            return (_m11 * _m22) - (_m12 * _m21);
        }
        /// <summary>
        /// Transposes this matrix.
        /// </summary>
        public void Transpose()
        {
            MathFunctions.Swap(ref _m12, ref _m21);
        }
        #endregion

        #region Comparison Operators
        /// <summary>
        /// Tests whether two specified matrices are equal.
        /// </summary>
        /// <param name="left">A <see cref="Matrix2"/> instance.</param>
        /// <param name="right">A <see cref="Matrix2"/> instance.</param>
        /// <returns><see langword="true"/> if the two matrices are equal; otherwise, <see langword="false"/>.</returns>
        public static bool operator ==(Matrix2 left, Matrix2 right)
        {
            return ValueType.Equals(left, right);
        }
        /// <summary>
        /// Tests whether two specified matrices are not equal.
        /// </summary>
        /// <param name="left">A <see cref="Matrix2"/> instance.</param>
        /// <param name="right">A <see cref="Matrix2"/> instance.</param>
        /// <returns><see langword="true"/> if the two matrices are not equal; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(Matrix2 left, Matrix2 right)
        {
            return !ValueType.Equals(left, right);
        }
        #endregion

        #region Binary Operators
        /// <summary>
        /// Adds two matrices.
        /// </summary>
        /// <param name="left">A <see cref="Matrix2"/> instance.</param>
        /// <param name="right">A <see cref="Matrix2"/> instance.</param>
        /// <returns>A new <see cref="Matrix2"/> instance containing the sum.</returns>
        public static Matrix2 operator +(Matrix2 left, Matrix2 right)
        {
            return Matrix2.Add(left, right);
        }
        /// <summary>
        /// Adds a matrix and a scalar.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix2"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <returns>A new <see cref="Matrix2"/> instance containing the sum.</returns>
        public static Matrix2 operator +(Matrix2 matrix, float scalar)
        {
            return Matrix2.Add(matrix, scalar);
        }
        /// <summary>
        /// Adds a matrix and a scalar.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix2"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <returns>A new <see cref="Matrix2"/> instance containing the sum.</returns>
        public static Matrix2 operator +(float scalar, Matrix2 matrix)
        {
            return Matrix2.Add(matrix, scalar);
        }
        /// <summary>
        /// Subtracts a matrix from a matrix.
        /// </summary>
        /// <param name="left">A <see cref="Matrix2"/> instance.</param>
        /// <param name="right">A <see cref="Matrix2"/> instance.</param>
        /// <returns>A new <see cref="Matrix2"/> instance containing the difference.</returns>
        public static Matrix2 operator -(Matrix2 left, Matrix2 right)
        {
            return Matrix2.Subtract(left, right);
        }
        /// <summary>
        /// Subtracts a scalar from a matrix.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix2"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <returns>A new <see cref="Matrix2"/> instance containing the difference.</returns>
        public static Matrix2 operator -(Matrix2 matrix, float scalar)
        {
            return Matrix2.Subtract(matrix, scalar);
        }
        /// <summary>
        /// Multiplies two matrices.
        /// </summary>
        /// <param name="left">A <see cref="Matrix2"/> instance.</param>
        /// <param name="right">A <see cref="Matrix2"/> instance.</param>
        /// <returns>A new <see cref="Matrix2"/> instance containing the result.</returns>
        public static Matrix2 operator *(Matrix2 left, Matrix2 right)
        {
            return Matrix2.Multiply(left, right);
        }
        /// <summary>
        /// Transforms a given vector by a matrix.
        /// </summary>
        /// <param name="matrix">A <see cref="Matrix2"/> instance.</param>
        /// <param name="vector">A <see cref="Vector2"/> instance.</param>
        /// <returns>A new <see cref="Vector2"/> instance containing the result.</returns>
        public static Vector2 operator *(Matrix2 matrix, Vector2 vector)
        {
            return Matrix2.Transform(matrix, vector);
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
                if (index < 0 || index >= 4)
                    throw new IndexOutOfRangeException("Invalid matrix index!");

                fixed (float* f = &_m11)
                {
                    return *(f + index);
                }
            }
            set
            {
                if (index < 0 || index >= 4)
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
                return this[(row - 1) * 2 + (column - 1)];
            }
            set
            {
                this[(row - 1) * 2 + (column - 1)] = value;
            }
        }
        #endregion
    }
}
