// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Notifiers
{
    public class VisibleChangesNotifier : Notifier
    {
        private readonly ICollection<WorldObject> _i_objects;

        public VisibleChangesNotifier(ICollection<WorldObject> objects)
        {
            _i_objects = objects;
        }

        public override void Visit(IList<Player> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Player player = objs[i];

                player.UpdateVisibilityOf(_i_objects);

                foreach (var visionPlayer in player.GetSharedVisionList())
                    if (visionPlayer.SeerView == player)
                        visionPlayer.UpdateVisibilityOf(_i_objects);
            }
        }

        public override void Visit(IList<Creature> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Creature creature = objs[i];

                foreach (var visionPlayer in creature.GetSharedVisionList())
                    if (visionPlayer.SeerView == creature)
                        visionPlayer.UpdateVisibilityOf(_i_objects);
            }
        }

        public override void Visit(IList<DynamicObject> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                DynamicObject dynamicObject = objs[i];
                Unit caster = dynamicObject.GetCaster();

                if (caster)
                {
                    Player pl = caster.ToPlayer();

                    if (pl && pl.SeerView == dynamicObject)
                        pl.UpdateVisibilityOf(_i_objects);
                }
            }
        }
    }
}