// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Framework.GameMath;

namespace Game.Collision
{
	public class WorkerCallback
	{
		public virtual void Invoke(Vector3 point, uint entry)
		{
		}

		public virtual void Invoke(Vector3 point, GameObjectModel obj)
		{
		}

		public virtual bool Invoke(Ray ray, uint entry, ref float distance, bool pStopAtFirstHit)
		{
			return false;
		}

		public virtual bool Invoke(Ray r, IModel obj, ref float distance)
		{
			return false;
		}
	}

	public class TriBoundFunc
	{
		private List<Vector3> vertices;

		public TriBoundFunc(List<Vector3> vert)
		{
			vertices = vert;
		}

		public void Invoke(MeshTriangle tri, out AxisAlignedBox value)
		{
			Vector3 lo = vertices[(int)tri.idx0];
			Vector3 hi = lo;

			lo = Vector3.Min(Vector3.Min(lo, vertices[(int)tri.idx1]), vertices[(int)tri.idx2]);
			hi = Vector3.Max(Vector3.Max(hi, vertices[(int)tri.idx1]), vertices[(int)tri.idx2]);

			value = new AxisAlignedBox(lo, hi);
		}
	}

	public class WModelAreaCallback : WorkerCallback
	{
		public GroupModel hit;

		private List<GroupModel> prims;
		public float zDist;
		private Vector3 zVec;

		public WModelAreaCallback(List<GroupModel> vals, Vector3 down)
		{
			prims = vals;
			hit   = null;
			zDist = float.PositiveInfinity;
			zVec  = down;
		}

		public override void Invoke(Vector3 point, uint entry)
		{
			float group_Z;

			if (prims[(int)entry].IsInsideObject(point, zVec, out group_Z))
				if (group_Z < zDist)
				{
					zDist = group_Z;
					hit   = prims[(int)entry];
				}
		}
	}

	public class WModelRayCallBack : WorkerCallback
	{
		public bool hit;
		private List<GroupModel> models;

		public WModelRayCallBack(List<GroupModel> mod)
		{
			models = mod;
			hit    = false;
		}

		public override bool Invoke(Ray ray, uint entry, ref float distance, bool pStopAtFirstHit)
		{
			bool result     = models[(int)entry].IntersectRay(ray, ref distance, pStopAtFirstHit);
			if (result) hit = true;

			return hit;
		}
	}

	public class GModelRayCallback : WorkerCallback
	{
		public bool hit;
		private List<MeshTriangle> triangles;

		private List<Vector3> vertices;

		public GModelRayCallback(List<MeshTriangle> tris, List<Vector3> vert)
		{
			vertices  = vert;
			triangles = tris;
			hit       = false;
		}

		public override bool Invoke(Ray ray, uint entry, ref float distance, bool pStopAtFirstHit)
		{
			hit = IntersectTriangle(triangles[(int)entry], vertices, ray, ref distance) || hit;

			return hit;
		}

		private bool IntersectTriangle(MeshTriangle tri, List<Vector3> points, Ray ray, ref float distance)
		{
			const float EPS = 1e-5f;

			// See RTR2 ch. 13.7 for the algorithm.

			Vector3 e1 = points[(int)tri.idx1] - points[(int)tri.idx0];
			Vector3 e2 = points[(int)tri.idx2] - points[(int)tri.idx0];
			Vector3 p  = Vector3.Cross(ray.Direction, e2);
			float   a  = Vector3.Dot(e1, p);

			if (Math.Abs(a) < EPS)
				// Determinant is ill-conditioned; abort early
				return false;

			float   f = 1.0f / a;
			Vector3 s = ray.Origin - points[(int)tri.idx0];
			float   u = f * Vector3.Dot(s, p);

			if ((u < 0.0f) ||
			    (u > 1.0f))
				// We hit the plane of the _geometry, but outside the _geometry
				return false;

			Vector3 q = Vector3.Cross(s, e1);
			float   v = f * Vector3.Dot(ray.Direction, q);

			if ((v < 0.0f) ||
			    ((u + v) > 1.0f))
				// We hit the plane of the triangle, but outside the triangle
				return false;

			float t = f * Vector3.Dot(e2, q);

			if ((t > 0.0f) &&
			    (t < distance))
			{
				// This is a new hit, closer than the previous one
				distance = t;

				return true;
			}

			// This hit is after the previous hit, so ignore it
			return false;
		}
	}

