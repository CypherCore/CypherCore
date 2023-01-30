// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;

namespace Game.Maps
{
    // CHECK modifiers
    public class NoopCheckCustomizer
    {
        public virtual bool Test(WorldObject o)
        {
            return true;
        }

        public virtual void Update(WorldObject o)
        {
        }
    }
}