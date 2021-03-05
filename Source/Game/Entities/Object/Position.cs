﻿/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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

using Framework.GameMath;
using Game.Maps;
using System;

namespace Game.Entities
{
    public class Position
    {
        public float posX;
        public float posY;
        public float posZ;
        public float Orientation;

        public Position(float x = 0f, float y = 0f, float z = 0f, float o = 0f)
        {
            posX = x;
            posY = y;
            posZ = z;
            Orientation = o;
        }

        public Position(Vector3 vector)
        {
            posX = vector.X;
            posY = vector.Y;
            posZ = vector.Z;
        }

        public Position(Position position)
        {
            posX = position.posX;
            posY = position.posY;
            posZ = position.posZ;
            Orientation = position.Orientation;
        }

        public float GetPositionX()
        {
            return posX;
        }
        public float GetPositionY()
        {
            return posY;
        }
        public float GetPositionZ()
        {
            return posZ;
        }
        public float GetOrientation()
        {
            return Orientation;
        }

        public void Relocate(float x, float y)
        {
            posX = x;
            posY = y;
        }
        public void Relocate(float x, float y, float z)
        {
            posX = x;
            posY = y;
            posZ = z;
        }
        public void Relocate(float x, float y, float z, float o)
        {
            posX = x;
            posY = y;
            posZ = z;
            SetOrientation(o);
        }
        public void Relocate(Position loc)
        {
            Relocate(loc.posX, loc.posY, loc.posZ, loc.Orientation);
        }
        public void Relocate(Vector3 pos)
        {
            Relocate(pos.X, pos.Y, pos.Z);
        }
        public void RelocateOffset(Position offset)
        {
            posX = (float)(posX + (offset.posX * Math.Cos(Orientation) + offset.posY * Math.Sin(Orientation + MathFunctions.PI)));
            posY = (float)(posY + (offset.posY * Math.Cos(Orientation) + offset.posX * Math.Sin(Orientation)));
            posZ = posZ + offset.posZ;
            SetOrientation(Orientation + offset.Orientation);
        }

        public bool IsPositionValid()
        {
            return GridDefines.IsValidMapCoord(posX, posY, posZ, Orientation);
        }

        public float GetRelativeAngle(Position pos)
        {
            return GetAngle(pos) - Orientation;
        }
        public float GetRelativeAngle(float x, float y)
        {
            return GetAngle(x, y) - Orientation;
        }

        public void GetPosition(out float x, out float y)
        {
            x = posX; y = posY;
        }
        public void GetPosition(out float x, out float y, out float z)
        {
            x = posX; y = posY; z = posZ;
        }
        public void GetPosition(out float x, out float y, out float z, out float o)
        {
            x = posX;
            y = posY;
            z = posZ;
            o = Orientation;
        }
        public Position GetPosition()
        {
            return this;
        }
        public void GetPositionOffsetTo(Position endPos, out Position retOffset)
        {
            retOffset = new Position();

            var dx = endPos.GetPositionX() - GetPositionX();
            var dy = endPos.GetPositionY() - GetPositionY();

            retOffset.posX = (float)(dx * Math.Cos(GetOrientation()) + dy * Math.Sin(GetOrientation()));
            retOffset.posY = (float)(dy * Math.Cos(GetOrientation()) - dx * Math.Sin(GetOrientation()));
            retOffset.posZ = endPos.GetPositionZ() - GetPositionZ();
            retOffset.SetOrientation(endPos.GetOrientation() - GetOrientation());
        }

        public Position GetPositionWithOffset(Position offset)
        {
            var ret = this;
            ret.RelocateOffset(offset);
            return ret;
        }

