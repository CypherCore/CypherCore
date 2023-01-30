// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;

namespace Game.Entities
{
    public class PerPlayerState
    {
        public GameObjectState? State;
        public DateTime ValidUntil = DateTime.MinValue;
        public bool Despawned { get; set; }
    }
}