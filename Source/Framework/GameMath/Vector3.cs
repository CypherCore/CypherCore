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
    /// Represents 3-Dimentional vector of single-precision floating point numbers.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    //[TypeConverter(typeof(Vector3Converter))]
    public struct Vector3 : ICloneable
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Vector3"/> class with the specified coordinates.
        /// </summary>
        /// <param name="x">The vector's X coordinate.</param>
        /// <param name="y">The vector's Y coordinate.</param>
        /// <param name="z">The vector's Z coordinate.</param>
        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Vector3"/> class with the specified coordinates.
        /// </summary>
        /// <param name="coordinates">An array containing the coordinate parameters.</param>
        public Vector3(float[] coordinates)
        {
            Debug.Assert(coordinates != null);
            Debug.Assert(coordinates.Length >= 3);

            X = coordinates[0];
            Y = coordinates[1];
            Z = coordinates[2];
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Vector3"/> class with the specified coordinates.
        /// </summary>
        /// <param name="coordinates">An array containing the coordinate parameters.</param>
        public Vector3(List<float> coordinates)
        {
            Debug.Assert(coordinates != null);
            Debug.Assert(coordinates.Count >= 3);

            X = coordinates[0];
            Y = coordinates[1];
            Z = coordinates[2];
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Vector3"/> class using coordinates from a given <see cref="Vector3"/> instance.
        /// </summary>
        /// <param name="vector">A <see cref="Vector3"/> to get the coordinates from.</param>
        public Vector3(Vector3 vector)
        {
            X = vector.X;
            Y = vector.Y;
            Z = vector.Z;
        }
        #endregion

        #region Constants
        /// <summary>
        /// 4-Dimentional single-precision floating point zero vector.
        /// </summary>
        public static readonly Vector3 Zero = new Vector3(0.0f, 0.0f, 0.0f);

        public static readonly Vector3 One = new Vector3(1.0f, 1.0f, 1.0f);

        public static readonly Vector3 Inf = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        /// <summary>
        /// 4-Dimentional single-precision floating point X-Axis vector.
        /// </summary>
        public static readonly Vector3 XAxis = new Vector3(1.0f, 0.0f, 0.0f);
        /// <summary>
        /// 4-Dimentional single-precision floating point Y-Axis vector.
        /// </summary>
        public static readonly Vector3 YAxis = new Vector3(0.0f, 1.0f, 0.0f);
        /// <summary>
        /// 4-Dimentional single-precision floating point Y-Axis vector.
        /// </summary>
        public static readonly Vector3 ZAxis = new Vector3(0.0f, 0.0f, 1.0f);
        #endregion

        #region Public properties
        /// <summary>
        /// Gets or sets the x-coordinate of this vector.
        /// </summary>
        /// <value>The x-coordinate of this vector.</value>
        public float X;
        /// <summary>
        /// Gets or sets the y-coordinate of this vector.
        /// </summary>
        /// <value>The y-coordinate of this vector.</value>
        public float Y;
        /// <summary>
        /// Gets or sets the z-coordinate of this vector.
        /// </summary>
        /// <value>The z-coordinate of this vector.</value>
        public float Z;
        #endregion

        #region ICloneable Members
        /// <summary>
        /// Creates an exact copy of this <see cref="Vector3"/> object.
        /// </summary>
        /// <returns>The <see cref="Vector3"/> object this method creates, cast as an object.</returns>
        object ICloneable.Clone()
        {
            return new Vector3(this);
        }
        /// <summary>
        /// Creates an exact copy of this <see cref="Vector3"/> object.
        /// </summary>
        /// <returns>The <see cref="Vector3"/> object this method creates.</returns>
        public Vector3 Clone()
        {
            return new Vector3(this);
        }
        #endregion

        #region Public Static Parse Methods
        /// <summary>
        /// Converts the specified string to its <see cref="Vector3"/> equivalent.
        /// </summary>
        /// <param name="value">A string representation of a <see cref="Vector3"/>.</param>
        /// <returns>A <see cref="Vector3"/> that represents the vector specified by the <paramref name="value"/> parameter.</returns>
        public static Vector3 Parse(string value)
        {
            Regex r = new Regex(@"\((?<x>.*),(?<y>.*),(?<z>.*)\)", RegexOptions.Singleline);
            Match m = r.Match(value);
            if (m.Success)
            {
                return new Vector3(
                    float.Parse(m.Result("${x}")),
                    float.Parse(m.Result("${y}")),
                    float.Parse(m.Result("${z}"))
                    );
            }
            else
            {
                throw new Exception("Unsuccessful Match.");
            }
        }
        /// <summary>
        /// Converts the specified string to its <see cref="Vector3"/> equivalent.
        /// A return value indicates whether the conversion succeeded or failed.
        /// </summary>
        /// <param name="value">A string representation of a <see cref="Vector3"/>.</param>
        /// <param name="result">
        /// When this method returns, if the conversion succeeded,
        /// contains a <see cref="Vector3"/> representing the vector specified by <paramref name="value"/>.
        /// </param>
        /// <returns><see langword="true"/> if value was converted successfully; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(string value, out Vector3 result)
        {
            Regex r = new Regex(@"\((?<x>.*),(?<y>.*),(?<z>.*)\)", RegexOptions.Singleline);
            Match m = r.Match(value);
            if (m.Success)
            {
                result = new Vector3(
                    float.Parse(m.Result("${x}")),
                    float.Parse(m.Result("${y}")),
                    float.Parse(m.Result("${z}"))
                    );

                return true;
            }

            result = Vector3.Zero;
            return false;
        }
        #endregion

        #region Public Static Vector Arithmetics
        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="left">A <see cref="Vector3"/> instance.</param>
        /// <param name="right">A <see cref="Vector3"/> instance.</param>
        /// <returns>A new <see cref="Vector3"/> instance containing the sum.</returns>
        public static Vector3 Add(Vector3 left, Vector3 right)
        {
            return new Vector3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        }
        /// <summary>
        /// Adds a vector and a scalar.
        /// </summary>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <returns>A new <see cref="Vector3"/> instance containing the sum.</returns>
        public static Vector3 Add(Vector3 vector, float scalar)
        {
            return new Vector3(vector.X + scalar, vector.Y + scalar, vector.Z + scalar);
        }
        /// <summary>
        /// Adds two vectors and put the result in the third vector.
        /// </summary>
        /// <param name="left">A <see cref="Vector3"/> instance.</param>
        /// <param name="right">A <see cref="Vector3"/> instance</param>
        /// <param name="result">A <see cref="Vector3"/> instance to hold the result.</param>
        public static void Add(Vector3 left, Vector3 right, ref Vector3 result)
        {
            result.X = left.X + right.X;
            result.Y = left.Y + right.Y;
            result.Z = left.Z + right.Z;
        }
        /// <summary>
        /// Adds a vector and a scalar and put the result into another vector.
        /// </summary>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <param name="result">A <see cref="Vector3"/> instance to hold the result.</param>
        public static void Add(Vector3 vector, float scalar, ref Vector3 result)
        {
            result.X = vector.X + scalar;
            result.Y = vector.Y + scalar;
            result.Z = vector.Z + scalar;
        }
        /// <summary>
        /// Subtracts a vector from a vector.
        /// </summary>
        /// <param name="left">A <see cref="Vector3"/> instance.</param>
        /// <param name="right">A <see cref="Vector3"/> instance.</param>
        /// <returns>A new <see cref="Vector3"/> instance containing the difference.</returns>
        /// <remarks>
        ///	result[i] = left[i] - right[i].
        /// </remarks>
        public static Vector3 Subtract(Vector3 left, Vector3 right)
        {
            return new Vector3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        }
        /// <summary>
        /// Subtracts a scalar from a vector.
        /// </summary>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <returns>A new <see cref="Vector3"/> instance containing the difference.</returns>
        /// <remarks>
        /// result[i] = vector[i] - scalar
        /// </remarks>
        public static Vector3 Subtract(Vector3 vector, float scalar)
        {
            return new Vector3(vector.X - scalar, vector.Y - scalar, vector.Z - scalar);
        }
        /// <summary>
        /// Subtracts a vector from a scalar.
        /// </summary>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <returns>A new <see cref="Vector3"/> instance containing the difference.</returns>
        /// <remarks>
        /// result[i] = scalar - vector[i]
        /// </remarks>
        public static Vector3 Subtract(float scalar, Vector3 vector)
        {
            return new Vector3(scalar - vector.X, scalar - vector.Y, scalar - vector.Z);
        }
        /// <summary>
        /// Subtracts a vector from a second vector and puts the result into a third vector.
        /// </summary>
        /// <param name="left">A <see cref="Vector3"/> instance.</param>
        /// <param name="right">A <see cref="Vector3"/> instance</param>
        /// <param name="result">A <see cref="Vector3"/> instance to hold the result.</param>
        /// <remarks>
        ///	result[i] = left[i] - right[i].
        /// </remarks>
        public static void Subtract(Vector3 left, Vector3 right, ref Vector3 result)
        {
            result.X = left.X - right.X;
            result.Y = left.Y - right.Y;
            result.Z = left.Z - right.Z;
        }
        /// <summary>
        /// Subtracts a vector from a scalar and put the result into another vector.
        /// </summary>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <param name="result">A <see cref="Vector3"/> instance to hold the result.</param>
        /// <remarks>
        /// result[i] = vector[i] - scalar
        /// </remarks>
        public static void Subtract(Vector3 vector, float scalar, ref Vector3 result)
        {
            result.X = vector.X - scalar;
            result.Y = vector.Y - scalar;
            result.Z = vector.Z - scalar;
        }
        /// <summary>
        /// Subtracts a scalar from a vector and put the result into another vector.
        /// </summary>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <param name="result">A <see cref="Vector3"/> instance to hold the result.</param>
        /// <remarks>
        /// result[i] = scalar - vector[i]
        /// </remarks>
        public static void Subtract(float scalar, Vector3 vector, ref Vector3 result)
        {
            result.X = scalar - vector.X;
            result.Y = scalar - vector.Y;
            result.Z = scalar - vector.Z;
        }
        /// <summary>
        /// Divides a vector by another vector.
        /// </summary>
        /// <param name="left">A <see cref="Vector3"/> instance.</param>
        /// <param name="right">A <see cref="Vector3"/> instance.</param>
        /// <returns>A new <see cref="Vector3"/> containing the quotient.</returns>
        /// <remarks>
        ///	result[i] = left[i] / right[i].
        /// </remarks>
        public static Vector3 Divide(Vector3 left, Vector3 right)
        {
            return new Vector3(left.X / right.X, left.Y / right.Y, left.Z / right.Z);
        }
        /// <summary>
        /// Divides a vector by a scalar.
        /// </summary>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <param name="scalar">A scalar</param>
        /// <returns>A new <see cref="Vector3"/> containing the quotient.</returns>
        /// <remarks>
        /// result[i] = vector[i] / scalar;
        /// </remarks>
        public static Vector3 Divide(Vector3 vector, float scalar)
        {
            return new Vector3(vector.X / scalar, vector.Y / scalar, vector.Z / scalar);
        }
        /// <summary>
        /// Divides a scalar by a vector.
        /// </summary>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <param name="scalar">A scalar</param>
        /// <returns>A new <see cref="Vector3"/> containing the quotient.</returns>
        /// <remarks>
        /// result[i] = scalar / vector[i]
        /// </remarks>
        public static Vector3 Divide(float scalar, Vector3 vector)
        {
            return new Vector3(scalar / vector.X, scalar / vector.Y, scalar / vector.Z);
        }
        /// <summary>
        /// Divides a vector by another vector.
        /// </summary>
        /// <param name="left">A <see cref="Vector3"/> instance.</param>
        /// <param name="right">A <see cref="Vector3"/> instance.</param>
        /// <param name="result">A <see cref="Vector3"/> instance to hold the result.</param>
        /// <remarks>
        /// result[i] = left[i] / right[i]
        /// </remarks>
        public static void Divide(Vector3 left, Vector3 right, ref Vector3 result)
        {
            result.X = left.X / right.X;
            result.Y = left.Y / right.Y;
            result.Z = left.Z / right.Z;
        }
        /// <summary>
        /// Divides a vector by a scalar.
        /// </summary>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <param name="scalar">A scalar</param>
        /// <param name="result">A <see cref="Vector3"/> instance to hold the result.</param>
        /// <remarks>
        /// result[i] = vector[i] / scalar
        /// </remarks>
        public static void Divide(Vector3 vector, float scalar, ref Vector3 result)
        {
            result.X = vector.X / scalar;
            result.Y = vector.Y / scalar;
            result.Z = vector.Z / scalar;
        }
        /// <summary>
        /// Divides a scalar by a vector.
        /// </summary>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <param name="scalar">A scalar</param>
        /// <param name="result">A <see cref="Vector3"/> instance to hold the result.</param>
        /// <remarks>
        /// result[i] = scalar / vector[i]
        /// </remarks>
        public static void Divide(float scalar, Vector3 vector, ref Vector3 result)
        {
            result.X = scalar / vector.X;
            result.Y = scalar / vector.Y;
            result.Z = scalar / vector.Z;
        }
        /// <summary>
        /// Multiplies a vector by a scalar.
        /// </summary>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <returns>A new <see cref="Vector3"/> containing the result.</returns>
        public static Vector3 Multiply(Vector3 vector, float scalar)
        {
            return new Vector3(vector.X * scalar, vector.Y * scalar, vector.Z * scalar);
        }
        /// <summary>
        /// Multiplies a vector by a scalar and put the result in another vector.
        /// </summary>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <param name="result">A <see cref="Vector3"/> instance to hold the result.</param>
        public static void Multiply(Vector3 vector, float scalar, ref Vector3 result)
        {
            result.X = vector.X * scalar;
            result.Y = vector.Y * scalar;
            result.Z = vector.Z * scalar;
        }
        /// <summary>
        /// Calculates the dot product of two vectors.
        /// </summary>
        /// <param name="left">A <see cref="Vector3"/> instance.</param>
        /// <param name="right">A <see cref="Vector3"/> instance.</param>
        /// <returns>The dot product value.</returns>
        public static float DotProduct(Vector3 left, Vector3 right)
        {
            return (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z);
        }
        /// <summary>
        /// Calculates the cross product of two vectors.
        /// </summary>
        /// <param name="left">A <see cref="Vector3"/> instance.</param>
        /// <param name="right">A <see cref="Vector3"/> instance.</param>
        /// <returns>A new <see cref="Vector3"/> containing the cross product result.</returns>
        public static Vector3 CrossProduct(Vector3 left, Vector3 right)
        {
            return new Vector3(
                left.Y * right.Z - left.Z * right.Y,
                left.Z * right.X - left.X * right.Z,
                left.X * right.Y - left.Y * right.X);
        }
        /// <summary>
        /// Calculates the cross product of two vectors.
        /// </summary>
        /// <param name="left">A <see cref="Vector3"/> instance.</param>
        /// <param name="right">A <see cref="Vector3"/> instance.</param>
        /// <param name="result">A <see cref="Vector3"/> instance to hold the cross product result.</param>
        public static void CrossProduct(Vector3 left, Vector3 right, ref Vector3 result)
        {
            result.X = left.Y * right.Z - left.Z * right.Y;
            result.Y = left.Z * right.X - left.X * right.Z;
            result.Z = left.X * right.Y - left.Y * right.X;
        }
        /// <summary>
        /// Negates a vector.
        /// </summary>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <returns>A new <see cref="Vector3"/> instance containing the negated values.</returns>
        public static Vector3 Negate(Vector3 vector)
        {
            return new Vector3(-vector.X, -vector.Y, -vector.Z);
        }
        /// <summary>
        /// Tests whether two vectors are approximately equal using default tolerance value.
        /// </summary>
        /// <param name="left">A <see cref="Vector3"/> instance.</param>
        /// <param name="right">A <see cref="Vector3"/> instance.</param>
        /// <returns><see langword="true"/> if the two vectors are approximately equal; otherwise, <see langword="false"/>.</returns>
        public static bool ApproxEqual(Vector3 left, Vector3 right)
        {
            return ApproxEqual(left, right, MathFunctions.Epsilon);
        }
        /// <summary>
        /// Tests whether two vectors are approximately equal given a tolerance value.
        /// </summary>
        /// <param name="left">A <see cref="Vector3"/> instance.</param>
        /// <param name="right">A <see cref="Vector3"/> instance.</param>
        /// <param name="tolerance">The tolerance value used to test approximate equality.</param>
        /// <returns><see langword="true"/> if the two vectors are approximately equal; otherwise, <see langword="false"/>.</returns>
        public static bool ApproxEqual(Vector3 left, Vector3 right, float tolerance)
        {
            return
                (
                (System.Math.Abs(left.X - right.X) <= tolerance) &&
                (System.Math.Abs(left.Y - right.Y) <= tolerance) &&
                (System.Math.Abs(left.Z - right.Z) <= tolerance)
                );
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Scale the vector so that its length is 1.
        /// </summary>
        public void Normalize()
        {
            float length = GetLength();
            if (length == 0)
            {
                throw new DivideByZeroException("Trying to normalize a vector with length of zero.");
            }

            X /= length;
            Y /= length;
            Z /= length;
        }
        /// <summary>
        /// Calculates the length of the vector.
        /// </summary>
        /// <returns>Returns the length of the vector. (Sqrt(X*X + Y*Y))</returns>
        public float GetLength()
        {
            return (float)System.Math.Sqrt(X * X + Y * Y + Z * Z);
        }
        /// <summary>
        /// Calculates the squared length of the vector.
        /// </summary>
        /// <returns>Returns the squared length of the vector. (X*X + Y*Y)</returns>
        public float GetLengthSquared()
        {
            return (X * X + Y * Y + Z * Z);
        }
        /// <summary>
        /// Clamps vector values to zero using a given tolerance value.
        /// </summary>
        /// <param name="tolerance">The tolerance to use.</param>
        /// <remarks>
        /// The vector values that are close to zero within the given tolerance are set to zero.
        /// </remarks>
        public void ClampZero(float tolerance)
        {
            X = MathFunctions.Clamp(X, 0, tolerance);
            Y = MathFunctions.Clamp(Y, 0, tolerance);
            Z = MathFunctions.Clamp(Z, 0, tolerance);
        }
        /// <summary>
        /// Clamps vector values to zero using the default tolerance value.
        /// </summary>
        /// <remarks>
        /// The vector values that are close to zero within the given tolerance are set to zero.
        /// The tolerance value used is <see cref="MathFunctions.Epsilon"/>
        /// </remarks>
        public void ClampZero()
        {
            X = MathFunctions.Clamp(X, 0);
            Y = MathFunctions.Clamp(Y, 0);
            Z = MathFunctions.Clamp(Z, 0);
        }
        #endregion

        #region System.Object Overrides
        /// <summary>
        /// Returns the hashcode for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        }
        /// <summary>
        /// Returns a value indicating whether this instance is equal to
        /// the specified object.
        /// </summary>
        /// <param name="obj">An object to compare to this instance.</param>
        /// <returns><see langword="true"/> if <paramref name="obj"/> is a <see cref="Vector3"/> and has the same values as this instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object obj)
        {
            if (obj is Vector3)
            {
                Vector3 v = (Vector3)obj;
                return (X == v.X) && (Y == v.Y) && (Z == v.Z);
            }
            return false;
        }
        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        /// <returns>A string representation of this object.</returns>
        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
        #endregion

        #region Comparison Operators
        /// <summary>
        /// Tests whether two specified vectors are equal.
        /// </summary>
        /// <param name="left">A <see cref="Vector3"/> instance.</param>
        /// <param name="right">A <see cref="Vector3"/> instance.</param>
        /// <returns><see langword="true"/> if the two vectors are equal; otherwise, <see langword="false"/>.</returns>
        public static bool operator ==(Vector3 left, Vector3 right)
        {
            return ValueType.Equals(left, right);
        }
        /// <summary>
        /// Tests whether two specified vectors are not equal.
        /// </summary>
        /// <param name="left">A <see cref="Vector3"/> instance.</param>
        /// <param name="right">A <see cref="Vector3"/> instance.</param>
        /// <returns><see langword="true"/> if the two vectors are not equal; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(Vector3 left, Vector3 right)
        {
            return !ValueType.Equals(left, right);
        }

        /// <summary>
        /// Tests if a vector's components are greater than another vector's components.
        /// </summary>
        /// <param name="left">A <see cref="Vector3"/> instance.</param>
        /// <param name="right">A <see cref="Vector3"/> instance.</param>
        /// <returns><see langword="true"/> if the left-hand vector's components are greater than the right-hand vector's component; otherwise, <see langword="false"/>.</returns>
        public static bool operator >(Vector3 left, Vector3 right)
        {
            return (
                (left.X > right.X) &&
                (left.Y > right.Y) &&
                (left.Z > right.Z));
        }
        /// <summary>
        /// Tests if a vector's components are smaller than another vector's components.
        /// </summary>
        /// <param name="left">A <see cref="Vector3"/> instance.</param>
        /// <param name="right">A <see cref="Vector3"/> instance.</param>
        /// <returns><see langword="true"/> if the left-hand vector's components are smaller than the right-hand vector's component; otherwise, <see langword="false"/>.</returns>
        public static bool operator <(Vector3 left, Vector3 right)
        {
            return (
                (left.X < right.X) &&
                (left.Y < right.Y) &&
                (left.Z < right.Z));
        }
        /// <summary>
        /// Tests if a vector's components are greater or equal than another vector's components.
        /// </summary>
        /// <param name="left">A <see cref="Vector3"/> instance.</param>
        /// <param name="right">A <see cref="Vector3"/> instance.</param>
        /// <returns><see langword="true"/> if the left-hand vector's components are greater or equal than the right-hand vector's component; otherwise, <see langword="false"/>.</returns>
        public static bool operator >=(Vector3 left, Vector3 right)
        {
            return (
                (left.X >= right.X) &&
                (left.Y >= right.Y) &&
                (left.Z >= right.Z));
        }
        /// <summary>
        /// Tests if a vector's components are smaller or equal than another vector's components.
        /// </summary>
        /// <param name="left">A <see cref="Vector3"/> instance.</param>
        /// <param name="right">A <see cref="Vector3"/> instance.</param>
        /// <returns><see langword="true"/> if the left-hand vector's components are smaller or equal than the right-hand vector's component; otherwise, <see langword="false"/>.</returns>
        public static bool operator <=(Vector3 left, Vector3 right)
        {
            return (
                (left.X <= right.X) &&
                (left.Y <= right.Y) &&
                (left.Z <= right.Z));
        }
        #endregion

        #region Unary Operators
        /// <summary>
        /// Negates the values of the given vector.
        /// </summary>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <returns>A new <see cref="Vector3"/> instance containing the negated values.</returns>
        public static Vector3 operator -(Vector3 vector)
        {
            return Vector3.Negate(vector);
        }
        #endregion

        #region Binary Operators
        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="left">A <see cref="Vector3"/> instance.</param>
        /// <param name="right">A <see cref="Vector3"/> instance.</param>
        /// <returns>A new <see cref="Vector3"/> instance containing the sum.</returns>
        public static Vector3 operator +(Vector3 left, Vector3 right)
        {
            return Vector3.Add(left, right);
        }
        /// <summary>
        /// Adds a vector and a scalar.
        /// </summary>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <returns>A new <see cref="Vector3"/> instance containing the sum.</returns>
        public static Vector3 operator +(Vector3 vector, float scalar)
        {
            return Vector3.Add(vector, scalar);
        }
        /// <summary>
        /// Adds a vector and a scalar.
        /// </summary>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <returns>A new <see cref="Vector3"/> instance containing the sum.</returns>
        public static Vector3 operator +(float scalar, Vector3 vector)
        {
            return Vector3.Add(vector, scalar);
        }
        /// <summary>
        /// Subtracts a vector from a vector.
        /// </summary>
        /// <param name="left">A <see cref="Vector3"/> instance.</param>
        /// <param name="right">A <see cref="Vector3"/> instance.</param>
        /// <returns>A new <see cref="Vector3"/> instance containing the difference.</returns>
        /// <remarks>
        ///	result[i] = left[i] - right[i].
        /// </remarks>
        public static Vector3 operator -(Vector3 left, Vector3 right)
        {
            return Vector3.Subtract(left, right);
        }
        /// <summary>
        /// Subtracts a scalar from a vector.
        /// </summary>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <returns>A new <see cref="Vector3"/> instance containing the difference.</returns>
        /// <remarks>
        /// result[i] = vector[i] - scalar
        /// </remarks>
        public static Vector3 operator -(Vector3 vector, float scalar)
        {
            return Vector3.Subtract(vector, scalar);
        }
        /// <summary>
        /// Subtracts a vector from a scalar.
        /// </summary>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <returns>A new <see cref="Vector3"/> instance containing the difference.</returns>
        /// <remarks>
        /// result[i] = scalar - vector[i]
        /// </remarks>
        public static Vector3 operator -(float scalar, Vector3 vector)
        {
            return Vector3.Subtract(scalar, vector);
        }
        /// <summary>
        /// Multiplies a vector by a scalar.
        /// </summary>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <returns>A new <see cref="Vector3"/> containing the result.</returns>
        public static Vector3 operator *(Vector3 vector, float scalar)
        {
            return Vector3.Multiply(vector, scalar);
        }
        /// <summary>
        /// Multiplies a vector by a scalar.
        /// </summary>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <param name="scalar">A single-precision floating-point number.</param>
        /// <returns>A new <see cref="Vector3"/> containing the result.</returns>
        public static Vector3 operator *(float scalar, Vector3 vector)
        {
            return Vector3.Multiply(vector, scalar);
        }
        /// <summary>
        /// Divides a vector by a scalar.
        /// </summary>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <param name="scalar">A scalar</param>
        /// <returns>A new <see cref="Vector3"/> containing the quotient.</returns>
        /// <remarks>
        /// result[i] = vector[i] / scalar;
        /// </remarks>
        public static Vector3 operator /(Vector3 vector, float scalar)
        {
            return Vector3.Divide(vector, scalar);
        }
        /// <summary>
        /// Divides a scalar by a vector.
        /// </summary>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <param name="scalar">A scalar</param>
        /// <returns>A new <see cref="Vector3"/> containing the quotient.</returns>
        /// <remarks>
        /// result[i] = scalar / vector[i]
        /// </remarks>
        public static Vector3 operator /(float scalar, Vector3 vector)
        {
            return Vector3.Divide(scalar, vector);
        }
        #endregion

        #region Array Indexing Operator
        /// <summary>
        /// Indexer ( [x, y, z] ).
        /// </summary>
        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return X;
                    case 1:
                        return Y;
                    case 2:
                        return Z;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        X = value;
                        break;
                    case 1:
                        Y = value;
                        break;
                    case 2:
                        Z = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

        }
        public float this[uint index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return X;
                    case 1:
                        return Y;
                    case 2:
                        return Z;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        X = value;
                        break;
                    case 1:
                        Y = value;
                        break;
                    case 2:
                        Z = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

        }
        #endregion

        #region Conversion Operators
        /// <summary>
        /// Converts the vector to an array of single-precision floating point values.
        /// </summary>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <returns>An array of single-precision floating point values.</returns>
        public static explicit operator float[] (Vector3 vector)
        {
            float[] array = new float[3];
            array[0] = vector.X;
            array[1] = vector.Y;
            array[2] = vector.Z;
            return array;
        }
        /// <summary>
        /// Converts the vector to a <see cref="System.Collections.Generic.List{T}"/> of single-precision floating point values.
        /// </summary>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <returns>A <see cref="System.Collections.Generic.List{T}"/> of single-precision floating point values.</returns>
        public static explicit operator List<float>(Vector3 vector)
        {
            List<float> list = new List<float>(3);
            list.Add(vector.X);
            list.Add(vector.Y);
            list.Add(vector.Z);

            return list;
        }
        /// <summary>
        /// Converts the vector to a <see cref="System.Collections.Generic.LinkedList{T}"/> of single-precision floating point values.
        /// </summary>
        /// <param name="vector">A <see cref="Vector3"/> instance.</param>
        /// <returns>A <see cref="System.Collections.Generic.LinkedList{T}"/> of single-precision floating point values.</returns>
        public static explicit operator LinkedList<float>(Vector3 vector)
        {
            LinkedList<float> list = new LinkedList<float>();
            list.AddLast(vector.X);
            list.AddLast(vector.Y);
            list.AddLast(vector.Z);

            return list;
        }
        #endregion

        public float magnitude()
        {
            return (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        }
        public float dot(Vector3 rkVector)
        {
            return X * rkVector.X + Y * rkVector.Y + Z * rkVector.Z;
        }

        public Vector3 cross(Vector3 rkVector)
        {
            return new Vector3(Y * rkVector.Z - Z * rkVector.Y, Z * rkVector.X - X * rkVector.Z,
                X * rkVector.Y - Y * rkVector.X);
        }

        public Vector3 Min(Vector3 v)
        {
            return new Vector3(Math.Min(v.X, X), Math.Min(v.Y, Y), Math.Min(v.Z, Z));
        }

        public Vector3 Max(Vector3 v)
        {
            return new Vector3(Math.Max(v.X, X), Math.Max(v.Y, Y), Math.Max(v.Z, Z));
        }

        public Axis primaryAxis()
        {
            Axis a = Axis.X;

            double nx = Math.Abs(X);
            double ny = Math.Abs(Y);
            double nz = Math.Abs(Z);

            if (nx > ny)
            {
                if (nx > nz)
                    a = Axis.X;
                else
                    a = Axis.Z;
            }
            else
            {
                if (ny > nz)
                    a = Axis.Y;
                else
                    a = Axis.Z;
            }

            return a;
        }

        public enum Axis { X = 0, Y = 1, Z = 2, Detect = -1 };

        public Vector3 lerp(Vector3 v, float alpha)
        {
            return (this) + (v - this) * alpha;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Vector3.Zero if the length is nearly zero, otherwise returns a unit vector</returns>
        public Vector3 directionOrZero()
        {
            float mag = magnitude();
            if (mag < 0.0000001f)
            {
                return Zero;
            }
            else if (mag < 1.00001f && mag > 0.99999f)
            {
                return this;
            }
            else
            {
                return this * (1.0f / mag);
            }
        }
    }

    #region Vector3Converter class
    /// <summary>
    /// Converts a <see cref="Vector3"/> to and from string representation.
    /// </summary>
    public class Vector3Converter : ExpandableObjectConverter
    {
        /// <summary>
        /// Returns whether this converter can convert an object of the given type to the type of this converter, using the specified context.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext"/> that provides a format context.</param>
        /// <param name="sourceType">A <see cref="Type"/> that represents the type you want to convert from.</param>
        /// <returns><b>true</b> if this converter can perform the conversion; otherwise, <b>false</b>.</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;

            return base.CanConvertFrom(context, sourceType);
        }
        /// <summary>
        /// Returns whether this converter can convert the object to the specified type, using the specified context.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext"/> that provides a format context.</param>
        /// <param name="destinationType">A <see cref="Type"/> that represents the type you want to convert to.</param>
        /// <returns><b>true</b> if this converter can perform the conversion; otherwise, <b>false</b>.</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string))
                return true;

            return base.CanConvertTo(context, destinationType);
        }
        /// <summary>
        /// Converts the given value object to the specified type, using the specified context and culture information.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext"/> that provides a format context.</param>
        /// <param name="culture">A <see cref="System.Globalization.CultureInfo"/> object. If a null reference (Nothing in Visual Basic) is passed, the current culture is assumed.</param>
        /// <param name="value">The <see cref="Object"/> to convert.</param>
        /// <param name="destinationType">The Type to convert the <paramref name="value"/> parameter to.</param>
        /// <returns>An <see cref="Object"/> that represents the converted value.</returns>
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if ((destinationType == typeof(string)) && (value is Vector3))
            {
                Vector3 v = (Vector3)value;
                return v.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
        /// <summary>
        /// Converts the given object to the type of this converter, using the specified context and culture information.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext"/> that provides a format context.</param>
        /// <param name="culture">The <see cref="System.Globalization.CultureInfo"/> to use as the current culture. </param>
        /// <param name="value">The <see cref="Object"/> to convert.</param>
        /// <returns>An <see cref="Object"/> that represents the converted value.</returns>
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value.GetType() == typeof(string))
            {
                return Vector3.Parse((string)value);
            }

            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// Returns whether this object supports a standard set of values that can be picked from a list.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext"/> that provides a format context.</param>
        /// <returns><b>true</b> if <see cref="GetStandardValues"/> should be called to find a common set of values the object supports; otherwise, <b>false</b>.</returns>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        /// <summary>
        /// Returns a collection of standard values for the data type this type converter is designed for when provided with a format context.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext"/> that provides a format context that can be used to extract additional information about the environment from which this converter is invoked. This parameter or properties of this parameter can be a null reference.</param>
        /// <returns>A <see cref="TypeConverter.StandardValuesCollection"/> that holds a standard set of valid values, or a null reference (Nothing in Visual Basic) if the data type does not support a standard set of values.</returns>
        public override System.ComponentModel.TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            StandardValuesCollection svc =
                new StandardValuesCollection(new object[4] { Vector3.Zero, Vector3.XAxis, Vector3.YAxis, Vector3.ZAxis });

            return svc;
        }
    }
    #endregion
}
