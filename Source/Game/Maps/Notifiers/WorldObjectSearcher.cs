// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps.Notifiers
{
    //Searchers
    public class WorldObjectSearcher : Notifier
    {
        private readonly ICheck<WorldObject> _check;
        private readonly GridMapTypeMask _mapTypeMask;
        private readonly PhaseShift _phaseShift;
        private WorldObject _object;

        public WorldObjectSearcher(WorldObject searcher, ICheck<WorldObject> check, GridMapTypeMask mapTypeMask = GridMapTypeMask.All)
        {
            _mapTypeMask = mapTypeMask;
            _phaseShift = searcher.GetPhaseShift();
            _check = check;
        }

        public override void Visit(IList<GameObject> objs)
        {
            if (!_mapTypeMask.HasAnyFlag(GridMapTypeMask.GameObject))
                return;

            // already found
            if (_object)
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                GameObject gameObject = objs[i];

                if (!gameObject.InSamePhase(_phaseShift))
                    continue;

                if (_check.Invoke(gameObject))
                {
                    _object = gameObject;

                    return;
                }
            }
        }

        public override void Visit(IList<Player> objs)
        {
            if (!_mapTypeMask.HasAnyFlag(GridMapTypeMask.Player))
                return;

            // already found
            if (_object)
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                Player player = objs[i];

                if (!player.InSamePhase(_phaseShift))
                    continue;

                if (_check.Invoke(player))
                {
                    _object = player;

                    return;
                }
            }
        }

        public override void Visit(IList<Creature> objs)
        {
            if (!_mapTypeMask.HasAnyFlag(GridMapTypeMask.Creature))
                return;

            // already found
            if (_object)
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                Creature creature = objs[i];

                if (!creature.InSamePhase(_phaseShift))
                    continue;

                if (_check.Invoke(creature))
                {
                    _object = creature;

                    return;
                }
            }
        }

        public override void Visit(IList<Corpse> objs)
        {
            if (!_mapTypeMask.HasAnyFlag(GridMapTypeMask.Corpse))
                return;

            // already found
            if (_object)
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                Corpse corpse = objs[i];

                if (!corpse.InSamePhase(_phaseShift))
                    continue;

                if (_check.Invoke(corpse))
                {
                    _object = corpse;

                    return;
                }
            }
        }

        public override void Visit(IList<DynamicObject> objs)
        {
            if (!_mapTypeMask.HasAnyFlag(GridMapTypeMask.DynamicObject))
                return;

            // already found
            if (_object)
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                DynamicObject dynamicObject = objs[i];

                if (!dynamicObject.InSamePhase(_phaseShift))
                    continue;

                if (_check.Invoke(dynamicObject))
                {
                    _object = dynamicObject;

                    return;
                }
            }
        }

        public override void Visit(IList<AreaTrigger> objs)
        {
            if (!_mapTypeMask.HasAnyFlag(GridMapTypeMask.AreaTrigger))
                return;

            // already found
            if (_object)
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                AreaTrigger areaTrigger = objs[i];

                if (!areaTrigger.InSamePhase(_phaseShift))
                    continue;

                if (_check.Invoke(areaTrigger))
                {
                    _object = areaTrigger;

                    return;
                }
            }
        }

        public override void Visit(IList<SceneObject> objs)
        {
            if (!_mapTypeMask.HasAnyFlag(GridMapTypeMask.SceneObject))
                return;

            // already found
            if (_object)
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                SceneObject sceneObject = objs[i];

                if (!sceneObject.InSamePhase(_phaseShift))
                    continue;

                if (_check.Invoke(sceneObject))
                {
                    _object = sceneObject;

                    return;
                }
            }
        }

        public override void Visit(IList<Conversation> objs)
        {
            if (!_mapTypeMask.HasAnyFlag(GridMapTypeMask.Conversation))
                return;

            // already found
            if (_object)
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                Conversation conversation = objs[i];

                if (!conversation.InSamePhase(_phaseShift))
                    continue;

                if (_check.Invoke(conversation))
                {
                    _object = conversation;

                    return;
                }
            }
        }

        public WorldObject GetTarget()
        {
            return _object;
        }
    }

}