using System;
using System.Collections.Generic;
using System.Diagnostics;

public static partial class Recast
{
    const ushort RC_UNSET_HEIGHT = 0xffff;

    public class rcHeightPatch
    {
        public rcHeightPatch()
        {
        }

        public ushort[] data = null;
        public int xmin = 0;
        public int ymin = 0;
        public int width = 0;
        public int height = 0;
    };


    public static float vdot2(float[] a, float[] b)
    {
        return a[0] * b[0] + a[2] * b[2];
    }

    public static float vdot2(float[] a, int aStart, float[] b, int bStart)
    {
        return a[aStart] * b[bStart] + a[aStart + 2] * b[bStart + 2];
    }

    public static float vdistSq2(float[] p, float[] q)
    {
        float dx = q[0] - p[0];
        float dy = q[2] - p[2];
        return dx * dx + dy * dy;
    }

    public static float vdistSq2(float[] p, int pStart, float[] q, int qStart)
    {
        float dx = q[qStart + 0] - p[pStart + 0];
        float dy = q[qStart + 2] - p[pStart + 2];
        return dx * dx + dy * dy;
    }


    public static float vdist2(float[] p, float[] q)
    {
        return (float)Math.Sqrt(vdistSq2(p, q));
    }

    public static float vdist2(float[] p, int pStart, float[] q, int qStart)
    {
        return (float)Math.Sqrt(vdistSq2(p, pStart, q, qStart));
    }

    public static float vcross2(float[] p1, float[] p2, float[] p3)
    {
        float u1 = p2[0] - p1[0];
        float v1 = p2[2] - p1[2];
        float u2 = p3[0] - p1[0];
        float v2 = p3[2] - p1[2];
        return u1 * v2 - v1 * u2;
    }


    public static float vcross2(float[] p1, int p1Start, float[] p2, int p2Start, float[] p3, int p3Start)
    {
        float u1 = p2[0 + p2Start] - p1[0 + p1Start];
        float v1 = p2[2 + p2Start] - p1[2 + p1Start];
        float u2 = p3[0 + p3Start] - p1[0 + p1Start];
        float v2 = p3[2 + p3Start] - p1[2 + p1Start];
        return u1 * v2 - v1 * u2;
    }

    public static bool circumCircle(float[] p1, float[] p2, float[] p3,
                             float[] c, ref float r)
    {
        const float EPS = 1e-6f;

        float cp = vcross2(p1, p2, p3);
        if (Math.Abs(cp) > EPS)
        {
            float p1Sq = vdot2(p1, p1);
            float p2Sq = vdot2(p2, p2);
            float p3Sq = vdot2(p3, p3);
            c[0] = (p1Sq * (p2[2] - p3[2]) + p2Sq * (p3[2] - p1[2]) + p3Sq * (p1[2] - p2[2])) / (2 * cp);
            c[2] = (p1Sq * (p3[0] - p2[0]) + p2Sq * (p1[0] - p3[0]) + p3Sq * (p2[0] - p1[0])) / (2 * cp);
            r = vdist2(c, p1);
            return true;
        }

        c[0] = p1[0];
        c[2] = p1[2];
        r = 0;
        return false;
    }

    public static bool circumCircle(float[] p1, int p1Start, float[] p2, int p2Start, float[] p3, int p3Start,
                            float[] c, int cStart, ref float r)
    {
        const float EPS = 1e-6f;

        float cp = vcross2(p1, p1Start, p2, p2Start, p3, p3Start);
        if (Math.Abs(cp) > EPS)
        {
            float p1Sq = vdot2(p1, p1Start, p1, p1Start);
            float p2Sq = vdot2(p2, p2Start, p2, p2Start);
            float p3Sq = vdot2(p3, p3Start, p3, p3Start);
            c[cStart + 0] = (p1Sq * (p2[p2Start + 2] - p3[p3Start + 2]) + p2Sq * (p3[p3Start + 2] - p1[p1Start + 2]) + p3Sq * (p1[p1Start + 2] - p2[p2Start + 2])) / (2 * cp);
            c[cStart + 2] = (p1Sq * (p3[p3Start + 0] - p2[p2Start + 0]) + p2Sq * (p1[p1Start + 0] - p3[p3Start + 0]) + p3Sq * (p2[p2Start + 0] - p1[p1Start + 0])) / (2 * cp);
            r = vdist2(c, cStart, p1, p1Start);
            return true;
        }

        c[cStart + 0] = p1[p1Start + 0];
        c[cStart + 2] = p1[p1Start + 2];
        r = 0;
        return false;
    }

    static float distPtTri(float[] p, float[] a, float[] b, float[] c)
    {
        float[] v0 = new float[3];
        float[] v1 = new float[3];
        float[] v2 = new float[3];
        rcVsub(v0, c, a);
        rcVsub(v1, b, a);
        rcVsub(v2, p, a);

        float dot00 = vdot2(v0, v0);
        float dot01 = vdot2(v0, v1);
        float dot02 = vdot2(v0, v2);
        float dot11 = vdot2(v1, v1);
        float dot12 = vdot2(v1, v2);

        // Compute barycentric coordinates
        float invDenom = 1.0f / (dot00 * dot11 - dot01 * dot01);
        float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        // If point lies inside the triangle, return interpolated y-coord.
        const float EPS = 1e-4f;
        if (u >= -EPS && v >= -EPS && (u + v) <= 1 + EPS)
        {
            float y = a[1] + v0[1] * u + v1[1] * v;
            return Math.Abs(y - p[1]);
        }
        return float.MaxValue;
    }
    static float distPtTri(float[] p, int pStart, float[] a, int aStart, float[] b, int bStart, float[] c, int cStart)
    {
        float[] v0 = new float[3];
        float[] v1 = new float[3];
        float[] v2 = new float[3];
        rcVsub(v0, 0, c, cStart, a, aStart);
        rcVsub(v1, 0, b, bStart, a, aStart);
        rcVsub(v2, 0, p, pStart, a, aStart);

        float dot00 = vdot2(v0, v0);
        float dot01 = vdot2(v0, v1);
        float dot02 = vdot2(v0, v2);
        float dot11 = vdot2(v1, v1);
        float dot12 = vdot2(v1, v2);

        // Compute barycentric coordinates
        float invDenom = 1.0f / (dot00 * dot11 - dot01 * dot01);
        float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        // If point lies inside the triangle, return interpolated y-coord.
        const float EPS = 1e-4f;
        if (u >= -EPS && v >= -EPS && (u + v) <= 1 + EPS)
        {
            float y = a[aStart + 1] + v0[1] * u + v1[1] * v;
            return Math.Abs(y - p[pStart + 1]);
        }
        return float.MaxValue;
    }


    static float distancePtSeg(float[] pt, float[] p, float[] q)
    {
        float pqx = q[0] - p[0];
        float pqy = q[1] - p[1];
        float pqz = q[2] - p[2];
        float dx = pt[0] - p[0];
        float dy = pt[1] - p[1];
        float dz = pt[2] - p[2];
        float d = pqx * pqx + pqy * pqy + pqz * pqz;
        float t = pqx * dx + pqy * dy + pqz * dz;
        if (d > 0)
            t /= d;
        if (t < 0)
            t = 0;
        else if (t > 1)
            t = 1;

        dx = p[0] + t * pqx - pt[0];
        dy = p[1] + t * pqy - pt[1];
        dz = p[2] + t * pqz - pt[2];

        return dx * dx + dy * dy + dz * dz;
    }
    static float distancePtSeg(float[] pt, int ptStart, float[] p, int pStart, float[] q, int qStart)
    {
        float pqx = q[qStart + 0] - p[pStart + 0];
        float pqy = q[qStart + 1] - p[pStart + 1];
        float pqz = q[qStart + 2] - p[pStart + 2];
        float dx = pt[ptStart + 0] - p[pStart + 0];
        float dy = pt[ptStart + 1] - p[pStart + 1];
        float dz = pt[ptStart + 2] - p[pStart + 2];
        float d = pqx * pqx + pqy * pqy + pqz * pqz;
        float t = pqx * dx + pqy * dy + pqz * dz;
        if (d > 0)
            t /= d;
        if (t < 0)
            t = 0;
        else if (t > 1)
            t = 1;

        dx = p[pStart + 0] + t * pqx - pt[ptStart + 0];
        dy = p[pStart + 1] + t * pqy - pt[ptStart + 1];
        dz = p[pStart + 2] + t * pqz - pt[ptStart + 2];

        return dx * dx + dy * dy + dz * dz;
    }

