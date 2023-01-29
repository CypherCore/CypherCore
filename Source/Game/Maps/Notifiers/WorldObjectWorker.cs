// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps.Notifiers
{
    public class WorldObjectWorker : Notifier
    {
        private readonly IDoWork<WorldObject> _do;
        private readonly GridMapTypeMask _mapTypeMask;
        private readonly PhaseShift _phaseShift;

        public WorldObjectWorker(WorldObject searcher, IDoWork<WorldObject> _do, GridMapTypeMask mapTypeMask = GridMapTypeMask.All)
        {
            _mapTypeMask = mapTypeMask;
            _phaseShift = searcher.GetPhaseShift();
            this._do = _do;
        }

        public override void Visit(IList<GameObject> objs)
        {
            if (!_mapTypeMask.HasAnyFlag(GridMapTypeMask.GameObject))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                GameObject gameObject = objs[i];

                if (gameObject.InSamePhase(_phaseShift))
                    _do.Invoke(gameObject);
            }
        }

        public override void Visit(IList<Player> objs)
        {
            if (!_mapTypeMask.HasAnyFlag(GridMapTypeMask.Player))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                Player player = objs[i];

                if (player.InSamePhase(_phaseShift))
                    _do.Invoke(player);
            }
        }

        public override void Visit(IList<Creature> objs)
        {
            if (!_mapTypeMask.HasAnyFlag(GridMapTypeMask.Creature))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                Creature creature = objs[i];

                if (creature.InSamePhase(_phaseShift))
                    _do.Invoke(creature);
            }
        }

        public override void Visit(IList<Corpse> objs)
        {
            if (!_mapTypeMask.HasAnyFlag(GridMapTypeMask.Corpse))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                Corpse corpse = objs[i];

                if (corpse.InSamePhase(_phaseShift))
                    _do.Invoke(corpse);
            }
        }

        public override void Visit(IList<DynamicObject> objs)
        {
            if (!_mapTypeMask.HasAnyFlag(GridMapTypeMask.DynamicObject))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                DynamicObject dynamicObject = objs[i];

                if (dynamicObject.InSamePhase(_phaseShift))
                    _do.Invoke(dynamicObject);
            }
        }

        public override void Visit(IList<AreaTrigger> objs)
        {
            if (!_mapTypeMask.HasAnyFlag(GridMapTypeMask.AreaTrigger))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                AreaTrigger areaTrigger = objs[i];

                if (areaTrigger.InSamePhase(_phaseShift))
                    _do.Invoke(areaTrigger);
            }
        }

        public override void Visit(IList<SceneObject> objs)
        {
            if (!_mapTypeMask.HasAnyFlag(GridMapTypeMask.SceneObject))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                SceneObject sceneObject = objs[i];

                if (sceneObject.InSamePhase(_phaseShift))
                    _do.Invoke(sceneObject);
            }
        }

        public override void Visit(IList<Conversation> objs)
        {
            if (!_mapTypeMask.HasAnyFlag(GridMapTypeMask.Conversation))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                Conversation conversation = objs[i];

                if (conversation.InSamePhase(_phaseShift))
                    _do.Invoke(conversation);
            }
        }
    }

}