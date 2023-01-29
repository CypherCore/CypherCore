// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Collision
{
    public class ManagedModel
    {
        private WorldModel _iModel;
        private int _iRefCount;

        public ManagedModel()
        {
            _iModel = new WorldModel();
            _iRefCount = 0;
        }

        public void SetModel(WorldModel model)
        {
            _iModel = model;
        }

        public WorldModel GetModel()
        {
            return _iModel;
        }

        public void IncRefCount()
        {
            ++_iRefCount;
        }

        public int DecRefCount()
        {
            return --_iRefCount;
        }
    }
}