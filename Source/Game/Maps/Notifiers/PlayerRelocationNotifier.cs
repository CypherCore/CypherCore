// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps.Notifiers
{
    public class PlayerRelocationNotifier : VisibleNotifier
    {
        public PlayerRelocationNotifier(Player player) : base(player)
        {
        }

        public override void Visit(IList<Player> objs)
        {
            base.Visit(objs);

            for (var i = 0; i < objs.Count; ++i)
            {
                Player player = objs[i];
                _vis_guids.Remove(player.GetGUID());

                _i_player.UpdateVisibilityOf(player, _i_data, _i_visibleNow);

                if (player.SeerView.IsNeedNotify(NotifyFlags.VisibilityChanged))
                    continue;

                player.UpdateVisibilityOf(_i_player);
            }
        }

        public override void Visit(IList<Creature> objs)
        {
            base.Visit(objs);

            bool relocated_for_ai = _i_player == _i_player.SeerView;

            for (var i = 0; i < objs.Count; ++i)
            {
                Creature creature = objs[i];
                _vis_guids.Remove(creature.GetGUID());

                _i_player.UpdateVisibilityOf(creature, _i_data, _i_visibleNow);

                if (relocated_for_ai && !creature.IsNeedNotify(NotifyFlags.VisibilityChanged))
                    CreatureUnitRelocationWorker(creature, _i_player);
            }
        }
    }
}