    static float distancePtSeg2d(float[] pt, float[] p, float[] q)
    {
        float pqx = q[0] - p[0];
        float pqz = q[2] - p[2];
        float dx = pt[0] - p[0];
        float dz = pt[2] - p[2];
        float d = pqx * pqx + pqz * pqz;
        float t = pqx * dx + pqz * dz;
        if (d > 0)
            t /= d;
        if (t < 0)
            t = 0;
        else if (t > 1)
            t = 1;

        dx = p[0] + t * pqx - pt[0];
        dz = p[2] + t * pqz - pt[2];

        return dx * dx + dz * dz;
    }

    static float distancePtSeg2d(float[] pt, int ptStart, float[] p, int pStart, float[] q, int qStart)
    {
        float pqx = q[qStart + 0] - p[0 + pStart];
        float pqz = q[qStart + 2] - p[2 + pStart];
        float dx = pt[ptStart + 0] - p[0 + pStart];
        float dz = pt[ptStart + 2] - p[2 + pStart];
        float d = pqx * pqx + pqz * pqz;
        float t = pqx * dx + pqz * dz;
        if (d > 0)
            t /= d;
        if (t < 0)
            t = 0;
        else if (t > 1)
            t = 1;

        dx = p[0 + pStart] + t * pqx - pt[ptStart + 0];
        dz = p[2 + pStart] + t * pqz - pt[ptStart + 2];

        return dx * dx + dz * dz;
    }

    static float distToTriMesh(float[] p, float[] verts, int nverts, List<int> tris, int ntris)
    {
        float dmin = float.MaxValue;
        for (int i = 0; i < ntris; ++i)
        {
            int vaStart = tris[i * 4 + 0] * 3;
            int vbStart = tris[i * 4 + 1] * 3;
            int vcStart = tris[i * 4 + 2] * 3;
            float d = distPtTri(p, 0, verts, vaStart, verts, vbStart, verts, vcStart);
            if (d < dmin)
                dmin = d;
        }
        if (dmin == float.MaxValue) return -1;
        return dmin;
    }

    static float distToPoly(int nvert, float[] verts, float[] p)
    {

        float dmin = float.MaxValue;
        int i, j;
        bool c = false;
        for (i = 0, j = nvert - 1; i < nvert; j = i++)
        {
            int viStart = i * 3;
            int vjStart = j * 3;
            if (((verts[viStart + 2] > p[2]) != (verts[vjStart + 2] > p[2])) &&
                (p[0] < (verts[vjStart + 0] - verts[viStart + 0]) * (p[2] - verts[viStart + 2]) / (verts[vjStart + 2] - verts[viStart + 2]) + verts[viStart + 0]))
                c = !c;
            dmin = Math.Min(dmin, distancePtSeg2d(p, 0, verts, vjStart, verts, viStart));
        }
        return c ? -dmin : dmin;
    }


    static ushort getHeight(float fx, float fy, float fz,
                                    float cs, float ics, float ch,
                                    int radius, rcHeightPatch hp)
    {
        int ix = (int)Math.Floor(fx * ics + 0.01f);
        int iz = (int)Math.Floor(fz * ics + 0.01f);
        ix = rcClamp(ix - hp.xmin, 0, hp.width - 1);
        iz = rcClamp(iz - hp.ymin, 0, hp.height - 1);
        ushort h = hp.data[ix + iz * hp.width];
        if (h == RC_UNSET_HEIGHT)
        {
            // Special case when data might be bad.
            // Walk adjacent cells in a spiral up to 'radius', and look
            // for a pixel which has a valid height.
            int x = 1, z = 0, dx = 1, dz = 0;
            int maxSize = radius * 2 + 1;
            int maxIter = maxSize * maxSize - 1;

            int nextRingIterStart = 8;
            int nextRingIters = 16;

            float dmin = float.MaxValue;
            for (int i = 0; i < 8; ++i)
            {
                int nx = ix + x;
                int nz = iz + z;
                if (nx >= 0 && nz >= 0 && nx < hp.width && nz < hp.height)
                {
                    ushort nh = hp.data[nx + nz * hp.width];
                    if (nh != RC_UNSET_HEIGHT)
                    {
                        float d = Math.Abs(nh * ch - fy);
                        if (d < dmin)
                        {
                            h = nh;
                            dmin = d;
                        }
                    }
                }

                // We are searching in a grid which looks approximately like this:
                //  __________
                // |2 ______ 2|
                // | |1 __ 1| |
                // | | |__| | |
                // | |______| |
                // |__________|
                // We want to find the best height as close to the center cell as possible. This means that
                // if we find a height in one of the neighbor cells to the center, we don't want to
                // expand further out than the 8 neighbors - we want to limit our search to the closest
                // of these "rings", but the best height in the ring.
                // For example, the center is just 1 cell. We checked that at the entrance to the function.
                // The next "ring" contains 8 cells (marked 1 above). Those are all the neighbors to the center cell.
                // The next one again contains 16 cells (marked 2). In general each ring has 8 additional cells, which
                // can be thought of as adding 2 cells around the "center" of each side when we expand the ring.
                // Here we detect if we are about to enter the next ring, and if we are and we have found
                // a height, we abort the search.
                if (i + 1 == nextRingIterStart)
                {
                    if (h != RC_UNSET_HEIGHT)
                        break;

                    nextRingIterStart += nextRingIters;
                    nextRingIters += 8;
                }

                if ((x == z) || ((x < 0) && (x == -z)) || ((x > 0) && (x == 1 - z)))
                {
                    int tmp = dx;
                    dx = -dz;
                    dz = tmp;
                }
                x += dx;
                z += dz;
            }
        }
        return h;
    }


    public static class EdgeValues
    {
        public const int UNDEF = -1;
        public const int HULL = -2;
    };

    static int findEdge(List<int> edges, int nedges, int s, int t)
    {
        for (int i = 0; i < nedges; i++)
        {
            //int[] e = &edges[i*4];
            int eIndex = i * 4;
            if ((edges[eIndex + 0] == s && edges[eIndex + 1] == t) || (edges[eIndex + 0] == t && edges[eIndex + 1] == s))
            {
                return i;
            }
        }
        return EdgeValues.UNDEF;
    }

    static int findEdge(int[] edges, int nedges, int s, int t)
    {
        for (int i = 0; i < nedges; i++)
        {
            //int[] e = &edges[i*4];
            int eIndex = i * 4;
            if ((edges[eIndex + 0] == s && edges[eIndex + 1] == t) || (edges[eIndex + 0] == t && edges[eIndex + 1] == s))
            {
                return i;
            }
        }
        return EdgeValues.UNDEF;
    }

    static int addEdge(rcContext ctx, List<int> edges, ref int nedges, int maxEdges, int s, int t, int l, int r)
    {
        if (nedges >= maxEdges)
        {
            ctx.log(rcLogCategory.RC_LOG_ERROR, "addEdge (list version): Too many edges (" + nedges + "/" + maxEdges + ").");
            return EdgeValues.UNDEF;
        }

        // Add edge if not already in the triangulation. 
        int e = findEdge(edges, nedges, s, t);
        if (e == EdgeValues.UNDEF)
        {
            //int* edge = &edges[nedges*4];
            //int edgeIndex = nedges*4;
            /*edges[edgeIndex + 0] = s;
		    edges[edgeIndex + 1] = t;
		    edges[edgeIndex + 2] = l;
		    edges[edgeIndex + 3] = r;*/
            edges.Add(s);
            edges.Add(t);
            edges.Add(l);
            edges.Add(r);
            return nedges++;
        }
        else
        {
            return EdgeValues.UNDEF;
        }
    }

    static int addEdge(rcContext ctx, int[] edges, ref int nedges, int maxEdges, int s, int t, int l, int r)
    {
        if (nedges >= maxEdges)
        {
            ctx.log(rcLogCategory.RC_LOG_ERROR, "addEdge: Too many edges (" + nedges + "/" + maxEdges + ").");
            return EdgeValues.UNDEF;
        }

        // Add edge if not already in the triangulation. 
        int e = findEdge(edges, nedges, s, t);
        if (e == EdgeValues.UNDEF)
        {
            //int* edge = &edges[nedges*4];
            int edgeIndex = nedges * 4;
            edges[edgeIndex + 0] = s;
            edges[edgeIndex + 1] = t;
            edges[edgeIndex + 2] = l;
            edges[edgeIndex + 3] = r;
            return nedges++;
        }
        else
        {
            return EdgeValues.UNDEF;
        }
    }
    static void updateLeftFace(List<int> e, int eStart, int s, int t, int f)
    {
        if (e[eStart + 0] == s && e[eStart + 1] == t && e[eStart + 2] == EdgeValues.UNDEF)
        {
            e[eStart + 2] = f;
        }
        else if (e[eStart + 1] == s && e[eStart + 0] == t && e[eStart + 3] == EdgeValues.UNDEF)
        {
            e[eStart + 3] = f;
        }
    }

