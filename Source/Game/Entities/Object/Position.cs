// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Numerics;
using Game.Maps;

namespace Game.Entities
{
    public class Position
    {
        public float Orientation;
        public float X;
        public float Y;
        public float Z;

        public Position(float x = 0f, float y = 0f, float z = 0f, float o = 0f)
		{
			X        = x;
			Y        = y;
			Z        = z;
			Orientation = NormalizeOrientation(o);
		}

		public Position(Vector3 vector)
		{
			X = vector.X;
			Y = vector.Y;
			Z = vector.Z;
		}

		public Position(Position position)
		{
			X        = position.X;
			Y        = position.Y;
			Z        = position.Z;
			Orientation = position.Orientation;
		}

		public float GetPositionX()
		{
			return X;
		}

		public float GetPositionY()
		{
			return Y;
		}

		public float GetPositionZ()
		{
			return Z;
		}

		public float GetOrientation()
		{
			return Orientation;
		}

		public void Relocate(float x, float y)
		{
			X = x;
			Y = y;
		}

		public void Relocate(float x, float y, float z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		public void Relocate(float x, float y, float z, float o)
		{
			X = x;
			Y = y;
			Z = z;
			SetOrientation(o);
		}

		public void Relocate(Position loc)
		{
			Relocate(loc.X, loc.Y, loc.Z, loc.Orientation);
		}

		public void Relocate(Vector3 pos)
		{
			Relocate(pos.X, pos.Y, pos.Z);
		}

		public void RelocateOffset(Position offset)
		{
			X =  (float)(X + (offset.X * Math.Cos(Orientation) + offset.Y * Math.Sin(Orientation + MathFunctions.PI)));
			Y =  (float)(Y + (offset.Y * Math.Cos(Orientation) + offset.X * Math.Sin(Orientation)));
			Z += offset.Z;
			SetOrientation(Orientation + offset.Orientation);
		}

		public bool IsPositionValid()
		{
			return GridDefines.IsValidMapCoord(X, Y, Z, Orientation);
		}

		private float ToRelativeAngle(float absAngle)
		{
			return NormalizeOrientation(absAngle - Orientation);
		}

		public float GetRelativeAngle(Position pos)
		{
			return ToRelativeAngle(GetAbsoluteAngle(pos));
		}

		public float GetRelativeAngle(float x, float y)
		{
			return ToRelativeAngle(GetAbsoluteAngle(x, y));
		}


		public void GetPosition(out float x, out float y, out float z)
		{
			x = X;
			y = Y;
			z = Z;
		}

		public void GetPosition(out float x, out float y, out float z, out float o)
		{
			x = X;
			y = Y;
			z = Z;
			o = Orientation;
		}

		public Position GetPosition()
		{
			return this;
		}

		public void GetPositionOffsetTo(Position endPos, out Position retOffset)
		{
			retOffset = new Position();

			float dx = endPos.GetPositionX() - GetPositionX();
			float dy = endPos.GetPositionY() - GetPositionY();

			retOffset.X = (float)(dx * Math.Cos(GetOrientation()) + dy * Math.Sin(GetOrientation()));
			retOffset.Y = (float)(dy * Math.Cos(GetOrientation()) - dx * Math.Sin(GetOrientation()));
			retOffset.Z = endPos.GetPositionZ() - GetPositionZ();
			retOffset.SetOrientation(endPos.GetOrientation() - GetOrientation());
		}

		public Position GetPositionWithOffset(Position offset)
		{
			Position ret = this;
			ret.RelocateOffset(offset);

			return ret;
		}

		public static float NormalizeOrientation(float o)
		{
			// fmod only supports positive numbers. Thus we have
			// to emulate negative numbers
			if (o < 0)
			{
				float mod = o * -1;
				mod %= (2.0f * MathFunctions.PI);
				mod =  -mod + 2.0f * MathFunctions.PI;

				return mod;
			}

			return o % (2.0f * MathFunctions.PI);
		}

		public float GetExactDist(float x, float y, float z)
		{
			return (float)Math.Sqrt(GetExactDistSq(x, y, z));
		}

		public float GetExactDist(Position pos)
		{
			return (float)Math.Sqrt(GetExactDistSq(pos));
		}

		public float GetExactDistSq(float x, float y, float z)
		{
			float dz = z - Z;

			return GetExactDist2dSq(x, y) + dz * dz;
		}

		public float GetExactDistSq(Position pos)
		{
			float dx = X - pos.X;
			float dy = Y - pos.Y;
			float dz = Z - pos.Z;

			return dx * dx + dy * dy + dz * dz;
		}

		public float GetExactDist2d(float x, float y)
		{
			return (float)Math.Sqrt(GetExactDist2dSq(x, y));
		}

		public float GetExactDist2d(Position pos)
		{
			return (float)Math.Sqrt(GetExactDist2dSq(pos));
		}

		public float GetExactDist2dSq(float x, float y)
		{
			float dx = x - X;
			float dy = y - Y;

			return dx * dx + dy * dy;
		}

		public float GetExactDist2dSq(Position pos)
		{
			float dx = pos.X - X;
			float dy = pos.Y - Y;

			return dx * dx + dy * dy;
		}

		public float GetAbsoluteAngle(float x, float y)
		{
			float dx = x - GetPositionX();
			float dy = y - GetPositionY();

			return NormalizeOrientation(MathF.Atan2(dy, dx));
		}

		public float GetAbsoluteAngle(Position pos)
		{
			if (pos == null)
				return 0;

			return GetAbsoluteAngle(pos.GetPositionX(), pos.GetPositionY());
		}

		public float ToAbsoluteAngle(float relAngle)
		{
			return NormalizeOrientation(relAngle + Orientation);
		}

		public bool IsInDist(float x, float y, float z, float dist)
		{
			return GetExactDistSq(x, y, z) < dist * dist;
		}

		public bool IsInDist(Position pos, float dist)
		{
			return GetExactDistSq(pos) < dist * dist;
		}

		public bool IsInDist2d(float x, float y, float dist)
		{
			return GetExactDist2dSq(x, y) < dist * dist;
		}

		public bool IsInDist2d(Position pos, float dist)
		{
			return GetExactDist2dSq(pos) < dist * dist;
		}

		public void SetOrientation(float orientation)
		{
			Orientation = NormalizeOrientation(orientation);
		}

		public bool IsWithinBox(Position center, float xradius, float yradius, float zradius)
		{
			// rotate the WorldObject position instead of rotating the whole cube, that way we can make a simplified
			// is-in-cube check and we have to calculate only one point instead of 4

			// 2PI = 360*, keep in mind that ingame orientation is counter-clockwise
			double rotation = 2 * Math.PI - center.GetOrientation();
			double sinVal   = Math.Sin(rotation);
			double cosVal   = Math.Cos(rotation);

			float BoxDistX = GetPositionX() - center.GetPositionX();
			float BoxDistY = GetPositionY() - center.GetPositionY();

			float rotX = (float)(center.GetPositionX() + BoxDistX * cosVal - BoxDistY * sinVal);
			float rotY = (float)(center.GetPositionY() + BoxDistY * cosVal + BoxDistX * sinVal);

			// box edges are parallel to coordiante axis, so we can treat every dimension independently :D
			float dz = GetPositionZ() - center.GetPositionZ();
			float dx = rotX - center.GetPositionX();
			float dy = rotY - center.GetPositionY();

			if ((Math.Abs(dx) > xradius) ||
			    (Math.Abs(dy) > yradius) ||
			    (Math.Abs(dz) > zradius))
				return false;

			return true;
		}

		public bool IsWithinDoubleVerticalCylinder(Position center, float radius, float height)
		{
			float verticalDelta = GetPositionZ() - center.GetPositionZ();

			return IsInDist2d(center, radius) && Math.Abs(verticalDelta) <= height;
		}

		public bool HasInArc(float arc, Position obj, float border = 2.0f)
		{
			// always have self in arc
			if (obj == this)
				return true;

			// move arc to range 0.. 2*pi
			arc = NormalizeOrientation(arc);

			// move angle to range -pi ... +pi
			float angle = GetRelativeAngle(obj);

			if (angle > MathFunctions.PI)
				angle -= 2.0f * MathFunctions.PI;

			float lborder = -1 * (arc / border); // in range -pi..0
			float rborder = (arc / border);      // in range 0..pi

			return ((angle >= lborder) && (angle <= rborder));
		}

		public bool HasInLine(Position pos, float objSize, float width)
		{
			if (!HasInArc(MathFunctions.PI, pos, 2.0f))
				return false;

			width += objSize;
			float angle = GetRelativeAngle(pos);

			return Math.Abs(Math.Sin(angle)) * GetExactDist2d(pos.GetPositionX(), pos.GetPositionY()) < width;
		}

		public override string ToString()
		{
			return $"X: {X} Y: {Y} Z: {Z} O: {Orientation}";
		}

		public static implicit operator Vector2(Position position)
		{
			return new Vector2(position.X, position.Y);
		}

		public static implicit operator Vector3(Position position)
		{
			return new Vector3(position.X, position.Y, position.Z);
		}
	}
}