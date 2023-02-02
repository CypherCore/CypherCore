// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Framework.GameMath
{
    /// <summary>
    /// Represents a ray in 3D space.
    /// </summary>
    /// <remarks>
    /// A ray is R(t) = Origin + t * Direction where t>=0. The Direction isnt necessarily of unit length.
    /// </remarks>
    [Serializable]
    [TypeConverter(typeof(RayConverter))]
    public struct Ray : ICloneable
    {
        #region Private Fields
        private static Vector3 _inf = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);

        private Vector3 _origin;
        private Vector3 _direction;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Ray"/> class using given origin and direction vectors.
        /// </summary>
        /// <param name="origin">Ray's origin point.</param>
        /// <param name="direction">Ray's direction vector.</param>
        public Ray(Vector3 origin, Vector3 direction)
        {
            _origin = origin;
            _direction = direction;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Ray"/> class using given ray.
        /// </summary>
        /// <param name="ray">A <see cref="Ray"/> instance to assign values from.</param>
        public Ray(Ray ray)
        {
            _origin = ray.Origin;
            _direction = ray.Direction;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets or sets the ray's origin.
        /// </summary>
        public Vector3 Origin
        {
            get { return _origin; }
            set { _origin = value; }
        }
        /// <summary>
        /// Gets or sets the ray's direction vector.
        /// </summary>
        public Vector3 Direction
        {
            get { return _direction; }
            set { _direction = value; }
        }
        #endregion

        #region ICloneable Members
        /// <summary>
        /// Creates an exact copy of this <see cref="Ray"/> object.
        /// </summary>
        /// <returns>The <see cref="Ray"/> object this method creates, cast as an object.</returns>
        object ICloneable.Clone()
        {
            return new Ray(this);
        }
        /// <summary>
        /// Creates an exact copy of this <see cref="Ray"/> object.
        /// </summary>
        /// <returns>The <see cref="Ray"/> object this method creates.</returns>
        public Ray Clone()
        {
            return new Ray(this);
        }
        #endregion

        #region Public Static Parse Methods
        /// <summary>
        /// Converts the specified string to its <see cref="Ray"/> equivalent.
        /// </summary>
        /// <param name="s">A string representation of a <see cref="Ray"/></param>
        /// <returns>A <see cref="Ray"/> that represents the vector specified by the <paramref name="s"/> parameter.</returns>
        public static Ray Parse(string s)
        {
            Regex r = new(@"\((?<origin>\([^\)]*\)), (?<direction>\([^\)]*\))\)", RegexOptions.None);
            Match m = r.Match(s);
            if (m.Success)
            {
                return new Ray(
                    m.Result("${origin}").ParseVector3(),
                    m.Result("${direction}").ParseVector3()
                    );
            }
            else
            {
                throw new Exception("Unsuccessful Match.");
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets a point on the ray.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public Vector3 GetPointOnRay(float t)
        {
            return (Origin + Direction * t);
        }

        #endregion

        #region Overrides
        /// <summary>
        /// Get the hashcode for this instance.
        /// </summary>
        /// <returns>Returns the hash code for this vector instance.</returns>
        public override int GetHashCode()
        {
            return _origin.GetHashCode() ^ _direction.GetHashCode();
        }
        /// <summary>
        /// Returns a value indicating whether this instance is equal to
        /// the specified object.
        /// </summary>
        /// <param name="obj">An object to compare to this instance.</param>
        /// <returns><see langword="true"/> if <paramref name="obj"/> is a <see cref="Vector3"/> and has the same values as this instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object obj)
        {
            if (obj is Ray)
            {
                Ray r = (Ray)obj;
                return ((_origin == r.Origin) && (_direction == r.Direction));
            }
            return false;
        }
        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        /// <returns>A string representation of this object.</returns>
        public override string ToString()
        {
            return $"({_origin}, {_direction})";
        }
        #endregion

        #region Comparison Operators
        /// <summary>
        /// Tests whether two specified rays are equal.
        /// </summary>
        /// <param name="a">The first of two rays to compare.</param>
        /// <param name="b">The second of two rays to compare.</param>
        /// <returns><see langword="true"/> if the two rays are equal; otherwise, <see langword="false"/>.</returns>
        public static bool operator ==(Ray a, Ray b)
        {
            return ValueType.Equals(a, b);
        }
        /// <summary>
        /// Tests whether two specified rays are not equal.
        /// </summary>
        /// <param name="a">The first of two rays to compare.</param>
        /// <param name="b">The second of two rays to compare.</param>
        /// <returns><see langword="true"/> if the two rays are not equal; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(Ray a, Ray b)
        {
            return !ValueType.Equals(a, b);
        }

        #endregion

        public Vector3 intersection(Plane plane)
        {
            float rate = Vector3.Dot(Direction, plane.Normal);
            if (rate >= 0.0f)
            {
                return _inf;
            }
            else
            {
                float t = -(plane.D + Vector3.Dot(Origin, plane.Normal)) / rate;
                return Origin + Direction * t;
            }
        }

        public float intersectionTime(AxisAlignedBox box)
        {
            Vector3 dummy = Vector3.Zero;
            bool inside;
            float time = CollisionDetection.collisionTimeForMovingPointFixedAABox(_origin, _direction, box, ref dummy, out inside);

            if (float.IsInfinity(time) && inside)
                return 0.0f;
            else
                return time;
        }

        public Vector3 invDirection()
        {
            return Vector3.Divide(Vector3.One, Direction);
        }
    }

    #region RayConverter class
    /// <summary>
    /// Converts a <see cref="Ray"/> to and from string representation.
    /// </summary>
    public class RayConverter : ExpandableObjectConverter
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
            if ((destinationType == typeof(string)) && (value is Ray))
            {
                Ray r = (Ray)value;
                return r.ToString();
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
                return Ray.Parse((string)value);
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
    #endregion
}
