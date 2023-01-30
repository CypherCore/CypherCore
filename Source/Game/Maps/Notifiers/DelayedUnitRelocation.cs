// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps.Notifiers
{
    public class DelayedUnitRelocation : Notifier
    {
        private readonly Cell _cell;

        private readonly Map _imap;
        private readonly float _iradius;
        private readonly CellCoord p;

        public DelayedUnitRelocation(Cell c, CellCoord pair, Map map, float radius)
        {
            _imap = map;
            _cell = c;
            p = pair;
            _iradius = radius;
        }

        public override void Visit(IList<Player> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Player player = objs[i];
                WorldObject viewPoint = player.SeerView;

                if (!viewPoint.IsNeedNotify(NotifyFlags.VisibilityChanged))
                    continue;

                if (player != viewPoint &&
                    !viewPoint.IsPositionValid())
                    continue;

                var relocate = new PlayerRelocationNotifier(player);
                Cell.VisitAllObjects(viewPoint, relocate, _iradius, false);

                relocate.SendToSelf();
            }
        }

        public override void Visit(IList<Creature> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Creature creature = objs[i];

                if (!creature.IsNeedNotify(NotifyFlags.VisibilityChanged))
                    continue;

                CreatureRelocationNotifier relocate = new(creature);

                var c2world_relocation = new Visitor(relocate, GridMapTypeMask.AllWorld);
                var c2grid_relocation = new Visitor(relocate, GridMapTypeMask.AllGrid);

                _cell.Visit(p, c2world_relocation, _imap, creature, _iradius);
                _cell.Visit(p, c2grid_relocation, _imap, creature, _iradius);
            }
        }
    }
}