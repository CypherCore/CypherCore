// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps.Notifiers
{
    public class CreatureRelocationNotifier : Notifier
    {
        private readonly Creature _icreature;

        public CreatureRelocationNotifier(Creature c)
        {
            _icreature = c;
        }

        public override void Visit(IList<Player> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Player player = objs[i];

                if (!player.SeerView.IsNeedNotify(NotifyFlags.VisibilityChanged))
                    player.UpdateVisibilityOf(_icreature);

                CreatureUnitRelocationWorker(_icreature, player);
            }
        }

        public override void Visit(IList<Creature> objs)
        {
            if (!_icreature.IsAlive())
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                Creature creature = objs[i];
                CreatureUnitRelocationWorker(_icreature, creature);

                if (!creature.IsNeedNotify(NotifyFlags.VisibilityChanged))
                    CreatureUnitRelocationWorker(creature, _icreature);
            }
        }
    }

}