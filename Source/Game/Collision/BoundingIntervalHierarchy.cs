/*
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Game.Collision
{
    public class BIH
    {
        public BIH()
        {
            InitEmpty();
        }

        void InitEmpty()
        {
            tree= new uint[3];
            objects = new uint[0];
            // create space for the first node
            tree[0] = (3u << 30); // dummy leaf
        }

        void BuildHierarchy(List<uint> tempTree, buildData dat, BuildStats stats)
        {
            // create space for the first node
            tempTree.Add(3u << 30); // dummy leaf
            tempTree.Add(0);
            tempTree.Add(0);

            // seed bbox
            var gridBox = new AABound();
            gridBox.lo = bounds.Lo;
            gridBox.hi = bounds.Hi;
            var nodeBox = gridBox;
            // seed subdivide function
            Subdivide(0, (int)(dat.numPrims - 1), tempTree, dat, gridBox, nodeBox, 0, 1, stats);
        }

        void Subdivide(int left, int right, List<uint> tempTree, buildData dat, AABound gridBox, AABound nodeBox, int nodeIndex, int depth, BuildStats stats)
        {
            if ((right - left + 1) <= dat.maxPrims || depth >= 64)
            {
                // write leaf node
                stats.UpdateLeaf(depth, right - left + 1);
                CreateNode(tempTree, nodeIndex, left, right);
                return;
            }
            // calculate extents
            int axis = -1, prevAxis, rightOrig;
            float clipL = float.NaN, clipR = float.NaN, prevClip = float.NaN;
            float split = float.NaN, prevSplit;
            var wasLeft = true;
            while (true)
            {
                prevAxis = axis;
                prevSplit = split;
                // perform quick consistency checks
                var d = gridBox.hi - gridBox.lo;
                for (var i = 0; i < 3; i++)
                {
                    if (nodeBox.hi[i] < gridBox.lo[i] || nodeBox.lo[i] > gridBox.hi[i])
                        Log.outError(LogFilter.Server, "Reached tree area in error - discarding node with: {0} objects", right - left + 1);
                }
                // find longest axis
                axis = (int)d.primaryAxis();
                split = 0.5f * (gridBox.lo[axis] + gridBox.hi[axis]);
                // partition L/R subsets
                clipL = float.NegativeInfinity;
                clipR = float.PositiveInfinity;
                rightOrig = right; // save this for later
                var nodeL = float.PositiveInfinity;
                var nodeR = float.NegativeInfinity;
                for (var i = left; i <= right; )
                {
                    var obj = (int)dat.indices[i];
                    var minb = dat.primBound[obj].Lo[axis];
                    var maxb = dat.primBound[obj].Hi[axis];
                    var center = (minb + maxb) * 0.5f;
                    if (center <= split)
                    {
                        // stay left
                        i++;
                        if (clipL < maxb)
                            clipL = maxb;
                    }
                    else
                    {
                        // move to the right most
                        var t = (int)dat.indices[i];
                        dat.indices[i] = dat.indices[right];
                        dat.indices[right] = (uint)t;
                        right--;
                        if (clipR > minb)
                            clipR = minb;
                    }
                    nodeL = Math.Min(nodeL, minb);
                    nodeR = Math.Max(nodeR, maxb);
                }
                // check for empty space
                if (nodeL > nodeBox.lo[axis] && nodeR < nodeBox.hi[axis])
                {
                    var nodeBoxW = nodeBox.hi[axis] - nodeBox.lo[axis];
                    var nodeNewW = nodeR - nodeL;
                    // node box is too big compare to space occupied by primitives?
                    if (1.3f * nodeNewW < nodeBoxW)
                    {
                        stats.UpdateBVH2();
                        var nextIndex1 = tempTree.Count;
                        // allocate child
                        tempTree.Add(0);
                        tempTree.Add(0);
                        tempTree.Add(0);
                        // write bvh2 clip node
                        stats.UpdateInner();
                        tempTree[nodeIndex + 0] = (uint)((axis << 30) | (1 << 29) | nextIndex1);
                        tempTree[nodeIndex + 1] = FloatToRawIntBits(nodeL);
                        tempTree[nodeIndex + 2] = FloatToRawIntBits(nodeR);
                        // update nodebox and recurse
                        nodeBox.lo[axis] = nodeL;
                        nodeBox.hi[axis] = nodeR;
                        Subdivide(left, rightOrig, tempTree, dat, gridBox, nodeBox, nextIndex1, depth + 1, stats);
                        return;
                    }
                }
                // ensure we are making progress in the subdivision
                if (right == rightOrig)
                {
                    // all left
                    if (prevAxis == axis && MathFunctions.fuzzyEq(prevSplit, split))
                    {
                        // we are stuck here - create a leaf
                        stats.UpdateLeaf(depth, right - left + 1);
                        CreateNode(tempTree, nodeIndex, left, right);
                        return;
                    }
                    if (clipL <= split)
                    {
                        // keep looping on left half
                        gridBox.hi[axis] = split;
                        prevClip = clipL;
                        wasLeft = true;
                        continue;
                    }
                    gridBox.hi[axis] = split;
                    prevClip = float.NaN;
                }
                else if (left > right)
                {
                    // all right
                    right = rightOrig;
                    if (prevAxis == axis && MathFunctions.fuzzyEq(prevSplit, split))
                    {
                        // we are stuck here - create a leaf
                        stats.UpdateLeaf(depth, right - left + 1);
                        CreateNode(tempTree, nodeIndex, left, right);
                        return;
                    }

                    if (clipR >= split)
                    {
                        // keep looping on right half
                        gridBox.lo[axis] = split;
                        prevClip = clipR;
                        wasLeft = false;
                        continue;
                    }
                    gridBox.lo[axis] = split;
                    prevClip = float.NaN;
                }
                else
                {
                    // we are actually splitting stuff
                    if (prevAxis != -1 && !float.IsNaN(prevClip))
                    {
                        // second time through - lets create the previous split
                        // since it produced empty space
                        var nextIndex0 = tempTree.Count;
                        // allocate child node
                        tempTree.Add(0);
                        tempTree.Add(0);
                        tempTree.Add(0);
                        if (wasLeft)
                        {
                            // create a node with a left child
                            // write leaf node
                            stats.UpdateInner();
                            tempTree[nodeIndex + 0] = (uint)((prevAxis << 30) | nextIndex0);
                            tempTree[nodeIndex + 1] = FloatToRawIntBits(prevClip);
                            tempTree[nodeIndex + 2] = FloatToRawIntBits(float.PositiveInfinity);
                        }
                        else
                        {
                            // create a node with a right child
                            // write leaf node
                            stats.UpdateInner();
                            tempTree[nodeIndex + 0] = (uint)((prevAxis << 30) | (nextIndex0 - 3));
                            tempTree[nodeIndex + 1] = FloatToRawIntBits(float.NegativeInfinity);
                            tempTree[nodeIndex + 2] = FloatToRawIntBits(prevClip);
                        }
                        // count stats for the unused leaf
                        depth++;
                        stats.UpdateLeaf(depth, 0);
                        // now we keep going as we are, with a new nodeIndex:
                        nodeIndex = nextIndex0;
                    }
                    break;
                }
            }
            // compute index of child nodes
            var nextIndex = tempTree.Count;
            // allocate left node
            var nl = right - left + 1;
            var nr = rightOrig - (right + 1) + 1;
            if (nl > 0)
            {
                tempTree.Add(0);
                tempTree.Add(0);
                tempTree.Add(0);
            }
            else
                nextIndex -= 3;
            // allocate right node
            if (nr > 0)
            {
                tempTree.Add(0);
                tempTree.Add(0);
                tempTree.Add(0);
            }
            // write leaf node
            stats.UpdateInner();
            tempTree[nodeIndex + 0] = (uint)((axis << 30) | nextIndex);
            tempTree[nodeIndex + 1] = FloatToRawIntBits(clipL);
            tempTree[nodeIndex + 2] = FloatToRawIntBits(clipR);
            // prepare L/R child boxes
            var gridBoxL = gridBox;
            var gridBoxR = gridBox;
            var nodeBoxL = nodeBox;
            var nodeBoxR = nodeBox;
            gridBoxL.hi[axis] = gridBoxR.lo[axis] = split;
            nodeBoxL.hi[axis] = clipL;
            nodeBoxR.lo[axis] = clipR;
            // recurse
            if (nl > 0)
                Subdivide(left, right, tempTree, dat, gridBoxL, nodeBoxL, nextIndex, depth + 1, stats);
            else
                stats.UpdateLeaf(depth + 1, 0);
            if (nr > 0)
                Subdivide(right + 1, rightOrig, tempTree, dat, gridBoxR, nodeBoxR, nextIndex + 3, depth + 1, stats);
            else
                stats.UpdateLeaf(depth + 1, 0);
        }

        public bool ReadFromFile(BinaryReader reader)
        {
            var lo = reader.Read<Vector3>();
            var hi = reader.Read<Vector3>();
            bounds = new AxisAlignedBox(lo, hi);

            var treeSize = reader.ReadUInt32();
            tree = reader.ReadArray<uint>(treeSize);

            var count = reader.ReadUInt32();
            objects = reader.ReadArray<uint>(count);

            return true;
        }

        public void Build<T>(List<T> primitives, uint leafSize = 3, bool printStats = false) where T : IModel
        {
            if (primitives.Count == 0)
            {
                InitEmpty();
                return;
            }

            buildData dat;
            dat.maxPrims = (int)leafSize;
            dat.numPrims = (uint)primitives.Count;
            dat.indices = new uint[dat.numPrims];
            dat.primBound = new AxisAlignedBox[dat.numPrims];
            bounds = primitives[0].GetBounds();
            for (var i = 0; i < dat.numPrims; ++i)
            {
                dat.indices[i] = (uint)i;
                dat.primBound[i] = primitives[i].GetBounds();
                bounds.merge(dat.primBound[i]);
            }
            var tempTree = new List<uint>();
            var stats = new BuildStats();
            BuildHierarchy(tempTree, dat, stats);

            objects = new uint[dat.numPrims];
            for (var i = 0; i < dat.numPrims; ++i)
                objects[i] = dat.indices[i];

            tree = tempTree.ToArray();
        }

        public uint PrimCount() { return (uint)objects.Length; }

        public void IntersectRay(Ray r, WorkerCallback intersectCallback, ref float maxDist, bool stopAtFirst = false)
        {
            var intervalMin = -1.0f;
            var intervalMax = -1.0f;
            var org = r.Origin;
            var dir = r.Direction;
            var invDir = new Vector3();
            for (var i = 0; i < 3; ++i)
            {
                invDir[i] = 1.0f / dir[i];
                if (MathFunctions.fuzzyNe(dir[i], 0.0f))
                {
                    var t1 = (bounds.Lo[i] - org[i]) * invDir[i];
                    var t2 = (bounds.Hi[i] - org[i]) * invDir[i];
                    if (t1 > t2)
                        MathFunctions.Swap<float>(ref t1, ref t2);
                    if (t1 > intervalMin)
                        intervalMin = t1;
                    if (t2 < intervalMax || intervalMax < 0.0f)
                        intervalMax = t2;
                    // intervalMax can only become smaller for other axis,
                    //  and intervalMin only larger respectively, so stop early
                    if (intervalMax <= 0 || intervalMin >= maxDist)
                        return;
                }
            }

            if (intervalMin > intervalMax)
                return;
            intervalMin = Math.Max(intervalMin, 0.0f);
            intervalMax = Math.Min(intervalMax, maxDist);

            var offsetFront = new uint[3];
            var offsetBack = new uint[3];
            var offsetFront3 = new uint[3];
            var offsetBack3 = new uint[3];
            // compute custom offsets from direction sign bit

            for (var i = 0; i < 3; ++i)
            {
                offsetFront[i] = FloatToRawIntBits(dir[i]) >> 31;
                offsetBack[i] = offsetFront[i] ^ 1;
                offsetFront3[i] = offsetFront[i] * 3;
                offsetBack3[i] = offsetBack[i] * 3;

                // avoid always adding 1 during the inner loop
                ++offsetFront[i];
                ++offsetBack[i];
            }

            var stack = new StackNode[64];
            var stackPos = 0;
            var node = 0;

            while (true)
            {
                while (true)
                {
                    var tn = tree[node];
                    var axis = (uint)(tn & (3 << 30)) >> 30;
                    var BVH2 = Convert.ToBoolean(tn & (1 << 29));
                    var offset = (int)(tn & ~(7 << 29));
                    if (!BVH2)
                    {
                        if (axis < 3)
                        {
                            // "normal" interior node
                            var tf = (IntBitsToFloat(tree[(int)(node + offsetFront[axis])]) - org[axis]) * invDir[axis];
                            var tb = (IntBitsToFloat(tree[(int)(node + offsetBack[axis])]) - org[axis]) * invDir[axis];
                            // ray passes between clip zones
                            if (tf < intervalMin && tb > intervalMax)
                                break;
                            var back = (int)(offset + offsetBack3[axis]);
                            node = back;
                            // ray passes through far node only
                            if (tf < intervalMin)
                            {
                                intervalMin = (tb >= intervalMin) ? tb : intervalMin;
                                continue;
                            }
                            node = offset + (int)offsetFront3[axis]; // front
                            // ray passes through near node only
                            if (tb > intervalMax)
                            {
                                intervalMax = (tf <= intervalMax) ? tf : intervalMax;
                                continue;
                            }
                            // ray passes through both nodes
                            // push back node
                            stack[stackPos].node = (uint)back;
                            stack[stackPos].tnear = (tb >= intervalMin) ? tb : intervalMin;
                            stack[stackPos].tfar = intervalMax;
                            stackPos++;
                            // update ray interval for front node
                            intervalMax = (tf <= intervalMax) ? tf : intervalMax;
                            continue;
                        }
                        else
                        {
                            // leaf - test some objects
                            var n = (int)tree[node + 1];
                            while (n > 0)
                            {
                                var hit = intersectCallback.Invoke(r, objects[offset], ref maxDist, stopAtFirst);
                                if (stopAtFirst && hit) 
                                    return;
                                --n;
                                ++offset;
                            }
                            break;
                        }
                    }
                    else
                    {
                        if (axis > 2)
                            return; // should not happen
                        var tf = (IntBitsToFloat(tree[(int)(node + offsetFront[axis])]) - org[axis]) * invDir[axis];
                        var tb = (IntBitsToFloat(tree[(int)(node + offsetBack[axis])]) - org[axis]) * invDir[axis];
                        node = offset;
                        intervalMin = (tf >= intervalMin) ? tf : intervalMin;
                        intervalMax = (tb <= intervalMax) ? tb : intervalMax;
                        if (intervalMin > intervalMax)
                            break;
                        continue;
                    }
                } // traversal loop
                do
                {
                    // stack is empty?
                    if (stackPos == 0)
                        return;
                    // move back up the stack
                    stackPos--;
                    intervalMin = stack[stackPos].tnear;
                    if (maxDist < intervalMin)
                        continue;
                    node = (int)stack[stackPos].node;
                    intervalMax = stack[stackPos].tfar;
                    break;
                } while (true);
            }
        }

        public void IntersectPoint(Vector3 p, WorkerCallback intersectCallback)
        {
            if (!bounds.contains(p))
                return;

            var stack = new StackNode[64];
            var stackPos = 0;
            var node = 0;

            while (true)
            {
                while (true)
                {
                    var tn = tree[node];
                    var axis = (uint)(tn & (3 << 30)) >> 30;
                    var BVH2 = Convert.ToBoolean(tn & (1 << 29));
                    var offset = (int)(tn & ~(7 << 29));
                    if (!BVH2)
                    {
                        if (axis < 3)
                        {
                            // "normal" interior node
                            var tl = IntBitsToFloat(tree[node + 1]);
                            var tr = IntBitsToFloat(tree[node + 2]);
                            // point is between clip zones
                            if (tl < p[(int)axis] && tr > p[axis])
                                break;
                            var right = offset + 3;
                            node = right;
                            // point is in right node only
                            if (tl < p[(int)axis])
                            {
                                continue;
                            }
                            node = offset; // left
                            // point is in left node only
                            if (tr > p[axis])
                            {
                                continue;
                            }
                            // point is in both nodes
                            // push back right node
                            stack[stackPos].node = (uint)right;
                            stackPos++;
                            continue;
                        }
                        else
                        {
                            // leaf - test some objects
                            var n = tree[node + 1];
                            while (n > 0)
                            {
                                intersectCallback.Invoke(p, objects[offset]); // !!!
                                --n;
                                ++offset;
                            }
                            break;
                        }
                    }
                    else // BVH2 node (empty space cut off left and right)
                    {
                        if (axis > 2)
                            return; // should not happen
                        var tl = IntBitsToFloat(tree[node + 1]);
                        var tr = IntBitsToFloat(tree[node + 2]);
                        node = offset;
                        if (tl > p[axis] || tr < p[axis])
                            break;
                        continue;
                    }
                } // traversal loop

                // stack is empty?
                if (stackPos == 0)
                    return;
                // move back up the stack
                stackPos--;
                node = (int)stack[stackPos].node;
            }
        }

        void CreateNode(List<uint> tempTree, int nodeIndex, int left, int right)
        {
            // write leaf node
            tempTree[nodeIndex + 0] = (uint)((3 << 30) | left);
            tempTree[nodeIndex + 1] = (uint)(right - left + 1);
        }

        struct buildData
        {
            public uint[] indices;
            public AxisAlignedBox[] primBound;
            public uint numPrims;
            public int maxPrims;
        }
        struct StackNode
        {
            public uint node;
            public float tnear;
            public float tfar;
        }
        public class BuildStats
        {
            public int numNodes;
            public int numLeaves;
            public int sumObjects;
            public int minObjects;
            public int maxObjects;
            public int sumDepth;
            public int minDepth;
            public int maxDepth;
            int[] numLeavesN = new int[6];
            int numBVH2;

            public BuildStats()
            {
                numNodes = 0;
                numLeaves = 0;
                sumObjects = 0;
                minObjects = 0x0FFFFFFF;
                maxObjects = -1;
                sumDepth = 0;
                minDepth = 0x0FFFFFFF;
                maxDepth = -1;
                numBVH2 = 0;

                for (var i = 0; i < 6; ++i)
                    numLeavesN[i] = 0;
            }

            public void UpdateInner() { numNodes++; }
            public void UpdateBVH2() { numBVH2++; }
            public void UpdateLeaf(int depth, int n)
            {
                numLeaves++;
                minDepth = Math.Min(depth, minDepth);
                maxDepth = Math.Max(depth, maxDepth);
                sumDepth += depth;
                minObjects = Math.Min(n, minObjects);
                maxObjects = Math.Max(n, maxObjects);
                sumObjects += n;
                var nl = Math.Min(n, 5);
                ++numLeavesN[nl];
            }
        }


        AxisAlignedBox bounds;
        uint[] tree;
        uint[] objects;

        [StructLayout(LayoutKind.Explicit)]
        public struct FloatToIntConverter
        {
            [FieldOffset(0)]
            public uint IntValue;
            [FieldOffset(0)]
            public float FloatValue;
        }

        uint FloatToRawIntBits(float f)
        {
            var converter = new FloatToIntConverter();
            converter.FloatValue = f;
            return converter.IntValue;
        }
        float IntBitsToFloat(uint i)
        {
            var converter = new FloatToIntConverter();
            converter.IntValue = i;
            return converter.FloatValue;
        }

    }
    public struct AABound
    {
        public Vector3 lo, hi;
    }
}
