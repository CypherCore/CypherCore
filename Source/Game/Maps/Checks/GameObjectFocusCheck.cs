// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Checks
{
    internal class GameObjectFocusCheck : ICheck<GameObject>
    {
        private readonly WorldObject _caster;
        private readonly uint _focusId;

        public GameObjectFocusCheck(WorldObject caster, uint focusId)
        {
            _caster = caster;
            _focusId = focusId;
        }

        public bool Invoke(GameObject go)
        {
            if (go.GetGoInfo().GetSpellFocusType() != _focusId)
                return false;

            if (!go.IsSpawned())
                return false;

            float dist = go.GetGoInfo().GetSpellFocusRadius();

            return go.IsWithinDist(_caster, dist);
        }
    }
}