// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;

namespace Game.Maps
{
    internal class NearestCheckCustomizer : NoopCheckCustomizer
    {
        private readonly WorldObject _obj;
        private float _range;

        public NearestCheckCustomizer(WorldObject obj, float range)
        {
            _obj = obj;
            _range = range;
        }

        public override bool Test(WorldObject o)
        {
            return _obj.IsWithinDist(o, _range);
        }

        public override void Update(WorldObject o)
        {
            _range = _obj.GetDistance(o);
        }
    }
}