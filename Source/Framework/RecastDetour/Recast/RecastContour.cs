using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public static partial class Recast
{
    static int getCornerHeight(int x, int y, int i, int dir,
                               rcCompactHeightfield chf,
                               ref bool isBorderVertex)
    {
        rcCompactSpan s = chf.spans[i];
        int ch = (int)s.y;
        int dirp = (dir + 1) & 0x3;

        uint[] regs = new uint[] { 0, 0, 0, 0 };

        // Combine region and area codes in order to prevent
        // border vertices which are in between two areas to be removed. 
        regs[0] = (uint)(chf.spans[i].reg | (chf.areas[i] << 16));

        if (rcGetCon(s, dir) != RC_NOT_CONNECTED)
        {
            int ax = x + rcGetDirOffsetX(dir);
            int ay = y + rcGetDirOffsetY(dir);
            int ai = (int)chf.cells[ax + ay * chf.width].index + rcGetCon(s, dir);
            rcCompactSpan aSpan = chf.spans[ai];
            ch = Math.Max(ch, (int)aSpan.y);
            regs[1] = (uint)(chf.spans[ai].reg | (chf.areas[ai] << 16));
            if (rcGetCon(aSpan, dirp) != RC_NOT_CONNECTED)
            {
                int ax2 = ax + rcGetDirOffsetX(dirp);
                int ay2 = ay + rcGetDirOffsetY(dirp);
                int ai2 = (int)chf.cells[ax2 + ay2 * chf.width].index + rcGetCon(aSpan, dirp);
                rcCompactSpan as2 = chf.spans[ai2];
                ch = Math.Max(ch, (int)as2.y);
                regs[2] = (uint)(chf.spans[ai2].reg | (chf.areas[ai2] << 16));
            }
        }
        if (rcGetCon(s, dirp) != RC_NOT_CONNECTED)
        {
            int ax = x + rcGetDirOffsetX(dirp);
            int ay = y + rcGetDirOffsetY(dirp);
            int ai = (int)chf.cells[ax + ay * chf.width].index + rcGetCon(s, dirp);
            rcCompactSpan aSpan = chf.spans[ai];
            ch = Math.Max(ch, (int)aSpan.y);
            regs[3] = (uint)(chf.spans[ai].reg | (chf.areas[ai] << 16));
            if (rcGetCon(aSpan, dir) != RC_NOT_CONNECTED)
            {
                int ax2 = ax + rcGetDirOffsetX(dir);
                int ay2 = ay + rcGetDirOffsetY(dir);
                int ai2 = (int)chf.cells[ax2 + ay2 * chf.width].index + rcGetCon(aSpan, dir);
                rcCompactSpan as2 = chf.spans[ai2];
                ch = Math.Max(ch, (int)as2.y);
                regs[2] = (uint)(chf.spans[ai2].reg | (chf.areas[ai2] << 16));
            }
        }

        // Check if the vertex is special edge vertex, these vertices will be removed later.
        for (int j = 0; j < 4; ++j)
        {
            int a = j;
            int b = (j + 1) & 0x3;
            int c = (j + 2) & 0x3;
            int d = (j + 3) & 0x3;

            // The vertex is a border vertex there are two same exterior cells in a row,
            // followed by two interior cells and none of the regions are out of bounds.
            bool twoSameExts = (regs[a] & regs[b] & RC_BORDER_REG) != 0 && regs[a] == regs[b];
            bool twoInts = ((regs[c] | regs[d]) & RC_BORDER_REG) == 0;
            bool intsSameArea = (regs[c] >> 16) == (regs[d] >> 16);
            bool noZeros = regs[a] != 0 && regs[b] != 0 && regs[c] != 0 && regs[d] != 0;
            if (twoSameExts && twoInts && intsSameArea && noZeros)
            {
                isBorderVertex = true;
                break;
            }
        }

        return ch;
    }

    public static void walkContour(int x, int y, int i,
                            rcCompactHeightfield chf,
                            byte[] flags, List<int> points)
    {
        // Choose the first non-connected edge
        byte dir = 0;
        while ((flags[i] & (1 << dir)) == 0)
            dir++;

        byte startDir = dir;
        int starti = i;

        byte area = chf.areas[i];

        int iter = 0;
        while (++iter < 40000)
        {
            if ((flags[i] & (1 << dir)) != 0)
            {
                // Choose the edge corner
                bool isBorderVertex = false;
                bool isAreaBorder = false;
                int px = x;
                int py = getCornerHeight(x, y, i, dir, chf, ref isBorderVertex);
                int pz = y;
                switch (dir)
                {
                    case 0: pz++; break;
                    case 1: px++; pz++; break;
                    case 2: px++; break;
                }
                int r = 0;
                rcCompactSpan s = chf.spans[i];
                if (rcGetCon(s, dir) != RC_NOT_CONNECTED)
                {
                    int ax = x + rcGetDirOffsetX(dir);
                    int ay = y + rcGetDirOffsetY(dir);
                    int ai = (int)chf.cells[ax + ay * chf.width].index + rcGetCon(s, dir);
                    r = (int)chf.spans[ai].reg;
                    if (area != chf.areas[ai])
                        isAreaBorder = true;
                }
                if (isBorderVertex)
                    r |= RC_BORDER_VERTEX;
                if (isAreaBorder)
                    r |= RC_AREA_BORDER;
                points.Add(px);
                points.Add(py);
                points.Add(pz);
                points.Add(r);

                flags[i] &= (byte)(~(1 << dir)); // Remove visited edges
                dir = (byte)((dir + 1) & 0x3);  // Rotate CW
            }
            else
            {
                int ni = -1;
                int nx = x + rcGetDirOffsetX(dir);
                int ny = y + rcGetDirOffsetY(dir);
                rcCompactSpan s = chf.spans[i];
                if (rcGetCon(s, dir) != RC_NOT_CONNECTED)
                {
                    rcCompactCell nc = chf.cells[nx + ny * chf.width];
                    ni = (int)nc.index + rcGetCon(s, dir);
                }
                if (ni == -1)
                {
                    // Should not happen.
                    return;
                }
                x = nx;
                y = ny;
                i = ni;
                dir = (byte)((dir + 3) & 0x3);  // Rotate CCW
            }

            if (starti == i && startDir == dir)
            {
                break;
            }
        }
    }

    public static float distancePtSeg(int x, int z,
                               int px, int pz,
                               int qx, int qz)
    {
        /*	float pqx = (float)(qx - px);
            float pqy = (float)(qy - py);
            float pqz = (float)(qz - pz);
            float dx = (float)(x - px);
            float dy = (float)(y - py);
            float dz = (float)(z - pz);
            float d = pqx*pqx + pqy*pqy + pqz*pqz;
            float t = pqx*dx + pqy*dy + pqz*dz;
            if (d > 0)
                t /= d;
            if (t < 0)
                t = 0;
            else if (t > 1)
                t = 1;

            dx = px + t*pqx - x;
            dy = py + t*pqy - y;
            dz = pz + t*pqz - z;

            return dx*dx + dy*dy + dz*dz;*/

        float pqx = (float)(qx - px);
        float pqz = (float)(qz - pz);
        float dx = (float)(x - px);
        float dz = (float)(z - pz);
        float d = pqx * pqx + pqz * pqz;
        float t = pqx * dx + pqz * dz;
        if (d > 0)
            t /= d;
        if (t < 0)
            t = 0;
        else if (t > 1)
            t = 1;

        dx = px + t * pqx - x;
        dz = pz + t * pqz - z;

        return dx * dx + dz * dz;
    }

    public static void simplifyContour(List<int> points, List<int> simplified,
                                double maxError, int maxEdgeLen, int buildFlags)
    {
        // Add initial points.
        bool hasConnections = false;
        for (int i = 0; i < points.Count; i += 4)
        {
            if ((points[i + 3] & RC_CONTOUR_REG_MASK) != 0)
            {
                hasConnections = true;
                break;
            }
        }

        if (hasConnections)
        {
            // The contour has some portals to other regions.
            // Add a new point to every location where the region changes.
            for (int i = 0, ni = points.Count / 4; i < ni; ++i)
            {
                int ii = (i + 1) % ni;
                bool differentRegs = (points[i * 4 + 3] & RC_CONTOUR_REG_MASK) != (points[ii * 4 + 3] & RC_CONTOUR_REG_MASK);
                bool areaBorders = (points[i * 4 + 3] & RC_AREA_BORDER) != (points[ii * 4 + 3] & RC_AREA_BORDER);
                if (differentRegs || areaBorders)
                {
                    simplified.Add(points[i * 4 + 0]);
                    simplified.Add(points[i * 4 + 1]);
                    simplified.Add(points[i * 4 + 2]);
                    simplified.Add(i);
                }
            }
        }

        if (simplified.Count == 0)
        {
            // If there is no connections at all,
            // create some initial points for the simplification process. 
            // Find lower-left and upper-right vertices of the contour.
            int llx = points[0];
            int lly = points[1];
            int llz = points[2];
            int lli = 0;
            int urx = points[0];
            int ury = points[1];
            int urz = points[2];
            int uri = 0;
            for (int i = 0; i < points.Count; i += 4)
            {
                int x = points[i + 0];
                int y = points[i + 1];
                int z = points[i + 2];
                if (x < llx || (x == llx && z < llz))
                {
                    llx = x;
                    lly = y;
                    llz = z;
                    lli = i / 4;
                }
                if (x > urx || (x == urx && z > urz))
                {
                    urx = x;
                    ury = y;
                    urz = z;
                    uri = i / 4;
                }
            }
            simplified.Add(llx);
            simplified.Add(lly);
            simplified.Add(llz);
            simplified.Add(lli);

            simplified.Add(urx);
            simplified.Add(ury);
            simplified.Add(urz);
            simplified.Add(uri);
        }

        // Add points until all raw points are within
        // error tolerance to the simplified shape.
        int pn = points.Count / 4;
        for (int i = 0; i < simplified.Count / 4;)
        {
            int ii = (i + 1) % (simplified.Count / 4);

            int ax = simplified[i * 4 + 0];
            int az = simplified[i * 4 + 2];
            int ai = simplified[i * 4 + 3];

            int bx = simplified[ii * 4 + 0];
            int bz = simplified[ii * 4 + 2];
            int bi = simplified[ii * 4 + 3];

            // Find maximum deviation from the segment.
            float maxd = 0;
            int maxi = -1;
            int ci, cinc, endi;

            // Traverse the segment in lexilogical order so that the
            // max deviation is calculated similarly when traversing
            // opposite segments.
            if (bx > ax || (bx == ax && bz > az))
            {
                cinc = 1;
                ci = (ai + cinc) % pn;
                endi = bi;
            }
            else
            {
                cinc = pn - 1;
                ci = (bi + cinc) % pn;
                endi = ai;
                rcSwap(ref ax, ref bx);
                rcSwap(ref az, ref bz);
            }

            // Tessellate only outer edges or edges between areas.
            if ((points[ci * 4 + 3] & RC_CONTOUR_REG_MASK) == 0 ||
                (points[ci * 4 + 3] & RC_AREA_BORDER) != 0)
            {
                while (ci != endi)
                {
                    float d = distancePtSeg(points[ci * 4 + 0], points[ci * 4 + 2], ax, az, bx, bz);
                    if (d > maxd)
                    {
                        maxd = d;
                        maxi = ci;
                    }
                    ci = (ci + cinc) % pn;
                }
            }


            // If the max deviation is larger than accepted error,
            // add new point, else continue to next segment.
            if (maxi != -1 && maxd > (maxError * maxError))
            {
                // Add space for the new point.
                //simplified.resize(simplified.Count+4);
                rccsResizeList(simplified, simplified.Count + 4);
                int n = simplified.Count / 4;
                for (int j = n - 1; j > i; --j)
                {
                    simplified[j * 4 + 0] = simplified[(j - 1) * 4 + 0];
                    simplified[j * 4 + 1] = simplified[(j - 1) * 4 + 1];
                    simplified[j * 4 + 2] = simplified[(j - 1) * 4 + 2];
                    simplified[j * 4 + 3] = simplified[(j - 1) * 4 + 3];
                }
                // Add the point.
                simplified[(i + 1) * 4 + 0] = points[maxi * 4 + 0];
                simplified[(i + 1) * 4 + 1] = points[maxi * 4 + 1];
                simplified[(i + 1) * 4 + 2] = points[maxi * 4 + 2];
                simplified[(i + 1) * 4 + 3] = maxi;
            }
            else
            {
                ++i;
            }
        }

        // Split too long edges.
        if (maxEdgeLen > 0 && (buildFlags & (int)(rcBuildContoursFlags.RC_CONTOUR_TESS_WALL_EDGES | rcBuildContoursFlags.RC_CONTOUR_TESS_AREA_EDGES)) != 0)
        {
            for (int i = 0; i < simplified.Count / 4;)
            {
                int ii = (i + 1) % (simplified.Count / 4);

                int ax = simplified[i * 4 + 0];
                int az = simplified[i * 4 + 2];
                int ai = simplified[i * 4 + 3];

                int bx = simplified[ii * 4 + 0];
                int bz = simplified[ii * 4 + 2];
                int bi = simplified[ii * 4 + 3];

                // Find maximum deviation from the segment.
                int maxi = -1;
                int ci = (ai + 1) % pn;

                // Tessellate only outer edges or edges between areas.
                bool tess = false;
                // Wall edges.
                if ((buildFlags & (int)rcBuildContoursFlags.RC_CONTOUR_TESS_WALL_EDGES) != 0 && (points[ci * 4 + 3] & RC_CONTOUR_REG_MASK) == 0)
                    tess = true;
                // Edges between areas.
                if ((buildFlags & (int)rcBuildContoursFlags.RC_CONTOUR_TESS_AREA_EDGES) != 0 && (points[ci * 4 + 3] & RC_AREA_BORDER) != 0)
                    tess = true;

                if (tess)
                {
                    int dx = bx - ax;
                    int dz = bz - az;
                    if (dx * dx + dz * dz > maxEdgeLen * maxEdgeLen)
                    {
                        // Round based on the segments in lexilogical order so that the
                        // max tesselation is consistent regardles in which direction
                        // segments are traversed.
                        int n = bi < ai ? (bi + pn - ai) : (bi - ai);
                        if (n > 1)
                        {
                            if (bx > ax || (bx == ax && bz > az))
                                maxi = (ai + n / 2) % pn;
                            else
                                maxi = (ai + (n + 1) / 2) % pn;
                        }
                    }
                }

                // If the max deviation is larger than accepted error,
                // add new point, else continue to next segment.
                if (maxi != -1)
                {
                    // Add space for the new point.
                    rccsResizeList(simplified, simplified.Count + 4);
                    int n = simplified.Count / 4;
                    for (int j = n - 1; j > i; --j)
                    {
                        simplified[j * 4 + 0] = simplified[(j - 1) * 4 + 0];
                        simplified[j * 4 + 1] = simplified[(j - 1) * 4 + 1];
                        simplified[j * 4 + 2] = simplified[(j - 1) * 4 + 2];
                        simplified[j * 4 + 3] = simplified[(j - 1) * 4 + 3];
                    }
                    // Add the point.
                    simplified[(i + 1) * 4 + 0] = points[maxi * 4 + 0];
                    simplified[(i + 1) * 4 + 1] = points[maxi * 4 + 1];
                    simplified[(i + 1) * 4 + 2] = points[maxi * 4 + 2];
                    simplified[(i + 1) * 4 + 3] = maxi;
                }
                else
                {
                    ++i;
                }
            }
        }

        for (int i = 0; i < simplified.Count / 4; ++i)
        {
            // The edge vertex flag is take from the current raw point,
            // and the neighbour region is take from the next raw point.
            int ai = (simplified[i * 4 + 3] + 1) % pn;
            int bi = simplified[i * 4 + 3];
            simplified[i * 4 + 3] = (points[ai * 4 + 3] & (RC_CONTOUR_REG_MASK | RC_AREA_BORDER)) | (points[bi * 4 + 3] & RC_BORDER_VERTEX);
        }

    }

    public static void removeDegenerateSegments(List<int> simplified)
    {
        // Remove adjacent vertices which are equal on xz-plane,
        // or else the triangulator will get confused.
        for (int i = 0; i < simplified.Count / 4; ++i)
        {
            int ni = i + 1;
            if (ni >= (simplified.Count / 4))
                ni = 0;

            if (simplified[i * 4 + 0] == simplified[ni * 4 + 0] &&
                simplified[i * 4 + 2] == simplified[ni * 4 + 2])
            {
                // Degenerate segment, remove.
                for (int j = i; j < simplified.Count / 4 - 1; ++j)
                {
                    simplified[j * 4 + 0] = simplified[(j + 1) * 4 + 0];
                    simplified[j * 4 + 1] = simplified[(j + 1) * 4 + 1];
                    simplified[j * 4 + 2] = simplified[(j + 1) * 4 + 2];
                    simplified[j * 4 + 3] = simplified[(j + 1) * 4 + 3];
                }
                //simplified.Capacity = (simplified.Count-4);
                rccsResizeList(simplified, simplified.Count - 4);
            }
        }
    }

    public static int calcAreaOfPolygon2D(int[] verts, int nverts)
    {
        int area = 0;
        for (int i = 0, j = nverts - 1; i < nverts; j = i++)
        {
            int viStart = i * 4;
            int vjStart = j * 4;
            area += verts[viStart + 0] * verts[vjStart + 2] - verts[vjStart + 0] * verts[viStart + 2];
        }
        return (area + 1) / 2;
    }

    public static bool ileft(int[] a, int[] b, int[] c)
    {
        return (b[0] - a[0]) * (c[2] - a[2]) - (c[0] - a[0]) * (b[2] - a[2]) <= 0;
    }


    public static bool ileft(int[] a, int aStart, int[] b, int bStart, int[] c, int cStart)
    {
        return (b[bStart + 0] - a[aStart + 0]) * (c[cStart + 2] - a[aStart + 2]) - (c[cStart + 0] - a[aStart + 0]) * (b[bStart + 2] - a[aStart + 2]) <= 0;
    }

    public static void getClosestIndices(int[] vertsa, int nvertsa,
                                  int[] vertsb, int nvertsb,
                                  ref int ia, ref int ib)
    {
        int closestDist = 0xfffffff;
        ia = -1;
        ib = -1;
        for (int i = 0; i < nvertsa; ++i)
        {
            int i_n = (i + 1) % nvertsa;
            int ip = (i + nvertsa - 1) % nvertsa;
            int vaStart = i * 4;
            int vanStart = i_n * 4;
            int vapStart = ip * 4;

            for (int j = 0; j < nvertsb; ++j)
            {
                int vbStart = j * 4;
                // vb must be "infront" of va.
                if (ileft(vertsa, vapStart, vertsa, vaStart, vertsb, vbStart) && ileft(vertsa, vaStart, vertsa, vanStart, vertsb, vbStart))
                {
                    int dx = vertsb[vbStart + 0] - vertsa[vaStart + 0];
                    int dz = vertsb[vbStart + 2] - vertsa[vaStart + 2];
                    int d = dx * dx + dz * dz;
                    if (d < closestDist)
                    {
                        ia = i;
                        ib = j;
                        closestDist = d;
                    }
                }
            }
        }
    }

    public static bool mergeContours(ref rcContour ca, ref rcContour cb, int ia, int ib)
    {
        int maxVerts = ca.nverts + cb.nverts + 2;
        int[] verts = new int[maxVerts * 4];//(int*)rcAlloc(sizeof(int)*maxVerts*4, RC_ALLOC_PERM);
        if (verts == null)
            return false;

        int nv = 0;

        // Copy contour A.
        for (int i = 0; i <= ca.nverts; ++i)
        {
            //int* dst = &verts[nv*4];
            int dstIndex = nv * 4;
            int srcIndex = ((ia + i) % ca.nverts) * 4;
            for (int j = 0; j < 4; ++j)
            {
                verts[dstIndex + j] = ca.verts[srcIndex + j];
            }
            nv++;
        }

        // Copy contour B
        for (int i = 0; i <= cb.nverts; ++i)
        {
            int dstIndex = nv * 4;
            int srcIndex = ((ib + i) % cb.nverts) * 4;
            //int* dst = &verts[nv*4];
            //const int* src = &cb.verts[((ib+i)%cb.nverts)*4];
            for (int j = 0; j < 4; ++j)
            {
                verts[dstIndex + j] = cb.verts[srcIndex + j];
            }
            nv++;
        }

        ca.verts = verts;
        ca.nverts = nv;

        cb.verts = null;
        cb.nverts = 0;

        return true;
    }

    static void mergeRegionHoles(rcContext ctx, rcContourRegion region)
    {
        // Sort holes from left to right.
        for (int i = 0; i < region.nholes; i++)
            findLeftMostVertex(region.holes[i].contour, ref region.holes[i].minx, ref region.holes[i].minz, ref region.holes[i].leftmost);

        var list = region.holes.ToList();
        list.RemoveAll(p => p.contour == null);
        list.Sort(new ContourHoldCompare<rcContourHole>());
        region.holes = list.ToArray();

        int maxVerts = region.outline.nverts;
        for (int i = 0; i < region.nholes; i++)
            maxVerts += region.holes[i].contour.nverts;

        rcPotentialDiagonal[] diags = new rcPotentialDiagonal[maxVerts];

        rcContour outline = region.outline;

        // Merge holes into the outline one by one.
        for (int i = 0; i < region.nholes; i++)
        {
            rcContour hole = region.holes[i].contour;

            int index = -1;
            int bestVertex = region.holes[i].leftmost;
            for (int iter = 0; iter < hole.nverts; iter++)
            {
                // Find potential diagonals.
                // The 'best' vertex must be in the cone described by 3 cosequtive vertices of the outline.
                // ..o j-1
                //   |
                //   |   * best
                //   |
                // j o-----o j+1
                //         :
                int ndiags = 0;
                int cornerIndex = bestVertex * 4;
                for (int j = 0; j < outline.nverts; j++)
                {
                    if (inCone(j, outline.nverts, outline.verts, hole.verts, cornerIndex))
                    {
                        int dx = outline.verts[j * 4 + 0] - hole.verts[cornerIndex + 0];
                        int dz = outline.verts[j * 4 + 2] - hole.verts[cornerIndex + 2];
                        diags[ndiags] = new rcPotentialDiagonal();
                        diags[ndiags].vert = j;
                        diags[ndiags].dist = dx * dx + dz * dz;
                        ndiags++;
                    }
                }

                List<rcPotentialDiagonal> sortedDiags = new();
                for (var gg = 0; gg < ndiags; ++gg)
                    sortedDiags.Add(diags[gg]);

                // Sort potential diagonals by distance, we want to make the connection as short as possible.
                sortedDiags.Sort(new PotentialDiagonalCompare<rcPotentialDiagonal>());

                // Find a diagonal that is not intersecting the outline not the remaining holes.
                index = -1;
                for (int j = 0; j < ndiags; j++)
                {
                    int ptStart = sortedDiags[j].vert * 4;
                    bool intersect = intersectSegCountour(outline.verts, ptStart, hole.verts, cornerIndex, sortedDiags[i].vert, outline.nverts, outline.verts, 0);
                    for (int k = i; k < region.nholes && !intersect; k++)
                        intersect |= intersectSegCountour(outline.verts, ptStart, hole.verts, cornerIndex, -1, region.holes[k].contour.nverts, region.holes[k].contour.verts, 0);
                    if (!intersect)
                    {
                        index = sortedDiags[j].vert;
                        break;
                    }
                }

                // If found non-intersecting diagonal, stop looking.
                if (index != -1)
                    break;
                // All the potential diagonals for the current vertex were intersecting, try next vertex.
                bestVertex = (bestVertex + 1) % hole.nverts;
            }

            if (index == -1)
            {
                //ctx->log(RC_LOG_WARNING, "mergeHoles: Failed to find merge points for %p and %p.", region.outline, hole);
                continue;
            }
            if (!mergeContours(ref region.outline, ref hole, index, bestVertex))
            {
                //ctx->log(RC_LOG_WARNING, "mergeHoles: Failed to merge contours %p and %p.", region.outline, hole);
                continue;
            }
        }
    }

    /// @par
    ///
    /// The raw contours will match the region outlines exactly. The @p maxError and @p maxEdgeLen
    /// parameters control how closely the simplified contours will match the raw contours.
    ///
    /// Simplified contours are generated such that the vertices for portals between areas match up. 
    /// (They are considered mandatory vertices.)
    ///
    /// Setting @p maxEdgeLength to zero will disabled the edge length feature.
    /// 
    /// See the #rcConfig documentation for more information on the configuration parameters.
    /// 
    /// @see rcAllocContourSet, rcCompactHeightfield, rcContourSet, rcConfig
    public static bool rcBuildContours(rcContext ctx, rcCompactHeightfield chf, double maxError, int maxEdgeLen, rcContourSet cset, int buildFlags = 1)
    {
        Debug.Assert(ctx != null, "rcContext is null");

        int w = chf.width;
        int h = chf.height;
        int borderSize = chf.borderSize;

        ctx.startTimer(rcTimerLabel.RC_TIMER_BUILD_CONTOURS);

        rcVcopy(cset.bmin, chf.bmin);
        rcVcopy(cset.bmax, chf.bmax);
        if (borderSize > 0)
        {
            // If the heightfield was build with bordersize, remove the offset.
            float pad = borderSize * chf.cs;
            cset.bmin[0] += pad;
            cset.bmin[2] += pad;
            cset.bmax[0] -= pad;
            cset.bmax[2] -= pad;
        }
        cset.cs = chf.cs;
        cset.ch = chf.ch;
        cset.width = chf.width - chf.borderSize * 2;
        cset.height = chf.height - chf.borderSize * 2;
        cset.borderSize = chf.borderSize;
        cset.maxError = (float)maxError;

        int maxContours = Math.Max((int)chf.maxRegions, 8);
        cset.conts = new rcContour[maxContours];
        for (var i  = 0; i < maxContours; ++i)
            cset.conts[i] = new rcContour();
        cset.nconts = 0;

        byte[] flags = new byte[chf.spanCount];
        if (flags == null)
        {
            ctx.log(rcLogCategory.RC_LOG_ERROR, "rcBuildContours: Out of memory 'flags' " + chf.spanCount);
            return false;
        }

        ctx.startTimer(rcTimerLabel.RC_TIMER_BUILD_CONTOURS_TRACE);

        // Mark boundaries.
        for (int y = 0; y < h; ++y)
        {
            for (int x = 0; x < w; ++x)
            {
                rcCompactCell c = chf.cells[x + y * w];
                for (int i = (int)c.index, ni = (int)(c.index + c.count); i < ni; ++i)
                {
                    byte res = 0;
                    rcCompactSpan s = chf.spans[i];
                    if (chf.spans[i].reg == 0 || (chf.spans[i].reg & RC_BORDER_REG) != 0)
                    {
                        flags[i] = 0;
                        continue;
                    }
                    for (int dir = 0; dir < 4; ++dir)
                    {
                        ushort r = 0;
                        if (rcGetCon(s, dir) != RC_NOT_CONNECTED)
                        {
                            int ax = x + rcGetDirOffsetX(dir);
                            int ay = y + rcGetDirOffsetY(dir);
                            int ai = (int)chf.cells[ax + ay * w].index + rcGetCon(s, dir);
                            r = chf.spans[ai].reg;
                        }
                        if (r == chf.spans[i].reg)
                            res |= (byte)(1 << dir);
                    }
                    flags[i] = (byte)(res ^ 0xf); // Inverse, mark non connected edges.
                }
            }
        }

        ctx.stopTimer(rcTimerLabel.RC_TIMER_BUILD_CONTOURS_TRACE);

        List<int> verts = new(256);
        List<int> simplified = new(64);

        for (int y = 0; y < h; ++y)
        {
            for (int x = 0; x < w; ++x)
            {
                rcCompactCell c = chf.cells[x + y * w];
                for (int i = (int)c.index, ni = (int)(c.index + c.count); i < ni; ++i)
                {
                    if (flags[i] == 0 || flags[i] == 0xf)
                    {
                        flags[i] = 0;
                        continue;
                    }
                    ushort reg = chf.spans[i].reg;
                    if (reg == 0 || (reg & RC_BORDER_REG) != 0)
                    {
                        continue;
                    }
                    byte area = chf.areas[i];

                    verts.Clear();
                    simplified.Clear();

                    ctx.startTimer(rcTimerLabel.RC_TIMER_BUILD_CONTOURS_TRACE);
                    walkContour(x, y, i, chf, flags, verts);
                    ctx.stopTimer(rcTimerLabel.RC_TIMER_BUILD_CONTOURS_TRACE);

                    ctx.startTimer(rcTimerLabel.RC_TIMER_BUILD_CONTOURS_SIMPLIFY);
                    simplifyContour(verts, simplified, maxError, maxEdgeLen, buildFlags);
                    removeDegenerateSegments(simplified);
                    ctx.stopTimer(rcTimerLabel.RC_TIMER_BUILD_CONTOURS_SIMPLIFY);

                    // Store region.contour remap info.
                    // Create contour.
                    if (simplified.Count / 4 >= 3)
                    {
                        if (cset.nconts >= maxContours)
                        {
                            // Allocate more contours.
                            // This can happen when there are tiny holes in the heightfield.
                            int oldMax = maxContours;
                            maxContours *= 2;
                            rcContour[] newConts = new rcContour[maxContours];
                            for (int j = 0; j < cset.nconts; ++j)
                            {
                                newConts[j] = cset.conts[j];
                                // Reset source pointers to prevent data deletion.
                                cset.conts[j].verts = null;
                                cset.conts[j].rverts = null;
                            }
                            cset.conts = newConts;

                            ctx.log(rcLogCategory.RC_LOG_WARNING, "rcBuildContours: Expanding max contours from " + oldMax + " to " + maxContours);
                        }

                        int contId = cset.nconts;

                        if (contId == 7)
                        {

                        }
                        cset.nconts++;
                        rcContour cont = cset.conts[contId];

                        cont.nverts = simplified.Count / 4;
                        cont.verts = new int[cont.nverts * 4];
                        if (cont.verts == null)
                        {
                            ctx.log(rcLogCategory.RC_LOG_ERROR, "rcBuildContours: Out of memory 'verts' " + cont.nverts);
                            return false;
                        }

                        for (int j = 0; j < cont.nverts * 4; ++j)
                        {
                            cont.verts[j] = simplified[j];
                        }
                        if (borderSize > 0)
                        {
                            // If the heightfield was build with bordersize, remove the offset.
                            for (int j = 0; j < cont.nverts; ++j)
                            {
                                cont.verts[j * 4] -= borderSize;
                                cont.verts[j * 4 + 2] -= borderSize;
                            }
                        }

                        cont.nrverts = verts.Count / 4;
                        cont.rverts = new int[cont.nrverts * 4];
                        if (cont.rverts == null)
                        {
                            ctx.log(rcLogCategory.RC_LOG_ERROR, "rcBuildContours: Out of memory 'rverts' " + cont.nrverts);
                            return false;
                        }

                        for (int j = 0; j < cont.nrverts * 4; ++j)
                        {
                            cont.rverts[j] = verts[j];
                        }
                        if (borderSize > 0)
                        {
                            // If the heightfield was build with bordersize, remove the offset.
                            for (int j = 0; j < cont.nrverts; ++j)
                            {
                                cont.rverts[j * 4] -= borderSize;
                                cont.rverts[j * 4 + 2] -= borderSize;
                            }
                        }

                        cont.reg = reg;
                        cont.area = area;

                        cset.conts[contId] = cont;
                    }
                }
            }
        }

        // Merge holes if needed.
        if (cset.nconts > 0)
        {
            // Calculate winding of all polygons.
            sbyte[] winding = new sbyte[cset.nconts];

            int nholes = 0;
            for (int i = 0; i < cset.nconts; ++i)
            {
                rcContour cont = cset.conts[i];
                // If the contour is wound backwards, it is a hole.
                winding[i] = (sbyte)(calcAreaOfPolygon2D(cont.verts, cont.nverts) < 0 ? -1 : 1);
                if (winding[i] < 0)
                    nholes++;
            }

            if (nholes > 0)
            {
                // Collect outline contour and holes contours per region.
                // We assume that there is one outline and multiple holes.
                int nregions = chf.maxRegions + 1;
                rcContourRegion[] regions = new rcContourRegion[nregions];
                for (var i = 0; i < nregions; ++i)
                    regions[i] = new rcContourRegion();

                rcContourHole[] holes = new rcContourHole[cset.nconts];
                for (var i = 0; i < cset.nconts; ++i)
                    holes[i] = new rcContourHole();

                for (int i = 0; i < cset.nconts; ++i)
                {
                    rcContour cont = cset.conts[i];
                    // Positively would contours are outlines, negative holes.
                    if (winding[i] > 0)
                    {
                        regions[cont.reg].outline = cont;
                    }
                    else
                    {
                        regions[cont.reg].nholes++;
                    }
                }
                int index = 0;
                for (int i = 0; i < nregions; i++)
                {
                    if (regions[i].nholes > 0)
                    {
                        regions[i].holes = new rcContourHole[cset.nconts];
                        Array.Copy(holes, index, regions[i].holes, 0, cset.nconts - index);
                        index += regions[i].nholes;
                        regions[i].nholes = 0;
                    }
                }
                for (int i = 0; i < cset.nconts; ++i)
                {
                    rcContour cont = cset.conts[i];
                    rcContourRegion reg = regions[cont.reg];
                    if (winding[i] < 0)
                        reg.holes[reg.nholes++].contour = cont;
                }

                // Finally merge each regions holes into the outline.
                for (int i = 0; i < nregions; i++)
                {
                    rcContourRegion reg = regions[i];
                    if (reg.nholes == 0)
                        continue;

                    if (reg.outline.verts != null)
                    {
                        mergeRegionHoles(ctx, reg);
                    }
                    else
                    {
                        // The region does not have an outline.
                        // This can happen if the contour becaomes selfoverlapping because of
                        // too aggressive simplification settings.
                        ctx.log(rcLogCategory.RC_LOG_ERROR, string.Format("rcBuildContours: Bad outline for region {0}, contour simplification is likely too aggressive.", i));
                    }
                }
            }
        }

        return true;
    }

    static bool inCone(int i, int n, int[] verts, int[] pj, int pjStart)
    {

        int piStart = i * 4;
        int pi1Start = next(i, n) * 4;
        int pin1Start = prev(i, n) * 4;

        // If P[i] is a convex vertex [ i+1 left or on (i-1,i) ].
        if (leftOn(verts, pin1Start, verts, piStart, verts, pi1Start))
            return left(verts, piStart, pj, pjStart, verts, pin1Start) && left(pj, pjStart, verts, piStart, verts, pi1Start);
        // Assume (i-1,i,i+1) not collinear.
        // else P[i] is reflex.
        return !(leftOn(verts, piStart, pj, pjStart, verts, pi1Start) && leftOn(pj, pjStart, verts, piStart, verts, pin1Start));
    }

    // Finds the lowest leftmost vertex of a contour.
    static void findLeftMostVertex(rcContour contour, ref int minx, ref int minz, ref int leftmost)
    {
        minx = contour.verts[0];
        minz = contour.verts[2];
        leftmost = 0;
        for (int i = 1; i < contour.nverts; i++)
        {
            int x = contour.verts[i * 4 + 0];
            int z = contour.verts[i * 4 + 2];
            if (x < minx || (x == minx && z < minz))
            {
                minx = x;
                minz = z;
                leftmost = i;
            }
        }
    }

    static bool intersectSegCountour(int[] d0, int d0Start, int[] d1, int d1Start, int i, int n, int[] verts, int vertsStart)
    {
        // For each edge (k,k+1) of P
        for (int k = 0; k < n; k++)
        {
            int k1 = next(k, n);
            // Skip edges incident to i.
            if (i == k || i == k1)
                continue;
            int p0Start = k * 4;
            int p1Start = k1 * 4;
            if (vequal(d0, d0Start, verts, p0Start) || vequal(d1, d1Start, verts, p0Start) || vequal(d0, d0Start, verts, p1Start) || vequal(d1, d1Start, verts, p1Start))
                continue;

            if (intersect(d0, d0Start, d1, d1Start, verts, p0Start, verts, p1Start))
                return true;
        }
        return false;
    }

    class PotentialDiagonalCompare<T> : Comparer<T> where T : rcPotentialDiagonal
    {
        public override int Compare(T va, T vb)
        {
            if (va.dist < vb.dist)
                return -1;
            if (va.dist > vb.dist)
                return 1;
            return 0;
        }
    }

    class ContourHoldCompare<T> : Comparer<T> where T : rcContourHole
    {
        public override int Compare(T a, T b)
        {
            if (a.minx == b.minx)
            {
                if (a.minz < b.minz)
                    return -1;
                if (a.minz > b.minz)
                    return 1;
            }
            else
            {
                if (a.minx < b.minx)
                    return -1;
                if (a.minx > b.minx)
                    return 1;
            }
            return 0;
        }
    }

    class rcContourHole
    {
        public rcContour contour;
        public int minx;
        public int minz;
        public int leftmost;
    }

    class rcContourRegion
    {
        public rcContour outline;
        public rcContourHole[] holes;
        public int nholes;
    }

    class rcPotentialDiagonal
    {
        public int vert;
        public int dist;
    }
}