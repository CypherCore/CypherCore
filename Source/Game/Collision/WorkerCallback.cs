// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Numerics;
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
}