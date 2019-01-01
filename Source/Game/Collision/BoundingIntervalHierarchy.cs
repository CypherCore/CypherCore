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

using Framework.GameMath;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Game.Collision
{
    public class BIH
    {
        public BIH()
        {
            init_empty();
        }

        void init_empty()
        {
            tree= new uint[3];
            objects = new uint[0];
            // create space for the first node
            tree[0] = (3u << 30); // dummy leaf
        }

        void buildHierarchy(List<uint> tempTree, buildData dat, BuildStats stats)
        {
            // create space for the first node
            tempTree.Add(3u << 30); // dummy leaf
            tempTree.Add(0);
            tempTree.Add(0);

            // seed bbox
            AABound gridBox = new AABound();
            gridBox.lo = bounds.Lo;
            gridBox.hi = bounds.Hi;
            AABound nodeBox = gridBox;
            // seed subdivide function
            subdivide(0, (int)(dat.numPrims - 1), tempTree, dat, gridBox, nodeBox, 0, 1, stats);
        }

        void subdivide(int left, int right, List<uint> tempTree, buildData dat, AABound gridBox, AABound nodeBox, int nodeIndex, int depth, BuildStats stats)
        {
            if ((right - left + 1) <= dat.maxPrims || depth >= 64)
            {
                // write leaf node
                stats.updateLeaf(depth, right - left + 1);
                createNode(tempTree, nodeIndex, left, right);
                return;
            }
            // calculate extents
            int axis = -1, prevAxis, rightOrig;
            float clipL = float.NaN, clipR = float.NaN, prevClip = float.NaN;
            float split = float.NaN, prevSplit;
            bool wasLeft = true;
            while (true)
            {
                prevAxis = axis;
                prevSplit = split;
                // perform quick consistency checks
                Vector3 d = gridBox.hi - gridBox.lo;
                for (int i = 0; i < 3; i++)
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
                float nodeL = float.PositiveInfinity;
                float nodeR = float.NegativeInfinity;
                for (int i = left; i <= right; )
                {
                    int obj = (int)dat.indices[i];
                    float minb = dat.primBound[obj].Lo[axis];
                    float maxb = dat.primBound[obj].Hi[axis];
                    float center = (minb + maxb) * 0.5f;
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
                        int t = (int)dat.indices[i];
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
                    float nodeBoxW = nodeBox.hi[axis] - nodeBox.lo[axis];
                    float nodeNewW = nodeR - nodeL;
                    // node box is too big compare to space occupied by primitives?
                    if (1.3f * nodeNewW < nodeBoxW)
                    {
                        stats.updateBVH2();
                        int nextIndex1 = tempTree.Count;
                        // allocate child
                        tempTree.Add(0);
                        tempTree.Add(0);
                        tempTree.Add(0);
                        // write bvh2 clip node
                        stats.updateInner();
                        tempTree[nodeIndex + 0] = (uint)((axis << 30) | (1 << 29) | nextIndex1);
                        tempTree[nodeIndex + 1] = floatToRawIntBits(nodeL);
                        tempTree[nodeIndex + 2] = floatToRawIntBits(nodeR);
                        // update nodebox and recurse
                        nodeBox.lo[axis] = nodeL;
                        nodeBox.hi[axis] = nodeR;
                        subdivide(left, rightOrig, tempTree, dat, gridBox, nodeBox, nextIndex1, depth + 1, stats);
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
                        stats.updateLeaf(depth, right - left + 1);
                        createNode(tempTree, nodeIndex, left, right);
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
                        stats.updateLeaf(depth, right - left + 1);
                        createNode(tempTree, nodeIndex, left, right);
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
                        int nextIndex0 = tempTree.Count;
                        // allocate child node
                        tempTree.Add(0);
                        tempTree.Add(0);
                        tempTree.Add(0);
                        if (wasLeft)
                        {
                            // create a node with a left child
                            // write leaf node
                            stats.updateInner();
                            tempTree[nodeIndex + 0] = (uint)((prevAxis << 30) | nextIndex0);
                            tempTree[nodeIndex + 1] = floatToRawIntBits(prevClip);
                            tempTree[nodeIndex + 2] = floatToRawIntBits(float.PositiveInfinity);
                        }
                        else
                        {
                            // create a node with a right child
                            // write leaf node
                            stats.updateInner();
                            tempTree[nodeIndex + 0] = (uint)((prevAxis << 30) | (nextIndex0 - 3));
                            tempTree[nodeIndex + 1] = floatToRawIntBits(float.NegativeInfinity);
                            tempTree[nodeIndex + 2] = floatToRawIntBits(prevClip);
                        }
                        // count stats for the unused leaf
                        depth++;
                        stats.updateLeaf(depth, 0);
                        // now we keep going as we are, with a new nodeIndex:
                        nodeIndex = nextIndex0;
                    }
                    break;
                }
            }
            // compute index of child nodes
            int nextIndex = tempTree.Count;
            // allocate left node
            int nl = right - left + 1;
            int nr = rightOrig - (right + 1) + 1;
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
            stats.updateInner();
            tempTree[nodeIndex + 0] = (uint)((axis << 30) | nextIndex);
            tempTree[nodeIndex + 1] = floatToRawIntBits(clipL);
            tempTree[nodeIndex + 2] = floatToRawIntBits(clipR);
            // prepare L/R child boxes
            AABound gridBoxL = gridBox;
            AABound gridBoxR = gridBox;
            AABound nodeBoxL = nodeBox;
            AABound nodeBoxR = nodeBox;
            gridBoxL.hi[axis] = gridBoxR.lo[axis] = split;
            nodeBoxL.hi[axis] = clipL;
            nodeBoxR.lo[axis] = clipR;
            // recurse
            if (nl > 0)
                subdivide(left, right, tempTree, dat, gridBoxL, nodeBoxL, nextIndex, depth + 1, stats);
            else
                stats.updateLeaf(depth + 1, 0);
            if (nr > 0)
                subdivide(right + 1, rightOrig, tempTree, dat, gridBoxR, nodeBoxR, nextIndex + 3, depth + 1, stats);
            else
                stats.updateLeaf(depth + 1, 0);
        }

        public bool readFromFile(BinaryReader reader)
        {
            var lo = reader.Read<Vector3>();
            var hi = reader.Read<Vector3>();
            bounds = new AxisAlignedBox(lo, hi);

            uint treeSize = reader.ReadUInt32();
            tree = reader.ReadArray<uint>(treeSize);

            var count = reader.ReadUInt32();
            objects = reader.ReadArray<uint>(count);

            return true;
        }

        public void build<T>(List<T> primitives, uint leafSize = 3, bool printStats = false) where T : IModel
        {
            if (primitives.Count == 0)
            {
                init_empty();
                return;
            }

            buildData dat;
            dat.maxPrims = (int)leafSize;
            dat.numPrims = (uint)primitives.Count;
            dat.indices = new uint[dat.numPrims];
            dat.primBound = new AxisAlignedBox[dat.numPrims];
            bounds = primitives[0].getBounds();
            for (int i = 0; i < dat.numPrims; ++i)
            {
                dat.indices[i] = (uint)i;
                dat.primBound[i] = primitives[i].getBounds();
                bounds.merge(dat.primBound[i]);
            }
            List<uint> tempTree = new List<uint>();
            BuildStats stats = new BuildStats();
            buildHierarchy(tempTree, dat, stats);

            objects = new uint[dat.numPrims];
            for (int i = 0; i < dat.numPrims; ++i)
                objects[i] = dat.indices[i];

            tree = tempTree.ToArray();
        }

        public uint primCount() { return (uint)objects.Length; }

        public void intersectRay(Ray r, WorkerCallback intersectCallback, ref float maxDist, bool stopAtFirst = false)
        {
            float intervalMin = -1.0f;
            float intervalMax = -1.0f;
            Vector3 org = r.Origin;
            Vector3 dir = r.Direction;
            Vector3 invDir = new Vector3();
            for (int i = 0; i < 3; ++i)
            {
                invDir[i] = 1.0f / dir[i];
                if (MathFunctions.fuzzyNe(dir[i], 0.0f))
                {
                    float t1 = (bounds.Lo[i] - org[i]) * invDir[i];
                    float t2 = (bounds.Hi[i] - org[i]) * invDir[i];
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

            uint[] offsetFront = new uint[3];
            uint[] offsetBack = new uint[3];
            uint[] offsetFront3 = new uint[3];
            uint[] offsetBack3 = new uint[3];
            // compute custom offsets from direction sign bit

            for (int i = 0; i < 3; ++i)
            {
                offsetFront[i] = floatToRawIntBits(dir[i]) >> 31;
                offsetBack[i] = offsetFront[i] ^ 1;
                offsetFront3[i] = offsetFront[i] * 3;
                offsetBack3[i] = offsetBack[i] * 3;

                // avoid always adding 1 during the inner loop
                ++offsetFront[i];
                ++offsetBack[i];
            }

            StackNode[] stack = new StackNode[64];
            int stackPos = 0;
            int node = 0;

            while (true)
            {
                while (true)
                {
                    uint tn = tree[node];
                    uint axis = (uint)(tn & (3 << 30)) >> 30;
                    bool BVH2 = Convert.ToBoolean(tn & (1 << 29));
                    int offset = (int)(tn & ~(7 << 29));
                    if (!BVH2)
                    {
                        if (axis < 3)
                        {
                            // "normal" interior node
                            float tf = (intBitsToFloat(tree[(int)(node + offsetFront[axis])]) - org[axis]) * invDir[axis];
                            float tb = (intBitsToFloat(tree[(int)(node + offsetBack[axis])]) - org[axis]) * invDir[axis];
                            // ray passes between clip zones
                            if (tf < intervalMin && tb > intervalMax)
                                break;
                            int back = (int)(offset + offsetBack3[axis]);
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
                            int n = (int)tree[node + 1];
                            while (n > 0)
                            {
                                bool hit = intersectCallback.Invoke(r, objects[offset], ref maxDist, stopAtFirst);
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
                        float tf = (intBitsToFloat(tree[(int)(node + offsetFront[axis])]) - org[axis]) * invDir[axis];
                        float tb = (intBitsToFloat(tree[(int)(node + offsetBack[axis])]) - org[axis]) * invDir[axis];
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

        public void intersectPoint(Vector3 p, WorkerCallback intersectCallback)
        {
            if (!bounds.contains(p))
                return;

            StackNode[] stack = new StackNode[64];
            int stackPos = 0;
            int node = 0;

            while (true)
            {
                while (true)
                {
                    uint tn = tree[node];
                    uint axis = (uint)(tn & (3 << 30)) >> 30;
                    bool BVH2 = Convert.ToBoolean(tn & (1 << 29));
                    int offset = (int)(tn & ~(7 << 29));
                    if (!BVH2)
                    {
                        if (axis < 3)
                        {
                            // "normal" interior node
                            float tl = intBitsToFloat(tree[node + 1]);
                            float tr = intBitsToFloat(tree[node + 2]);
                            // point is between clip zones
                            if (tl < p[(int)axis] && tr > p[axis])
                                break;
                            int right = offset + 3;
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
                            uint n = tree[node + 1];
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
                        float tl = intBitsToFloat(tree[node + 1]);
                        float tr = intBitsToFloat(tree[node + 2]);
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

        void createNode(List<uint> tempTree, int nodeIndex, int left, int right)
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

                for (int i = 0; i < 6; ++i)
                    numLeavesN[i] = 0;
            }

            public void updateInner() { numNodes++; }
            public void updateBVH2() { numBVH2++; }
            public void updateLeaf(int depth, int n)
            {
                numLeaves++;
                minDepth = Math.Min(depth, minDepth);
                maxDepth = Math.Max(depth, maxDepth);
                sumDepth += depth;
                minObjects = Math.Min(n, minObjects);
                maxObjects = Math.Max(n, maxObjects);
                sumObjects += n;
                int nl = Math.Min(n, 5);
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

        uint floatToRawIntBits(float f)
        {
            FloatToIntConverter converter = new FloatToIntConverter();
            converter.FloatValue = f;
            return converter.IntValue;
        }
        float intBitsToFloat(uint i)
        {
            FloatToIntConverter converter = new FloatToIntConverter();
            converter.IntValue = i;
            return converter.FloatValue;
        }

    }
    public struct AABound
    {
        public Vector3 lo, hi;
    }
}