	public class MapRayCallback : WorkerCallback
	{
		private ModelIgnoreFlags flags;
		private bool hit;

		private ModelInstance[] prims;

		public MapRayCallback(ModelInstance[] val, ModelIgnoreFlags ignoreFlags)
		{
			prims = val;
			hit   = false;
			flags = ignoreFlags;
		}

		public override bool Invoke(Ray ray, uint entry, ref float distance, bool pStopAtFirstHit = true)
		{
			if (prims[entry] == null)
				return false;

			bool result = prims[entry].IntersectRay(ray, ref distance, pStopAtFirstHit, flags);

			if (result)
				hit = true;

			return result;
		}

		public bool DidHit()
		{
			return hit;
		}
	}

	public class AreaInfoCallback : WorkerCallback
	{
		public AreaInfo aInfo = new();

		private ModelInstance[] prims;

		public AreaInfoCallback(ModelInstance[] val)
		{
			prims = val;
		}

		public override void Invoke(Vector3 point, uint entry)
		{
			if (prims[entry] == null)
				return;

			prims[entry].IntersectPoint(point, aInfo);
		}
	}

	public class LocationInfoCallback : WorkerCallback
	{
		private LocationInfo locInfo;

		private ModelInstance[] prims;
		public bool result;

		public LocationInfoCallback(ModelInstance[] val, LocationInfo info)
		{
			prims   = val;
			locInfo = info;
			result  = false;
		}

		public override void Invoke(Vector3 point, uint entry)
		{
			if (prims[entry] != null &&
			    prims[entry].GetLocationInfo(point, locInfo))
				result = true;
		}
	}

	public class DynamicTreeIntersectionCallback : WorkerCallback
	{
		private bool _didHit;
		private PhaseShift _phaseShift;

		public DynamicTreeIntersectionCallback(PhaseShift phaseShift)
		{
			_didHit     = false;
			_phaseShift = phaseShift;
		}

		public override bool Invoke(Ray r, IModel obj, ref float distance)
		{
			_didHit = obj.IntersectRay(r, ref distance, true, _phaseShift, ModelIgnoreFlags.Nothing);

			return _didHit;
		}

		public bool DidHit()
		{
			return _didHit;
		}
	}

	public class DynamicTreeAreaInfoCallback : WorkerCallback
	{
		private AreaInfo _areaInfo;

		private PhaseShift _phaseShift;

		public DynamicTreeAreaInfoCallback(PhaseShift phaseShift)
		{
			_phaseShift = phaseShift;
			_areaInfo   = new AreaInfo();
		}

		public override void Invoke(Vector3 p, GameObjectModel obj)
		{
			obj.IntersectPoint(p, _areaInfo, _phaseShift);
		}

		public AreaInfo GetAreaInfo()
		{
			return _areaInfo;
		}
	}

	public class DynamicTreeLocationInfoCallback : WorkerCallback
	{
		private GameObjectModel _hitModel = new();
		private LocationInfo _locationInfo = new();

		private PhaseShift _phaseShift;

		public DynamicTreeLocationInfoCallback(PhaseShift phaseShift)
		{
			_phaseShift = phaseShift;
		}

		public override void Invoke(Vector3 p, GameObjectModel obj)
		{
			if (obj.GetLocationInfo(p, _locationInfo, _phaseShift))
				_hitModel = obj;
		}

		public LocationInfo GetLocationInfo()
		{
			return _locationInfo;
		}

		public GameObjectModel GetHitModel()
		{
			return _hitModel;
		}
	}
}