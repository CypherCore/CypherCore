// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Numerics;
using Framework.GameMath;

namespace Game.Collision
{
    public class BIHWrap<T> where T : IModel
    {
        private readonly Dictionary<T, uint> _obj2Idx = new();
        private readonly List<T> _objects = new();
        private readonly HashSet<T> _objects_to_push = new();

        private readonly BIH _tree = new();
        private int unbalanced_times;

        public void Insert(T obj)
        {
            ++unbalanced_times;
            _objects_to_push.Add(obj);
        }

        public void Remove(T obj)
        {
            ++unbalanced_times;
            uint Idx;

            if (_obj2Idx.TryGetValue(obj, out Idx))
                _objects[(int)Idx] = null;
            else
                _objects_to_push.Remove(obj);
        }

        public void Balance()
        {
            if (unbalanced_times == 0)
                return;

            unbalanced_times = 0;
            _objects.Clear();
            _objects.AddRange(_obj2Idx.Keys);
            _objects.AddRange(_objects_to_push);

            _tree.Build(_objects);
        }

        public void IntersectRay(Ray ray, WorkerCallback intersectCallback, ref float maxDist)
        {
            Balance();
            MDLCallback temp_cb = new(intersectCallback, _objects.ToArray(), (uint)_objects.Count);
            _tree.IntersectRay(ray, temp_cb, ref maxDist, true);
        }

        public void IntersectPoint(Vector3 point, WorkerCallback intersectCallback)
        {
            Balance();
            MDLCallback callback = new(intersectCallback, _objects.ToArray(), (uint)_objects.Count);
            _tree.IntersectPoint(point, callback);
        }

        public class MDLCallback : WorkerCallback
        {
            private readonly WorkerCallback _callback;
            private readonly T[] objects;
            private readonly uint objects_size;

            public MDLCallback(WorkerCallback callback, T[] objects_array, uint size)
            {
                objects = objects_array;
                _callback = callback;
                objects_size = size;
            }

            /// Intersect ray
            public override bool Invoke(Ray ray, uint idx, ref float maxDist, bool stopAtFirst)
            {
                if (idx >= objects_size)
                    return false;

                T obj = objects[idx];

                if (obj != null)
                    return _callback.Invoke(ray, obj, ref maxDist);

                return false;
            }

            /// Intersect point
            public override void Invoke(Vector3 p, uint idx)
            {
                if (idx >= objects_size)
                    return;

                T obj = objects[idx];

                if (obj != null)
                    _callback.Invoke(p, obj as GameObjectModel);
            }
        }
    }
}