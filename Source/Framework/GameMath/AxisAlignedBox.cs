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

using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Framework.GameMath
{
    /// <summary> 
    /// Represents an axis aligned box in 3D space. 
    /// </summary> 
    /// <remarks> 
    /// An axis-aligned box is a box whose faces coincide with the standard basis axes. 
    /// </remarks> 
    [Serializable]
    public struct AxisAlignedBox : ISerializable, ICloneable
    {
        #region Private Fields
        private Vector3 _lo;
        private Vector3 _hi;
        #endregion

        #region Constructors
        /// <summary> 
        /// Initializes a new instance of the <see cref="AxisAlignedBox"/> class using given minimum and maximum points. 
        /// </summary> 
        /// <param name="min">A <see cref="Vector3"/> instance representing the minimum point.</param> 
        /// <param name="max">A <see cref="Vector3"/> instance representing the maximum point.</param> 
        public AxisAlignedBox(Vector3 min, Vector3 max)
        {
            _lo = min;
            _hi = max;
        }
        /// <summary> 
        /// Initializes a new instance of the <see cref="AxisAlignedBox"/> class using given values from another box instance. 
        /// </summary> 
        /// <param name="box">A <see cref="AxisAlignedBox"/> instance to take values from.</param> 
        public AxisAlignedBox(AxisAlignedBox box)
        {
            _lo = box.Lo;
            _hi = box.Hi;
        }
        /// <summary> 
        /// Initializes a new instance of the <see cref="AxisAlignedBox"/> class with serialized data. 
        /// </summary> 
        /// <param name="info">The object that holds the serialized object data.</param> 
        /// <param name="context">The contextual information about the source or destination.</param> 
        private AxisAlignedBox(SerializationInfo info, StreamingContext context)
        {
            _lo = (Vector3)info.GetValue("Min", typeof(Vector3));
            _hi = (Vector3)info.GetValue("Max", typeof(Vector3));
        }
        #endregion

        #region Public Properties
        /// <summary> 
        /// Gets or sets the minimum point which is the box's minimum X and Y coordinates. 
        /// </summary> 
        public Vector3 Lo
        {
            get { return _lo; }
            set { _lo = value; }
        }
        /// <summary> 
        /// Gets or sets the maximum point which is the box's maximum X and Y coordinates. 
        /// </summary> 
        public Vector3 Hi
        {
            get { return _hi; }
            set { _hi = value; }
        }
        #endregion

        #region ISerializable Members
        /// <summary> 
        /// Populates a <see cref="SerializationInfo"/> with the data needed to serialize the target object. 
        /// </summary> 
        /// <param name="info">The <see cref="SerializationInfo"/> to populate with data. </param> 
        /// <param name="context">The destination (see <see cref="StreamingContext"/>) for this serialization.</param> 
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Max", _hi, typeof(Vector3));
            info.AddValue("Min", _lo, typeof(Vector3));
        }
        #endregion

        #region ICloneable Members
        /// <summary> 
        /// Creates an exact copy of this <see cref="AxisAlignedBox"/> object. 
        /// </summary> 
        /// <returns>The <see cref="AxisAlignedBox"/> object this method creates, cast as an object.</returns> 
        object ICloneable.Clone()
        {
            return new AxisAlignedBox(this);
        }
        /// <summary> 
        /// Creates an exact copy of this <see cref="AxisAlignedBox"/> object. 
        /// </summary> 
        /// <returns>The <see cref="AxisAlignedBox"/> object this method creates.</returns> 
        public AxisAlignedBox Clone()
        {
            return new AxisAlignedBox(this);
        }
        #endregion

        #region Public Methods
        /// <summary> 
        /// Computes the box vertices.  
        /// </summary> 
        /// <returns>An array of <see cref="Vector3"/> containing the box vertices.</returns> 
        public Vector3[] ComputeVertices()
        {
            Vector3[] vertices = new Vector3[8];

            vertices[0] = _lo;
            vertices[1] = new Vector3(_hi.X, _lo.Y, _lo.Z);
            vertices[2] = new Vector3(_hi.X, _hi.Y, _lo.Z);
            vertices[3] = new Vector3(_lo.X, _hi.Y, _lo.Z);

            vertices[4] = new Vector3(_lo.X, _lo.Y, _hi.Z);
            vertices[5] = new Vector3(_hi.X, _lo.Y, _hi.Z);
            vertices[6] = _hi;
            vertices[7] = new Vector3(_lo.X, _hi.Y, _hi.Z);

            return vertices;
        }

        #endregion

        #region Overrides
        /// <summary> 
        /// Returns the hashcode for this instance. 
        /// </summary> 
        /// <returns>A 32-bit signed integer hash code.</returns> 
        public override int GetHashCode()
        {
            return _lo.GetHashCode() ^ _hi.GetHashCode();
        }
        /// <summary> 
        /// Returns a value indicating whether this instance is equal to 
        /// the specified object. 
        /// </summary> 
        /// <param name="obj">An object to compare to this instance.</param> 
        /// <returns>True if <paramref name="obj"/> is a <see cref="Vector3"/> and has the same values as this instance; otherwise, False.</returns> 
        public override bool Equals(object obj)
        {
            if (obj is AxisAlignedBox)
            {
                AxisAlignedBox box = (AxisAlignedBox)obj;
                return (_lo == box.Lo) && (_hi == box.Hi);
            }
            return false;
        }

        /// <summary> 
        /// Returns a string representation of this object. 
        /// </summary> 
        /// <returns>A string representation of this object.</returns> 
        public override string ToString()
        {
            return $"AxisAlignedBox(Min={_lo}, Max={_hi})";
        }
        #endregion

        #region Comparison Operators
        /// <summary> 
        /// Checks if the two given boxes are equal. 
        /// </summary> 
        /// <param name="a">The first of two boxes to compare.</param> 
        /// <param name="b">The second of two boxes to compare.</param> 
        /// <returns><b>true</b> if the boxes are equal; otherwise, <b>false</b>.</returns> 
        public static bool operator ==(AxisAlignedBox a, AxisAlignedBox b)
        {
            if (Equals(a, null))
            {
                return Equals(b, null);
            }

            if (Equals(b, null))
            {
                return Equals(a, null);
            }

            return (a.Lo == b.Lo) && (a.Hi == b.Hi);
        }

        /// <summary> 
        /// Checks if the two given boxes are not equal. 
        /// </summary> 
        /// <param name="a">The first of two boxes to compare.</param> 
        /// <param name="b">The second of two boxes to compare.</param> 
        /// <returns><b>true</b> if the vectors are not equal; otherwise, <b>false</b>.</returns> 
        public static bool operator !=(AxisAlignedBox a, AxisAlignedBox b)
        {
            if (Object.Equals(a, null) == true)
            {
                return !Object.Equals(b, null);
            }
            else if (Object.Equals(b, null) == true)
            {
                return !Object.Equals(a, null);
            }
            return !((a.Lo == b.Lo) && (a.Hi == b.Hi));
        }
        #endregion

        public bool contains(Vector3 point)
        {
            return
                (point.X >= _lo.X) &&
                (point.Y >= _lo.Y) &&
                (point.Z >= _lo.Z) &&
                (point.X <= _hi.X) &&
                (point.Y <= _hi.Y) &&
                (point.Z <= _hi.Z);
        }

        public void merge(AxisAlignedBox a)
        {
            Lo = Lo.Min(a.Lo);
            Hi = Hi.Max(a.Hi);
        }

        public void merge(Vector3 a)
        {
            _lo = _lo.Min(a);
            _hi = _hi.Max(a);
        }

        public static AxisAlignedBox Zero()
        {
            return new AxisAlignedBox(Vector3.Zero, Vector3.Zero);
        }

        public Vector3 corner(int index)
        {
            // default constructor inits all components to 0
            Vector3 v = new Vector3();

            switch (index)
            {
                case 0:
                    v.X = _lo.X;
                    v.Y = _lo.Y;
                    v.Z = _hi.Z;
                    break;

                case 1:
                    v.X = _hi.X;
                    v.Y = _lo.Y;
                    v.Z = _hi.Z;
                    break;

                case 2:
                    v.X = _hi.X;
                    v.Y = _hi.Y;
                    v.Z = _hi.Z;
                    break;

                case 3:
                    v.X = _lo.X;
                    v.Y = _hi.Y;
                    v.Z = _hi.Z;
                    break;

                case 4:
                    v.X = _lo.X;
                    v.Y = _lo.Y;
                    v.Z = _lo.Z;
                    break;

                case 5:
                    v.X = _hi.X;
                    v.Y = _lo.Y;
                    v.Z = _lo.Z;
                    break;

                case 6:
                    v.X = _hi.X;
                    v.Y = _hi.Y;
                    v.Z = _lo.Z;
                    break;

                case 7:
                    v.X = _lo.X;
                    v.Y = _hi.Y;
                    v.Z = _lo.Z;
                    break;

                default:
                    break;
            }

            return v;
        }

        public static AxisAlignedBox operator +(AxisAlignedBox box, Vector3 v)
        {
            AxisAlignedBox outt = new AxisAlignedBox();
            outt.Lo = box.Lo + v;
            outt.Hi = box.Hi + v;
            return outt;
        }
    }
}
