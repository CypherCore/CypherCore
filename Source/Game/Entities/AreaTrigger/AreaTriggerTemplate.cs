// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;

namespace Game.Entities
{
    public class AreaTriggerTemplate
    {
        public List<AreaTriggerAction> Actions { get; set; } = new();
        public AreaTriggerFlags Flags { get; set; }
        public AreaTriggerId Id;

        public bool HasFlag(AreaTriggerFlags flag)
        {
            return Flags.HasAnyFlag(flag);
        }
    }
}