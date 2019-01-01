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

namespace Game.Collision
{
    public class BIHWrap<T> where T : IModel
    {
        public void insert(T obj)
        {
            ++unbalanced_times;
            m_objects_to_push.Add(obj);
        }
        public void remove(T obj)
        {
            ++unbalanced_times;
            uint Idx = 0;
            if (m_obj2Idx.TryGetValue(obj, out Idx))
                m_objects[(int)Idx] = null;
            else
                m_objects_to_push.Remove(obj);
        }

        public void balance()
        {
            if (unbalanced_times == 0)
                return;

            unbalanced_times = 0;
            m_objects.Clear();
            m_objects.AddRange(m_obj2Idx.Keys);
            m_objects.AddRange(m_objects_to_push);

            m_tree.build(m_objects);
        }

        public void intersectRay(Ray ray, WorkerCallback intersectCallback, ref float maxDist)
        {
            balance();
            MDLCallback temp_cb = new MDLCallback(intersectCallback, m_objects.ToArray(), (uint)m_objects.Count);
            m_tree.intersectRay(ray, temp_cb, ref maxDist, true);
        }

        public void intersectPoint(Vector3 point, WorkerCallback intersectCallback)
        {
            balance();
            MDLCallback callback = new MDLCallback(intersectCallback, m_objects.ToArray(), (uint)m_objects.Count);
            m_tree.intersectPoint(point, callback);
        }

        BIH m_tree = new BIH();
        List<T> m_objects = new List<T>();
        Dictionary<T, uint> m_obj2Idx = new Dictionary<T, uint>();
        HashSet<T> m_objects_to_push = new HashSet<T>();
        int unbalanced_times;

        public class MDLCallback : WorkerCallback
        {
            T[] objects;
            WorkerCallback _callback;
            uint objects_size;

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
                    _callback.Invoke(p, obj);
            }
        }
    }
}
