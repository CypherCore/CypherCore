// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Game.DataStorage
{
    internal class UiMapAssignmentStatus
    {
        // distances if inside
        public class InsideStruct
        {
            public float DistanceToRegionBottom { get; set; } = float.MaxValue;
            public float DistanceToRegionCenterSquared { get; set; } = float.MaxValue;
        }

        // distances if outside
        public class OutsideStruct
        {
            public float DistanceToRegionBottom { get; set; } = float.MaxValue;
            public float DistanceToRegionEdgeSquared { get; set; } = float.MaxValue;
            public float DistanceToRegionTop { get; set; } = float.MaxValue;
        }

        public sbyte AreaPriority { get; set; }
        public InsideStruct Inside { get; set; }
        public sbyte MapPriority { get; set; }
        public OutsideStruct Outside { get; set; }
        public UiMapAssignmentRecord UiMapAssignment { get; set; }
        public sbyte WmoPriority { get; set; }

        public UiMapAssignmentStatus()
        {
            Inside = new InsideStruct();
            Outside = new OutsideStruct();
            MapPriority = 3;
            AreaPriority = -1;
            WmoPriority = 3;
        }

        public static bool operator <(UiMapAssignmentStatus left, UiMapAssignmentStatus right)
        {
            bool leftInside = left.IsInside();
            bool rightInside = right.IsInside();

            if (leftInside != rightInside)
                return leftInside;

            if (left.UiMapAssignment != null &&
                right.UiMapAssignment != null &&
                left.UiMapAssignment.UiMapID == right.UiMapAssignment.UiMapID &&
                left.UiMapAssignment.OrderIndex != right.UiMapAssignment.OrderIndex)
                return left.UiMapAssignment.OrderIndex < right.UiMapAssignment.OrderIndex;

            if (left.WmoPriority != right.WmoPriority)
                return left.WmoPriority < right.WmoPriority;

            if (left.AreaPriority != right.AreaPriority)
                return left.AreaPriority < right.AreaPriority;

            if (left.MapPriority != right.MapPriority)
                return left.MapPriority < right.MapPriority;

            if (leftInside)
            {
                if (left.Inside.DistanceToRegionBottom != right.Inside.DistanceToRegionBottom)
                    return left.Inside.DistanceToRegionBottom < right.Inside.DistanceToRegionBottom;

                float leftUiSizeX = left.UiMapAssignment != null ? (left.UiMapAssignment.UiMax.X - left.UiMapAssignment.UiMin.X) : 0.0f;
                float rightUiSizeX = right.UiMapAssignment != null ? (right.UiMapAssignment.UiMax.X - right.UiMapAssignment.UiMin.X) : 0.0f;

                if (leftUiSizeX > float.Epsilon &&
                    rightUiSizeX > float.Epsilon)
                {
                    float leftScale = (left.UiMapAssignment.Region[1].X - left.UiMapAssignment.Region[0].X) / leftUiSizeX;
                    float rightScale = (right.UiMapAssignment.Region[1].X - right.UiMapAssignment.Region[0].X) / rightUiSizeX;

                    if (leftScale != rightScale)
                        return leftScale < rightScale;
                }

                if (left.Inside.DistanceToRegionCenterSquared != right.Inside.DistanceToRegionCenterSquared)
                    return left.Inside.DistanceToRegionCenterSquared < right.Inside.DistanceToRegionCenterSquared;
            }
            else
            {
                if (left.Outside.DistanceToRegionTop != right.Outside.DistanceToRegionTop)
                    return left.Outside.DistanceToRegionTop < right.Outside.DistanceToRegionTop;

                if (left.Outside.DistanceToRegionBottom != right.Outside.DistanceToRegionBottom)
                    return left.Outside.DistanceToRegionBottom < right.Outside.DistanceToRegionBottom;

                if (left.Outside.DistanceToRegionEdgeSquared != right.Outside.DistanceToRegionEdgeSquared)
                    return left.Outside.DistanceToRegionEdgeSquared < right.Outside.DistanceToRegionEdgeSquared;
            }

            return true;
        }

        public static bool operator >(UiMapAssignmentStatus left, UiMapAssignmentStatus right)
        {
            bool leftInside = left.IsInside();
            bool rightInside = right.IsInside();

            if (leftInside != rightInside)
                return leftInside;

            if (left.UiMapAssignment != null &&
                right.UiMapAssignment != null &&
                left.UiMapAssignment.UiMapID == right.UiMapAssignment.UiMapID &&
                left.UiMapAssignment.OrderIndex != right.UiMapAssignment.OrderIndex)
                return left.UiMapAssignment.OrderIndex > right.UiMapAssignment.OrderIndex;

            if (left.WmoPriority != right.WmoPriority)
                return left.WmoPriority > right.WmoPriority;

            if (left.AreaPriority != right.AreaPriority)
                return left.AreaPriority > right.AreaPriority;

            if (left.MapPriority != right.MapPriority)
                return left.MapPriority > right.MapPriority;

            if (leftInside)
            {
                if (left.Inside.DistanceToRegionBottom != right.Inside.DistanceToRegionBottom)
                    return left.Inside.DistanceToRegionBottom > right.Inside.DistanceToRegionBottom;

                float leftUiSizeX = left.UiMapAssignment != null ? (left.UiMapAssignment.UiMax.X - left.UiMapAssignment.UiMin.X) : 0.0f;
                float rightUiSizeX = right.UiMapAssignment != null ? (right.UiMapAssignment.UiMax.X - right.UiMapAssignment.UiMin.X) : 0.0f;

                if (leftUiSizeX > float.Epsilon &&
                    rightUiSizeX > float.Epsilon)
                {
                    float leftScale = (left.UiMapAssignment.Region[1].X - left.UiMapAssignment.Region[0].X) / leftUiSizeX;
                    float rightScale = (right.UiMapAssignment.Region[1].X - right.UiMapAssignment.Region[0].X) / rightUiSizeX;

                    if (leftScale != rightScale)
                        return leftScale > rightScale;
                }

                if (left.Inside.DistanceToRegionCenterSquared != right.Inside.DistanceToRegionCenterSquared)
                    return left.Inside.DistanceToRegionCenterSquared > right.Inside.DistanceToRegionCenterSquared;
            }
            else
            {
                if (left.Outside.DistanceToRegionTop != right.Outside.DistanceToRegionTop)
                    return left.Outside.DistanceToRegionTop > right.Outside.DistanceToRegionTop;

                if (left.Outside.DistanceToRegionBottom != right.Outside.DistanceToRegionBottom)
                    return left.Outside.DistanceToRegionBottom > right.Outside.DistanceToRegionBottom;

                if (left.Outside.DistanceToRegionEdgeSquared != right.Outside.DistanceToRegionEdgeSquared)
                    return left.Outside.DistanceToRegionEdgeSquared > right.Outside.DistanceToRegionEdgeSquared;
            }

            return true;
        }

        private bool IsInside()
        {
            return Outside.DistanceToRegionEdgeSquared < float.Epsilon &&
                   Math.Abs(Outside.DistanceToRegionTop) < float.Epsilon &&
                   Math.Abs(Outside.DistanceToRegionBottom) < float.Epsilon;
        }
    }
}