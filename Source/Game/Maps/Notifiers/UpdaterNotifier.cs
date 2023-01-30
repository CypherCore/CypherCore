// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps.Notifiers
{
    public class UpdaterNotifier : Notifier
    {
        private readonly uint _timeDiff;

        public UpdaterNotifier(uint diff)
        {
            _timeDiff = diff;
        }

        public override void Visit(IList<WorldObject> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                WorldObject obj = objs[i];

                if (obj.IsTypeId(TypeId.Player) ||
                    obj.IsTypeId(TypeId.Corpse))
                    continue;

                if (obj.IsInWorld)
                    obj.Update(_timeDiff);
            }
        }
    }
}