    static void updateLeftFace(List<int> e, int s, int t, int f)
    {
        if (e[0] == s && e[1] == t && e[2] == EdgeValues.UNDEF)
        {
            e[2] = f;
        }
        else if (e[1] == s && e[0] == t && e[3] == EdgeValues.UNDEF)
        {
            e[3] = f;
        }
    }

    static int overlapSegSeg2d(float[] a, float[] b, float[] c, float[] d)
    {
        float a1 = vcross2(a, b, d);
        float a2 = vcross2(a, b, c);
        if (a1 * a2 < 0.0f)
        {
            float a3 = vcross2(c, d, a);
            float a4 = a3 + a2 - a1;
            if (a3 * a4 < 0.0f)
            {
                return 1;
            }
        }
        return 0;
    }

    static int overlapSegSeg2d(float[] a, int aStart, float[] b, int bStart, float[] c, int cStart, float[] d, int dStart)
    {
        float a1 = vcross2(a, aStart, b, bStart, d, dStart);
        float a2 = vcross2(a, aStart, b, bStart, c, cStart);
        if (a1 * a2 < 0.0f)
        {
            float a3 = vcross2(c, cStart, d, dStart, a, aStart);
            float a4 = a3 + a2 - a1;
            if (a3 * a4 < 0.0f)
            {
                return 1;
            }
        }
        return 0;
    }


    static bool overlapEdges(float[] pts, List<int> edges, int nedges, int s1, int t1)
    {
        for (int i = 0; i < nedges; ++i)
        {
            int s0 = edges[i * 4 + 0];
            int t0 = edges[i * 4 + 1];
            // Same or connected edges do not overlap.
            if (s0 == s1 || s0 == t1 || t0 == s1 || t0 == t1)
            {
                continue;
            }
            if (overlapSegSeg2d(pts, s0 * 3, pts, t0 * 3, pts, s1 * 3, pts, t1 * 3) != 0)
            {
                return true;
            }
        }
        return false;
    }

    static void completeFacet(rcContext ctx, float[] pts, int npts, List<int> edges, ref int nedges, int maxEdges, ref int nfaces, int e)
    {
        const float EPS = 1e-5f;

        //int[] edge = &edges[e*4];
        int edgeIndex = e * 4;

        // Cache s and t.
        int s, t;
        if (edges[edgeIndex + 2] == EdgeValues.UNDEF)
        {
            s = edges[edgeIndex + 0];
            t = edges[edgeIndex + 1];
        }
        else if (edges[edgeIndex + 3] == EdgeValues.UNDEF)
        {
            s = edges[edgeIndex + 1];
            t = edges[edgeIndex + 0];
        }
        else
        {
            // Edge already completed. 
            return;
        }

        // Find best point on left of edge. 
        int pt = npts;
        float[] c = new float[] { 0, 0, 0 };
        float r = -1.0f;
        for (int u = 0; u < npts; ++u)
        {
            if (u == s || u == t)
            {
                continue;
            }
            if (vcross2(pts, s * 3, pts, t * 3, pts, u * 3) > EPS)
            {
                if (r < 0)
                {
                    // The circle is not updated yet, do it now.
                    pt = u;
                    circumCircle(pts, s * 3, pts, t * 3, pts, u * 3, c, 0, ref r);
                    continue;
                }
                float d = vdist2(c, 0, pts, u * 3);
                float tol = 0.001f;
                if (d > r * (1 + tol))
                {
                    // Outside current circumcircle, skip.
                    continue;
                }
                else if (d < r * (1 - tol))
                {
                    // Inside safe circumcircle, update circle.
                    pt = u;
                    circumCircle(pts, s * 3, pts, t * 3, pts, u * 3, c, 0, ref r);
                }
                else
                {
                    // Inside epsilon circum circle, do extra tests to make sure the edge is valid.
                    // s-u and t-u cannot overlap with s-pt nor t-pt if they exists.
                    if (overlapEdges(pts, edges, nedges, s, u))
                        continue;
                    if (overlapEdges(pts, edges, nedges, t, u))
                        continue;
                    // Edge is valid.
                    pt = u;
                    circumCircle(pts, s * 3, pts, t * 3, pts, u * 3, c, 0, ref r);
                }
            }
        }

        // Add new triangle or update edge info if s-t is on hull. 
        if (pt < npts)
        {
            // Update face information of edge being completed. 
            updateLeftFace(edges, e * 4, s, t, nfaces);

            // Add new edge or update face info of old edge. 
            e = findEdge(edges, nedges, pt, s);
            if (e == EdgeValues.UNDEF)
                addEdge(ctx, edges, ref nedges, maxEdges, pt, s, nfaces, EdgeValues.UNDEF);
            else
                updateLeftFace(edges, e * 4, pt, s, nfaces);

            // Add new edge or update face info of old edge. 
            e = findEdge(edges, nedges, t, pt);
            if (e == EdgeValues.UNDEF)
                addEdge(ctx, edges, ref nedges, maxEdges, t, pt, nfaces, EdgeValues.UNDEF);
            else
                updateLeftFace(edges, e * 4, t, pt, nfaces);

            nfaces++;
        }
        else
        {
            updateLeftFace(edges, e * 4, s, t, EdgeValues.HULL);
        }
    }

    static void delaunayHull(rcContext ctx, int npts, float[] pts,
                             int nhull, int[] hull,
                             List<int> tris, List<int> edges)
    {
        int nfaces = 0;
        int nedges = 0;
        int maxEdges = npts * 10;
        //edges.resize(maxEdges*4);
        edges.Capacity = maxEdges * 4;

        for (int i = 0, j = nhull - 1; i < nhull; j = i++)
        {
            addEdge(ctx, edges, ref nedges, maxEdges, hull[j], hull[i], EdgeValues.HULL, EdgeValues.UNDEF);
        }

        int currentEdge = 0;
        while (currentEdge < nedges)
        {
            if (edges[currentEdge * 4 + 2] == EdgeValues.UNDEF)
            {
                completeFacet(ctx, pts, npts, edges, ref nedges, maxEdges, ref nfaces, currentEdge);
            }
            if (edges[currentEdge * 4 + 3] == EdgeValues.UNDEF)
            {
                completeFacet(ctx, pts, npts, edges, ref nedges, maxEdges, ref nfaces, currentEdge);
            }
            currentEdge++;
        }

        // Create tris
        //tris.resize(nfaces*4);
        tris.Capacity = nfaces * 4;
        tris.Clear();
        for (int i = 0; i < nfaces * 4; ++i)
        {
            //tris[i] = -1;
            tris.Add(-1);
        }

        for (int i = 0; i < nedges; ++i)
        {
            //const int* e = &edges[i*4];
            int edgeIndex = i * 4;
            if (edges[edgeIndex + 3] >= 0)
            {
                // Left face
                //int* t = &tris[e[3]*4];
                int tIndex = edges[edgeIndex + 3] * 4;
                if (tris[tIndex + 0] == -1)
                {
                    tris[tIndex + 0] = edges[edgeIndex + 0];
                    tris[tIndex + 1] = edges[edgeIndex + 1];
                }
                else if (tris[tIndex + 0] == edges[edgeIndex + 1])
                    tris[tIndex + 2] = edges[edgeIndex + 0];
                else if (tris[tIndex + 1] == edges[edgeIndex + 0])
                    tris[tIndex + 2] = edges[edgeIndex + 1];
            }
            if (edges[edgeIndex + 2] >= 0)
            {
                // Right
                //int* t = &tris[e[2]*4];
                int tIndex = edges[edgeIndex + 2] * 4;
                if (tris[tIndex + 0] == -1)
                {
                    tris[tIndex + 0] = edges[edgeIndex + 1];
                    tris[tIndex + 1] = edges[edgeIndex + 0];
                }
                else if (tris[tIndex + 0] == edges[edgeIndex + 0])
                    tris[tIndex + 2] = edges[edgeIndex + 1];
                else if (tris[tIndex + 1] == edges[edgeIndex + 1])
                    tris[tIndex + 2] = edges[edgeIndex + 0];
            }
        }

        for (int i = 0; i < tris.Count / 4; ++i)
        {
            //int* t = &tris[i*4];
            int tIndex = i * 4;
            if (tris[tIndex + 0] == -1 || tris[tIndex + 1] == -1 || tris[tIndex + 2] == -1)
            {
                ctx.log(rcLogCategory.RC_LOG_WARNING, "delaunayHull: Removing dangling face " + i + " [" + tris[tIndex + 0] + "," + tris[tIndex + 1] + "," + tris[tIndex + 2] + "].");
                tris[tIndex + 0] = tris[tris.Count - 4];
                tris[tIndex + 1] = tris[tris.Count - 3];
                tris[tIndex + 2] = tris[tris.Count - 2];
                tris[tIndex + 3] = tris[tris.Count - 1];
                //tris.resize(tris.Count-4);
                //tris.Capacity = tris.Count - 4;
                tris.RemoveRange(tris.Count - 4, 4);
                --i;
            }
        }
    }