        public static float NormalizeOrientation(float o)
        {
            // fmod only supports positive numbers. Thus we have
            // to emulate negative numbers
            if (o < 0)
            {
                var mod = o * -1;
                mod = mod % (2.0f * MathFunctions.PI);
                mod = -mod + 2.0f * MathFunctions.PI;
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
            var dz = posZ - z;

            return GetExactDist2dSq(x, y) + dz * dz;
        }
        public float GetExactDistSq(Position pos)
        {
            var dx = posX - pos.posX;
            var dy = posY - pos.posY;
            var dz = posZ - pos.posZ;

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
            var dx = posX - x;
            var dy = posY - y;

            return dx * dx + dy * dy;
        }
        public float GetExactDist2dSq(Position pos)
        {
            var dx = posX - pos.posX;
            var dy = posY - pos.posY;

            return dx * dx + dy * dy;
        }

        public float GetAngle(float x, float y)
        {
            var dx = x - GetPositionX();
            var dy = y - GetPositionY();

            var ang = (float)Math.Atan2(dy, dx);
            ang = ang >= 0 ? ang : 2 * MathFunctions.PI + ang;
            return ang;
        }
        public float GetAngle(Position pos)
        {
            if (pos == null)
                return 0;

            return GetAngle(pos.GetPositionX(), pos.GetPositionY());
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
            var rotation = 2 * Math.PI - center.GetOrientation();
            var sinVal = Math.Sin(rotation);
            var cosVal = Math.Cos(rotation);

            var BoxDistX = GetPositionX() - center.GetPositionX();
            var BoxDistY = GetPositionY() - center.GetPositionY();

            var rotX = (float)(center.GetPositionX() + BoxDistX * cosVal - BoxDistY * sinVal);
            var rotY = (float)(center.GetPositionY() + BoxDistY * cosVal + BoxDistX * sinVal);

            // box edges are parallel to coordiante axis, so we can treat every dimension independently :D
            var dz = GetPositionZ() - center.GetPositionZ();
            var dx = rotX - center.GetPositionX();
            var dy = rotY - center.GetPositionY();
            if ((Math.Abs(dx) > xradius) || (Math.Abs(dy) > yradius) || (Math.Abs(dz) > zradius))
                return false;

            return true;
        }

        public bool IsWithinDoubleVerticalCylinder(Position center, float radius, float height)
        {
            var verticalDelta = GetPositionZ() - center.GetPositionZ();
            return IsInDist2d(center, radius) && Math.Abs(verticalDelta) <= height;
        }

        public bool HasInArc(float arc, Position obj, float border = 2.0f)
        {
            // always have self in arc
            if (obj == this)
                return true;

            // move arc to range 0.. 2*pi
            arc = NormalizeOrientation(arc);

            var angle = GetAngle(obj);
            angle -= GetOrientation();

            // move angle to range -pi ... +pi
            angle = NormalizeOrientation(angle);
            if (angle > MathFunctions.PI)
                angle -= 2.0f * MathFunctions.PI;

            var lborder = -1 * (arc / border);                        // in range -pi..0
            var rborder = (arc / border);                             // in range 0..pi
            return ((angle >= lborder) && (angle <= rborder));
        }

        public bool HasInLine(Position pos, float objSize, float width)
        {
            if (!HasInArc(MathFunctions.PI, pos))
                return false;

            width += objSize;
            var angle = GetRelativeAngle(pos);
            return Math.Abs(Math.Sin(angle)) * GetExactDist2d(pos.GetPositionX(), pos.GetPositionY()) < width;
        }

        public void GetSinCos(float x, float y, out float vsin, out float vcos)
        {
            var dx = GetPositionX() - x;
            var dy = GetPositionY() - y;

            if (Math.Abs(dx) < 0.001f && Math.Abs(dy) < 0.001f)
            {
                var angle = (float)RandomHelper.NextDouble() * MathFunctions.TwoPi;
                vcos = (float)Math.Cos(angle);
                vsin = (float)Math.Sin(angle);
            }
            else
            {
                var dist = (float)Math.Sqrt((dx * dx) + (dy * dy));
                vcos = dx / dist;
                vsin = dy / dist;
            }
        }

        public override string ToString()
        {
            return $"X: {posX} Y: {posY} Z: {posZ} O: {Orientation}";
        }

        public static implicit operator Vector2(Position position)
        {
            return new(position.posX, position.posY);
        }
    }

    public class WorldLocation : Position
    {
        private uint _mapId;
        private Cell currentCell;
        public ObjectCellMoveState _moveState;

        public Position _newPosition = new Position();

        public WorldLocation(uint mapId = 0xFFFFFFFF, float x = 0, float y = 0, float z = 0, float o = 0)
        {
            _mapId = mapId;
            Relocate(x, y, z, o);
        }
        public WorldLocation(uint mapId, Position pos)
        {
            _mapId = mapId;
            Relocate(pos);
        }
        public WorldLocation(WorldLocation loc)
        {
            _mapId = loc._mapId;
            Relocate(loc);
        }
        public WorldLocation(Position pos)
        {
            _mapId = 0xFFFFFFFF;
            Relocate(pos);
        }

        public void WorldRelocate(uint mapId, Position pos)
        {
            _mapId = mapId;
            Relocate(pos);
        }
        
        public void WorldRelocate(WorldLocation loc)
        {
            _mapId = loc._mapId;
            Relocate(loc);
        }

        public void WorldRelocate(uint mapId = 0xFFFFFFFF, float x = 0.0f, float y = 0.0f, float z = 0.0f, float o = 0.0f)
        {
            _mapId = mapId;
            Relocate(x, y, z, o);
        }

        public uint GetMapId() { return _mapId; }
        public void SetMapId(uint mapId) { _mapId = mapId; }

        public Cell GetCurrentCell()
        {
            if (currentCell == null)
                Log.outError(LogFilter.Server, "Calling currentCell  but its null");

            return currentCell;
        }
        public void SetCurrentCell(Cell cell) { currentCell = cell; }
        public void SetNewCellPosition(float x, float y, float z, float o)
        {
            _moveState = ObjectCellMoveState.Active;
            _newPosition.Relocate(x, y, z, o);
        }

        public WorldLocation GetWorldLocation()
        {
            return this;
        }
    }
}
