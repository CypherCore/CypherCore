// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps.Dos
{
    public class RespawnDo : IDoWork<WorldObject>
    {
        public void Invoke(WorldObject obj)
        {
            switch (obj.GetTypeId())
            {
                case TypeId.Unit:
                    obj.ToCreature().Respawn();

                    break;
                case TypeId.GameObject:
                    obj.ToGameObject().Respawn();

                    break;
            }
        }
    }
}