    // Calculate minimum extend of the polygon.
    static float polyMinExtent(float[] verts, int nverts)
    {

        float minDist = float.MaxValue;
        for (int i = 0; i < nverts; i++)
        {
            int ni = (i + 1) % nverts;
            int p1Index = i * 3;
            int p2Index = ni * 3;
            float maxEdgeDist = 0;
            for (int j = 0; j < nverts; j++)
            {
                if (j == i || j == ni) continue;
                float d = distancePtSeg2d(verts, j * 3, verts, p1Index, verts, p2Index);
                maxEdgeDist = Math.Max(maxEdgeDist, d);
            }
            minDist = Math.Min(minDist, maxEdgeDist);
        }
        return (float)Math.Sqrt(minDist);
    }

    static void triangulateHull(int nverts, float[] verts, int nhull, int[] hull, List<int> tris)
    {
        int start = 0, left = 1, right = nhull - 1;

        // Start from an ear with shortest perimeter.
        // This tends to favor well formed triangles as starting point.
        float dmin = 0;
        for (int i = 0; i < nhull; i++)
        {
            int pi = prev(i, nhull);
            int ni = next(i, nhull);
            int pvIndex = hull[pi] * 3;
            int cvIndex = hull[i] * 3;
            int nvIndex = hull[ni] * 3;
            float d = vdist2(verts, pvIndex, verts, cvIndex) + vdist2(verts, cvIndex, verts, nvIndex) + vdist2(verts, nvIndex, verts, pvIndex);
            if (d < dmin)
            {
                start = i;
                left = ni;
                right = pi;
                dmin = d;
            }
        }

        // Add first triangle
        tris.Add(hull[start]);
        tris.Add(hull[left]);
        tris.Add(hull[right]);
        tris.Add(0);

        // Triangulate the polygon by moving left or right,
        // depending on which triangle has shorter perimeter.
        // This heuristic was chose emprically, since it seems
        // handle tesselated straight edges well.
        while (next(left, nhull) != right)
        {
            // Check to see if se should advance left or right.
            int nleft = next(left, nhull);
            int nright = prev(right, nhull);

            int cvleftIndex = hull[left] * 3;
            int nvleftIndex = hull[nleft] * 3;
            int cvrightIndex = hull[right] * 3;
            int nvrightIndex = hull[nright] * 3;
            float dleft = vdist2(verts, cvleftIndex, verts, nvleftIndex) + vdist2(verts, nvleftIndex, verts, cvrightIndex);
            float dright = vdist2(verts, cvrightIndex, verts, nvrightIndex) + vdist2(verts, cvleftIndex, verts, nvrightIndex);

            if (dleft < dright)
            {
                tris.Add(hull[left]);
                tris.Add(hull[nleft]);
                tris.Add(hull[right]);
                tris.Add(0);
                left = nleft;
            }
            else
            {
                tris.Add(hull[left]);
                tris.Add(hull[nright]);
                tris.Add(hull[right]);
                tris.Add(0);
                right = nright;
            }
        }
    }

    public static float getJitterX(int i)
    {
        return (((i * 0x8da6b343) & 0xffff) / 65535.0f * 2.0f) - 1.0f;
    }

    public static float getJitterY(int i)
    {
        return (((i * 0xd8163841) & 0xffff) / 65535.0f * 2.0f) - 1.0f;
    }

