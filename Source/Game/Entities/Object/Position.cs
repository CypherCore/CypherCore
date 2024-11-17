// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.DataStorage;
using Game.Maps;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Game.Entities
{
    public class Position
    {
        public float posX;
        public float posY;
        public float posZ;
        public float Orientation;

        public Position() { }

        public Position(float x = 0f, float y = 0f)
        {
            posX = x;
            posY = y;
        }

        public Position(float x = 0f, float y = 0f, float z = 0f)
        {
            posX = x;
            posY = y;
            posZ = z;
        }

        public Position(float x = 0f, float y = 0f, float z = 0f, float o = 0f)
        {
            posX = x;
            posY = y;
            posZ = z;
            Orientation = NormalizeOrientation(o);
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
            posZ += offset.posZ;
            SetOrientation(Orientation + offset.Orientation);
        }

        public bool IsPositionValid()
        {
            return GridDefines.IsValidMapCoord(posX, posY, posZ, Orientation);
        }

        float ToRelativeAngle(float absAngle)
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

            float dx = endPos.GetPositionX() - GetPositionX();
            float dy = endPos.GetPositionY() - GetPositionY();

            retOffset.posX = (float)(dx * Math.Cos(GetOrientation()) + dy * Math.Sin(GetOrientation()));
            retOffset.posY = (float)(dy * Math.Cos(GetOrientation()) - dx * Math.Sin(GetOrientation()));
            retOffset.posZ = endPos.GetPositionZ() - GetPositionZ();
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
            float dz = z - posZ;

            return GetExactDist2dSq(x, y) + dz * dz;
        }
        public float GetExactDistSq(Position pos)
        {
            float dx = posX - pos.posX;
            float dy = posY - pos.posY;
            float dz = posZ - pos.posZ;

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
            float dx = x - posX;
            float dy = y - posY;

            return dx * dx + dy * dy;
        }
        public float GetExactDist2dSq(Position pos)
        {
            float dx = pos.posX - posX;
            float dy = pos.posY - posY;

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

        public bool IsWithinBox(Position boxOrigin, float length, float width, float height)
        {
            // rotate the WorldObject position instead of rotating the whole cube, that way we can make a simplified
            // is-in-cube check and we have to calculate only one point instead of 4

            // 2PI = 360*, keep in mind that ingame orientation is counter-clockwise
            double rotation = 2 * Math.PI - boxOrigin.GetOrientation();
            double sinVal = Math.Sin(rotation);
            double cosVal = Math.Cos(rotation);

            float BoxDistX = GetPositionX() - boxOrigin.GetPositionX();
            float BoxDistY = GetPositionY() - boxOrigin.GetPositionY();

            float rotX = (float)(boxOrigin.GetPositionX() + BoxDistX * cosVal - BoxDistY * sinVal);
            float rotY = (float)(boxOrigin.GetPositionY() + BoxDistY * cosVal + BoxDistX * sinVal);

            // box edges are parallel to coordiante axis, so we can treat every dimension independently :D
            float dz = GetPositionZ() - boxOrigin.GetPositionZ();
            float dx = rotX - boxOrigin.GetPositionX();
            float dy = rotY - boxOrigin.GetPositionY();
            if ((Math.Abs(dx) > length) || (Math.Abs(dy) > width) || (Math.Abs(dz) > height))
                return false;

            return true;
        }

        public bool IsWithinVerticalCylinder(Position cylinderOrigin, float radius, float height, bool isDoubleVertical = false)
        {
            float verticalDelta = GetPositionZ() - cylinderOrigin.GetPositionZ();
            bool isValidPositionZ = isDoubleVertical ? Math.Abs(verticalDelta) <= height : 0 <= verticalDelta && verticalDelta <= height;

            return isValidPositionZ && IsInDist2d(cylinderOrigin, radius);
        }

        public bool IsInPolygon2D(Position polygonOrigin, List<Position> vertices)
        {
            float testX = GetPositionX();
            float testY = GetPositionY();

            //this method uses the ray tracing algorithm to determine if the point is in the polygon
            bool locatedInPolygon = false;

            for (int vertex = 0; vertex < vertices.Count; ++vertex)
            {
                int nextVertex;

                //repeat loop for all sets of points
                if (vertex == (vertices.Count - 1))
                {
                    //if i is the last vertex, let j be the first vertex
                    nextVertex = 0;
                }
                else
                {
                    //for all-else, let j=(i+1)th vertex
                    nextVertex = vertex + 1;
                }

                float vertX_i = polygonOrigin.GetPositionX() + vertices[vertex].GetPositionX();
                float vertY_i = polygonOrigin.GetPositionY() + vertices[vertex].GetPositionY();
                float vertX_j = polygonOrigin.GetPositionX() + vertices[nextVertex].GetPositionX();
                float vertY_j = polygonOrigin.GetPositionY() + vertices[nextVertex].GetPositionY();

                // following statement checks if testPoint.Y is below Y-coord of i-th vertex
                bool belowLowY = vertY_i > testY;
                // following statement checks if testPoint.Y is below Y-coord of i+1-th vertex
                bool belowHighY = vertY_j > testY;

                /* following statement is true if testPoint.Y satisfies either (only one is possible)
                -->(i).Y < testPoint.Y < (i+1).Y        OR
                -->(i).Y > testPoint.Y > (i+1).Y

                (Note)
                Both of the conditions indicate that a point is located within the edges of the Y-th coordinate
                of the (i)-th and the (i+1)- th vertices of the polygon. If neither of the above
                conditions is satisfied, then it is assured that a semi-infinite horizontal line draw
                to the right from the testpoint will NOT cross the line that connects vertices i and i+1
                of the polygon
                */
                bool withinYsEdges = belowLowY != belowHighY;

                if (withinYsEdges)
                {
                    // this is the slope of the line that connects vertices i and i+1 of the polygon
                    float slopeOfLine = (vertX_j - vertX_i) / (vertY_j - vertY_i);

                    // this looks up the x-coord of a point lying on the above line, given its y-coord
                    float pointOnLine = (slopeOfLine * (testY - vertY_i)) + vertX_i;

                    //checks to see if x-coord of testPoint is smaller than the point on the line with the same y-coord
                    bool isLeftToLine = testX < pointOnLine;

                    if (isLeftToLine)
                    {
                        //this statement changes true to false (and vice-versa)
                        locatedInPolygon = !locatedInPolygon;
                    }//end if (isLeftToLine)
                }//end if (withinYsEdges
            }

            return locatedInPolygon;
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

            float lborder = -1 * (arc / border);                        // in range -pi..0
            float rborder = (arc / border);                             // in range 0..pi
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
            return $"X: {posX} Y: {posY} Z: {posZ} O: {Orientation}";
        }

        public static implicit operator Vector2(Position position)
        {
            return new(position.posX, position.posY);
        }
        public static implicit operator Vector3(Position position)
        {
            return new(position.posX, position.posY, position.posZ);
        }
    }

    public class WorldLocation : Position
    {
        uint _mapId;
        Cell currentCell;
        public ObjectCellMoveState _moveState;

        public Position _newPosition = new();

        public WorldLocation()
        {
            _mapId = 0xFFFFFFFF;
        }

        public WorldLocation(uint mapId = 0xFFFFFFFF, float x = 0, float y = 0) : base(x, y)
        {
            _mapId = mapId;
        }
        public WorldLocation(uint mapId = 0xFFFFFFFF, float x = 0, float y = 0, float z = 0) : base(x, y, z)
        {
            _mapId = mapId;
        }
        public WorldLocation(uint mapId = 0xFFFFFFFF, float x = 0, float y = 0, float z = 0, float o = 0) : base(x, y, z, o)
        {
            _mapId = mapId;
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

        public void WorldRelocate(uint mapId, float x, float y, float z, float o)
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

        public virtual string GetDebugInfo()
        {
            var mapEntry = CliDB.MapStorage.LookupByKey(_mapId);
            return $"MapID: {_mapId} Map name: '{(mapEntry != null ? mapEntry.MapName[Global.WorldMgr.GetDefaultDbcLocale()] : "<not found>")}' {base.ToString()}";
        }

        public override string ToString()
        {
            return $"X: {posX} Y: {posY} Z: {posZ} O: {Orientation} MapId: {_mapId}";
        }
    }
}