    static bool buildPolyDetail(rcContext ctx, float[] _in, int nin,
                                float sampleDist, float sampleMaxError,
                                int heightSearchRadius, rcCompactHeightfield chf,
                                rcHeightPatch hp, float[] verts, ref int nverts,
                                List<int> tris, List<int> edges, List<int> samples)
    {
        const int MAX_VERTS = 127;
        const int MAX_TRIS = 255;   // Max tris for delaunay is 2n-2-k (n=num verts, k=num hull verts).
        const int MAX_VERTS_PER_EDGE = 32;
        float[] edge = new float[(MAX_VERTS_PER_EDGE + 1) * 3];
        int[] hull = new int[MAX_VERTS];
        int nhull = 0;

        nverts = nin;

        for (int i = 0; i < nin; ++i)
        {
            rcVcopy(verts, i * 3, _in, i * 3);
        }

        edges.Clear();
        tris.Clear();

        float cs = chf.cs;
        float ics = 1.0f / cs;

        // Calculate minimum extents of the polygon based on input data.
        float minExtent = polyMinExtent(verts, nverts);

        // Tessellate outlines.
        // This is done in separate pass in order to ensure
        // seamless height values across the ply boundaries.
        if (sampleDist > 0)
        {
            for (int i = 0, j = nin - 1; i < nin; j = i++)
            {
                //const float* vj = &in[j*3];
                //const float* vi = &in[i*3];
                int vjStart = j * 3;
                int viStart = i * 3;
                bool swapped = false;
                // Make sure the segments are always handled in same order
                // using lexological sort or else there will be seams.
                if (Math.Abs(_in[vjStart] - _in[viStart]) < 1e-6f)
                {
                    if (_in[vjStart + 2] > _in[viStart + 2])
                    {
                        rcSwap(ref vjStart, ref viStart);
                        swapped = true;
                    }
                }
                else
                {
                    if (_in[vjStart] > _in[viStart])
                    {
                        rcSwap(ref vjStart, ref viStart);
                        swapped = true;
                    }
                }
                // Create samples along the edge.
                float dx = _in[viStart] - _in[vjStart];//vi[0] - vj[0];
                float dy = _in[viStart + 1] - _in[vjStart + 1];//vi[1] - vj[1];
                float dz = _in[viStart + 2] - _in[vjStart + 2];//vi[2] - vj[2];
                float d = (float)Math.Sqrt(dx * dx + dz * dz);
                int nn = 1 + (int)Math.Floor(d / sampleDist);
                if (nn >= MAX_VERTS_PER_EDGE)
                {
                    nn = MAX_VERTS_PER_EDGE - 1;
                }
                if (nverts + nn >= MAX_VERTS)
                {
                    nn = MAX_VERTS - 1 - nverts;
                }

                for (int k = 0; k <= nn; ++k)
                {
                    float u = (float)k / (float)nn;
                    //float* pos = &edge[k*3];
                    int posStart = k * 3;
                    edge[posStart + 0] = _in[vjStart + 0] + dx * u;
                    edge[posStart + 1] = _in[vjStart + 1] + dy * u;
                    edge[posStart + 2] = _in[vjStart + 2] + dz * u;
                    edge[posStart + 1] = getHeight(edge[posStart + 0], edge[posStart + 1], edge[posStart + 2], cs, ics, chf.ch, heightSearchRadius, hp) * chf.ch;
                }
                // Simplify samples.
                int[] idx = new int[MAX_VERTS_PER_EDGE];// {0,nn};
                idx[1] = nn;
                int nidx = 2;
                for (int k = 0; k < nidx - 1;)
                {
                    int a = idx[k];
                    int b = idx[k + 1];
                    //float* va = &edge[a*3];
                    //float* vb = &edge[b*3];
                    int vaStart = a * 3;
                    int vbStart = b * 3;
                    // Find maximum deviation along the segment.
                    float maxd = 0;
                    int maxi = -1;
                    for (int m = a + 1; m < b; ++m)
                    {
                        int ptStart = m * 3;
                        float dev = distancePtSeg(edge, ptStart, edge, vaStart, edge, vbStart);
                        if (dev > maxd)
                        {
                            maxd = dev;
                            maxi = m;
                        }
                    }
                    // If the max deviation is larger than accepted error,
                    // add new point, else continue to next segment.
                    if (maxi != -1 && maxd > sampleMaxError * sampleMaxError)
                    {
                        for (int m = nidx; m > k; --m)
                            idx[m] = idx[m - 1];
                        idx[k + 1] = maxi;
                        nidx++;
                    }
                    else
                    {
                        ++k;
                    }
                }

                hull[nhull++] = j;
                // Add new vertices.
                if (swapped)
                {
                    for (int k = nidx - 2; k > 0; --k)
                    {
                        //rcVcopy(&verts[nverts*3], &edge[idx[k]*3]);
                        rcVcopy(verts, nverts * 3, edge, idx[k] * 3);
                        hull[nhull++] = nverts;
                        nverts++;
                    }
                }
                else
                {
                    for (int k = 1; k < nidx - 1; ++k)
                    {
                        //rcVcopy(&verts[nverts*3], &edge[idx[k]*3]);
                        rcVcopy(verts, nverts * 3, edge, idx[k] * 3);
                        hull[nhull++] = nverts;
                        nverts++;
                    }
                }
            }
        }

        // If the polygon minimum extent is small (sliver or small triangle), do not try to add internal points.
        if (minExtent < sampleDist * 2)
        {
            triangulateHull(nverts, verts, nhull, hull, tris);
            return true;
        }

        triangulateHull(nverts, verts, nhull, hull, tris);

        if (tris.Count == 0)
        {
            // Could not triangulate the poly, make sure there is some valid data there.
            ctx.log(rcLogCategory.RC_LOG_WARNING, "buildPolyDetail: Could not triangulate polygon, adding default data.");
            return true;
        }

        if (sampleDist > 0)
        {
            // Create sample locations in a grid.
            float[] bmin = new float[3];
            float[] bmax = new float[3];
            rcVcopy(bmin, _in);
            rcVcopy(bmax, _in);
            for (int i = 1; i < nin; ++i)
            {
                rcVmin(bmin, 0, _in, i * 3);
                rcVmax(bmax, 0, _in, i * 3);
            }
            int x0 = (int)Math.Floor(bmin[0] / sampleDist);
            int x1 = (int)Math.Ceiling(bmax[0] / sampleDist);
            int z0 = (int)Math.Floor(bmin[2] / sampleDist);
            int z1 = (int)Math.Ceiling(bmax[2] / sampleDist);
            //samples.resize(0);
            samples.Clear();
            for (int z = z0; z < z1; ++z)
            {
                for (int x = x0; x < x1; ++x)
                {
                    float[] pt = new float[3];
                    pt[0] = x * sampleDist;
                    pt[1] = (bmax[1] + bmin[1]) * 0.5f;
                    pt[2] = z * sampleDist;
                    // Make sure the samples are not too close to the edges.
                    if (distToPoly(nin, _in, pt) > -sampleDist / 2)
                    {
                        continue;
                    }
                    samples.Add(x);
                    samples.Add(getHeight(pt[0], pt[1], pt[2], cs, ics, chf.ch, heightSearchRadius, hp));
                    samples.Add(z);
                    samples.Add(0); // Not added
                }
            }

            // Add the samples starting from the one that has the most
            // error. The procedure stops when all samples are added
            // or when the max error is within treshold.
            int nsamples = samples.Count / 4;
            for (int iter = 0; iter < nsamples; ++iter)
            {
                if (nverts >= MAX_VERTS)
                {
                    break;
                }

                // Find sample with most error.
                float[] bestpt = new float[] { 0.0f, 0.0f, 0.0f };
                float bestd = 0;
                int besti = -1;
                for (int i = 0; i < nsamples; ++i)
                {
                    // int* s = &samples[i*4];
                    int sStart = i * 4;
                    if (samples[sStart + 3] != 0)
                        continue; // skip added.
                    float[] pt = new float[3];
                    // The sample location is jittered to get rid of some bad triangulations
                    // which are cause by symmetrical data from the grid structure.
                    pt[0] = samples[sStart + 0] * sampleDist + getJitterX(i) * cs * 0.1f;
                    pt[1] = samples[sStart + 1] * chf.ch;
                    pt[2] = samples[sStart + 2] * sampleDist + getJitterY(i) * cs * 0.1f;
                    float d = distToTriMesh(pt, verts, nverts, tris, tris.Count / 4);
                    if (d < 0)
                        continue; // did not hit the mesh.
                    if (d > bestd)
                    {
                        bestd = d;
                        besti = i;
                        rcVcopy(bestpt, pt);
                    }
                }
                // If the max error is within accepted threshold, stop tesselating.
                if (bestd <= sampleMaxError || besti == -1)
                    break;
                // Mark sample as added.
                samples[besti * 4 + 3] = 1;
                // Add the new sample point.
                rcVcopy(verts, nverts * 3, bestpt, 0);
                nverts++;

                // Create new triangulation.
                // TODO: Incremental add instead of full rebuild.
                //edges.resize(0);
                //tris.resize(0);
                edges.Clear();
                tris.Clear();
                delaunayHull(ctx, nverts, verts, nhull, hull, tris, edges);
            }
        }

        int ntris = tris.Count / 4;
        if (ntris > MAX_TRIS)
        {
            //tris.resize(MAX_TRIS*4);
            tris.RemoveRange(MAX_TRIS * 4, tris.Count - MAX_TRIS * 4);
            ctx.log(rcLogCategory.RC_LOG_ERROR, "rcBuildPolyMeshDetail: Shrinking triangle count from " + ntris + " to max " + MAX_TRIS + ".");
        }

        return true;
    }


    static void seedArrayWithPolyCenter(rcCompactHeightfield chf,
                              ushort[] poly, int polyStart, int npoly,
                              ushort[] verts, int bs,
                              rcHeightPatch hp, List<int> stack)
    {
        // Note: Reads to the compact heightfield are offset by border size (bs)
        // since border size offset is already removed from the polymesh vertices.

        int[] offset = new int[9 * 2]
        {
            0,0, -1,-1, 0,-1, 1,-1, 1,0, 1,1, 0,1, -1,1, -1,0,
        };

        // Find cell closest to a poly vertex
        int startCellX = 0, startCellY = 0, startSpanIndex = -1;
        int dmin = RC_UNSET_HEIGHT;
        for (int j = 0; j < npoly && dmin > 0; ++j)
        {
            for (int k = 0; k < 9 && dmin > 0; ++k)
            {
                int ax = (int)verts[poly[polyStart + j] * 3 + 0] + offset[k * 2 + 0];
                int ay = (int)verts[poly[polyStart + j] * 3 + 1];
                int az = (int)verts[poly[polyStart + j] * 3 + 2] + offset[k * 2 + 1];
                if (ax < hp.xmin || ax >= hp.xmin + hp.width ||
                    az < hp.ymin || az >= hp.ymin + hp.height)
                    continue;

                rcCompactCell c = chf.cells[(ax + bs) + (az + bs) * chf.width];
                for (int i = (int)c.index, ni = (int)(c.index + c.count); i < ni && dmin > 0; ++i)
                {
                    rcCompactSpan s = chf.spans[i];
                    int d = Math.Abs(ay - (int)s.y);
                    if (d < dmin)
                    {
                        startCellX = ax;
                        startCellY = az;
                        startSpanIndex = i;
                        dmin = d;
                    }
                }
            }
        }

        // Find center of the polygon
        int pcx = 0, pcy = 0;
        for (int j = 0; j < npoly; ++j)
        {
            pcx += (int)verts[poly[polyStart + j] * 3 + 0];
            pcy += (int)verts[poly[polyStart + j] * 3 + 2];
        }
        pcx /= npoly;
        pcy /= npoly;

        // Use seeds array as a stack for DFS
        //array.resize(0);
        stack.Clear();
        stack.Add(startCellX);
        stack.Add(startCellY);
        stack.Add(startSpanIndex);

        int[] dirs = { 0, 1, 2, 3 };
        for (var i = 0; i < hp.width * hp.height; ++i)
            hp.data[i] = 0;
        //memset(hp.data, 0, sizeof(unsigned short) * hp.width * hp.height);
        // DFS to move to the center. Note that we need a DFS here and can not just move
        // directly towards the center without recording intermediate nodes, even though the polygons
        // are convex. In very rare we can get stuck due to contour simplification if we do not
        // record nodes.
        int cx = -1, cy = -1, ci = -1;
        while (true)
        {
            if (stack.Count < 3)
            {
                //ctx.log(RC_LOG_WARNING, "Walk towards polygon center failed to reach center");
                break;
            }

            ci = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            cy = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            cx = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);

            if (cx == pcx && cy == pcy)
                break;

            // If we are already at the correct X-position, prefer direction
            // directly towards the center in the Y-axis; otherwise prefer
            // direction in the X-axis
            int directDir;
            if (cx == pcx)
                directDir = rcGetDirForOffset(0, pcy > cy ? 1 : -1);
            else
                directDir = rcGetDirForOffset(pcx > cx ? 1 : -1, 0);

            // Push the direct dir last so we start with this on next iteration
            rcSwap(ref dirs[directDir], ref dirs[3]);

            rcCompactSpan cs = chf.spans[ci];
            for (int i = 0; i < 4; ++i)
            {
                int dir = dirs[i];
                if (rcGetCon(cs, dir) == RC_NOT_CONNECTED)
                    continue;

                int newX = cx + rcGetDirOffsetX(dir);
                int newY = cy + rcGetDirOffsetY(dir);

                int hpx = newX - hp.xmin;
                int hpy = newY - hp.ymin;
                if (hpx < 0 || hpx >= hp.width || hpy < 0 || hpy >= hp.height)
                    continue;

                if (hp.data[hpx + hpy * hp.width] != 0)
                    continue;

                hp.data[hpx + hpy * hp.width] = 1;
                stack.Add(newX);
                stack.Add(newY);
                stack.Add((int)chf.cells[(newX + bs) + (newY + bs) * chf.width].index + rcGetCon(cs, dir));
            }

            rcSwap(ref dirs[directDir], ref dirs[3]);
        }

        //array.resize(0);
        stack.Clear();
        // getHeightData seeds are given in coordinates with borders
        stack.Add(cx + bs);
        stack.Add(cy + bs);
        stack.Add(ci);

        //memset(hp.data, 0xff, sizeof(unsigned short) * hp.width * hp.height);
        for (var i = 0; i < hp.width * hp.height; ++i)
            hp.data[i] = 0xFFFF;

        rcCompactSpan cs1 = chf.spans[ci];
        hp.data[cx - hp.xmin + (cy - hp.ymin) * hp.width] = cs1.y;
    }



    static void getHeightData(rcCompactHeightfield chf,
                              ushort[] poly, int polyStart, int npoly,
                              ushort[] verts, int bs,
                              rcHeightPatch hp, List<int> stack,
                              int region)
    {
        // Note: Reads to the compact heightfield are offset by border size (bs)
        // since border size offset is already removed from the polymesh vertices.

        //stack.resize(0);
        //memset(hp.data, 0xff, sizeof(ushort)*hp.width*hp.height);
        stack.Clear();
        for (int i = 0; i < hp.data.Length; ++i)
        {
            hp.data[i] = 0xffff;
        }
        bool empty = true;

        // We cannot sample from this poly if it was created from polys
        // of different regions. If it was then it could potentially be overlapping
        // with polys of that region and the heights sampled here could be wrong.
        if (region != RC_MULTIPLE_REGS)
        {
            // Copy the height from the same region, and mark region borders
            // as seed points to fill the rest.
            for (int hy = 0; hy < hp.height; hy++)
            {
                int y = hp.ymin + hy + bs;
                for (int hx = 0; hx < hp.width; hx++)
                {
                    int x = hp.xmin + hx + bs;
                    rcCompactCell c = chf.cells[x + y * chf.width];
                    for (int i = (int)c.index, ni = (int)(c.index + c.count); i < ni; ++i)
                    {
                        rcCompactSpan s = chf.spans[i];
                        if (s.reg == region)
                        {
                            // Store height
                            hp.data[hx + hy * hp.width] = s.y;
                            empty = false;

                            // If any of the neighbours is not in same region,
                            // add the current location as flood fill start
                            bool border = false;
                            for (int dir = 0; dir < 4; ++dir)
                            {
                                if (rcGetCon(s, dir) != RC_NOT_CONNECTED)
                                {
                                    int ax = x + rcGetDirOffsetX(dir);
                                    int ay = y + rcGetDirOffsetY(dir);
                                    int ai = (int)chf.cells[ax + ay * chf.width].index + rcGetCon(s, dir);
                                    rcCompactSpan aSpan = chf.spans[ai];
                                    if (aSpan.reg != region)
                                    {
                                        border = true;
                                        break;
                                    }
                                }
                            }
                            if (border)
                            {
                                stack.Add(x);
                                stack.Add(y);
                                stack.Add(i);
                            }
                            break;
                        }
                    }
                }
            }
        }

        // if the polygon does not contain any points from the current region (rare, but happens)
        // then use the cells closest to the polygon vertices as seeds to fill the height field
        if (empty)
        {
            seedArrayWithPolyCenter(chf, poly, polyStart, npoly, verts, bs, hp, stack);
        }

        const int RETRACT_SIZE = 256;
        int head = 0;

        while (head * 3 < stack.Count)
        {
            int cx = stack[head * 3 + 0];
            int cy = stack[head * 3 + 1];
            int ci = stack[head * 3 + 2];
            head++;
            if (head >= RETRACT_SIZE)
            {
                head = 0;
                if (stack.Count > RETRACT_SIZE * 3)
                {
                    //memmove(&stack[0], &stack[RETRACT_SIZE*3], sizeof(int)*(stack.Count-RETRACT_SIZE*3));
                    for (int i = 0; i < stack.Count - RETRACT_SIZE * 3; ++i)
                    {
                        stack[i] = stack[RETRACT_SIZE * 3 + i];
                    }
                }
                //stack.resize(stack.Count-RETRACT_SIZE*3);
                int newSize = stack.Count - RETRACT_SIZE * 3;
                Debug.Assert(newSize >= 0, "Resizing under zero");
                stack.RemoveRange(newSize, stack.Count - newSize);
            }

            rcCompactSpan cs = chf.spans[ci];
            for (int dir = 0; dir < 4; ++dir)
            {
                if (rcGetCon(cs, dir) == RC_NOT_CONNECTED)
                    continue;

                int ax = cx + rcGetDirOffsetX(dir);
                int ay = cy + rcGetDirOffsetY(dir);
                int hx = ax - hp.xmin - bs;
                int hy = ay - hp.ymin - bs;

                if (hx < 0 || hx >= hp.width || hy < 0 || hy >= hp.height)
                    continue;

                if (hp.data[hx + hy * hp.width] != RC_UNSET_HEIGHT)
                    continue;

                int ai = (int)chf.cells[ax + ay * chf.width].index + rcGetCon(cs, dir);
                rcCompactSpan aSpan = chf.spans[ai];

                hp.data[hx + hy * hp.width] = aSpan.y;

                stack.Add(ax);
                stack.Add(ay);
                stack.Add(ai);
            }
        }
    }

    static byte getEdgeFlags(float[] va, float[] vb,
                                      float[] vpoly, int npoly)
    {
        // Return true if edge (va,vb) is part of the polygon.
        float thrSqr = 0.001f * 0.001f;
        for (int i = 0, j = npoly - 1; i < npoly; j = i++)
        {
            if (distancePtSeg2d(va, 0, vpoly, j * 3, vpoly, i * 3) < thrSqr &&
                distancePtSeg2d(vb, 0, vpoly, j * 3, vpoly, i * 3) < thrSqr)
                return 1;
        }
        return 0;
    }
    static byte getEdgeFlags(float[] va, int vaStart, float[] vb, int vbStart,
                                      float[] vpoly, int vpolyStart, int npoly)
    {
        // Return true if edge (va,vb) is part of the polygon.
        float thrSqr = 0.001f * 0.001f;
        for (int i = 0, j = npoly - 1; i < npoly; j = i++)
        {
            if (distancePtSeg2d(va, vaStart, vpoly, vpolyStart + j * 3, vpoly, vpolyStart + i * 3) < thrSqr &&
                distancePtSeg2d(vb, vbStart, vpoly, vpolyStart + j * 3, vpoly, vpolyStart + i * 3) < thrSqr)
                return 1;
        }
        return 0;
    }

    static byte getTriFlags(float[] va, float[] vb, float[] vc,
                                     float[] vpoly, int npoly)
    {
        byte flags = 0;
        flags |= (byte)(getEdgeFlags(va, vb, vpoly, npoly) << 0);
        flags |= (byte)(getEdgeFlags(vb, vc, vpoly, npoly) << 2);
        flags |= (byte)(getEdgeFlags(vc, va, vpoly, npoly) << 4);
        return flags;
    }
    static byte getTriFlags(float[] va, int vaStart, float[] vb, int vbStart, float[] vc, int vcStart,
                                     float[] vpoly, int vpolyStart, int npoly)
    {
        byte flags = 0;
        flags |= (byte)(getEdgeFlags(va, vaStart, vb, vbStart, vpoly, vpolyStart, npoly) << 0);
        flags |= (byte)(getEdgeFlags(vb, vbStart, vc, vcStart, vpoly, vpolyStart, npoly) << 2);
        flags |= (byte)(getEdgeFlags(vc, vcStart, va, vaStart, vpoly, vpolyStart, npoly) << 4);
        return flags;
    }

    public static int rccsPop(List<int> list)
    {
        //Let it crash if empty, so that we know there s a pb
        int ret = list[list.Count - 1];
        list.RemoveAt(list.Count - 1);
        return ret;
    }

    public static void rccsResizeList(List<int> list, int length)
    {
        if (length > list.Count)
        {
            for (int i = list.Count; i < length; ++i)
            {
                list.Add(0);
            }
        }
        else if (length < list.Count)
        {
            list.RemoveRange(length, list.Count - length);
        }
    }
    /// @par
    ///
    /// See the #rcConfig documentation for more information on the configuration parameters.
    ///
    /// @see rcAllocPolyMeshDetail, rcPolyMesh, rcCompactHeightfield, rcPolyMeshDetail, rcConfig
    public static bool rcBuildPolyMeshDetail(rcContext ctx, rcPolyMesh mesh, rcCompactHeightfield chf,
                               float sampleDist, float sampleMaxError,
                               rcPolyMeshDetail dmesh)
    {
        Debug.Assert(ctx != null, "rcContext is null");

        ctx.startTimer(rcTimerLabel.RC_TIMER_BUILD_POLYMESHDETAIL);

        if (mesh.nverts == 0 || mesh.npolys == 0)
            return true;

        int nvp = mesh.nvp;
        float cs = mesh.cs;
        float ch = mesh.ch;
        float[] orig = mesh.bmin;
        int borderSize = mesh.borderSize;
        int heightSearchRadius = Math.Max(1, (int)Math.Ceiling(mesh.maxEdgeError));

        List<int> edges = new();
        List<int> tris = new();
        List<int> stack = new();
        List<int> samples = new();
        edges.Capacity = 64;
        tris.Capacity = 512;
        stack.Capacity = 512;
        samples.Capacity = 512;
        float[] verts = new float[256 * 3];
        rcHeightPatch hp = new();
        int nPolyVerts = 0;
        int maxhw = 0, maxhh = 0;

        int[] bounds = new int[mesh.npolys * 4];
        if (bounds == null)
        {
            ctx.log(rcLogCategory.RC_LOG_ERROR, "rcBuildPolyMeshDetail: Out of memory 'bounds' (" + mesh.npolys * 4 + ").");
            return false;
        }

        float[] poly = new float[nvp * 3];
        if (poly == null)
        {
            ctx.log(rcLogCategory.RC_LOG_ERROR, "rcBuildPolyMeshDetail: Out of memory 'poly' (" + nvp * 3 + ").");
            return false;
        }

        // Find max size for a polygon area.
        for (int i = 0; i < mesh.npolys; ++i)
        {
            //ushort* p = &mesh.polys[i*nvp*2];
            int pStart = i * nvp * 2;
            //int& xmin = bounds[i*4+0];
            //int& xmax = bounds[i*4+1];
            //int& ymin = bounds[i*4+2];
            //int& ymax = bounds[i*4+3];
            int xmin = i * 4 + 0;
            int xmax = i * 4 + 1;
            int ymin = i * 4 + 2;
            int ymax = i * 4 + 3;
            bounds[xmin] = chf.width;
            bounds[xmax] = 0;
            bounds[ymin] = chf.height;
            bounds[ymax] = 0;
            for (int j = 0; j < nvp; ++j)
            {
                if (mesh.polys[pStart + j] == RC_MESH_NULL_IDX)
                    break;
                //t ushort* v = &mesh.verts[p[j]*3];
                int vIndex = mesh.polys[pStart + j] * 3;
                bounds[xmin] = Math.Min(bounds[xmin], (int)mesh.verts[vIndex + 0]);
                bounds[xmax] = Math.Max(bounds[xmax], (int)mesh.verts[vIndex + 0]);
                bounds[ymin] = Math.Min(bounds[ymin], (int)mesh.verts[vIndex + 2]);
                bounds[ymax] = Math.Max(bounds[ymax], (int)mesh.verts[vIndex + 2]);
                nPolyVerts++;
            }
            bounds[xmin] = Math.Max(0, bounds[xmin] - 1);
            bounds[xmax] = Math.Min(chf.width, bounds[xmax] + 1);
            bounds[ymin] = Math.Max(0, bounds[ymin] - 1);
            bounds[ymax] = Math.Min(chf.height, bounds[ymax] + 1);
            if (bounds[xmin] >= bounds[xmax] || bounds[ymin] >= bounds[ymax]) continue;
            maxhw = Math.Max(maxhw, bounds[xmax] - bounds[xmin]);
            maxhh = Math.Max(maxhh, bounds[ymax] - bounds[ymin]);
        }

        //hp.data = (ushort*)rcAlloc(sizeof(ushort)*maxhw*maxhh, RC_ALLOC_TEMP);
        hp.data = new ushort[maxhh * maxhw];
        if (hp.data == null)
        {
            ctx.log(rcLogCategory.RC_LOG_ERROR, "rcBuildPolyMeshDetail: Out of memory 'hp.data' (" + maxhw * maxhh + ").");
            return false;
        }

        dmesh.nmeshes = mesh.npolys;
        dmesh.nverts = 0;
        dmesh.ntris = 0;
        //dmesh.meshes = (uint*)rcAlloc(sizeof(uint)*dmesh.nmeshes*4, RC_ALLOC_PERM);
        dmesh.meshes = new uint[dmesh.nmeshes * 4];
        if (dmesh.meshes == null)
        {
            ctx.log(rcLogCategory.RC_LOG_ERROR, "rcBuildPolyMeshDetail: Out of memory 'dmesh.meshes' (" + dmesh.nmeshes * 4 + ").");
            return false;
        }

        int vcap = nPolyVerts + nPolyVerts / 2;
        int tcap = vcap * 2;

        dmesh.nverts = 0;
        //dmesh.verts = (float*)rcAlloc(sizeof(float)*vcap*3, RC_ALLOC_PERM);
        dmesh.verts = new float[vcap * 3];
        if (dmesh.verts == null)
        {
            ctx.log(rcLogCategory.RC_LOG_ERROR, "rcBuildPolyMeshDetail: Out of memory 'dmesh.verts' (" + vcap * 3 + ").");
            return false;
        }
        dmesh.ntris = 0;
        //dmesh.tris = (byte*)rcAlloc(sizeof(byte*)*tcap*4, RC_ALLOC_PERM);
        dmesh.tris = new byte[tcap * 4];
        if (dmesh.tris == null)
        {
            ctx.log(rcLogCategory.RC_LOG_ERROR, "rcBuildPolyMeshDetail: Out of memory 'dmesh.tris' (" + tcap * 4 + ").");
            return false;
        }

        for (int i = 0; i < mesh.npolys; ++i)
        {
            //const ushort* p = &mesh.polys[i*nvp*2];
            int pIndex = i * nvp * 2;

            // Store polygon vertices for processing.
            int npoly = 0;
            for (int j = 0; j < nvp; ++j)
            {
                if (mesh.polys[pIndex + j] == RC_MESH_NULL_IDX)
                    break;
                //const ushort* v = &mesh.verts[p[j]*3];
                int vIndex = mesh.polys[pIndex + j] * 3;
                poly[j * 3 + 0] = mesh.verts[vIndex + 0] * cs;
                poly[j * 3 + 1] = mesh.verts[vIndex + 1] * ch;
                poly[j * 3 + 2] = mesh.verts[vIndex + 2] * cs;
                npoly++;
            }

            // Get the height data from the area of the polygon.
            hp.xmin = bounds[i * 4 + 0];
            hp.ymin = bounds[i * 4 + 2];
            hp.width = bounds[i * 4 + 1] - bounds[i * 4 + 0];
            hp.height = bounds[i * 4 + 3] - bounds[i * 4 + 2];
            getHeightData(chf, mesh.polys, pIndex, npoly, mesh.verts, borderSize, hp, stack, mesh.regs[i]);

            // Build detail mesh.
            int nverts = 0;
            if (!buildPolyDetail(ctx, poly, npoly,
                                 sampleDist, sampleMaxError,
                                 heightSearchRadius, chf, hp,
                                 verts, ref nverts, tris,
                                 edges, samples))
            {
                return false;
            }

            // Move detail verts to world space.
            for (int j = 0; j < nverts; ++j)
            {
                verts[j * 3 + 0] += orig[0];
                verts[j * 3 + 1] += orig[1] + chf.ch; // Is this offset necessary?
                verts[j * 3 + 2] += orig[2];
            }
            // Offset poly too, will be used to flag checking.
            for (int j = 0; j < npoly; ++j)
            {
                poly[j * 3 + 0] += orig[0];
                poly[j * 3 + 1] += orig[1];
                poly[j * 3 + 2] += orig[2];
            }

            // Store detail submesh.
            int ntris = tris.Count / 4;

            dmesh.meshes[i * 4 + 0] = (uint)dmesh.nverts;
            dmesh.meshes[i * 4 + 1] = (uint)nverts;
            dmesh.meshes[i * 4 + 2] = (uint)dmesh.ntris;
            dmesh.meshes[i * 4 + 3] = (uint)ntris;

            // Store vertices, allocate more memory if necessary.
            if (dmesh.nverts + nverts > vcap)
            {
                while (dmesh.nverts + nverts > vcap)
                {
                    vcap += 256;
                }

                //float* newv = (float*)rcAlloc(sizeof(float)*vcap*3, RC_ALLOC_PERM);
                float[] newv = new float[vcap * 3];
                if (newv == null)
                {
                    ctx.log(rcLogCategory.RC_LOG_ERROR, "rcBuildPolyMeshDetail: Out of memory 'newv' (" + vcap * 3 + ").");
                    return false;
                }
                if (dmesh.nverts != 0)
                {
                    //memcpy(newv, dmesh.verts, sizeof(float)*3*dmesh.nverts);
                    for (int j = 0; j < 3 * dmesh.nverts; ++j)
                    {
                        newv[j] = dmesh.verts[j];
                    }
                }
                //dmesh.verts = null;
                dmesh.verts = newv;
            }
            for (int j = 0; j < nverts; ++j)
            {
                dmesh.verts[dmesh.nverts * 3 + 0] = verts[j * 3 + 0];
                dmesh.verts[dmesh.nverts * 3 + 1] = verts[j * 3 + 1];
                dmesh.verts[dmesh.nverts * 3 + 2] = verts[j * 3 + 2];
                dmesh.nverts++;
            }

            // Store triangles, allocate more memory if necessary.
            if (dmesh.ntris + ntris > tcap)
            {
                while (dmesh.ntris + ntris > tcap)
                {
                    tcap += 256;
                }
                //byte* newt = (byte*)rcAlloc(sizeof(byte)*tcap*4, RC_ALLOC_PERM);
                byte[] newt = new byte[tcap * 4];
                if (newt == null)
                {
                    ctx.log(rcLogCategory.RC_LOG_ERROR, "rcBuildPolyMeshDetail: Out of memory 'newt' (" + tcap * 4 + ").");
                    return false;
                }
                if (dmesh.ntris != 0)
                {
                    //memcpy(newt, dmesh.tris, sizeof(byte)*4*dmesh.ntris);
                    for (int j = 0; j < 4 * dmesh.ntris; ++j)
                    {
                        newt[j] = dmesh.tris[j];
                    }
                }
                dmesh.tris = newt;
            }
            for (int j = 0; j < ntris; ++j)
            {
                //const int* t = &tris[j*4];
                int tIndex = j * 4;
                dmesh.tris[dmesh.ntris * 4 + 0] = (byte)tris[tIndex + 0];
                dmesh.tris[dmesh.ntris * 4 + 1] = (byte)tris[tIndex + 1];
                dmesh.tris[dmesh.ntris * 4 + 2] = (byte)tris[tIndex + 2];
                dmesh.tris[dmesh.ntris * 4 + 3] = getTriFlags(verts, tris[tIndex + 0] * 3, verts, tris[tIndex + 1] * 3, verts, tris[tIndex + 2] * 3, poly, 0, npoly);
                dmesh.ntris++;
            }
        }

        ctx.stopTimer(rcTimerLabel.RC_TIMER_BUILD_POLYMESHDETAIL);

        return true;
    }

    /// @see rcAllocPolyMeshDetail, rcPolyMeshDetail
    public static bool rcMergePolyMeshDetails(rcContext ctx, rcPolyMeshDetail[] meshes, int nmeshes, rcPolyMeshDetail mesh)
    {
        Debug.Assert(ctx != null, "rcContext is null");

        ctx.startTimer(rcTimerLabel.RC_TIMER_MERGE_POLYMESHDETAIL);

        int maxVerts = 0;
        int maxTris = 0;
        int maxMeshes = 0;

        for (int i = 0; i < nmeshes; ++i)
        {
            if (meshes[i] == null)
            {
                continue;
            }
            maxVerts += meshes[i].nverts;
            maxTris += meshes[i].ntris;
            maxMeshes += meshes[i].nmeshes;
        }

        mesh.nmeshes = 0;
        //mesh.meshes = (uint*)rcAlloc(sizeof(uint)*maxMeshes*4, RC_ALLOC_PERM);
        mesh.meshes = new uint[maxMeshes * 4];
        if (mesh.meshes == null)
        {
            ctx.log(rcLogCategory.RC_LOG_ERROR, "rcBuildPolyMeshDetail: Out of memory 'pmdtl.meshes' (" + maxMeshes * 4 + ").");
            return false;
        }

        mesh.ntris = 0;
        //mesh.tris = (byte*)rcAlloc(sizeof(byte)*maxTris*4, RC_ALLOC_PERM);
        mesh.tris = new byte[maxTris * 4];
        if (mesh.tris == null)
        {
            ctx.log(rcLogCategory.RC_LOG_ERROR, "rcBuildPolyMeshDetail: Out of memory 'dmesh.tris' (" + maxTris * 4 + ").");
            return false;
        }

        mesh.nverts = 0;
        //mesh.verts = (float*)rcAlloc(sizeof(float)*maxVerts*3, RC_ALLOC_PERM);
        mesh.verts = new float[maxVerts * 3];
        if (mesh.verts == null)
        {
            ctx.log(rcLogCategory.RC_LOG_ERROR, "rcBuildPolyMeshDetail: Out of memory 'dmesh.verts' (" + maxVerts * 3 + ").");
            return false;
        }

        // Merge datas.
        for (int i = 0; i < nmeshes; ++i)
        {
            rcPolyMeshDetail dm = meshes[i];
            if (dm == null)
            {
                continue;
            }
            for (int j = 0; j < dm.nmeshes; ++j)
            {
                //uint* dst = &mesh.meshes[mesh.nmeshes*4];
                //uint* src = &dm.meshes[j*4];
                int dstIndex = mesh.nmeshes * 4;
                int srcIndex = j * 4;
                mesh.meshes[dstIndex + 0] = (uint)mesh.nverts + dm.meshes[srcIndex + 0];
                mesh.meshes[dstIndex + 1] = dm.meshes[srcIndex + 1];
                mesh.meshes[dstIndex + 2] = (uint)mesh.ntris + dm.meshes[srcIndex + 2];
                mesh.meshes[dstIndex + 3] = dm.meshes[srcIndex + 3];
                mesh.nmeshes++;
            }

            for (int k = 0; k < dm.nverts; ++k)
            {
                rcVcopy(mesh.verts, mesh.nverts * 3, dm.verts, k * 3);
                mesh.nverts++;
            }
            for (int k = 0; k < dm.ntris; ++k)
            {
                mesh.tris[mesh.ntris * 4 + 0] = dm.tris[k * 4 + 0];
                mesh.tris[mesh.ntris * 4 + 1] = dm.tris[k * 4 + 1];
                mesh.tris[mesh.ntris * 4 + 2] = dm.tris[k * 4 + 2];
                mesh.tris[mesh.ntris * 4 + 3] = dm.tris[k * 4 + 3];
                mesh.ntris++;
            }
        }

        ctx.stopTimer(rcTimerLabel.RC_TIMER_MERGE_POLYMESHDETAIL);

        return true;
    }
}

